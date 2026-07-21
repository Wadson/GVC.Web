using System.ComponentModel.DataAnnotations;
using GVC.Web.Data;
using GVC.Web.Models;
using GVC.Web.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Financeiro.ContasPagar;

public class CreateModel(ErpDbContext db) : BasePageModel
{
    [BindProperty]
    public ContaAPagarInput Input { get; set; } = new();

    public IReadOnlyList<SelectListItem> Fornecedores { get; private set; } = [];

    public IReadOnlyList<SelectListItem> PlanosContas { get; private set; } = [];

    public IReadOnlyList<SelectListItem> FormasPagamento { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Input.DataEmissao = DateTime.Today;
        Input.DataVencimento = DateTime.Today;
        await CarregarOpcoesAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Input.Descricao = Input.Descricao?.Trim() ?? string.Empty;
        Input.NumeroDocumento = NormalizarOpcional(Input.NumeroDocumento);
        Input.Observacoes = NormalizarOpcional(Input.Observacoes);

        if (Input.Valor <= 0)
            ModelState.AddModelError("Input.Valor", "O valor deve ser maior que zero.");

        if (Input.DataVencimento.Date < Input.DataEmissao.Date)
            ModelState.AddModelError("Input.DataVencimento", "O vencimento não pode ser anterior à emissão.");

        if (Input.FornecedorId.HasValue && !await db.Fornecedores
                .AnyAsync(x => x.FornecedorId == Input.FornecedorId && x.EmpresaId == EmpresaId, cancellationToken))
        {
            ModelState.AddModelError("Input.FornecedorId", "Selecione um fornecedor válido.");
        }

        bool planoValido = await db.PlanosContas.AnyAsync(
            x => x.PlanoContasId == Input.PlanoContasId &&
                 x.EmpresaId == EmpresaId &&
                 x.Tipo == "D",
            cancellationToken);

        if (!planoValido)
            ModelState.AddModelError("Input.PlanoContasId", "Selecione uma conta de despesa válida.");

        if (Input.FormaPgtoId.HasValue && !await db.FormasPagamento
                .AnyAsync(x => x.FormaPgtoId == Input.FormaPgtoId && x.Ativo, cancellationToken))
        {
            ModelState.AddModelError("Input.FormaPgtoId", "Selecione uma forma de pagamento válida.");
        }

        if (!ModelState.IsValid)
        {
            await CarregarOpcoesAsync(cancellationToken);
            return Page();
        }

        var conta = new ContaAPagar
        {
            EmpresaId = EmpresaId,
            FornecedorId = Input.FornecedorId,
            PlanoContasId = Input.PlanoContasId,
            Descricao = Input.Descricao,
            NumeroDocumento = Input.NumeroDocumento,
            DataEmissao = Input.DataEmissao.Date,
            DataVencimento = Input.DataVencimento.Date,
            Valor = Input.Valor,
            ValorPago = 0,
            Status = "Pendente",
            FormaPgtoId = Input.FormaPgtoId,
            Observacoes = Input.Observacoes
        };

        db.ContasAPagar.Add(conta);
        await db.SaveChangesAsync(cancellationToken);

        TempData["Success"] = "Conta a pagar cadastrada com sucesso!";
        return RedirectToPage("Index");
    }

    private async Task CarregarOpcoesAsync(CancellationToken cancellationToken)
    {
        Fornecedores = await db.Fornecedores
            .AsNoTracking()
            .Where(x => x.EmpresaId == EmpresaId)
            .OrderBy(x => x.Nome)
            .Select(x => new SelectListItem(x.Nome, x.FornecedorId.ToString()))
            .ToListAsync(cancellationToken);

        PlanosContas = await db.PlanosContas
            .AsNoTracking()
            .Where(x => x.EmpresaId == EmpresaId && x.Tipo == "D")
            .OrderBy(x => x.CodigoClassificacao)
            .Select(x => new SelectListItem(
                x.CodigoClassificacao + " - " + x.Descricao,
                x.PlanoContasId.ToString()))
            .ToListAsync(cancellationToken);

        FormasPagamento = await db.FormasPagamento
            .AsNoTracking()
            .Where(x => x.Ativo)
            .OrderBy(x => x.NomeFormaPagamento)
            .Select(x => new SelectListItem(x.NomeFormaPagamento, x.FormaPgtoId.ToString()))
            .ToListAsync(cancellationToken);
    }

    private static string? NormalizarOpcional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public sealed class ContaAPagarInput
    {
        [Display(Name = "Fornecedor")]
        public int? FornecedorId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Selecione uma conta de despesa.")]
        [Display(Name = "Plano de contas")]
        public int PlanoContasId { get; set; }

        [Required(ErrorMessage = "Informe a descrição.")]
        [StringLength(200)]
        [Display(Name = "Descrição")]
        public string Descricao { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Número do documento")]
        public string? NumeroDocumento { get; set; }

        [Required, DataType(DataType.Date)]
        [Display(Name = "Data de emissão")]
        public DateTime DataEmissao { get; set; }

        [Required, DataType(DataType.Date)]
        [Display(Name = "Data de vencimento")]
        public DateTime DataVencimento { get; set; }

        [ModelBinder(BinderType = typeof(FlexibleDecimalModelBinder))]
        [Display(Name = "Valor")]
        public decimal Valor { get; set; }

        [Display(Name = "Forma de pagamento prevista")]
        public int? FormaPgtoId { get; set; }

        [StringLength(2000)]
        [Display(Name = "Observações")]
        public string? Observacoes { get; set; }
    }
}
