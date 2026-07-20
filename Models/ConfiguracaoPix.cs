using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("ConfiguracaoPix")]
public class ConfiguracaoPix
{
    [Key]
    public int ConfiguracaoPixId
    {
        get; set;
    }

    public int EmpresaId
    {
        get; set;
    }

    [Required, StringLength(200)]
    public string ChavePix { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string NomeBeneficiario { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Cidade { get; set; } = string.Empty;

    public Empresa Empresa { get; set; } = null!;
}