using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("ProdutoVariacaoAtributo")]
public class ProdutoVariacaoAtributo
{
    [Key, Column("AtributoID")]
    public int AtributoId { get; set; }

    [Column("VariacaoID")]
    public int VariacaoId { get; set; }

    [Required, StringLength(50)]
    public string NomeAtributo { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string ValorAtributo { get; set; } = string.Empty;

    public ProdutoVariacao Variacao { get; set; } = null!;
}
