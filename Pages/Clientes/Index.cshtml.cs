using GVC.Web.Data;
using GVC.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Clientes;

public class IndexModel(ErpDbContext db) : BasePageModel
{
    public IReadOnlyList<Cliente> Clientes { get; private set; } = [];

    public async Task OnGetAsync() => Clientes = await db.Clientes.AsNoTracking().Where(x => x.EmpresaId == EmpresaId).OrderBy(x => x.Nome).ToListAsync();

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        var item = await db.Clientes.SingleOrDefaultAsync(x => x.ClienteId == id && x.EmpresaId == EmpresaId);

        if (item is null)
            return NotFound();

        try
        {
            db.Clientes.Remove(item);

            await db.SaveChangesAsync();

            TempData["Success"] = "Cliente excluído.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "O cliente possui movimentações e não pode ser excluído. Inative-o na edição.";
        }

        return RedirectToPage();
    }
}