using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("PagamentoComissaoVendedor")]
public class PagamentoComissaoVendedor
{
    [Key]
    public int PagamentoComissaoId
    {
        get; set;
    }

    public int VendedorId
    {
        get; set;
    }

    public int EmpresaId
    {
        get; set;
    }

    [Column(TypeName = "date")]
    public DateTime DataInicial
    {
        get; set;
    }

    [Column(TypeName = "date")]
    public DateTime DataFinal
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalVendas
    {
        get; set;
    }

    [Column(TypeName = "decimal(5,2)")]
    public decimal PercentualComissao
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ValorComissao
    {
        get; set;
    }

    public int? UsuarioId
    {
        get; set;
    }

    [Required, StringLength(20), Column(TypeName = "varchar(20)")]
    public string Status { get; set; } = "Pendente";

    public DateTime DataPagamento
    {
        get; set;
    }

    [StringLength(500)]
    public string? Observacoes
    {
        get; set;
    }

    public Vendedor Vendedor { get; set; } = null!;

    public Empresa Empresa { get; set; } = null!;

    public Usuario? Usuario
    {
        get; set;
    }
}