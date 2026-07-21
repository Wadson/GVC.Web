using System.ComponentModel.DataAnnotations;

namespace GVC.Web.DTOs;

public class EntradaEstoqueItemDTO
{
    [Range(1, int.MaxValue)]
    public int ProdutoId { get; set; }

    public int? VariacaoID { get; set; }

    public string ProdutoNome { get; set; } = string.Empty;

    public string? CodigoFornecedor { get; set; }

    public string? DescricaoFornecedor { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantidade { get; set; } = 1;

    [Range(typeof(decimal), "0.01", "9999999999999999", ParseLimitsInInvariantCulture = true)]
    public decimal PrecoUnitarioCompra { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999", ParseLimitsInInvariantCulture = true)]
    public decimal PrecoCustoUnitario { get; set; }

    public decimal ValorTotalItem { get; set; }

    public string? Cfop { get; set; }

    public string? Ncm { get; set; }
}
