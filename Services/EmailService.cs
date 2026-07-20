using System.Net;
using System.Net.Mail;

namespace GVC.Web.Services;

public sealed class EmailService(IConfiguration configuration) : IEmailService
{
    public async Task SendAsync(string destination, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var settings = configuration.GetSection("Email");

        var host = settings["Host"];

        var fromAddress = settings["FromAddress"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress))
            throw new InvalidOperationException("O envio de e-mail não foi configurado. Defina Email:Host e Email:FromAddress.");

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

        await client.SendMailAsync(message).WaitAsync(cancellationToken);
    }
}