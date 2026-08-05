using System.Globalization;
using System.Security.Cryptography;

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

        // Configurações iniciais do envio do e-mail (censurado)
        EmailSettings emailSettings = new EmailSettings
        {
            To = "seu-email@exemplo.com",
            From = "alertas@exemplo.com",
            FromName = "Stock Quote Alert"
        };

        // Configurações iniciais do protocolo (censurado)
        SmtpSettings smtpSettings = new SmtpSettings
        {
            Host = "smtp.gmail.com",
            Port =  587,
            User = "alertas@exemplo.com",
            AppPassword = "sua-senha-de-app-aqui",
            EnableSsl =  true
        };

        var emailSender = new EmailAlertSender(emailSettings, smtpSettings);
        await emailSender.SendAlertAsync();
        return 0;
    }
}