using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("Promocoes")]
public class Promocao
{
    [Key]
    public int PromocaoId
    {
        get; set;
    }

    public int ProdutoId
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecoOriginal
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecoPromocional
    {
        get; set;
    }

    public DateTime DataInicio
    {
        get; set;
    }

    public DateTime DataFim
    {
        get; set;
    }

    public bool Ativo { get; set; } = true;

    public Produto Produto { get; set; } = null!;
}