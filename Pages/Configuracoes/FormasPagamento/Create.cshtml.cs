using GVC.Web.Data;
using GVC.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace GVC.Web.Pages.Configuracoes.FormasPagamento;

public class CreateModel(ErpDbContext db) : BasePageModel
{
    [BindProperty]
    public FormaPagamento Item { get; set; } = new() { Ativo = true };

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        db.FormasPagamento.Add(Item);

        await db.SaveChangesAsync();

        return RedirectToPage("Index");
    }
}