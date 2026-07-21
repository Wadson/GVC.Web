using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("DocumentoEntradaItem")]
public class DocumentoEntradaItem
{
    [Key, Column("DocumentoEntradaItemID")]
    public int DocumentoEntradaItemId { get; set; }

    [Column("DocumentoEntradaID")]
    public int DocumentoEntradaId { get; set; }

    [Column("ProdutoID")]
    public int ProdutoId { get; set; }

    [Column("VariacaoID")]
    public int? VariacaoID { get; set; }

    public int Quantidade { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecoUnitarioCompra { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecoCustoUnitario { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ValorTotalItem { get; set; }

    [StringLength(4)]
    public string? Cfop { get; set; }

    [StringLength(10)]
    public string? Ncm { get; set; }

    public DocumentoEntrada DocumentoEntrada { get; set; } = null!;

    public Produto Produto { get; set; } = null!;

    public ProdutoVariacao? Variacao { get; set; }
}
