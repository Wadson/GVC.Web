using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GVC.Web.Data;
using GVC.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Account;

public class LoginModel(ErpDbContext db, IPasswordHasher passwordHasher) : PageModel
{
    [BindProperty]
    public LoginInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl
    {
        get; set;
    }

    public IReadOnlyList<EmpresaOption> Empresas { get; private set; } = [];

    public async Task OnGetAsync() => await LoadEmpresasAsync();

    public async Task<IActionResult> OnGetLogoEmpresaAsync(int empresaId)
    {
        var logo = await db.Empresas.AsNoTracking().Where(x => x.EmpresaId == empresaId).Select(x => x.Logo).SingleOrDefaultAsync();

        if (logo is null || logo.Length == 0)
            return NotFound();

        var contentType = logo.Length > 3 && logo[0] == 0x89 && logo[1] == 0x50 ? "image/png" : logo.Length > 2 && logo[0] == 0xFF && logo[1] == 0xD8 ? "image/jpeg" : "image/webp";

        return File(logo, contentType);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadEmpresasAsync();

            return Page();
        }

        var empresa = await db.Empresas.AsNoTracking().SingleOrDefaultAsync(x => x.EmpresaId == Input.EmpresaId);

        var usuarios = await db.Usuarios.Where(x => x.Email == Input.Email.Trim() &&
            (x.EmpresaId == Input.EmpresaId || x.TipoUsuario == "Administrador")).ToListAsync();

        var usuario = usuarios.FirstOrDefault(x => passwordHasher.Verify(Input.Senha, x.Senha));

        if (empresa is null || usuario is null || (!string.Equals(usuario.TipoUsuario, "Administrador", StringComparison.OrdinalIgnoreCase) && usuario.EmpresaId != empresa.EmpresaId))
        {
            ModelState.AddModelError(string.Empty, "Empresa, e-mail ou senha inválidos.");

            await LoadEmpresasAsync();

            return Page();
        }

        if (passwordHasher.NeedsRehash(usuario.Senha))
        {
            usuario.Senha = passwordHasher.Hash(Input.Senha);

            await db.SaveChangesAsync();
        }

        var empresaNome = string.IsNullOrWhiteSpace(empresa.NomeFantasia) ? empresa.RazaoSocial : empresa.NomeFantasia;

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()), new Claim(ClaimTypes.Name, usuario.NomeCompleto), new Claim(ClaimTypes.Email, usuario.Email), new Claim(ClaimTypes.Role, usuario.TipoUsuario), new Claim("UsuarioID", usuario.UsuarioId.ToString()), new Claim("NomeCompleto", usuario.NomeCompleto), new Claim("TipoUsuario", usuario.TipoUsuario), new Claim("UsuarioEmpresaID", usuario.EmpresaId.ToString()), new Claim("EmpresaID", empresa.EmpresaId.ToString()), new Claim("EmpresaNome", empresaNome) };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)), new AuthenticationProperties { IsPersistent = Input.Lembrar, AllowRefresh = true });

        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/Dashboard");
    }

    private async Task LoadEmpresasAsync() => Empresas = await db.Empresas.AsNoTracking().OrderBy(x => x.NomeFantasia ?? x.RazaoSocial).Select(x => new EmpresaOption(x.EmpresaId, x.NomeFantasia ?? x.RazaoSocial)).ToListAsync();

    public sealed record EmpresaOption(int Id, string Nome);

    public sealed class LoginInput
    {
        [Range(1, int.MaxValue, ErrorMessage = "Selecione a empresa."), Display(Name = "Empresa")]
        public int EmpresaId
        {
            get; set;
        }

        [Required, EmailAddress, Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Display(Name = "Senha")]
        public string Senha { get; set; } = string.Empty;

        [Display(Name = "Manter conectado")]
        public bool Lembrar
        {
            get; set;
        }
    }
}
