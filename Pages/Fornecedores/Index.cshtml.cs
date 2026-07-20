using GVC.Web.Data;
using GVC.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Fornecedores;

public class IndexModel(ErpDbContext db) : BasePageModel
{
    public IReadOnlyList<Fornecedor> Itens { get; private set; } = [];

    public async Task OnGetAsync() => Itens = await db.Fornecedores.AsNoTracking().Where(x => x.EmpresaId == EmpresaId).OrderBy(x => x.Nome).ToListAsync();

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        var item = await db.Fornecedores.SingleOrDefaultAsync(x => x.FornecedorId == id && x.EmpresaId == EmpresaId);

        if (item is null)
            return NotFound();

        try
        {
            db.Fornecedores.Remove(item);

            await db.SaveChangesAsync();

            TempData["Success"] = "Fornecedor excluído.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "O fornecedor possui vínculos e não pode ser excluído.";
        }

        return RedirectToPage();
    }
}