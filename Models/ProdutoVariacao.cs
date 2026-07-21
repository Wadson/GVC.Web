using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("ProdutoVariacao")]
public class ProdutoVariacao
{
    [Key, Column("VariacaoID")]
    public int VariacaoId { get; set; }

    [Column("ProdutoID")]
    public int ProdutoId { get; set; }

    [StringLength(50)]
    public string? Sku { get; set; }

    [StringLength(20)]
    public string? GtinEan { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PrecoCusto { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PrecoDeVenda { get; set; }

    public int Estoque { get; set; }

    [StringLength(255)]
    public string? Imagem { get; set; }

    [Required, StringLength(20)]
    public string Status { get; set; } = "Ativo";

    public DateTime DataCriacao { get; set; } = DateTime.Now;

    public Produto Produto { get; set; } = null!;

    public ICollection<ProdutoVariacaoAtributo> Atributos { get; set; } = new List<ProdutoVariacaoAtributo>();
}
