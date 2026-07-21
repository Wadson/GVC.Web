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

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Produtos = await BuscarProdutosAsync(Pesquisa, cancellationToken);
    }

    public async Task<PartialViewResult> OnGetPesquisarAsync(
        string? pesquisa,
        CancellationToken cancellationToken)
    {
        var produtos = await BuscarProdutosAsync(pesquisa, cancellationToken);
        return Partial("_ListaProdutos", produtos);
    }

    private async Task<IReadOnlyList<Produto>> BuscarProdutosAsync(
        string? pesquisa,
        CancellationToken cancellationToken)
    {
        var query = db.Produtos.AsNoTracking()
            .Include(x => x.Variacoes)
                .ThenInclude(x => x.Atributos)
            .Where(x => x.EmpresaId == EmpresaId);

        var termo = pesquisa?.Trim();

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var codigoValido = int.TryParse(termo, out var codigo);

            query = query.Where(x =>
                x.NomeProduto.Contains(termo) ||
                (x.Referencia != null && x.Referencia.Contains(termo)) ||
                (x.GtinEan != null && x.GtinEan.Contains(termo)) ||
                x.Variacoes.Any(v =>
                    (v.Sku != null && v.Sku.Contains(termo)) ||
                    (v.GtinEan != null && v.GtinEan.Contains(termo)) ||
                    v.Atributos.Any(a =>
                        a.NomeAtributo.Contains(termo) ||
                        a.ValorAtributo.Contains(termo))) ||
                (codigoValido && x.ProdutoId == codigo));
        }

        return await query
            .OrderBy(x => x.NomeProduto)
            .ThenBy(x => x.ProdutoId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        var item = await db.Produtos.Include(x => x.Variacoes)
            .SingleOrDefaultAsync(x => x.ProdutoId == id && x.EmpresaId == EmpresaId);

        if (item is null)
            return NotFound();

        try
        {
            db.Produtos.Remove(item);

            await db.SaveChangesAsync();

            ProductImageStorage.Delete(item.Imagem, environment);
            foreach (var imagem in item.Variacoes.Select(x => x.Imagem).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
                ProductImageStorage.Delete(imagem, environment);

            TempData["Success"] = "Produto excluído.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "O produto possui movimentações e não pode ser excluído. Altere o status para Inativo.";
        }

        return RedirectToPage();
    }
}
