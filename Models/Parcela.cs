using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("Parcela")]
public class Parcela
{
    [Key]
    public int ParcelaId
    {
        get; set;
    }

    public int VendaId
    {
        get; set;
    }

    public int NumeroParcela
    {
        get; set;
    }

    [Column(TypeName = "date")]
    public DateTime DataVencimento
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ValorParcela
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ValorRecebido
    {
        get; set;
    }

    public StatusParcela Status { get; set; } = StatusParcela.Pendente;

    [NotMapped]
    public StatusParcela StatusAtual =>
        Status == StatusParcela.Pendente && DataVencimento.Date < DateTime.Today
            ? StatusParcela.Atrasada
            : Status;

    [NotMapped]
    public bool PodeReceber => Status is not (StatusParcela.Pago or StatusParcela.Cancelada);

    [Column(TypeName = "date")]
    public DateTime? DataPagamento
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Juros
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Multa
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

    public Venda Venda { get; set; } = null!;

    public Empresa Empresa { get; set; } = null!;
}
