using System.ComponentModel.DataAnnotations;
using System.Data;
using GVC.Web.Data;
using GVC.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GVC.Web.Pages.Financeiro.Recebiveis;

public class ReceberModel(ErpDbContext db, ILogger<ReceberModel>? pageLogger = null) : BasePageModel
{
    private readonly ILogger<ReceberModel> logger = pageLogger ?? NullLogger<ReceberModel>.Instance;

    public Parcela Parcela { get; private set; } = null!;

    public SelectList FormasPagamento { get; private set; } = null!;

    [BindProperty]
    public RecebimentoInput Input { get; set; } = new() { DataPagamento = DateTime.Today };

    public decimal Saldo => Parcela.ValorParcela - (Parcela.ValorRecebido ?? 0);

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await LoadAsync(id))
            return NotFound();

        if (!Parcela.PodeReceber)
        {
            TempData["Error"] = "Esta parcela não está disponível para recebimento.";

            return RedirectToPage("Index");
        }

        Input.ParcelaId = id;

        Input.Valor = Saldo;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!await LoadAsync(Input.ParcelaId))
            return NotFound();

        var saldo = Saldo;

        if (!Parcela.PodeReceber)
            ModelState.AddModelError(string.Empty, "Esta parcela não está disponível para recebimento.");

        if (Input.Valor <= 0 || Input.Valor > saldo)
            ModelState.AddModelError("Input.Valor", $"Informe um valor entre R$ 0,01 e {saldo:C}.");

        if (Input.DataPagamento.Date > DateTime.Today)
            ModelState.AddModelError("Input.DataPagamento", "A data do pagamento não pode ser futura.");

        if (!await db.FormasPagamento.AnyAsync(x => x.FormaPgtoId == Input.FormaPagamentoId && x.Ativo, cancellationToken))
            ModelState.AddModelError("Input.FormaPagamentoId", "Selecione uma forma de pagamento válida.");

        var caixa = await db.Caixas.SingleOrDefaultAsync(x => x.EmpresaId == EmpresaId &&
            x.UsuarioAberturaId == UsuarioId && x.DataCaixa == DateTime.Today && x.Status == "Aberto", cancellationToken);

        if (caixa is null)
            ModelState.AddModelError(string.Empty, "Abra o caixa antes de receber uma conta.");

        if (!ModelState.IsValid)
            return Page();

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        caixa = await db.Caixas.SingleOrDefaultAsync(x => x.EmpresaId == EmpresaId &&
            x.UsuarioAberturaId == UsuarioId && x.DataCaixa == DateTime.Today && x.Status == "Aberto", cancellationToken);
        if (caixa is null)
        {
            ModelState.AddModelError(string.Empty, "O caixa foi fechado. Abra um caixa antes de receber.");
            return Page();
        }

        var entity = await db.Parcelas.SingleAsync(x => x.ParcelaId == Input.ParcelaId && x.EmpresaId == EmpresaId, cancellationToken);

        if (entity.Status is StatusParcela.Pago or StatusParcela.Cancelada)
        {
            ModelState.AddModelError(string.Empty, "A situação da parcela foi alterada. Recarregue a página.");
            return Page();
        }

        var saldoAtual = entity.ValorParcela - (entity.ValorRecebido ?? 0);

        if (Input.Valor > saldoAtual)
        {
            ModelState.AddModelError("Input.Valor", "O saldo da parcela foi alterado. Recarregue a página.");

            return Page();
        }

        entity.ValorRecebido = (entity.ValorRecebido ?? 0) + Input.Valor;

        var quitada = entity.ValorRecebido >= entity.ValorParcela - 0.01m;

        entity.Status = quitada ? StatusParcela.Pago : StatusParcela.ParcialmentePago;

        entity.DataPagamento = quitada ? Input.DataPagamento.Date : null;

        var pagamento = new PagamentoParcial { ParcelaId = entity.ParcelaId, EmpresaId = EmpresaId, ValorPago = Input.Valor, DataPagamento = Input.DataPagamento.Date, FormaPgtoId = Input.FormaPagamentoId, Observacao = Input.Observacao };

        db.PagamentosParciais.Add(pagamento);

        await db.SaveChangesAsync(cancellationToken);

        db.CaixaMovimentos.Add(new CaixaMovimento { CaixaId = caixa.CaixaId, EmpresaId = EmpresaId, UsuarioId = UsuarioId, FormaPgtoId = Input.FormaPagamentoId, Tipo = "ENTRADA", Valor = Input.Valor, Historico = $"Recebimento parcela {entity.NumeroParcela} da venda #{entity.VendaId}", Origem = "Recebimento", ReferenciaId = pagamento.PagamentoId, DataHora = DateTime.Now });

        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Recebimento registrado. Parcela {ParcelaId}, empresa {EmpresaId}, usuário {UsuarioId}, valor {Valor}, status {Status}",
            entity.ParcelaId, EmpresaId, UsuarioId, Input.Valor, entity.Status);

        TempData["Success"] = quitada ? "Parcela quitada com sucesso." : "Pagamento parcial registrado com sucesso.";

        return RedirectToPage("Index");
    }

    private async Task<bool> LoadAsync(int id)
    {
        Parcela = await db.Parcelas.AsNoTracking().Include(x => x.Venda).ThenInclude(x => x.Cliente).SingleOrDefaultAsync(x => x.ParcelaId == id && x.EmpresaId == EmpresaId) ?? null!;

        FormasPagamento = new SelectList(await db.FormasPagamento.AsNoTracking().Where(x => x.Ativo).OrderBy(x => x.NomeFormaPagamento).ToListAsync(), "FormaPgtoId", "NomeFormaPagamento");

        return Parcela is not null;
    }

    public sealed class RecebimentoInput
    {
        [Required]
        public int ParcelaId
        {
            get; set;
        }

        [Display(Name = "Valor recebido")]
        public decimal Valor
        {
            get; set;
        }

        [Required, DataType(DataType.Date), Display(Name = "Data do pagamento")]
        public DateTime DataPagamento
        {
            get; set;
        }

        [Range(1, int.MaxValue, ErrorMessage = "Selecione a forma de pagamento."), Display(Name = "Forma de pagamento")]
        public int FormaPagamentoId
        {
            get; set;
        }

        [StringLength(500), Display(Name = "Observação")]
        public string? Observacao
        {
            get; set;
        }
    }
}
