using GVC.Web.Data;
using GVC.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Configuracoes.FormasPagamento;

public class IndexModel(ErpDbContext db) : BasePageModel
{
    public IReadOnlyList<FormaPagamento> Itens { get; private set; } = [];

    public async Task OnGetAsync() => Itens = await db.FormasPagamento.AsNoTracking().OrderBy(x => x.NomeFormaPagamento).ToListAsync();

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        var item = await db.FormasPagamento.FindAsync(id);

        if (item is null)
            return NotFound();

        try
        {
            db.FormasPagamento.Remove(item);

            await db.SaveChangesAsync();

            TempData["Success"] = "Forma de pagamento excluída.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "A forma possui movimentações e não pode ser excluída. Desative-a na edição.";
        }

        return RedirectToPage();
    }
}