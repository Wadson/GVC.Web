using GVC.Web.Data;
using GVC.Web.Models;
using GVC.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GVC.Web.ViewModels;

namespace GVC.Web.Pages.Produtos;

public class CreateModel(ErpDbContext db, IWebHostEnvironment environment) : BasePageModel
{
    [BindProperty]
    public Produto Produto { get; set; } = new() { Status = "Disponível", Unidade = "UN" };

    [BindProperty]
    public IFormFile? FotoUpload
    {
        get; set;
    }

    [BindProperty]
    public List<ProdutoVariacaoInputModel> Variacoes { get; set; } = [];

    public SelectList Fornecedores { get; private set; } = null!;

    public SelectList Marcas { get; private set; } = null!;

    public IReadOnlyList<SelectListItem> Categorias { get; private set; } = [];

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Produto.EmpresaId = EmpresaId;

        Produto.DataDeEntrada = DateTime.Now;

        await ValidateAsync();

        if (!ModelState.IsValid)
        {
            await LoadAsync();

            return Page();
        }

        var novasImagens = new List<string>();
        try
        {
            if (FotoUpload is not null && FotoUpload.Length > 0)
            {
                Produto.Imagem = await ProductImageStorage.SaveAsync(FotoUpload, environment, cancellationToken);
                novasImagens.Add(Produto.Imagem);
            }

            if (Produto.TemVariacao)
                foreach (var input in Variacoes)
                    Produto.Variacoes.Add(await CriarVariacaoAsync(input, novasImagens, cancellationToken));

            db.Produtos.Add(Produto);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            foreach (var imagem in novasImagens) ProductImageStorage.Delete(imagem, environment);
            throw;
        }

        TempData["Success"] = "Produto cadastrado.";

