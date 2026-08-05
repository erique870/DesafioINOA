namespace StockQuoteAlert;

public class AppConfig
{
    public EmailSettings Email { get; set; } = new();
    public SmtpSettings Smtp { get; set; } = new();
    public string? BrapiToken { get; set; }
}

public class EmailSettings
{
    // Destinatário dos alertas
    public string To { get; set; } = "";

    // Remetente dos alertas
    public string From { get; set; } = "";

    // Nome de exibição do remetente
    public string FromName { get; set; } = "Stock Quote Alert";
}

public class SmtpSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string User { get; set; } = "";
    public string AppPassword { get; set; } = "";
    public bool EnableSsl { get; set; } = true;
}