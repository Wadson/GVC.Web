using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("BoletoBancario")]
public class BoletoBancario
{
    [Key]
    public int BoletoId
    {
        get; set;
    }

    public int ParcelaId
    {
        get; set;
    }

    [Required, StringLength(5)]
    public string BancoId { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string NossoNumero { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string NumeroDocumento { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LinhaDigitavel { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string CodigoBarras { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime DataEmissao
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ValorBoleto
    {
        get; set;
    }

    [Required, StringLength(20)]
    public string StatusBoleto { get; set; } = "Emitido";

    [StringLength(100)]
    public string? ArquivoRemessa
    {
        get; set;
    }

    [StringLength(100)]
    public string? ArquivoRetorno
    {
        get; set;
    }

    public Parcela Parcela { get; set; } = null!;
}