using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("Marca")]
public class Marca
{
    [Key]
    public int MarcaId
    {
        get; set;
    }

    [Required, StringLength(100)]
    public string NomeMarca { get; set; } = string.Empty;

    public int EmpresaId
    {
        get; set;
    }

    public Empresa Empresa { get; set; } = null!;
}