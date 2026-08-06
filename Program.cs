using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;

namespace StockQuoteAlert;
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length != 3)
        {   
            Console.WriteLine("Quantidade incorreta de argumentos.");
            return 1;
        }

        string ticker = args[0].Trim().ToUpperInvariant();

        // Safe Parser para decimais, aceita vírgulas e pontos como separadores
        if (!decimal.TryParse(args[1], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal sellPrice))
        {
            Console.WriteLine("Valor informado inválido.");            
            return 1;
        }
        
        // Safe Parser para decimais, aceita vírgulas e pontos como separadores
        if (!decimal.TryParse(args[2], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal buyPrice))
        {
            Console.WriteLine("Valor informado inválido.");            
            return 1;
        }

        if(buyPrice >= sellPrice)
        {
            Console.WriteLine("O preço de referência para compra deve ser menor que o preço de referência para venda.");
            return 1;
        }

        Console.WriteLine($"ticker = {ticker} - sellPrice = {sellPrice} - buyPrice = {buyPrice}");

        // Carregar configurações de envio de e-mail e protocolo de arquivo .json

        string configPath = Path.Combine(AppContext.BaseDirectory, "config.json");

        if (!File.Exists(configPath))
        {
            configPath = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
        }

        if (!File.Exists(configPath))
        {
            Console.WriteLine("Arquivo de configuração não encontrado.");
            return 1;
        }

        AppConfig config;
        try
        {
            string json = await File.ReadAllTextAsync(configPath);
            config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("Arquivo de configuração existe mas é vazio.");
        }
        catch (Exception ex)
        {   
            Console.WriteLine($"Falha ao ler config.jason: {ex.Message}");
            return 1;
        }

        // Verifica se ambos os endereço do destinatário e o endereço do host do protocolo estão em config.json
        if (   string.IsNullOrWhiteSpace(config.Email.To) 
            || string.IsNullOrWhiteSpace(config.Email.From)
            || string.IsNullOrWhiteSpace(config.Email.FromName)
            || string.IsNullOrWhiteSpace(config.Smtp.Host)
            || (config.Smtp.Port <= 0) 
            || (config.Smtp.Port > 65535)
            || string.IsNullOrWhiteSpace(config.Smtp.User)
            || string.IsNullOrWhiteSpace(config.Smtp.AppPassword)
        )
        {
            Console.WriteLine("Configuração incompleta: verifique os campos \"Email\" e \"Smtp\" em config.json.");
            return 1;       
        }

        using var  httpClient = new HttpClient();

        var quoteProvider = new QuoteProvider(httpClient, config.BrapiToken);

        var emailSender = new EmailAlertSender(config.Email, config.Smtp);

        while (true)
        {
            try
            {
                decimal currentPrice = await quoteProvider.GetCurrentPriceAsync(ticker);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {ticker} = R$ {currentPrice:F2}");

                if (currentPrice > sellPrice)
                {
                    Console.WriteLine($"Preço acima de R$ {sellPrice:F2}. Enviando alerta de VENDA...");
                    await emailSender.SendAlertAsync(ticker, AlertType.Venda, currentPrice, sellPrice);
                }
                else if (currentPrice < buyPrice)
                {
                    Console.WriteLine($"Preço abaixo de R$ {buyPrice:F2}. Enviando alerta de COMPRA...");
                    await emailSender.SendAlertAsync(ticker, AlertType.Compra, currentPrice, buyPrice);
                }

            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Erro ao consultar/alertar: {ex.Message}");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(config.QuoteUpdateIntervalSeconds));   
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
        return 0;
    }
}