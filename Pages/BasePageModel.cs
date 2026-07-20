using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GVC.Web.Pages;

public abstract class BasePageModel : PageModel
{
    protected int EmpresaId => GetIntClaim("EmpresaID");

    protected int UsuarioId => GetIntClaim("UsuarioID");

    protected string TipoUsuario => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    private int GetIntClaim(string name) =>
        int.TryParse(User.FindFirstValue(name), out var value)
            ? value
            : throw new InvalidOperationException($"Claim obrigatória '{name}' não encontrada.");
}