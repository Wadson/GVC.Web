
namespace GVC.Web.Services;

public interface IEmailService
{
    Task SendAsync(string destination, string subject, string htmlBody, CancellationToken cancellationToken = default);
}