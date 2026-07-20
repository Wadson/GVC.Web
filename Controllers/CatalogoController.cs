using System.Security.Claims;
using GVC.Web.Data;
using GVC.Web.Services;
using GVC.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace GVC.Web.Controllers;

[Route("Catalogo")]
public sealed class CatalogoController(ErpDbContext db, IWebHostEnvironment environment) : Controller
{
    private static readonly string[] StatusVisiveis = ["Disponível", "Ativo"];

    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [HttpGet("{empresaId:int}")]
    public async Task<IActionResult> Index(
        int empresaId, int? categoriaId, int? marcaId, string? busca,
        CancellationToken cancellationToken)
    {
        CatalogoVirtualViewModel? model = await CriarCatalogoAsync(
            empresaId, categoriaId, marcaId, busca, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [AllowAnonymous]
    [HttpGet("ExportarPdf/{empresaId:int}")]
    public async Task<IActionResult> ExportarPdf(
        int empresaId, int? categoriaId, CancellationToken cancellationToken)
    {
        CatalogoVirtualViewModel? catalogo = await CriarCatalogoAsync(
            empresaId, categoriaId, null, null, cancellationToken);
        if (catalogo is null) return NotFound();

        byte[]? logo = await db.Empresas.AsNoTracking().Where(x => x.EmpresaId == empresaId)
            .Select(x => x.Logo).SingleAsync(cancellationToken);
        var model = new CatalogoPdfViewModel
        {
            EmpresaNome = catalogo.EmpresaNome,
            RazaoSocial = catalogo.RazaoSocial,
            Logo = logo,
            Contato = string.Join(" • ", new[] { catalogo.Telefone, catalogo.Email }.Where(x => !string.IsNullOrWhiteSpace(x))),
            Endereco = catalogo.Endereco,
            Produtos = catalogo.Produtos.Select(x => new ItemCatalogoPdfViewModel
            {
                Nome = x.Nome,
                Marca = x.Marca,
                PrecoOriginal = x.PrecoOriginal,
                Preco = x.Preco,
                EmPromocao = x.EmPromocao,
                Imagem = LerImagemLocal(x.Imagem)
            }).ToList()
        };

        byte[] pdf = new CatalogoPdfDocument(model).GeneratePdf();
        return File(pdf, "application/pdf", $"catalogo-{empresaId}-{DateTime.Today:yyyyMMdd}.pdf");
    }

    [Authorize]
    [HttpGet("Gerenciar")]
    public async Task<IActionResult> Gerenciar(CancellationToken cancellationToken)
    {
        if (!TryGetEmpresaId(out int empresaId)) return Forbid();
        var model = new CatalogoGerenciarViewModel
        {
            EmpresaId = empresaId,
            LinkPublico = $"{Request.Scheme}://{Request.Host}/Catalogo/{empresaId}",
            Categorias = await db.Categorias.AsNoTracking().Where(x => x.EmpresaId == empresaId)
                .OrderBy(x => x.NomeCategoria)
                .Select(x => new CatalogoFiltroViewModel(x.CategoriaId, x.NomeCategoria,
                    db.Produtos.Count(p => p.EmpresaId == empresaId && p.CategoriaId == x.CategoriaId &&
                        p.Estoque >= 1 && p.Imagem != null && p.Imagem.Trim() != "" &&
                        StatusVisiveis.Contains(p.Status))))
                .ToListAsync(cancellationToken),
            Produtos = await db.Produtos.AsNoTracking().Where(x => x.EmpresaId == empresaId)
                .OrderBy(x => x.NomeProduto)
                .Select(x => new CatalogoGerenciarProdutoViewModel(
                    x.ProdutoId, x.NomeProduto, x.Imagem,
                    x.Categoria == null ? "Sem categoria" : x.Categoria.NomeCategoria,
                    x.Marca == null ? "Sem marca" : x.Marca.NomeMarca,
                    x.PrecoDeVenda, x.Status, StatusVisiveis.Contains(x.Status)))
                .ToListAsync(cancellationToken)
        };
        return View(model);
    }

    [Authorize]
    [ValidateAntiForgeryToken]
    [HttpPost("AlternarProduto/{produtoId:int}")]
    public async Task<IActionResult> AlternarProduto(int produtoId, CancellationToken cancellationToken)
    {
        if (!TryGetEmpresaId(out int empresaId)) return Forbid();
        var produto = await db.Produtos.SingleOrDefaultAsync(
            x => x.ProdutoId == produtoId && x.EmpresaId == empresaId, cancellationToken);
        if (produto is null) return NotFound();

        produto.Status = StatusVisiveis.Contains(produto.Status) ? "Indisponível" : "Disponível";
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = produto.Status == "Disponível"
            ? "Produto ativado no catálogo." : "Produto removido do catálogo.";
        return RedirectToAction(nameof(Gerenciar));
    }

    private async Task<CatalogoVirtualViewModel?> CriarCatalogoAsync(
        int empresaId, int? categoriaId, int? marcaId, string? busca,
        CancellationToken cancellationToken)
    {
        var empresa = await db.Empresas.AsNoTracking().Where(x => x.EmpresaId == empresaId)
            .Select(x => new
            {
                x.RazaoSocial, x.NomeFantasia, x.Logo, x.Telefone, x.Email,
                x.Logradouro, x.Numero, x.Bairro, Cidade = x.Cidade.Nome, Uf = x.Cidade.Estado.Uf
            }).SingleOrDefaultAsync(cancellationToken);
        if (empresa is null) return null;

        var baseQuery = db.Produtos.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId &&
                        x.Estoque >= 1 &&
                        x.Imagem != null && x.Imagem.Trim() != "" &&
                        StatusVisiveis.Contains(x.Status));

        var categoriasAgrupadas = await baseQuery.Where(x => x.CategoriaId.HasValue)
            .GroupBy(x => new { Id = x.CategoriaId!.Value, Nome = x.Categoria!.NomeCategoria })
            .Select(g => new { g.Key.Id, g.Key.Nome, Quantidade = g.Count() })
            .OrderBy(x => x.Nome)
            .ToListAsync(cancellationToken);
        var categorias = categoriasAgrupadas
            .Select(x => new CatalogoFiltroViewModel(x.Id, x.Nome, x.Quantidade))
            .ToList();

        var marcasAgrupadas = await baseQuery.Where(x => x.MarcaId.HasValue)
            .GroupBy(x => new { Id = x.MarcaId!.Value, Nome = x.Marca!.NomeMarca })
            .Select(g => new { g.Key.Id, g.Key.Nome, Quantidade = g.Count() })
            .OrderBy(x => x.Nome)
            .ToListAsync(cancellationToken);
        var marcas = marcasAgrupadas
            .Select(x => new CatalogoFiltroViewModel(x.Id, x.Nome, x.Quantidade))
            .ToList();

        if (categoriaId.HasValue) baseQuery = baseQuery.Where(x => x.CategoriaId == categoriaId.Value);
        if (marcaId.HasValue) baseQuery = baseQuery.Where(x => x.MarcaId == marcaId.Value);
        busca = busca?.Trim();
        if (!string.IsNullOrWhiteSpace(busca))
            baseQuery = baseQuery.Where(x => x.NomeProduto.Contains(busca) ||
                (x.Referencia != null && x.Referencia.Contains(busca)));

        DateTime agora = DateTime.Now;
        var produtos = await baseQuery.OrderBy(x => x.NomeProduto).Select(x => new
        {
            x.ProdutoId, Nome = x.NomeProduto, x.Imagem, x.PrecoDeVenda, x.Estoque,
            Categoria = x.Categoria == null ? "Sem categoria" : x.Categoria.NomeCategoria,
            Marca = x.Marca == null ? "Sem marca" : x.Marca.NomeMarca,
            Promocao = db.Promocoes.Where(p => p.ProdutoId == x.ProdutoId && p.Ativo && p.DataInicio <= agora && p.DataFim >= agora)
                .OrderByDescending(p => p.DataInicio)
                .Select(p => new { p.PrecoOriginal, p.PrecoPromocional }).FirstOrDefault()
        }).ToListAsync(cancellationToken);

        return new CatalogoVirtualViewModel
        {
            EmpresaId = empresaId,
            EmpresaNome = empresa.NomeFantasia ?? empresa.RazaoSocial,
            RazaoSocial = empresa.RazaoSocial,
            LogoDataUri = empresa.Logo is null ? null : $"data:image/png;base64,{Convert.ToBase64String(empresa.Logo)}",
            Telefone = empresa.Telefone,
            Email = empresa.Email,
            Endereco = $"{empresa.Logradouro}, {empresa.Numero} - {empresa.Bairro}, {empresa.Cidade}/{empresa.Uf}",
            WhatsApp = new string((empresa.Telefone ?? string.Empty).Where(char.IsDigit).ToArray()),
            CategoriaId = categoriaId,
            MarcaId = marcaId,
            Busca = busca,
            Categorias = categorias,
            Marcas = marcas,
            Produtos = produtos.Where(x => x.Estoque >= 1).Select(x => new CatalogoProdutoViewModel
            {
                ProdutoId = x.ProdutoId, Nome = x.Nome, Imagem = x.Imagem,
                Categoria = x.Categoria, Marca = x.Marca,
                PrecoOriginal = x.Promocao?.PrecoOriginal ?? x.PrecoDeVenda,
                Preco = x.Promocao?.PrecoPromocional ?? x.PrecoDeVenda,
                EmPromocao = x.Promocao is not null
            }).ToList()
        };
    }

    private byte[]? LerImagemLocal(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return null;
        string root = Path.GetFullPath(Path.Combine(environment.WebRootPath, "uploads", "produtos"));
        string file = Path.GetFullPath(Path.Combine(environment.WebRootPath,
            relativePath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar)));
        return file.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(file)
            ? System.IO.File.ReadAllBytes(file) : null;
    }

    private bool TryGetEmpresaId(out int empresaId) =>
        int.TryParse(User.FindFirstValue("EmpresaID"), out empresaId) && empresaId > 0;
}
