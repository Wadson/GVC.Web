using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("Estado")]
public class Estado
{
    [Key]
    public int EstadoId
    {
        get; set;
    }

    [Required, StringLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required, StringLength(2), Column(TypeName = "char(2)")]
    public string Uf { get; set; } = string.Empty;

    public int? Ibge
    {
        get; set;
    }

    public int? Pais
    {
        get; set;
    }

    [StringLength(10)]
    public string? Ddd
    {
        get; set;
    }

    public ICollection<Cidade> Cidades { get; set; } = [];
}