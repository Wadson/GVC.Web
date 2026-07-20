namespace GVC.Web.ViewModels;

public sealed class CatalogoPdfViewModel
{
    public string EmpresaNome { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public byte[]? Logo { get; set; }
    public string Contato { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public DateTime AtualizadoEm { get; set; } = DateTime.Now;
    public List<ItemCatalogoPdfViewModel> Produtos { get; set; } = [];
}

public sealed class ItemCatalogoPdfViewModel
{
    public string Nome { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public decimal PrecoOriginal { get; set; }
    public decimal Preco { get; set; }
    public bool EmPromocao { get; set; }
    public byte[]? Imagem { get; set; }
}
