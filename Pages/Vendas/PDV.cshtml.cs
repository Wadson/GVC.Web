using GVC.Web.Data;
using GVC.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Vendas;

public class PDVModel(ErpDbContext db, IVendaService vendaService) : BasePageModel
{
    public IReadOnlyList<FormaPagamentoView> FormasPagamento { get; private set; } = [];

    public async Task OnGetAsync() => FormasPagamento = await db.FormasPagamento.AsNoTracking().Where(x => x.Ativo)
        .OrderBy(x => x.NomeFormaPagamento).Select(x => new FormaPagamentoView(x.FormaPgtoId, x.NomeFormaPagamento)).ToListAsync();

    public async Task<IActionResult> OnGetBuscarProdutosAsync(string termo)
    {
        termo = (termo ?? string.Empty).Trim();

        if (termo.Length < 1)
            return new JsonResult(Array.Empty<object>());

        var codigoValido = int.TryParse(termo, out var codigo);

        var produtosPai = await db.Produtos.AsNoTracking().Where(x => x.EmpresaId == EmpresaId && !x.TemVariacao && (x.Status == "Ativo" || x.Status == "Disponível") && x.Estoque > 0 &&
                (x.GtinEan == termo || x.NomeProduto.Contains(termo) || (x.Referencia != null && x.Referencia.Contains(termo)) || (codigoValido && x.ProdutoId == codigo)))
            .Select(x => new ProdutoPdvView(x.ProdutoId, null, x.NomeProduto, x.Referencia, x.GtinEan, x.PrecoDeVenda, x.Estoque, x.Imagem, x.GtinEan == termo))
            .ToListAsync();

        var variacoesBanco = await db.ProdutosVariacoes.AsNoTracking()
            .Where(x => x.Produto.EmpresaId == EmpresaId && x.Produto.TemVariacao && x.Status == "Ativo" && x.Estoque > 0 &&
                        (x.GtinEan == termo || (x.Sku != null && x.Sku.Contains(termo)) || x.Produto.NomeProduto.Contains(termo) ||
                         (x.Produto.Referencia != null && x.Produto.Referencia.Contains(termo)) || (codigoValido && x.ProdutoId == codigo)))
            .Select(x => new
            {
                x.ProdutoId, VariacaoId = (int?)x.VariacaoId, Nome = x.Produto.NomeProduto,
                Referencia = x.Sku ?? x.Produto.Referencia, x.GtinEan,
                PrecoVenda = x.PrecoDeVenda ?? x.Produto.PrecoDeVenda, EstoqueAtual = x.Estoque,
                Imagem = x.Imagem ?? x.Produto.Imagem, Exato = x.GtinEan == termo,
                Atributos = x.Atributos.OrderBy(a => a.AtributoId).Select(a => a.NomeAtributo + ": " + a.ValorAtributo).ToList()
            }).ToListAsync();

        var variacoes = variacoesBanco.Select(x => new ProdutoPdvView(
            x.ProdutoId, x.VariacaoId,
            x.Atributos.Count == 0 ? x.Nome : x.Nome + " — " + string.Join(" / ", x.Atributos),
            x.Referencia, x.GtinEan, x.PrecoVenda, x.EstoqueAtual, x.Imagem, x.Exato));

        var produtos = produtosPai.Concat(variacoes)
            .OrderByDescending(x => x.Exato)
            .ThenBy(x => x.Nome)
            .Take(15)
            .ToList();

        return new JsonResult(produtos);
    }

    public async Task<IActionResult> OnGetBuscarClientesAsync(string termo)
    {
        termo = (termo ?? string.Empty).Trim();

        if (termo.Length < 1)
            return new JsonResult(Array.Empty<object>());

        var codigoValido = int.TryParse(termo, out var codigo);

        var itens = await db.Clientes.AsNoTracking().Where(x => x.EmpresaId == EmpresaId && x.Status == 1 && (x.Nome.Contains(termo) || (codigoValido && x.ClienteId == codigo)))
            .OrderBy(x => x.Nome).Take(15).Select(x => new { id = x.ClienteId, nome = x.Nome, documento = x.Cpf ?? x.Cnpj }).ToListAsync();

        return new JsonResult(itens);
    }

    public async Task<IActionResult> OnGetBuscarVendedoresAsync(string termo)
    {
        termo = (termo ?? string.Empty).Trim();

        if (termo.Length < 1)
            return new JsonResult(Array.Empty<object>());

        var codigoValido = int.TryParse(termo, out var codigo);

        var itens = await db.Vendedores.AsNoTracking().Where(x => x.EmpresaId == EmpresaId && x.Status == 1 && (x.Nome.Contains(termo) || (codigoValido && x.VendedorId == codigo)))
            .OrderBy(x => x.Nome).Take(15).Select(x => new { id = x.VendedorId, nome = x.Nome, documento = x.Cpf }).ToListAsync();

        return new JsonResult(itens);
    }

    public async Task<IActionResult> OnPostFinalizarAsync([FromBody]
FinalizarVendaInput input, CancellationToken cancellationToken)
    {
        try
        {
            return new JsonResult(new
            {
                vendaId = await vendaService.FinalizarAsync(EmpresaId, UsuarioId, input, cancellationToken)
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    public sealed record FormaPagamentoView(int FormaPagamentoId, string Descricao);

    private sealed record ProdutoPdvView(
        int ProdutoId, int? VariacaoId, string Nome, string? Referencia, string? GtinEan,
        decimal PrecoVenda, int EstoqueAtual, string? Imagem, bool Exato);
}
