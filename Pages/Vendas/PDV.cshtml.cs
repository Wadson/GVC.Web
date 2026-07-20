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

        var produtos = await db.Produtos.AsNoTracking().Where(x => x.EmpresaId == EmpresaId && (x.Status == "Ativo" || x.Status == "Disponível") && x.Estoque > 0 &&
                (x.GtinEan == termo || x.NomeProduto.Contains(termo) || (x.Referencia != null && x.Referencia.Contains(termo)) || (codigoValido && x.ProdutoId == codigo)))
            .OrderByDescending(x => x.GtinEan == termo).ThenBy(x => x.NomeProduto).Take(15)
            .Select(x => new { x.ProdutoId, Nome = x.NomeProduto, x.Referencia, x.GtinEan, PrecoVenda = x.PrecoDeVenda, EstoqueAtual = x.Estoque, x.Imagem }).ToListAsync();

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
}