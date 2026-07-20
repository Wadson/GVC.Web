using GVC.Web.Data;
using GVC.Web.Models;
using GVC.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Usuarios;

public class CreateModel(ErpDbContext db, IPasswordHasher hasher) : BasePageModel
{
    [BindProperty]
    public Usuario Usuario { get; set; } = new();

    [BindProperty]
    public string? Senha
    {
        get; set;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Usuario.EmpresaId = EmpresaId;

        Usuario.Cpf = new string((Usuario.Cpf ?? "").Where(char.IsDigit).ToArray());

        if (string.IsNullOrWhiteSpace(Senha) || Senha.Length < 8)
            ModelState.AddModelError(nameof(Senha), "Use ao menos 8 caracteres.");

        if (await db.Usuarios.AnyAsync(x => x.EmpresaId == EmpresaId && (x.NomeUsuario == Usuario.NomeUsuario || x.Email == Usuario.Email)))
            ModelState.AddModelError(string.Empty, "Usuário ou e-mail já cadastrado.");

        ModelState.Remove("Usuario.Senha");

        if (!ModelState.IsValid)
            return Page();

        Usuario.Senha = hasher.Hash(Senha!);

        Usuario.DataCriacao = DateTime.Now;

        db.Usuarios.Add(Usuario);

        await db.SaveChangesAsync();

        TempData["Success"] = "Usuário cadastrado.";

        return RedirectToPage("Index");
    }
}