using System.ComponentModel.DataAnnotations;
using System.Data;
using GVC.Web.Data;
using GVC.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Estoque;

public class AjusteManualModel(ErpDbContext db) : BasePageModel
{
    [BindProperty, Required(ErrorMessage = "Selecione o produto ou SKU.")]
    public string Alvo { get; set; } = string.Empty;

    [BindProperty, Range(0, int.MaxValue, ErrorMessage = "O estoque não pode ser negativo.")]
    public int NovoEstoque { get; set; }

    [BindProperty, StringLength(255)]
    public string? Observacao { get; set; }

    public IReadOnlyList<SelectListItem> Opcoes { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        await CarregarOpcoesAsync(cancellationToken);

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        bool variacaoSelecionada = Alvo.StartsWith('V');
        int id = 0;
        if (Alvo.Length < 2 || !int.TryParse(Alvo[1..], out id) || id <= 0 ||
            (Alvo[0] != 'P' && Alvo[0] != 'V'))
            ModelState.AddModelError(nameof(Alvo), "Selecione um produto ou SKU válido.");

        if (!ModelState.IsValid)
        {
            await CarregarOpcoesAsync(cancellationToken);
            return Page();
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        Produto produto;
        ProdutoVariacao? variacao = null;
        int anterior;

        if (variacaoSelecionada)
        {
            variacao = await db.ProdutosVariacoes
                .Include(x => x.Produto)
                .SingleOrDefaultAsync(x => x.VariacaoId == id &&
                    x.Produto.EmpresaId == EmpresaId && x.Produto.TemVariacao && x.Status == "Ativo", cancellationToken);
            if (variacao is null) return NotFound();
            produto = variacao.Produto;
            anterior = variacao.Estoque;
            variacao.Estoque = NovoEstoque;
        }
        else
        {
            produto = await db.Produtos.SingleOrDefaultAsync(
                x => x.ProdutoId == id && x.EmpresaId == EmpresaId, cancellationToken) ?? null!;
            if (produto is null) return NotFound();
            if (produto.TemVariacao)
            {
                ModelState.AddModelError(nameof(Alvo), "Este produto controla estoque por SKU. Selecione uma variação.");
                await CarregarOpcoesAsync(cancellationToken);
                return Page();
            }
            anterior = produto.Estoque;
            produto.Estoque = NovoEstoque;
        }

        db.MovimentacoesEstoque.Add(new MovimentacaoEstoque
        {
            EmpresaId = EmpresaId,
            ProdutoId = produto.ProdutoId,
            VariacaoID = variacao?.VariacaoId,
            TipoMovimentacao = "AJUSTE",
            Quantidade = Math.Abs(NovoEstoque - anterior),
            EstoqueAnterior = anterior,
            EstoqueAtual = NovoEstoque,
            Origem = "AJUSTE_MANUAL",
            Observacao = Observacao?.Trim(),
            Usuario = UsuarioId.ToString(),
            DataMovimentacao = DateTime.Now
        });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        TempData["Success"] = "Estoque ajustado com sucesso.";
        return RedirectToPage("Extrato");
    }

    private async Task CarregarOpcoesAsync(CancellationToken cancellationToken)
    {
        var produtos = await db.Produtos.AsNoTracking()
            .Where(x => x.EmpresaId == EmpresaId && !x.TemVariacao)
            .OrderBy(x => x.NomeProduto)
            .Select(x => new SelectListItem(x.NomeProduto + " — estoque: " + x.Estoque, "P" + x.ProdutoId))
            .ToListAsync(cancellationToken);

        var variacoesBanco = await db.ProdutosVariacoes.AsNoTracking()
            .Where(x => x.Produto.EmpresaId == EmpresaId && x.Produto.TemVariacao && x.Status == "Ativo")
            .OrderBy(x => x.Produto.NomeProduto).ThenBy(x => x.Sku)
            .Select(x => new { x.VariacaoId, x.Produto.NomeProduto, x.Sku, x.Estoque,
                Atributos = x.Atributos.OrderBy(a => a.AtributoId).Select(a => a.ValorAtributo).ToList() })
            .ToListAsync(cancellationToken);

        Opcoes = produtos.Concat(variacoesBanco.Select(x => new SelectListItem(
                x.NomeProduto + " — " + (x.Atributos.Count > 0 ? string.Join(" / ", x.Atributos) : x.Sku) +
                " — estoque: " + x.Estoque,
                "V" + x.VariacaoId)))
            .ToList();
    }
}
