namespace GVC.Web.ViewModels;

public sealed class CatalogoVirtualViewModel
{
    public int EmpresaId { get; set; }
    public string EmpresaNome { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public string? LogoDataUri { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string Endereco { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
    public int? CategoriaId { get; set; }
    public int? MarcaId { get; set; }
    public string? Busca { get; set; }
    public List<CatalogoFiltroViewModel> Categorias { get; set; } = [];
    public List<CatalogoFiltroViewModel> Marcas { get; set; } = [];
    public List<CatalogoProdutoViewModel> Produtos { get; set; } = [];
}

public sealed record CatalogoFiltroViewModel(int Id, string Nome, int Quantidade);

public sealed class CatalogoProdutoViewModel
{
    public int ProdutoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Imagem { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public decimal PrecoOriginal { get; set; }
    public decimal Preco { get; set; }
    public bool EmPromocao { get; set; }
}

public sealed class CatalogoGerenciarViewModel
{
    public int EmpresaId { get; set; }
    public string LinkPublico { get; set; } = string.Empty;
    public List<CatalogoFiltroViewModel> Categorias { get; set; } = [];
    public List<CatalogoGerenciarProdutoViewModel> Produtos { get; set; } = [];
}

public sealed record CatalogoGerenciarProdutoViewModel(
    int ProdutoId, string Nome, string? Imagem, string Categoria, string Marca,
    decimal Preco, string Status, bool Visivel);
