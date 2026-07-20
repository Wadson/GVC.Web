using GVC.Web.Data;
using GVC.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Vendedores;

public class IndexModel(ErpDbContext db) : BasePageModel
{
    public IReadOnlyList<Vendedor> Itens { get; private set; } = [];

    public async Task OnGetAsync() => Itens = await db.Vendedores.AsNoTracking().Where(x => x.EmpresaId == EmpresaId).OrderBy(x => x.Nome).ToListAsync();

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        var item = await db.Vendedores.SingleOrDefaultAsync(x => x.VendedorId == id && x.EmpresaId == EmpresaId);

        if (item is null)
            return NotFound();

        try
        {
            db.Vendedores.Remove(item);

            await db.SaveChangesAsync();

            TempData["Success"] = "Vendedor excluído.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "O vendedor possui vendas ou comissões e não pode ser excluído. Inative-o.";
        }

        return RedirectToPage();
    }
}