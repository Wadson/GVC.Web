using GVC.Web.Data;
using GVC.Web.Extensions;
using GVC.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Fornecedores;

public class CreateModel(ErpDbContext db) : BasePageModel
{
    [BindProperty]
    public Fornecedor Item { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Item.EmpresaId = EmpresaId;

        Item.Cnpj = Item.Cnpj.OnlyDigits();

        if (Item.Cnpj.Length > 0 && Item.Cnpj.Length != 14)
            ModelState.AddModelError("Item.Cnpj", "CNPJ inválido.");

        if (!await db.Cidades.AnyAsync(x => x.CidadeId == Item.CidadeId))
            ModelState.AddModelError("Item.CidadeId", "Selecione uma cidade válida.");

        if (!ModelState.IsValid)
            return Page();

        Item.DataCriacao = DateTime.Now;

        db.Fornecedores.Add(Item);

        await db.SaveChangesAsync();

        TempData["Success"] = "Fornecedor cadastrado.";

        return RedirectToPage("Index");
    }
}