using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("CaixaMovimento")]
public class CaixaMovimento
{
    [Key]
    public int CaixaMovimentoId
    {
        get; set;
    }

    public int CaixaId
    {
        get; set;
    }

    [Column(TypeName = "datetime2(0)")]
    public DateTime DataHora
    {
        get; set;
    }

    [Required, StringLength(10)]
    public string Tipo { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Valor
    {
        get; set;
    }

    public int? FormaPgtoId
    {
        get; set;
    }

    [Required, StringLength(255)]
    public string Historico { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Origem
    {
        get; set;
    }

    public int? ReferenciaId
    {
        get; set;
    }

    public int UsuarioId
    {
        get; set;
    }

    public int EmpresaId
    {
        get; set;
    }

    public Caixa Caixa { get; set; } = null!;

    public FormaPagamento? FormaPagamento
    {
        get; set;
    }

    public Usuario Usuario { get; set; } = null!;

    public Empresa Empresa { get; set; } = null!;
}