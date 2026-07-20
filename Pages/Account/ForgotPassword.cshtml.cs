using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using GVC.Web.Data;
using GVC.Web.Models;
using GVC.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Account;

public class ForgotPasswordModel(ErpDbContext db, IEmailService email) : PageModel
{
    [BindProperty, Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Page();

        var normalizedEmail = Email.Trim();

        var user = await db.Usuarios.SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (user is not null)
        {
            await db.TokensRedefinicao.Where(x => x.UsuarioId == user.UsuarioId).ExecuteDeleteAsync(cancellationToken);

            var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(48));

            db.TokensRedefinicao.Add(new TokenRedefinicao
            {
                UsuarioId = user.UsuarioId,
                Token = token,
                DataExpiracao = DateTime.Now.AddHours(2)
            });

            await db.SaveChangesAsync(cancellationToken);

            var link = Url.Page("/Account/ResetPassword", null, new
            {
                token
            }, Request.Scheme)!;

            await email.SendAsync(user.Email, "Redefinição de senha",
                $"<p>Use este link em até 2 horas:</p><p><a href=\"{link}\">Redefinir senha</a></p><p>Se você não solicitou esta alteração, ignore este e-mail.</p>", cancellationToken);
        }

        TempData["Message"] = "Se o e-mail estiver cadastrado, as instruções serão enviadas.";

        return RedirectToPage("Login");
    }
}