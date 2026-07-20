using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("ItensPedido")]
public class ItemPedido
{
    [Key]
    public int ItensPedidoId
    {
        get; set;
    }

    public int PedidoId
    {
        get; set;
    }

    public int ProdutoId
    {
        get; set;
    }

    [StringLength(15)]
    public string? Referencia
    {
        get; set;
    }

    public int Quantidade
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecoCustoUnitario
    {
        get; set;
    }

    public Pedido Pedido { get; set; } = null!;

    public Produto Produto { get; set; } = null!;
}