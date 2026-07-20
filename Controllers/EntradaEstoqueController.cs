using System.Security.Claims;
using System.Xml;
using GVC.Web.Data;
using GVC.Web.DTOs;
using GVC.Web.Services;
using GVC.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Controllers;

[Authorize]
[Route("EntradaEstoque")]
public class EntradaEstoqueController(ErpDbContext db, IEntradaEstoqueService service) : Controller
{
    [HttpGet("Nova")]
    public async Task<IActionResult> Nova(CancellationToken cancellationToken)
    {
        if (!TryGetContexto(out int empresaId, out _))
            return Forbid();

        var viewModel = new EntradaEstoqueViewModel
        {
            DataEmissao = DateTime.Today,
            Parcelas =
            [
                new ParcelaEntradaDTO
                {
                    DataVencimento = DateTime.Today.AddDays(30)
                }
            ]
        };
        await CarregarOpcoesAsync(viewModel, empresaId, cancellationToken);
        return View(viewModel);
    }

    [HttpPost("UploadXml")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadXml(IFormFile? arquivoXml, CancellationToken cancellationToken)
    {
        if (!TryGetContexto(out int empresaId, out _))
            return Forbid();

        if (arquivoXml is null || arquivoXml.Length == 0 ||
            !string.Equals(Path.GetExtension(arquivoXml.FileName), ".xml", StringComparison.OrdinalIgnoreCase))
        {
            var vazio = new EntradaEstoqueViewModel { OrigemXml = true };
            ModelState.AddModelError(string.Empty, "Selecione um arquivo XML válido.");
            await CarregarOpcoesAsync(vazio, empresaId, cancellationToken);
            return View("Nova", vazio);
        }

        try
        {
            await using Stream stream = arquivoXml.OpenReadStream();
            EntradaEstoqueViewModel viewModel = await service.ProcessarXmlAsync(empresaId, stream, cancellationToken);
            await CarregarOpcoesAsync(viewModel, empresaId, cancellationToken);
            return View("Nova", viewModel);
        }
        catch (InvalidOperationException ex)
        {
            var invalido = new EntradaEstoqueViewModel { OrigemXml = true };
            ModelState.AddModelError(string.Empty, ex.Message);
            await CarregarOpcoesAsync(invalido, empresaId, cancellationToken);
            return View("Nova", invalido);
        }
        catch (XmlException)
        {
            var invalido = new EntradaEstoqueViewModel { OrigemXml = true };
            ModelState.AddModelError(string.Empty, "Não foi possível interpretar o arquivo XML.");
            await CarregarOpcoesAsync(invalido, empresaId, cancellationToken);
            return View("Nova", invalido);
        }
    }

    [HttpPost("Salvar")]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(ValueCountLimit = 10000)]
    public async Task<IActionResult> Salvar(EntradaEstoqueViewModel viewModel, CancellationToken cancellationToken)
    {
        if (!TryGetContexto(out int empresaId, out int usuarioId))
            return Forbid();

        if (!ModelState.IsValid)
        {
            await CarregarOpcoesAsync(viewModel, empresaId, cancellationToken);
            return View("Nova", viewModel);
        }

        try
        {
            int documentoId = await service.SalvarAsync(
                empresaId,
                usuarioId,
                User.Identity?.Name ?? usuarioId.ToString(),
                viewModel,
                cancellationToken);

            TempData["Success"] = $"Entrada #{documentoId} processada; estoque e financeiro atualizados.";
            return Redirect("/Estoque/Extrato");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await CarregarOpcoesAsync(viewModel, empresaId, cancellationToken);
            return View("Nova", viewModel);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Não foi possível gravar a entrada. Verifique se o documento ou o código do fornecedor já foi processado.");
            await CarregarOpcoesAsync(viewModel, empresaId, cancellationToken);
            return View("Nova", viewModel);
        }
    }

    [HttpGet("BuscarProdutos")]
    public async Task<IActionResult> BuscarProdutos(string? termo, CancellationToken cancellationToken)
    {
        if (!TryGetContexto(out int empresaId, out _))
            return Forbid();

        termo = termo?.Trim() ?? string.Empty;
        var query = db.Produtos.AsNoTracking().Where(x => x.EmpresaId == empresaId);
        if (!string.IsNullOrWhiteSpace(termo))
        {
            bool possuiId = int.TryParse(termo, out int produtoId);
            query = query.Where(x => x.NomeProduto.Contains(termo) ||
                                     (x.Referencia != null && x.Referencia.Contains(termo)) ||
                                     (possuiId && x.ProdutoId == produtoId));
        }

        var produtos = await query.OrderBy(x => x.NomeProduto).Take(30)
            .Select(x => new
            {
                id = x.ProdutoId,
                descricao = x.NomeProduto,
                referencia = x.Referencia,
                precoCompra = x.PrecoCompra ?? 0,
                precoCusto = x.PrecoCusto
            })
            .ToListAsync(cancellationToken);
        return Json(produtos);
    }

