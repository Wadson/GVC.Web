using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("PlanoContas")]
public class PlanoContas
{
    [Key]
    public int PlanoContasId
    {
        get; set;
    }

    [Required, StringLength(20)]
    public string CodigoClassificacao { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Descricao { get; set; } = string.Empty;

    [Required, StringLength(1), Column(TypeName = "char(1)")]
    public string Tipo { get; set; } = string.Empty;

    public int EmpresaId
    {
        get; set;
    }

    public Empresa Empresa { get; set; } = null!;
}