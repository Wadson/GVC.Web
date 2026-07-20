using Microsoft.AspNetCore.Http;

namespace GVC.Web.ViewModels;

public sealed class ProdutoViewModel
{
    public IFormFile? FotoUpload { get; set; }

    public string? Imagem { get; set; }
}
