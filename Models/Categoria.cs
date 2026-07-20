using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("Categoria")]
public class Categoria
{
    [Key]
    public int CategoriaId
    {
        get; set;
    }

    [Required, StringLength(100)]
    public string NomeCategoria { get; set; } = string.Empty;

    public int EmpresaId
    {
        get; set;
    }

    public Empresa Empresa { get; set; } = null!;
}