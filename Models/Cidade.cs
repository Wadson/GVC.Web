using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("Cidade")]
public class Cidade
{
    [Key]
    public int CidadeId
    {
        get; set;
    }

    [Required, StringLength(100)]
    public string Nome { get; set; } = string.Empty;

    public int EstadoId
    {
        get; set;
    }

    [StringLength(10)]
    public string? CodigoIbge
    {
        get; set;
    }

    public Estado Estado { get; set; } = null!;
}