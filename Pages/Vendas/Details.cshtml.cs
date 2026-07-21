using GVC.Web.Data;
using GVC.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

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

        venda.Cliente = await db.Clientes.AsNoTracking().SingleAsync(
            x => x.ClienteId == venda.ClienteId && x.EmpresaId == EmpresaId);

        if (venda.VendedorId.HasValue)
            venda.Vendedor = await db.Vendedores.AsNoTracking().SingleOrDefaultAsync(
                x => x.VendedorId == venda.VendedorId && x.EmpresaId == EmpresaId);

        if (venda.FormaPgtoId.HasValue)
            venda.FormaPagamento = await db.FormasPagamento.AsNoTracking().SingleOrDefaultAsync(x => x.FormaPgtoId == venda.FormaPgtoId);

        venda.Itens = await db.ItensVenda.AsNoTracking().Where(x => x.VendaId == id && x.EmpresaId == EmpresaId)
            .Select(x => new ItemVenda
            {
                ItemVendaId = x.ItemVendaId,
                VendaId = x.VendaId,
                ProdutoId = x.ProdutoId,
                VariacaoID = x.VariacaoID,
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

    public async Task<IActionResult> OnPostCancelarAsync(int id, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var venda = await db.Vendas
            .Include(x => x.Itens)
            .SingleOrDefaultAsync(x => x.VendaId == id && x.EmpresaId == EmpresaId, cancellationToken);
        if (venda is null) return NotFound();
        if (venda.StatusVenda == StatusVenda.Cancelada)
        {
            TempData["Error"] = "A venda já está cancelada.";
            return RedirectToPage(new { id });
        }

        var recebimentos = await db.PagamentosParciais
            .Where(x => x.EmpresaId == EmpresaId && x.Parcela.VendaId == id)
            .ToListAsync(cancellationToken);
        if (recebimentos.Count > 0)
        {
            var caixa = await db.Caixas.SingleOrDefaultAsync(x => x.EmpresaId == EmpresaId &&
                x.UsuarioAberturaId == UsuarioId && x.DataCaixa == DateTime.Today && x.Status == "Aberto", cancellationToken);
            if (caixa is null)
            {
                TempData["Error"] = "Abra seu caixa de hoje antes de cancelar uma venda recebida.";
                return RedirectToPage(new { id });
            }

            db.CaixaMovimentos.Add(new CaixaMovimento
            {
                CaixaId = caixa.CaixaId, EmpresaId = EmpresaId, UsuarioId = UsuarioId,
                FormaPgtoId = venda.FormaPgtoId, Tipo = "SAIDA",
                Valor = recebimentos.Sum(x => x.ValorPago),
                Historico = $"Cancelamento da venda #{venda.VendaId}",
                Origem = "CANCELAMENTO_VENDA", ReferenciaId = venda.VendaId, DataHora = DateTime.Now
            });
            db.PagamentosParciais.RemoveRange(recebimentos);
        }

        if (venda.MovimentouEstoque)
        {
            int[] produtoIds = venda.Itens.Select(x => x.ProdutoId).Distinct().ToArray();
            int[] variacaoIds = venda.Itens.Where(x => x.VariacaoID.HasValue)
                .Select(x => x.VariacaoID!.Value).Distinct().ToArray();
            var produtos = await db.Produtos.Where(x => x.EmpresaId == EmpresaId && produtoIds.Contains(x.ProdutoId))
                .ToDictionaryAsync(x => x.ProdutoId, cancellationToken);
            var variacoes = await db.ProdutosVariacoes.Where(x => variacaoIds.Contains(x.VariacaoId) && x.Produto.EmpresaId == EmpresaId)
                .ToDictionaryAsync(x => x.VariacaoId, cancellationToken);

            foreach (var item in venda.Itens)
            {
                Produto produto = produtos[item.ProdutoId];
                ProdutoVariacao? variacao = item.VariacaoID.HasValue ? variacoes.GetValueOrDefault(item.VariacaoID.Value) : null;
                if (item.VariacaoID.HasValue && variacao is null)
                    throw new InvalidOperationException("A variação original da venda não foi encontrada.");
                if (!item.VariacaoID.HasValue && produto.TemVariacao)
                    throw new InvalidOperationException("Venda inconsistente: produto com grade sem VariacaoID.");

                int anterior = variacao?.Estoque ?? produto.Estoque;
                if (variacao is null) produto.Estoque += item.Quantidade;
                else variacao.Estoque += item.Quantidade;

                db.MovimentacoesEstoque.Add(new MovimentacaoEstoque
                {
                    EmpresaId = EmpresaId, ProdutoId = produto.ProdutoId, VariacaoID = variacao?.VariacaoId,
                    TipoMovimentacao = "ENTRADA", Quantidade = item.Quantidade,
                    EstoqueAnterior = anterior, EstoqueAtual = variacao?.Estoque ?? produto.Estoque,
                    Origem = "CANCELAMENTO_VENDA", Documento = venda.VendaId.ToString(),
                    Usuario = UsuarioId.ToString(), DataMovimentacao = DateTime.Now
                });
            }
        }

        venda.StatusVenda = StatusVenda.Cancelada;
        var parcelas = await db.Parcelas.Where(x => x.VendaId == id && x.EmpresaId == EmpresaId).ToListAsync(cancellationToken);
        foreach (var parcela in parcelas)
        {
            parcela.Status = StatusParcela.Cancelada;
            parcela.ValorRecebido = null;
            parcela.DataPagamento = null;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        TempData["Success"] = "Venda cancelada e estoque devolvido com sucesso.";
        return RedirectToPage(new { id });
    }
}
