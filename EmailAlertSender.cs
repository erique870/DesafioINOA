using System.Net;
using System.Net.Mail;

namespace StockQuoteAlert;

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

    public async Task SendAlertAsync()
    {
        // TODO: Escrever template de e-mail
        string subject = "TITULO E-MAIL";
        string body = "MENSAGEM E-MAIL";

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