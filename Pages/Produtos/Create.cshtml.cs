using GVC.Web.Data;
using GVC.Web.Models;
using GVC.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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

    public SelectList Fornecedores { get; private set; } = null!;

    public SelectList Marcas { get; private set; } = null!;

    public SelectList Categorias { get; private set; } = null!;

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

        string? novaImagem = null;
        try
        {
            if (FotoUpload is not null && FotoUpload.Length > 0)
                novaImagem = Produto.Imagem = await ProductImageStorage.SaveAsync(FotoUpload, environment, cancellationToken);

            db.Produtos.Add(Produto);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            ProductImageStorage.Delete(novaImagem, environment);
            throw;
        }

        TempData["Success"] = "Produto cadastrado.";

        return RedirectToPage("Index");
    }

    private async Task ValidateAsync()
    {
        if (Produto.PrecoDeVenda < 0 || Produto.PrecoCusto < 0 || Produto.PrecoCompra < 0)
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