        return RedirectToPage("Index");
    }

    private async Task ValidateAsync()
    {
        NormalizarVariacoes();
        if (Produto.TemVariacao)
        {
            Produto.Estoque = 0;
            Produto.GtinEan = null;
            if (Variacoes.Count == 0)
                ModelState.AddModelError("Variacoes", "Gere ao menos uma variação.");
            await ValidarVariacoesAsync(0);
            if (Variacoes.Any(x => x.VariacaoId != 0))
                ModelState.AddModelError("Variacoes", "A grade contém identificadores inválidos para um novo produto.");
        }
        if (Produto.PrecoDeVenda < 0 || Produto.PrecoCusto < 0 || Produto.PrecoCompra < 0)
            ModelState.AddModelError(string.Empty, "Preços não podem ser negativos.");

        if (Produto.Estoque < 0)
            ModelState.AddModelError("Produto.Estoque", "Estoque não pode ser negativo.");

        var imageError = ProductImageStorage.Validate(FotoUpload);

        if (imageError is not null)
            ModelState.AddModelError("FotoUpload", imageError);

        for (var index = 0; index < Variacoes.Count; index++)
        {
            var variationImageError = ProductImageStorage.Validate(Variacoes[index].ArquivoImagem);
            if (variationImageError is not null)
                ModelState.AddModelError($"Variacoes[{index}].ArquivoImagem", variationImageError);
        }

        if (Produto.CategoriaId.HasValue && !await db.Categorias.AnyAsync(x => x.CategoriaId == Produto.CategoriaId && x.EmpresaId == EmpresaId))
            ModelState.AddModelError("Produto.CategoriaId", "Selecione uma categoria válida.");

        if (Produto.MarcaId.HasValue && !await db.Marcas.AnyAsync(x => x.MarcaId == Produto.MarcaId && x.EmpresaId == EmpresaId))
            ModelState.AddModelError("Produto.MarcaId", "Selecione uma marca válida.");

        if (Produto.FornecedorId.HasValue && !await db.Fornecedores.AnyAsync(x => x.FornecedorId == Produto.FornecedorId && x.EmpresaId == EmpresaId))
            ModelState.AddModelError("Produto.FornecedorId", "Selecione um fornecedor válido.");
    }

    private void NormalizarVariacoes()
    {
        foreach (var variacao in Variacoes)
        {
            variacao.Sku = variacao.Sku?.Trim() ?? string.Empty;
            variacao.GtinEan = string.IsNullOrWhiteSpace(variacao.GtinEan) ? null : variacao.GtinEan.Trim();
            variacao.Status = string.IsNullOrWhiteSpace(variacao.Status) ? "Ativo" : variacao.Status.Trim();
            variacao.Atributos = variacao.Atributos.Where(x => !string.IsNullOrWhiteSpace(x.NomeAtributo) && !string.IsNullOrWhiteSpace(x.ValorAtributo)).ToList();
            foreach (var atributo in variacao.Atributos) { atributo.NomeAtributo = atributo.NomeAtributo.Trim(); atributo.ValorAtributo = atributo.ValorAtributo.Trim(); }
            if (variacao.Estoque < 0 || variacao.PrecoCusto < 0 || variacao.PrecoDeVenda < 0) ModelState.AddModelError("Variacoes", "Estoque e preços das variações não podem ser negativos.");
        }
        if (Variacoes.GroupBy(x => x.Sku, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1)) ModelState.AddModelError("Variacoes", "Existem SKUs repetidos na grade.");
        if (Variacoes.Where(x => x.GtinEan is not null).GroupBy(x => x.GtinEan, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1)) ModelState.AddModelError("Variacoes", "Existem códigos EAN repetidos na grade.");
    }

    private async Task ValidarVariacoesAsync(int produtoId)
    {
        string[] skus = Variacoes.Select(x => x.Sku).Where(x => x.Length > 0).ToArray();
        string[] eans = Variacoes.Select(x => x.GtinEan).Where(x => x is not null).Cast<string>().ToArray();
        if (Variacoes.Any(x => string.IsNullOrWhiteSpace(x.Sku) || x.Atributos.Count == 0)) ModelState.AddModelError("Variacoes", "Cada variação deve possuir SKU e atributos.");
        if (await db.ProdutosVariacoes.AsNoTracking().AnyAsync(x => x.Produto.EmpresaId == EmpresaId && x.ProdutoId != produtoId && ((x.Sku != null && skus.Contains(x.Sku)) || (x.GtinEan != null && eans.Contains(x.GtinEan)))))
            ModelState.AddModelError("Variacoes", "Já existe uma variação com um dos SKUs ou códigos EAN informados.");
    }

    private async Task<ProdutoVariacao> CriarVariacaoAsync(
        ProdutoVariacaoInputModel input,
        ICollection<string> novasImagens,
        CancellationToken cancellationToken)
    {
        string? imagem = null;
        if (input.ArquivoImagem is { Length: > 0 })
        {
            imagem = await ProductImageStorage.SaveVariationAsync(input.ArquivoImagem, environment, cancellationToken);
            novasImagens.Add(imagem);
        }

        return new ProdutoVariacao
        {
            Sku = input.Sku, GtinEan = input.GtinEan, PrecoCusto = input.PrecoCusto, PrecoDeVenda = input.PrecoDeVenda,
            Estoque = input.Estoque, Imagem = imagem, Status = input.Status, DataCriacao = DateTime.Now,
            Atributos = input.Atributos.Select(x => new ProdutoVariacaoAtributo { NomeAtributo = x.NomeAtributo, ValorAtributo = x.ValorAtributo }).ToList()
        };
    }

    private async Task LoadAsync()
    {
        Fornecedores = new SelectList(await db.Fornecedores.AsNoTracking().Where(x => x.EmpresaId == EmpresaId).OrderBy(x => x.Nome).ToListAsync(), "FornecedorId", "Nome");

        Marcas = new SelectList(await db.Marcas.AsNoTracking().Where(x => x.EmpresaId == EmpresaId).OrderBy(x => x.NomeMarca).ToListAsync(), "MarcaId", "NomeMarca");

        Categorias = await db.Categorias.AsNoTracking()
            .Where(x => x.EmpresaId == EmpresaId)
            .OrderBy(x => x.NomeCategoria)
            .Select(x => new SelectListItem(x.NomeCategoria, x.CategoriaId.ToString()))
            .ToListAsync();
    }
}
