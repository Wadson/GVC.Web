using GVC.Web.Data;
using GVC.Web.Extensions;
using GVC.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Vendedores;

public class CreateModel(ErpDbContext db) : BasePageModel
{
    [BindProperty]
    public Vendedor Item { get; set; } = new() { Status = 1 };

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Item.EmpresaId = EmpresaId;

        Item.Cpf = Item.Cpf.OnlyDigits();

        if (Item.Cpf.Length > 0 && Item.Cpf.Length != 11)
            ModelState.AddModelError("Item.Cpf", "CPF inválido.");

        if (Item.CidadeId.HasValue && !await db.Cidades.AnyAsync(x => x.CidadeId == Item.CidadeId))
            ModelState.AddModelError("Item.CidadeId", "Selecione uma cidade válida.");

        if (Item.Comissao is < 0 or > 100)
            ModelState.AddModelError("Item.Comissao", "A comissão deve estar entre 0 e 100.");

        if (!ModelState.IsValid)
            return Page();

        Item.DataCriacao = DateTime.Now;

        Item.UsuarioCriacao = User.Identity?.Name;

        db.Vendedores.Add(Item);

        await db.SaveChangesAsync();

        TempData["Success"] = "Vendedor cadastrado.";

        return RedirectToPage("Index");
    }
}