using System.ComponentModel.DataAnnotations;
using GVC.Web.Data;
using GVC.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Account;

public class ResetPasswordModel(ErpDbContext db, IPasswordHasher hasher, ILogger<ResetPasswordModel> logger) : PageModel
{
    [BindProperty, Required]
    public string Token { get; set; } = string.Empty;

    [BindProperty, Required(ErrorMessage = "Informe a nova senha."), MinLength(8, ErrorMessage = "Use ao menos 8 caracteres."), DataType(DataType.Password), Display(Name = "Nova senha")]
    public string? Senha
    {
        get; set;
    }

    [BindProperty, Required(ErrorMessage = "Confirme a nova senha."), DataType(DataType.Password), Display(Name = "Confirmar senha")]
    public string? ConfirmarSenha
    {
        get; set;
    }

    public async Task<IActionResult> OnGetAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToPage("Login");

        Token = token.Trim();

        var expiration = await db.TokensRedefinicao.AsNoTracking().Where(x => x.Token == Token).Select(x => (DateTime?)x.DataExpiracao).FirstOrDefaultAsync();

        if (expiration is null || expiration <= DateTime.Now)
        {
            TempData["ResetError"] = "Este link expirou. Solicite um novo e-mail de recuperação.";

            return RedirectToPage("ForgotPassword");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!string.Equals(Senha, ConfirmarSenha, StringComparison.Ordinal))
            ModelState.AddModelError("ConfirmarSenha", "As senhas não coincidem.");

        if (!ModelState.IsValid)
            return Page();

        var token = Token.Trim();

        var item = await db.TokensRedefinicao.AsNoTracking().FirstOrDefaultAsync(x => x.Token == token, cancellationToken);

        if (item is null || item.DataExpiracao <= DateTime.Now)
        {
            ModelState.AddModelError(string.Empty, "Token inválido ou expirado. Solicite um novo link.");

            return Page();
        }

        var usuario = await db.Usuarios.SingleOrDefaultAsync(x => x.UsuarioId == item.UsuarioId, cancellationToken);

        if (usuario is null)
        {
            ModelState.AddModelError(string.Empty, "Usuário não encontrado.");

            return Page();
        }

        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            usuario.Senha = hasher.Hash(Senha!);

            await db.SaveChangesAsync(cancellationToken);

            await db.TokensRedefinicao.Where(x => x.UsuarioId == usuario.UsuarioId).ExecuteDeleteAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            TempData["Message"] = "Senha alterada com sucesso. Entre com a nova senha.";

            return RedirectToPage("Login");
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Erro ao redefinir a senha do usuário {UsuarioId}.", usuario.UsuarioId);

            ModelState.AddModelError(string.Empty, "Não foi possível alterar a senha. Solicite um novo link e tente novamente.");

            return Page();
        }
    }
}