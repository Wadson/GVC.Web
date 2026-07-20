using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("PagamentosParciais")]
public class PagamentoParcial
{
    [Key]
    public int PagamentoId
    {
        get; set;
    }

    public int ParcelaId
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ValorPago
    {
        get; set;
    }

    [Column(TypeName = "date")]
    public DateTime DataPagamento
    {
        get; set;
    }

    public int? FormaPgtoId
    {
        get; set;
    }

    public string? Observacao
    {
        get; set;
    }

    public int EmpresaId
    {
        get; set;
    }

    public Parcela Parcela { get; set; } = null!;

    public FormaPagamento? FormaPagamento
    {
        get; set;
    }

    public Empresa Empresa { get; set; } = null!;
}