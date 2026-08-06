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

        // Safe Parser para decimais, aceita vírgulas e pontos como separadores de números decimais
        if (!decimal.TryParse(args[1], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal sellPrice))
        {
            Console.WriteLine("Valor informado inválido.");            
            return 1;
        }
        
        // Safe Parser para decimais, aceita vírgulas e pontos como separadores de números decimais
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

        // Carregar configurações de envio de e-mail e protocolo do arquivo de configuração config.json

        string configPath = Path.Combine(AppContext.BaseDirectory, "config.json");

        if (!File.Exists(configPath))
        {
            configPath = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
        }

        if (!File.Exists(configPath))
        {
            Console.WriteLine("Arquivo de configuração \"config.json\" não encontrado.");
            return 1;
        }

        AppConfig config;
        try
        {
            string json = await File.ReadAllTextAsync(configPath);
            config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("Arquivo de configuração \"config.json\" existe mas está vazio.");
        }
        catch (Exception ex)
        {   
            Console.WriteLine($"Falha ao ler arquivo de configuração \"config.json\": {ex.Message}");
            return 1;
        }

        // Verifica se ambos os endereço do destinatário e o endereço do host do protocolo estão no arquivo de configuração config.json
        if (   string.IsNullOrWhiteSpace(config.Email.To) 
            || string.IsNullOrWhiteSpace(config.Email.From)
            || string.IsNullOrWhiteSpace(config.Email.FromName)
            || string.IsNullOrWhiteSpace(config.Smtp.Host)
            || (config.Smtp.Port <= 0) 
            || (config.Smtp.Port > 65535)
            || string.IsNullOrWhiteSpace(config.Smtp.User)
            || string.IsNullOrWhiteSpace(config.Smtp.AppPassword)
            || (config.QuoteUpdateIntervalSeconds <= 0)
        )
        {
            Console.WriteLine("Configuração incompleta ou incorreta: verifique os campos \"Email\", \"Smtp\" e \"QuoteUpdateIntervalSeconds\" em \"config.json\".");
            return 1;       
        }

        using var  httpClient = new HttpClient();

        var quoteProvider = new QuoteProvider(httpClient, config.BrapiToken);

        var emailSender = new EmailAlertSender(config.Email, config.Smtp);

        // Para evitar spam de notificações, notifico apenas quando o valor de cotação extrapola a faixa de valores informada
        bool sellAlertActive = false;
        bool buyAlertActive = false;    

        // Loop de requisição de cotação e envio de alertas sempre que necessário, pare a execução via terminal com CTRL + C
        while (true)
        {
            try
            {
                decimal currentPrice = await quoteProvider.GetCurrentPriceAsync(ticker);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {ticker} = R$ {currentPrice:F2}");

                if (currentPrice > sellPrice)
                {
                    // Verifica se a última notificação NÃO foi de VENDA 
                    if (!sellAlertActive)
                    {    
                        Console.WriteLine($"Preço acima de R$ {sellPrice:F2}. Enviando alerta de VENDA...");

                        await emailSender.SendAlertAsync(ticker, AlertType.Venda, currentPrice, sellPrice);
                        
                        Console.WriteLine($"Alerta de VENDA do ativo {ticker} enviado com sucesso para {config.Email.To}!");
                        
                        // Última notificação é de VENDA
                        sellAlertActive = true; 
                    }
                    buyAlertActive = false;
                }
                else if (currentPrice < buyPrice)
                {
                    // Verifica se a última notificação NÃO foi de COMPRA
                    if (!buyAlertActive)
                    {
                        Console.WriteLine($"Preço abaixo de R$ {buyPrice:F2}. Enviando alerta de COMPRA...");
                        
                        await emailSender.SendAlertAsync(ticker, AlertType.Compra, currentPrice, buyPrice);    
                        
                        Console.WriteLine($"Alerta de COMPRA do ativo {ticker} enviado com sucesso para {config.Email.To}!");
                        
                        // Última notificação foi de COMPRA
                        buyAlertActive = true; 
                    }
                    sellAlertActive = false;
                }
                else
                {
                    // A cotação atual está na faixa entre os valores informados, não dispara nada para o endereço de e-mail informado
                    buyAlertActive = false;
                    sellAlertActive = false;
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