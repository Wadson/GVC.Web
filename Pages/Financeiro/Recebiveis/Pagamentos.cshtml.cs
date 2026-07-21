using System.Data;
using GVC.Web.Data;
using GVC.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Financeiro.Recebiveis;

public class PagamentosModel(ErpDbContext db) : BasePageModel
{
    public Parcela Parcela { get; private set; } = null!;

    public IReadOnlyList<PagamentoParcial> Pagamentos { get; private set; } = [];

    [BindProperty]
    public List<int> PagamentoIds { get; set; } = [];

    public decimal TotalPago => Pagamentos.Sum(x => x.ValorPago);

    public decimal Saldo => Math.Max(0, Parcela.ValorParcela - TotalPago);

    public async Task<IActionResult> OnGetAsync(int id) => await LoadAsync(id) ? Page() : NotFound();

    public async Task<IActionResult> OnPostEstornarAsync(int id, CancellationToken cancellationToken)
    {
        if (PagamentoIds.Count == 0)
        {
            TempData["Error"] = "Selecione ao menos um pagamento para estornar.";

            return RedirectToPage(new
            {
                id
            });
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var caixa = await db.Caixas.SingleOrDefaultAsync(x => x.EmpresaId == EmpresaId &&
            x.UsuarioAberturaId == UsuarioId && x.DataCaixa == DateTime.Today && x.Status == "Aberto", cancellationToken);

        if (caixa is null)
        {
            TempData["Error"] = "Abra o caixa antes de realizar o estorno.";

            return RedirectToPage(new
            {
                id
            });
        }

        var parcela = await db.Parcelas.SingleOrDefaultAsync(x => x.ParcelaId == id && x.EmpresaId == EmpresaId, cancellationToken);

        if (parcela is null)
            return NotFound();

        var pagamentos = await db.PagamentosParciais.Where(x => x.ParcelaId == id && x.EmpresaId == EmpresaId && PagamentoIds.Contains(x.PagamentoId)).ToListAsync(cancellationToken);

        if (pagamentos.Count != PagamentoIds.Distinct().Count())
        {
            TempData["Error"] = "Um ou mais pagamentos selecionados não são válidos.";

            return RedirectToPage(new
            {
                id
            });
        }

        foreach (var pagamento in pagamentos)
        {
            db.CaixaMovimentos.Add(new CaixaMovimento { CaixaId = caixa.CaixaId, EmpresaId = EmpresaId, UsuarioId = UsuarioId, FormaPgtoId = pagamento.FormaPgtoId, Tipo = "SAIDA", Valor = pagamento.ValorPago, Historico = $"Estorno do pagamento #{pagamento.PagamentoId} - parcela {parcela.NumeroParcela} da venda #{parcela.VendaId}", Origem = "EstornoRecebimento", ReferenciaId = pagamento.PagamentoId, DataHora = DateTime.Now });
        }

        db.PagamentosParciais.RemoveRange(pagamentos);

        await db.SaveChangesAsync(cancellationToken);

        var restantes = await db.PagamentosParciais.AsNoTracking().Where(x => x.ParcelaId == id && x.EmpresaId == EmpresaId).ToListAsync(cancellationToken);

        var totalRestante = restantes.Sum(x => x.ValorPago);

        parcela.ValorRecebido = totalRestante > 0 ? totalRestante : null;

        if (totalRestante >= parcela.ValorParcela - 0.01m)
        {
            parcela.Status = StatusParcela.Pago;

            parcela.DataPagamento = restantes.Max(x => (DateTime?)x.DataPagamento);
        }
        else
            if (totalRestante > 0)
            {
                parcela.Status = StatusParcela.ParcialmentePago;

                parcela.DataPagamento = null;
            }
            else
            {
                parcela.Status = StatusParcela.Pendente;

                parcela.DataPagamento = null;
            }

        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        TempData["Success"] = pagamentos.Count == 1 ? "Pagamento estornado com sucesso." : $"{pagamentos.Count} pagamentos estornados com sucesso.";

        return RedirectToPage(new
        {
            id
        });
    }

    private async Task<bool> LoadAsync(int id)
    {
        Parcela = await db.Parcelas.AsNoTracking().Include(x => x.Venda).ThenInclude(x => x.Cliente)
            .SingleOrDefaultAsync(x => x.ParcelaId == id && x.EmpresaId == EmpresaId && x.Venda.EmpresaId == EmpresaId) ?? null!;

        if (Parcela is null)
            return false;

        Pagamentos = await db.PagamentosParciais.AsNoTracking().Include(x => x.FormaPagamento)
            .Where(x => x.ParcelaId == id && x.EmpresaId == EmpresaId).OrderBy(x => x.DataPagamento).ThenBy(x => x.PagamentoId).ToListAsync();

        return true;
    }
}
