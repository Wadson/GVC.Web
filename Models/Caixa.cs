using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("Caixa")]
public class Caixa
{
    [Key]
    public int CaixaId
    {
        get; set;
    }

    [Column(TypeName = "date")]
    public DateTime DataCaixa
    {
        get; set;
    }

    [Column(TypeName = "datetime2(0)")]
    public DateTime DataAbertura
    {
        get; set;
    }

    public int UsuarioAberturaId
    {
        get; set;
    }

    [Column(TypeName = "datetime2(0)")]
    public DateTime? DataFechamento
    {
        get; set;
    }

    public int? UsuarioFechamentoId
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SaldoInicial
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? SaldoFinalSistema
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? SaldoFinalInformado
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Diferenca
    {
        get; set;
    }

    [Required, StringLength(20)]
    public string Status { get; set; } = "Aberto";

    public string? Observacao
    {
        get; set;
    }

    public int EmpresaId
    {
        get; set;
    }

    public Usuario UsuarioAbertura { get; set; } = null!;

    public Usuario? UsuarioFechamento
    {
        get; set;
    }

    public Empresa Empresa { get; set; } = null!;
}