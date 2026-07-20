using GVC.Web.Data;
using GVC.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Configuracoes.FormasPagamento;

public class EditModel(ErpDbContext db) : BasePageModel
{
    [BindProperty]
    public FormaPagamento Item { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Item = await db.FormasPagamento.AsNoTracking().SingleOrDefaultAsync(x => x.FormaPgtoId == id) ?? null!;

        return Item is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var entity = await db.FormasPagamento.FindAsync(Item.FormaPgtoId);

        if (entity is null)
            return NotFound();

        if (!ModelState.IsValid)
            return Page();

        entity.NomeFormaPagamento = Item.NomeFormaPagamento;

        entity.Ativo = Item.Ativo;

        await db.SaveChangesAsync();

        TempData["Success"] = "Forma de pagamento alterada.";

        return RedirectToPage("Index");
    }
}