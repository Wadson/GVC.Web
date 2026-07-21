using System.ComponentModel.DataAnnotations;

namespace GVC.Web.ViewModels;

public sealed class ProdutoVariacaoInputModel
{
    public int VariacaoId { get; set; }

    [Required, StringLength(50)]
    public string Sku { get; set; } = string.Empty;

    [StringLength(20)]
    public string? GtinEan { get; set; }

    public decimal? PrecoCusto { get; set; }

    public decimal? PrecoDeVenda { get; set; }

    public string? ImagemAtual { get; set; }

    public IFormFile? ArquivoImagem { get; set; }

    public bool RemoverImagem { get; set; }

    [Range(0, int.MaxValue)]
    public int Estoque { get; set; }

    [Required, StringLength(20)]
    public string Status { get; set; } = "Ativo";

    public List<ProdutoVariacaoAtributoInputModel> Atributos { get; set; } = [];
}

public sealed class ProdutoVariacaoAtributoInputModel
{
    [Required, StringLength(50)]
    public string NomeAtributo { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string ValorAtributo { get; set; } = string.Empty;
}
