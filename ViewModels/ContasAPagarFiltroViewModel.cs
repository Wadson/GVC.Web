using Microsoft.AspNetCore.Mvc.Rendering;

namespace GVC.Web.ViewModels;

public sealed class ContasAPagarFiltroViewModel
{
    public string? Busca { get; set; }
    public DateTime? DataInicial { get; set; }
    public DateTime? DataFinal { get; set; }
    public string? Status { get; set; }
    public int? PlanoContasId { get; set; }
    public decimal TotalAPagar { get; set; }
    public decimal TotalPago { get; set; }
    public decimal TotalVencido { get; set; }
    public IReadOnlyList<SelectListItem> Categorias { get; set; } = [];
    public IReadOnlyList<SelectListItem> FormasPagamento { get; set; } = [];
    public IReadOnlyList<ContaAPagarListaItemViewModel> Itens { get; set; } = [];
}

public sealed class ContaAPagarListaItemViewModel
{
    public int ContasAPagarId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? NumeroDocumento { get; set; }
    public string? Fornecedor { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public DateTime DataVencimento { get; set; }
    public decimal Valor { get; set; }
    public decimal ValorPago { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool PodeEditar { get; set; }
    public bool PodeBaixar { get; set; }
    public bool PodeEstornar { get; set; }
    public bool PodeCancelar { get; set; }
    public decimal Saldo => Math.Max(0, Valor - ValorPago);
}