    [HttpGet("PedidoOrigem/{pedidoId:int}")]
    public async Task<IActionResult> PedidoOrigem(int pedidoId, CancellationToken cancellationToken)
    {
        if (!TryGetContexto(out int empresaId, out _))
            return Forbid();

        var pedido = await db.Pedidos.AsNoTracking()
            .Where(x => x.PedidoId == pedidoId &&
                        x.EmpresaId == empresaId &&
                        (x.Status == "Pendente" || x.Status == "Aguardando Entrega"))
            .Select(x => new
            {
                pedidoId = x.PedidoId,
                fornecedorId = x.FornecedorId,
                fornecedor = x.Fornecedor.Nome,
                itens = x.Itens
                    .OrderBy(item => item.ItensPedidoId)
                    .Select(item => new
                    {
                        produtoId = item.ProdutoId,
                        codigo = item.Referencia ?? item.Produto.Referencia,
                        descricao = item.Produto.NomeProduto,
                        quantidade = item.Quantidade,
                        valorUnitario = item.PrecoCustoUnitario
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        return pedido is null
            ? NotFound(new { mensagem = "Pedido pendente não encontrado para a empresa ativa." })
            : Json(pedido);
    }

    private async Task CarregarOpcoesAsync(
        EntradaEstoqueViewModel viewModel,
        int empresaId,
        CancellationToken cancellationToken)
    {
        viewModel.Fornecedores = await db.Fornecedores.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId)
            .OrderBy(x => x.Nome)
            .Select(x => new EntradaEstoqueOptionDTO(x.FornecedorId, x.Nome))
            .ToListAsync(cancellationToken);

        var pedidos = await db.Pedidos.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId &&
                        (x.Status == "Pendente" || x.Status == "Aguardando Entrega"))
            .OrderByDescending(x => x.DataPedido)
            .ThenByDescending(x => x.PedidoId)
            .Select(x => new { x.PedidoId, Fornecedor = x.Fornecedor.Nome, x.DataPedido })
            .ToListAsync(cancellationToken);

        viewModel.Pedidos = pedidos
            .Select(x => new EntradaEstoqueOptionDTO(
                x.PedidoId,
                $"#{x.PedidoId} - {x.Fornecedor} ({x.DataPedido:dd/MM/yyyy})"))
            .ToList();

        viewModel.PlanosContas = await db.PlanosContas.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.Tipo == "D")
            .OrderBy(x => x.CodigoClassificacao)
            .Select(x => new EntradaEstoqueOptionDTO(
                x.PlanoContasId,
                x.CodigoClassificacao + " - " + x.Descricao))
            .ToListAsync(cancellationToken);

        if (viewModel.PlanoContasId == 0)
        {
            viewModel.PlanoContasId = await db.PlanosContas.AsNoTracking()
                .Where(x => x.EmpresaId == empresaId && x.Tipo == "D")
                .OrderByDescending(x => x.CodigoClassificacao == "2.01")
                .ThenBy(x => x.CodigoClassificacao)
                .Select(x => x.PlanoContasId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        viewModel.Produtos = await db.Produtos.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId)
            .OrderBy(x => x.NomeProduto)
            .Select(x => new EntradaEstoqueProdutoOptionDTO(
                x.ProdutoId,
                x.NomeProduto + (x.Referencia == null ? "" : " - " + x.Referencia),
                x.PrecoCompra ?? 0,
                x.PrecoCusto))
            .ToListAsync(cancellationToken);
    }

    private bool TryGetContexto(out int empresaId, out int usuarioId)
    {
        bool empresaValida = int.TryParse(User.FindFirstValue("EmpresaID"), out empresaId) && empresaId > 0;
        bool usuarioValido = int.TryParse(User.FindFirstValue("UsuarioID"), out usuarioId) && usuarioId > 0;
        return empresaValida && usuarioValido;
    }
}
