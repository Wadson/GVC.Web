using GVC.Web.Data;
using GVC.Web.Models;
using GVC.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Usuarios;

public class EditModel(ErpDbContext db, IPasswordHasher hasher) : BasePageModel
{
    [BindProperty]
    public Usuario Usuario { get; set; } = null!;

    [BindProperty]
    public string? NovaSenha
    {
        get; set;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Usuario = await db.Usuarios.AsNoTracking().SingleOrDefaultAsync(x => x.UsuarioId == id && x.EmpresaId == EmpresaId) ?? null!;

        return Usuario is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var entity = await db.Usuarios.SingleOrDefaultAsync(x => x.UsuarioId == Usuario.UsuarioId && x.EmpresaId == EmpresaId);

        if (entity is null)
            return NotFound();

        Usuario.Cpf = new string((Usuario.Cpf ?? "").Where(char.IsDigit).ToArray());

        if (Usuario.Cpf.Length != 11)
            ModelState.AddModelError("Usuario.Cpf", "CPF inválido.");

        if (await db.Usuarios.AnyAsync(x => x.EmpresaId == EmpresaId && x.UsuarioId != Usuario.UsuarioId && (x.NomeUsuario == Usuario.NomeUsuario || x.Email == Usuario.Email)))
            ModelState.AddModelError(string.Empty, "Usuário ou e-mail já cadastrado.");

        if (!string.IsNullOrEmpty(NovaSenha) && NovaSenha.Length < 8)
            ModelState.AddModelError("NovaSenha", "Use ao menos 8 caracteres.");

        ModelState.Remove("Usuario.Senha");

        if (!ModelState.IsValid)
            return Page();

        entity.NomeCompleto = Usuario.NomeCompleto;

        entity.TipoUsuario = Usuario.TipoUsuario;

        entity.Cpf = Usuario.Cpf;

        entity.DataNascimento = Usuario.DataNascimento;

        entity.NomeUsuario = Usuario.NomeUsuario;

        entity.Email = Usuario.Email;

        if (!string.IsNullOrEmpty(NovaSenha))
            entity.Senha = hasher.Hash(NovaSenha);

        await db.SaveChangesAsync();

        TempData["Success"] = "Usuário alterado.";

        return RedirectToPage("Index");
    }
}