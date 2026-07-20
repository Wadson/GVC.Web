using System.Security.Claims;
using GVC.Web.Data;
using GVC.Web.DTOs;
using GVC.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Controllers;

[Authorize]
[Route("Relatorios")]
public sealed class RelatoriosController(ErpDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryGetEmpresaId(out int empresaId)) return Forbid();
        var model = new CentralRelatoriosViewModel
        {
            Vendas = await db.Vendas.AsNoTracking().Where(x => x.EmpresaId == empresaId)
                .OrderByDescending(x => x.DataVenda).Take(10)
                .Select(x => new DocumentoRecenteViewModel(x.VendaId, $"Venda #{x.VendaId} — {x.Cliente.Nome}", x.DataVenda, x.TotalLiquido))
                .ToListAsync(cancellationToken),
            Recibos = await db.PagamentosParciais.AsNoTracking().Where(x => x.EmpresaId == empresaId)
                .OrderByDescending(x => x.DataPagamento).ThenByDescending(x => x.PagamentoId).Take(10)
                .Select(x => new DocumentoRecenteViewModel(x.PagamentoId, $"Recibo #{x.PagamentoId} — {x.Parcela.Venda.Cliente.Nome}", x.DataPagamento, x.ValorPago))
                .ToListAsync(cancellationToken),
            Caixas = await db.Caixas.AsNoTracking().Where(x => x.EmpresaId == empresaId)
                .OrderByDescending(x => x.DataAbertura).Take(10)
                .Select(x => new DocumentoRecenteViewModel(x.CaixaId, $"Caixa #{x.CaixaId} — {x.Status}", x.DataAbertura, x.SaldoFinalSistema ?? x.SaldoInicial))
                .ToListAsync(cancellationToken),
            Comissoes = await db.PagamentosComissao.AsNoTracking().Where(x => x.EmpresaId == empresaId)
                .OrderByDescending(x => x.DataPagamento).Take(10)
                .Select(x => new DocumentoRecenteViewModel(x.PagamentoComissaoId, $"Comissão — {x.Vendedor.Nome}", x.DataPagamento, x.ValorComissao))
                .ToListAsync(cancellationToken)
        };
        return View(model);
    }

    [HttpGet("Estoque")]
    public async Task<IActionResult> Estoque(CancellationToken cancellationToken)
    {
        if (!TryGetEmpresaId(out int empresaId)) return Forbid();
        var model = new RelatorioEstoqueViewModel
        {
            Empresa = await ObterEmpresaAsync(empresaId, cancellationToken),
            Itens = await db.Produtos.AsNoTracking()
                .Where(x => x.EmpresaId == empresaId && x.Status == "Ativo")
                .OrderBy(x => x.Categoria == null ? "Sem categoria" : x.Categoria.NomeCategoria)
                .ThenBy(x => x.Marca == null ? "Sem marca" : x.Marca.NomeMarca)
                .ThenBy(x => x.NomeProduto)
                .Select(x => new RelatorioEstoqueItemViewModel
                {
                    Categoria = x.Categoria == null ? "Sem categoria" : x.Categoria.NomeCategoria,
                    Marca = x.Marca == null ? "Sem marca" : x.Marca.NomeMarca,
                    Codigo = x.Referencia ?? x.ProdutoId.ToString(),
                    Produto = x.NomeProduto,
                    Estoque = x.Estoque,
                    PrecoCusto = x.PrecoCusto,
                    PrecoVenda = x.PrecoDeVenda
                }).ToListAsync(cancellationToken)
        };
        return View(model);
    }

    [HttpGet("/ContasAReceber/RelatorioInadimplencia")]
    public async Task<IActionResult> Inadimplencia(
        int? clienteId, DateTime? vencimentoInicial, DateTime? vencimentoFinal,
        string? status, CancellationToken cancellationToken)
    {
        if (!TryGetEmpresaId(out int empresaId)) return Forbid();
        DateTime hoje = DateTime.Today;
        var query = db.Parcelas.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.Status != "Cancelado");
        if (clienteId.HasValue) query = query.Where(x => x.Venda.ClienteId == clienteId.Value);
        if (vencimentoInicial.HasValue) query = query.Where(x => x.DataVencimento >= vencimentoInicial.Value.Date);
        if (vencimentoFinal.HasValue) query = query.Where(x => x.DataVencimento <= vencimentoFinal.Value.Date);

        status = status?.Trim();
        if (status == "Pago") query = query.Where(x => x.Status == "Pago");
        else if (status == "Atrasado") query = query.Where(x => x.Status != "Pago" && x.DataVencimento < hoje && x.ValorParcela > (x.ValorRecebido ?? 0));
        else if (status == "Pendente") query = query.Where(x => x.Status != "Pago" && x.DataVencimento >= hoje && x.ValorParcela > (x.ValorRecebido ?? 0));

        var parcelas = await query.OrderBy(x => x.Venda.Cliente.Nome).ThenBy(x => x.DataVencimento)
            .Select(x => new
            {
                x.Venda.ClienteId,
                Cliente = x.Venda.Cliente.Nome,
                Documento = x.Venda.Cliente.Cnpj ?? x.Venda.Cliente.Cpf,
                x.VendaId, x.NumeroParcela, x.DataVencimento, x.ValorParcela,
                Recebido = x.ValorRecebido ?? 0, x.Status
            }).ToListAsync(cancellationToken);

        var model = new RelatorioInadimplenciaViewModel
        {
            Empresa = await ObterEmpresaAsync(empresaId, cancellationToken),
            ClienteId = clienteId,
            VencimentoInicial = vencimentoInicial,
            VencimentoFinal = vencimentoFinal,
            Status = status,
            Clientes = await db.Clientes.AsNoTracking().Where(x => x.EmpresaId == empresaId)
                .OrderBy(x => x.Nome).Select(x => new EntradaEstoqueOptionDTO(x.ClienteId, x.Nome)).ToListAsync(cancellationToken),
            ClientesAgrupados = parcelas.GroupBy(x => new { x.ClienteId, x.Cliente, x.Documento })
                .Select(g => new RelatorioInadimplenciaClienteViewModel
                {
                    Cliente = g.Key.Cliente,
                    Documento = g.Key.Documento,
                    TotalVencido = g.Where(x => x.Status != "Pago" && x.DataVencimento < hoje).Sum(x => x.ValorParcela - x.Recebido),
                    TotalAVencer = g.Where(x => x.Status != "Pago" && x.DataVencimento >= hoje).Sum(x => x.ValorParcela - x.Recebido),
                    SaldoDevedor = g.Where(x => x.Status != "Pago").Sum(x => x.ValorParcela - x.Recebido),
                    Parcelas = g.Select(x => new RelatorioInadimplenciaParcelaViewModel(
                        x.VendaId, x.NumeroParcela, x.DataVencimento, x.ValorParcela,
                        x.Recebido, x.ValorParcela - x.Recebido,
                        x.Status == "Pago" ? "Pago" : x.DataVencimento < hoje ? "Atrasado" : "Pendente")).ToList()
                }).OrderBy(x => x.Cliente).ToList()
        };
        return View("~/Views/ContasAReceber/Inadimplencia.cshtml", model);
    }

    private async Task<EmpresaImpressaoViewModel> ObterEmpresaAsync(int empresaId, CancellationToken cancellationToken)
    {
        var empresa = await db.Empresas.AsNoTracking().Where(x => x.EmpresaId == empresaId)
            .Select(x => new { x.RazaoSocial, x.NomeFantasia, x.Cnpj, x.Logradouro, x.Numero, x.Bairro, x.Cep, x.Telefone, x.Logo })
            .SingleAsync(cancellationToken);
        return new EmpresaImpressaoViewModel
        {
            RazaoSocial = empresa.RazaoSocial, NomeFantasia = empresa.NomeFantasia, Cnpj = empresa.Cnpj,
            Endereco = $"{empresa.Logradouro}, {empresa.Numero} - {empresa.Bairro} - CEP {empresa.Cep}",
            Telefone = empresa.Telefone,
            LogoDataUri = empresa.Logo is null ? null : $"data:image/png;base64,{Convert.ToBase64String(empresa.Logo)}"
        };
    }

    private bool TryGetEmpresaId(out int empresaId) =>
        int.TryParse(User.FindFirstValue("EmpresaID"), out empresaId) && empresaId > 0;
}
