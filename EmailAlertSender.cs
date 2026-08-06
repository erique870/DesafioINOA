using System.Net;
using System.Net.Mail;
using System.Reflection.Metadata;

namespace StockQuoteAlert;

public enum AlertType
{
    Venda,
    Compra
}

public class EmailAlertSender
{
    private readonly EmailSettings _emailSettings;
    private readonly SmtpSettings _smtpSettings;

    // Construtor da classe
    public EmailAlertSender(EmailSettings emailSettings, SmtpSettings smtpSettings)
    {
        _emailSettings = emailSettings;
        _smtpSettings = smtpSettings;
    }

    public async Task SendAlertAsync(string ticker, AlertType type, decimal currentPrice, decimal referencePrice)
    {
        string subject;
        string body;
        
        switch (type)
        {
            case AlertType.Venda:

                subject = $"[Alerta {ticker}] Preço acima da referência de venda";
                body = $"O ativo {ticker} está sendo cotado a R$ {currentPrice:F2}, " +
                       $"acima do preço de referência para venda de R$ {referencePrice:F2}.\n\n" +
                       $"Considere avaliar a VENDA do ativo.\n\n" +
                       $"Horário da checagem: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                break;
            
            case AlertType.Compra:

                subject = $"[Alerta {ticker}] Preço abaixo da referência de compra";
                body = $"O ativo {ticker} está sendo cotado a R$ {currentPrice:F2}, " +
                       $"abaixo do preço de referência para compra de R$ {referencePrice:F2}.\n\n" +
                       $"Considere avaliar a COMPRA do ativo.\n\n" +
                       $"Horário da checagem: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                break;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Tipo de alerta inválido.");

        }

        using var message = new MailMessage{
            From = new MailAddress(_emailSettings.From, _emailSettings.FromName), 
            Subject = subject,
            Body = body,
            IsBodyHtml = false 
        };
        message.To.Add(_emailSettings.To);

        using var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
        {
            EnableSsl = _smtpSettings.EnableSsl,
            Credentials = new NetworkCredential(_smtpSettings.User, _smtpSettings.AppPassword)  
        };

        await client.SendMailAsync(message);
    }
}