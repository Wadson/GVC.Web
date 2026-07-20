using GVC.Web.Data;
using GVC.Web.Models;
using GVC.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Produtos;

public class EditModel(ErpDbContext db, IWebHostEnvironment environment) : BasePageModel
{
    [BindProperty]
    public Produto Produto { get; set; } = null!;

    [BindProperty]
    public IFormFile? FotoUpload
    {
        get; set;
    }

    public SelectList Fornecedores { get; private set; } = null!;

    public SelectList Marcas { get; private set; } = null!;

    public SelectList Categorias { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Produto = await db.Produtos.AsNoTracking().SingleOrDefaultAsync(x => x.ProdutoId == id && x.EmpresaId == EmpresaId) ?? null!;

        if (Produto is null)
            return NotFound();

        await LoadAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var entity = await db.Produtos.SingleOrDefaultAsync(x => x.ProdutoId == Produto.ProdutoId && x.EmpresaId == EmpresaId, cancellationToken);

        if (entity is null)
            return NotFound();

        await ValidateAsync();

        if (!ModelState.IsValid)
        {
            Produto.Imagem = entity.Imagem;

            await LoadAsync();

            return Page();
        }

        var id = entity.ProdutoId;

        var empresa = entity.EmpresaId;

        var entrada = entity.DataDeEntrada;

        var currentImage = entity.Imagem;

        db.Entry(entity).CurrentValues.SetValues(Produto);

        entity.ProdutoId = id;

        entity.EmpresaId = empresa;

        entity.DataDeEntrada = entrada;

        entity.Imagem = currentImage;

        string? novaImagem = null;
        try
        {
            if (FotoUpload is not null && FotoUpload.Length > 0)
                novaImagem = entity.Imagem = await ProductImageStorage.SaveAsync(FotoUpload, environment, cancellationToken);

            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            ProductImageStorage.Delete(novaImagem, environment);
            throw;
        }

        if (!string.Equals(currentImage, entity.Imagem, StringComparison.OrdinalIgnoreCase))
            ProductImageStorage.Delete(currentImage, environment);

        TempData["Success"] = "Produto alterado.";

        return RedirectToPage("Index");
    }

    private async Task ValidateAsync()
    {
        if (Produto.PrecoCusto < 0 || Produto.PrecoDeVenda < 0 || Produto.PrecoCompra < 0)
            ModelState.AddModelError(string.Empty, "Preços não podem ser negativos.");

        if (Produto.Estoque < 0)
            ModelState.AddModelError("Produto.Estoque", "Estoque não pode ser negativo.");

        var imageError = ProductImageStorage.Validate(FotoUpload);

        if (imageError is not null)
            ModelState.AddModelError("FotoUpload", imageError);

        if (Produto.CategoriaId.HasValue && !await db.Categorias.AnyAsync(x => x.CategoriaId == Produto.CategoriaId && x.EmpresaId == EmpresaId))
            ModelState.AddModelError("Produto.CategoriaId", "Selecione uma categoria válida.");

        if (Produto.MarcaId.HasValue && !await db.Marcas.AnyAsync(x => x.MarcaId == Produto.MarcaId && x.EmpresaId == EmpresaId))
            ModelState.AddModelError("Produto.MarcaId", "Selecione uma marca válida.");

        if (Produto.FornecedorId.HasValue && !await db.Fornecedores.AnyAsync(x => x.FornecedorId == Produto.FornecedorId && x.EmpresaId == EmpresaId))
            ModelState.AddModelError("Produto.FornecedorId", "Selecione um fornecedor válido.");
    }

    private async Task LoadAsync()
    {
        Fornecedores = new SelectList(await db.Fornecedores.AsNoTracking().Where(x => x.EmpresaId == EmpresaId).OrderBy(x => x.Nome).ToListAsync(), "FornecedorId", "Nome");

        Marcas = new SelectList(await db.Marcas.AsNoTracking().Where(x => x.EmpresaId == EmpresaId).OrderBy(x => x.NomeMarca).ToListAsync(), "MarcaId", "NomeMarca");

        Categorias = new SelectList(await db.Categorias.AsNoTracking().Where(x => x.EmpresaId == EmpresaId).OrderBy(x => x.NomeCategoria).ToListAsync(), "CategoriaId", "NomeCategoria");
    }
}
