using System.Security.Claims;
using GVC.Web.Data;
using GVC.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Controllers;

[Authorize]
public sealed class ComprovantesController(ErpDbContext db) : Controller
{
    [HttpGet("/ContasAReceber/ImprimirRecibo/{pagamentoId:int}")]
    public async Task<IActionResult> ImprimirRecibo(int pagamentoId, CancellationToken cancellationToken)
    {
        if (!TryGetEmpresaId(out int empresaId)) return Forbid();

        var model = await db.PagamentosParciais.AsNoTracking()
            .Where(x => x.PagamentoId == pagamentoId && x.EmpresaId == empresaId)
            .Select(x => new ReciboPagamentoViewModel
            {
                PagamentoId = x.PagamentoId,
                VendaId = x.Parcela.VendaId,
                NumeroParcela = x.Parcela.NumeroParcela,
                ClienteNome = x.Parcela.Venda.Cliente.Nome,
                ClienteDocumento = x.Parcela.Venda.Cliente.Cnpj ?? x.Parcela.Venda.Cliente.Cpf,
                ValorPago = x.ValorPago,
                Juros = x.Parcela.Juros ?? 0,
                Multa = x.Parcela.Multa ?? 0,
                FormaPagamento = x.FormaPagamento == null ? "Não informada" : x.FormaPagamento.NomeFormaPagamento,
                DataPagamento = x.DataPagamento,
                Observacao = x.Observacao
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (model is null) return NotFound();
        model.Empresa = await ObterEmpresaAsync(empresaId, cancellationToken);
        return View("~/Views/ContasAReceber/Recibo.cshtml", model);
    }

    [HttpGet("/Venda/ImprimirComprovante/{vendaId:int}")]
    public async Task<IActionResult> ImprimirComprovante(int vendaId, CancellationToken cancellationToken)
    {
        if (!TryGetEmpresaId(out int empresaId)) return Forbid();

        var model = await db.Vendas.AsNoTracking()
            .Where(x => x.VendaId == vendaId && x.EmpresaId == empresaId)
            .Select(x => new ComprovanteVendaViewModel
            {
                VendaId = x.VendaId,
                DataVenda = x.DataVenda,
                ClienteNome = x.Cliente.Nome,
                ClienteDocumento = x.Cliente.Cnpj ?? x.Cliente.Cpf,
                VendedorNome = x.Vendedor == null ? null : x.Vendedor.Nome,
                Status = x.StatusVenda,
                TotalBruto = x.TotalBruto,
                TotalDesconto = x.TotalDesconto,
                TotalLiquido = x.TotalLiquido,
                Observacoes = x.Observacoes,
                Itens = x.Itens.OrderBy(i => i.ItemVendaId).Select(i => new ComprovanteVendaItemViewModel(
                    i.Produto.Referencia ?? i.ProdutoId.ToString(), i.Produto.NomeProduto,
                    i.Quantidade, i.PrecoUnitario, i.DescontoItem ?? 0,
                    i.Quantidade * i.PrecoUnitario - (i.DescontoItem ?? 0))).ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (model is null) return NotFound();
        model.Parcelas = await db.Parcelas.AsNoTracking()
            .Where(x => x.VendaId == vendaId && x.EmpresaId == empresaId && x.Status != "Cancelado")
            .OrderBy(x => x.NumeroParcela)
            .Select(x => new ComprovanteVendaParcelaViewModel(x.NumeroParcela, x.DataVencimento, x.ValorParcela, x.Status))
            .ToListAsync(cancellationToken);
        model.Empresa = await ObterEmpresaAsync(empresaId, cancellationToken);
        return View("~/Views/Venda/Comprovante.cshtml", model);
    }

    [HttpGet("/Caixa/ImprimirFechamento/{caixaId:int}")]
    public async Task<IActionResult> ImprimirFechamento(int caixaId, CancellationToken cancellationToken)
    {
        if (!TryGetEmpresaId(out int empresaId)) return Forbid();

        var model = await db.Caixas.AsNoTracking()
            .Where(x => x.CaixaId == caixaId && x.EmpresaId == empresaId)
            .Select(x => new FechamentoCaixaViewModel
            {
                CaixaId = x.CaixaId,
                DataAbertura = x.DataAbertura,
                DataFechamento = x.DataFechamento,
                UsuarioAbertura = x.UsuarioAbertura.NomeUsuario,
                UsuarioFechamento = x.UsuarioFechamento == null ? null : x.UsuarioFechamento.NomeUsuario,
                Status = x.Status,
                SaldoInicial = x.SaldoInicial,
                SaldoFinalSistema = x.SaldoFinalSistema ?? 0,
                SaldoFinalInformado = x.SaldoFinalInformado ?? 0,
                Diferenca = x.Diferenca ?? 0
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (model is null) return NotFound();

        var movimentos = await db.CaixaMovimentos.AsNoTracking()
            .Where(x => x.CaixaId == caixaId && x.EmpresaId == empresaId)
            .Select(x => new
            {
                x.DataHora, x.Tipo, x.Historico, x.Origem, x.Valor,
                Forma = x.FormaPagamento == null ? "Não informada" : x.FormaPagamento.NomeFormaPagamento
            }).ToListAsync(cancellationToken);

        model.TotaisPorForma = movimentos.GroupBy(x => x.Forma)
            .Select(g => new FechamentoCaixaFormaViewModel(
                g.Key,
                g.Where(x => x.Tipo == "Entrada").Sum(x => x.Valor),
                g.Where(x => x.Tipo == "Saida" || x.Tipo == "Saída").Sum(x => x.Valor)))
            .OrderBy(x => x.FormaPagamento).ToList();
        model.SangriasSuprimentos = movimentos
            .Where(x => (x.Origem ?? "").Contains("SANGRIA", StringComparison.OrdinalIgnoreCase) ||
                        (x.Origem ?? "").Contains("SUPRIMENTO", StringComparison.OrdinalIgnoreCase) ||
                        x.Historico.Contains("sangria", StringComparison.OrdinalIgnoreCase) ||
                        x.Historico.Contains("suprimento", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.DataHora)
            .Select(x => new FechamentoCaixaMovimentoViewModel(x.DataHora, x.Tipo, x.Historico, x.Valor, x.Forma))
            .ToList();
        model.Empresa = await ObterEmpresaAsync(empresaId, cancellationToken);
        return View("~/Views/Caixa/Fechamento.cshtml", model);
    }

    [HttpGet("/Comissao/ImprimirComprovante/{pagamentoComissaoId:int}")]
    public async Task<IActionResult> ImprimirComissao(int pagamentoComissaoId, CancellationToken cancellationToken)
    {
        if (!TryGetEmpresaId(out int empresaId)) return Forbid();
        var model = await db.PagamentosComissao.AsNoTracking()
            .Where(x => x.PagamentoComissaoId == pagamentoComissaoId && x.EmpresaId == empresaId)
            .Select(x => new ComprovanteComissaoViewModel
            {
                PagamentoComissaoId = x.PagamentoComissaoId,
                Vendedor = x.Vendedor.Nome,
                Cpf = x.Vendedor.Cpf,
                DataInicial = x.DataInicial,
                DataFinal = x.DataFinal,
                TotalVendas = x.TotalVendas,
                PercentualComissao = x.PercentualComissao,
                ValorPago = x.ValorComissao,
                DataPagamento = x.DataPagamento,
                Status = x.Status,
                Observacoes = x.Observacoes
            }).SingleOrDefaultAsync(cancellationToken);
        if (model is null) return NotFound();
        model.Empresa = await ObterEmpresaAsync(empresaId, cancellationToken);
        return View("~/Views/Comissao/Comprovante.cshtml", model);
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
