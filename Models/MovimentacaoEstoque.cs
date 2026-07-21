using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("MovimentacaoEstoque")]
public class MovimentacaoEstoque
{
    [Key]
    public int MovimentacaoId
    {
        get; set;
    }

    public int ProdutoId
    {
        get; set;
    }

    public int? VariacaoID { get; set; }

    [Required, StringLength(20)]
    public string TipoMovimentacao { get; set; } = string.Empty;

    public int Quantidade
    {
        get; set;
    }

    public int EstoqueAnterior
    {
        get; set;
    }

    public int EstoqueAtual
    {
        get; set;
    }

    [Required, StringLength(30)]
    public string Origem { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Documento
    {
        get; set;
    }

    [StringLength(255)]
    public string? Observacao
    {
        get; set;
    }

    [Column(TypeName = "datetime")]
    public DateTime DataMovimentacao
    {
        get; set;
    }

    [StringLength(50)]
    public string? Usuario
    {
        get; set;
    }

    public int EmpresaId
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PrecoCompra
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PrecoCustoEntrada
    {
        get; set;
    }

    public int? FornecedorId
    {
        get; set;
    }

    public Produto Produto { get; set; } = null!;

    public ProdutoVariacao? Variacao { get; set; }

    public Fornecedor? Fornecedor
    {
        get; set;
    }

    public Empresa Empresa { get; set; } = null!;
}
