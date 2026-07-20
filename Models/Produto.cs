using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("Produtos")]
public class Produto
{
    [Key]
    public int ProdutoId
    {
        get; set;
    }

    [Required, StringLength(100)]
    public string NomeProduto { get; set; } = string.Empty;

    [StringLength(15)]
    public string? Referencia
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecoCusto
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecoDeVenda
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PrecoCompra
    {
        get; set;
    }

    public int Estoque
    {
        get; set;
    }

    public DateTime DataDeEntrada
    {
        get; set;
    }

    [Required, StringLength(20)]
    public string Status { get; set; } = "Ativo";

    [StringLength(50)]
    public string? Situacao
    {
        get; set;
    }

    [StringLength(20)]
    public string? Unidade
    {
        get; set;
    }

    public DateTime? DataValidade
    {
        get; set;
    }

    [StringLength(20)]
    public string? GtinEan
    {
        get; set;
    }

    [StringLength(255)]
    public string? Imagem
    {
        get; set;
    }

    public int? FornecedorId
    {
        get; set;
    }

    public int? MarcaId
    {
        get; set;
    }

    public int? CategoriaId
    {
        get; set;
    }

    public int EmpresaId
    {
        get; set;
    }

    [StringLength(10)]
    public string? Ncm
    {
        get; set;
    }

    [StringLength(10)]
    public string? Cest
    {
        get; set;
    }

    public int Origem
    {
        get; set;
    }

    [StringLength(4), Column("CFOP_Padrao")]
    public string? CfopPadrao
    {
        get; set;
    }

    [StringLength(4)]
    public string? Csosn
    {
        get; set;
    }

    [StringLength(3), Column("CST_ICMS")]
    public string? CstIcms
    {
        get; set;
    }

    [Column(TypeName = "decimal(5,2)")]
    public decimal AliquotaIcms
    {
        get; set;
    }

    [Column(TypeName = "decimal(5,2)")]
    public decimal AliquotaIpi
    {
        get; set;
    }

    [Column(TypeName = "decimal(5,2)")]
    public decimal AliquotaPis
    {
        get; set;
    }

    [Column(TypeName = "decimal(5,2)")]
    public decimal AliquotaCofins
    {
        get; set;
    }

    public Fornecedor? Fornecedor
    {
        get; set;
    }

    public Marca? Marca
    {
        get; set;
    }

    public Categoria? Categoria
    {
        get; set;
    }

    public Empresa Empresa { get; set; } = null!;
}