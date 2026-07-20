using GVC.Web.Data;
using GVC.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Vendas;

public class DetailsModel(ErpDbContext db) : BasePageModel
{
    public Venda Venda { get; private set; } = null!;

    public IReadOnlyList<Parcela> Parcelas { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var venda = await db.Vendas.AsNoTracking().SingleOrDefaultAsync(x => x.VendaId == id && x.EmpresaId == EmpresaId);

        if (venda is null)
            return NotFound();

        venda.Cliente = await db.Clientes.AsNoTracking().SingleAsync(x => x.ClienteId == venda.ClienteId);

        if (venda.VendedorId.HasValue)
            venda.Vendedor = await db.Vendedores.AsNoTracking().SingleOrDefaultAsync(x => x.VendedorId == venda.VendedorId);

        if (venda.FormaPgtoId.HasValue)
            venda.FormaPagamento = await db.FormasPagamento.AsNoTracking().SingleOrDefaultAsync(x => x.FormaPgtoId == venda.FormaPgtoId);

        venda.Itens = await db.ItensVenda.AsNoTracking().Where(x => x.VendaId == id && x.EmpresaId == EmpresaId)
            .Select(x => new ItemVenda
            {
                ItemVendaId = x.ItemVendaId,
                VendaId = x.VendaId,
                ProdutoId = x.ProdutoId,
                Quantidade = x.Quantidade,
                PrecoUnitario = x.PrecoUnitario,
                DescontoItem = x.DescontoItem,
                EmpresaId = x.EmpresaId,
                Produto = new Produto { ProdutoId = x.Produto.ProdutoId, NomeProduto = x.Produto.NomeProduto }
            }).ToListAsync();

        Venda = venda;

        Parcelas = await db.Parcelas.AsNoTracking().Where(x => x.VendaId == id && x.EmpresaId == EmpresaId)
            .OrderBy(x => x.NumeroParcela).ToListAsync();

        return Page();
    }
}