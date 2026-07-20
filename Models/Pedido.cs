using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("Pedidos")]
public class Pedido
{
    [Key]
    public int PedidoId
    {
        get; set;
    }

    public int FornecedorId
    {
        get; set;
    }

    [Column(TypeName = "date")]
    public DateTime DataPedido
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ValorTotalPedido
    {
        get; set;
    }

    [Required, StringLength(20)]
    public string Status { get; set; } = "Pendente";

    public int EmpresaId
    {
        get; set;
    }

    public Fornecedor Fornecedor { get; set; } = null!;

    public Empresa Empresa { get; set; } = null!;

    public ICollection<ItemPedido> Itens { get; set; } = [];
}