using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("FiscalDocumento")]
public class FiscalDocumento
{
    [Key]
    public int DocumentoFiscalId
    {
        get; set;
    }

    public int? VendaId
    {
        get; set;
    }

    public int EmpresaId
    {
        get; set;
    }

    [StringLength(44), Column(TypeName = "char(44)")]
    public string? ChaveAcesso
    {
        get; set;
    }

    public int NumeroNota
    {
        get; set;
    }

    public int Serie
    {
        get; set;
    }

    public int Modelo { get; set; } = 55;

    public DateTime DataEmissao
    {
        get; set;
    }

    [Required, StringLength(30)]
    public string StatusSefaz { get; set; } = "NaoEnviada";

    [StringLength(255)]
    public string? MotivoRejeicao
    {
        get; set;
    }

    public string? XmlEnviado
    {
        get; set;
    }

    public string? XmlRetornado
    {
        get; set;
    }

    [Required, StringLength(100)]
    public string NaturezaOperacao { get; set; } = "Venda de mercadorias";

    public Venda? Venda
    {
        get; set;
    }

    public Empresa Empresa { get; set; } = null!;
}