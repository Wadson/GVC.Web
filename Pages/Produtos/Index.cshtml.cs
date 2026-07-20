using GVC.Web.Data;
using GVC.Web.Models;
using GVC.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Produtos;

public class IndexModel(ErpDbContext db, IWebHostEnvironment environment) : BasePageModel
{
    public IReadOnlyList<Produto> Produtos { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Pesquisa
    {
        get; set;
    }

    public async Task OnGetAsync()
    {
        var query = db.Produtos.AsNoTracking().Where(x => x.EmpresaId == EmpresaId);

        var termo = Pesquisa?.Trim();

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var codigoValido = int.TryParse(termo, out var codigo);

            query = query.Where(x =>
                x.NomeProduto.Contains(termo) ||
                (x.Referencia != null && x.Referencia.Contains(termo)) ||
                (x.GtinEan != null && x.GtinEan.Contains(termo)) ||
                (codigoValido && x.ProdutoId == codigo));
        }

        Produtos = await query.OrderBy(x => x.NomeProduto).ToListAsync();
    }

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        var item = await db.Produtos.SingleOrDefaultAsync(x => x.ProdutoId == id && x.EmpresaId == EmpresaId);

        if (item is null)
            return NotFound();

        try
        {
            db.Produtos.Remove(item);

            await db.SaveChangesAsync();

            ProductImageStorage.Delete(item.Imagem, environment);

            TempData["Success"] = "Produto excluído.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "O produto possui movimentações e não pode ser excluído. Altere o status para Inativo.";
        }

        return RedirectToPage();
    }
}
