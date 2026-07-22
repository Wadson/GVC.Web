using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging.Abstractions;

namespace GVC.Web.Services;

public sealed class EmailService(IConfiguration configuration, ILogger<EmailService>? serviceLogger = null) : IEmailService
{
    private readonly ILogger<EmailService> logger = serviceLogger ?? NullLogger<EmailService>.Instance;

    public async Task SendAsync(string destination, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var settings = configuration.GetSection("Email");

        var host = settings["Host"];

        var fromAddress = settings["FromAddress"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress))
        {
            logger.LogWarning("Envio de e-mail rejeitado: configuração SMTP incompleta");
            throw new InvalidOperationException("O envio de e-mail não foi configurado. Defina Email:Host e Email:FromAddress.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, settings["FromName"] ?? "GVC ERP"),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        message.To.Add(destination);

        using var client = new SmtpClient(host, settings.GetValue("Port", 587))
        {
            EnableSsl = settings.GetValue("UseSsl", true)
        };

        var userName = settings["UserName"];

        if (!string.IsNullOrWhiteSpace(userName))
            client.Credentials = new NetworkCredential(userName, settings["Password"]);

        try
        {
            logger.LogInformation("Enviando e-mail para {Destino} com assunto {Assunto}", destination, subject);
            await client.SendMailAsync(message).WaitAsync(cancellationToken);
            logger.LogInformation("E-mail enviado para {Destino}", destination);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Falha no envio de e-mail para {Destino}", destination);
            throw;
        }
    }
}
