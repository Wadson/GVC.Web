using System.Security.Claims;
using GVC.Web.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Account;

public class SwitchCompanyModel(ErpDbContext db) : PageModel
{
    [BindProperty]
    public int EmpresaId
    {
        get; set;
    }

    [BindProperty]
    public string? ReturnUrl
    {
        get; set;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var origin = int.TryParse(User.FindFirstValue("UsuarioEmpresaID"), out var id) ? id : 0;

        if (!User.IsInRole("Administrador") && EmpresaId != origin)
            return Forbid();

        var empresa = await db.Empresas.AsNoTracking().SingleOrDefaultAsync(x => x.EmpresaId == EmpresaId);

        if (empresa is null)
            return NotFound();

        var claims = User.Claims.Where(x => x.Type != "EmpresaID" && x.Type != "EmpresaNome").ToList();

        claims.Add(new Claim("EmpresaID", empresa.EmpresaId.ToString()));

        claims.Add(new Claim("EmpresaNome", empresa.NomeFantasia ?? empresa.RazaoSocial));

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)), new AuthenticationProperties { IsPersistent = true, AllowRefresh = true });

        TempData["Success"] = $"Empresa alterada para {empresa.NomeFantasia ?? empresa.RazaoSocial}.";

        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/Dashboard");
    }
}