using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("Venda")]
public class Venda
{
    [Key]
    public int VendaId
    {
        get; set;
    }

    public int ClienteId
    {
        get; set;
    }

    public int? FormaPgtoId
    {
        get; set;
    }

    public DateTime DataVenda
    {
        get; set;
    }

    public string? Observacoes
    {
        get; set;
    }

    public StatusVenda StatusVenda { get; set; } = StatusVenda.Aberta;

    [NotMapped]
    public bool MovimentouEstoque => StatusVenda is
        StatusVenda.Concluida or StatusVenda.AguardandoPagamento;

    public int? VendedorId
    {
        get; set;
    }

    public int EmpresaId
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalBruto
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalDesconto
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalLiquido
    {
        get; set;
    }

    public Cliente Cliente { get; set; } = null!;

    public FormaPagamento? FormaPagamento
    {
        get; set;
    }

    public Vendedor? Vendedor
    {
        get; set;
    }

    public Empresa Empresa { get; set; } = null!;

    public ICollection<ItemVenda> Itens { get; set; } = [];
}
