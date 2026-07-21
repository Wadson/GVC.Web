using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("ItemVenda")]
public class ItemVenda
{
    [Key]
    public int ItemVendaId
    {
        get; set;
    }

    public int VendaId
    {
        get; set;
    }

    public int ProdutoId
    {
        get; set;
    }

    [Column("VariacaoID")]
    public int? VariacaoID { get; set; }

    public int Quantidade
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecoUnitario
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DescontoItem
    {
        get; set;
    }

    public int EmpresaId
    {
        get; set;
    }

    public Venda Venda { get; set; } = null!;

    public Produto Produto { get; set; } = null!;

    public ProdutoVariacao? Variacao { get; set; }

    public Empresa Empresa { get; set; } = null!;
}
