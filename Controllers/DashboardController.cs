using System.Security.Claims;
using GVC.Web.Data;
using GVC.Web.DTOs;
using GVC.Web.Models;
using GVC.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Controllers;

[Authorize]
[Route("Dashboard")]
public class DashboardController(IDbContextFactory<ErpDbContext> dbFactory) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryGetEmpresaId(out int empresaId))
        {
            return Forbid();
        }

        DateTime hoje = DateTime.Today;
        DateTime amanha = hoje.AddDays(1);
        DateTime inicioMes = new(hoje.Year, hoje.Month, 1);
        DateTime inicioGrafico = hoje.AddDays(-29);

        Task<decimal> vendasHojeTask = ObterVendasHojeAsync(empresaId, hoje, amanha, cancellationToken);
        Task<decimal> contasAPagarTask = ObterContasAPagarHojeAsync(empresaId, hoje, cancellationToken);
        Task<decimal> margemTask = ObterMargemMediaMesAsync(empresaId, inicioMes, amanha, cancellationToken);
        Task<int> caixasTask = ObterCaixasAbertosAsync(empresaId, cancellationToken);
        Task<int> notasTask = ObterNotasRejeitadasAsync(empresaId, cancellationToken);
        Task<int> pedidosTask = ObterPedidosAtrasadosAsync(empresaId, hoje, cancellationToken);
        Task<List<FaturamentoDiarioDTO>> faturamentoTask =
            ObterFaturamentoAsync(empresaId, inicioGrafico, amanha, cancellationToken);

        await Task.WhenAll(
            vendasHojeTask,
            contasAPagarTask,
            margemTask,
            caixasTask,
            notasTask,
            pedidosTask,
            faturamentoTask);

        var viewModel = new DashboardViewModel
        {
            VendasHoje = await vendasHojeTask,
            ContasAPagarHoje = await contasAPagarTask,
            MargemMediaMes = await margemTask,
            CaixasAbertosPendentes = await caixasTask,
            NotasRejeitadas = await notasTask,
            PedidosAtrasados = await pedidosTask,
            FaturamentoUltimos30Dias = PreencherDiasSemFaturamento(
                inicioGrafico,
                hoje,
                await faturamentoTask)
        };

        viewModel.TarefasPendentes = CriarTarefas(viewModel);

        return View(viewModel);
    }

    private async Task<decimal> ObterVendasHojeAsync(
        int empresaId,
        DateTime inicio,
        DateTime fim,
        CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Vendas
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId &&
                        x.DataVenda >= inicio &&
                        x.DataVenda < fim &&
                        (x.StatusVenda == StatusVenda.Concluida ||
                         x.StatusVenda == StatusVenda.AguardandoPagamento))
            .SumAsync(x => (decimal?)x.TotalLiquido, cancellationToken) ?? 0;
    }

    private async Task<decimal> ObterContasAPagarHojeAsync(
        int empresaId,
        DateTime hoje,
        CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.ContasAPagar
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId &&
                        x.DataVencimento == hoje &&
                        x.Status == "Pendente")
            .SumAsync(x => (decimal?)x.Valor, cancellationToken) ?? 0;
    }

    private async Task<decimal> ObterMargemMediaMesAsync(
        int empresaId,
        DateTime inicio,
        DateTime fim,
        CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var totais = await db.ItensVenda
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId &&
                        x.Venda.EmpresaId == empresaId &&
                        x.Produto.EmpresaId == empresaId &&
                        x.Venda.DataVenda >= inicio &&
                        x.Venda.DataVenda < fim &&
                        (x.Venda.StatusVenda == StatusVenda.Concluida ||
                         x.Venda.StatusVenda == StatusVenda.AguardandoPagamento))
            .GroupBy(_ => 1)
            .Select(grupo => new
            {
                Receita = grupo.Sum(x => x.PrecoUnitario * x.Quantidade - (x.DescontoItem ?? 0)),
                Custo = grupo.Sum(x => x.Produto.PrecoCusto * x.Quantidade)
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (totais is null || totais.Receita <= 0)
        {
            return 0;
        }

        return Math.Round((totais.Receita - totais.Custo) / totais.Receita * 100, 2);
    }

    private async Task<int> ObterCaixasAbertosAsync(int empresaId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Caixas
            .AsNoTracking()
            .CountAsync(x => x.EmpresaId == empresaId &&
                             x.Status == "Aberto" &&
                             x.DataFechamento == null,
                cancellationToken);
    }

    private async Task<int> ObterNotasRejeitadasAsync(int empresaId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.DocumentosFiscais
            .AsNoTracking()
            .CountAsync(x => x.EmpresaId == empresaId && x.StatusSefaz == "Rejeitada", cancellationToken);
    }

    private async Task<int> ObterPedidosAtrasadosAsync(
        int empresaId,
        DateTime hoje,
        CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Pedidos
            .AsNoTracking()
            .CountAsync(x => x.EmpresaId == empresaId &&
                             x.Status == "Pendente" &&
                             x.DataPedido < hoje,
                cancellationToken);
    }

    private async Task<List<FaturamentoDiarioDTO>> ObterFaturamentoAsync(
        int empresaId,
        DateTime inicio,
        DateTime fim,
        CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Vendas
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId &&
                        x.DataVenda >= inicio &&
                        x.DataVenda < fim &&
                        (x.StatusVenda == StatusVenda.Concluida ||
                         x.StatusVenda == StatusVenda.AguardandoPagamento))
            .GroupBy(x => x.DataVenda.Date)
            .Select(grupo => new FaturamentoDiarioDTO
            {
                Data = grupo.Key,
                ValorTotal = grupo.Sum(x => x.TotalLiquido)
            })
            .OrderBy(x => x.Data)
            .ToListAsync(cancellationToken);
    }

    private bool TryGetEmpresaId(out int empresaId) =>
        int.TryParse(User.FindFirstValue("EmpresaID"), out empresaId) && empresaId > 0;

    private static List<FaturamentoDiarioDTO> PreencherDiasSemFaturamento(
        DateTime inicio,
        DateTime fim,
        IEnumerable<FaturamentoDiarioDTO> faturamento)
    {
        var valoresPorDia = faturamento.ToDictionary(x => x.Data.Date, x => x.ValorTotal);

        return Enumerable.Range(0, (fim.Date - inicio.Date).Days + 1)
            .Select(dias => inicio.Date.AddDays(dias))
            .Select(data => new FaturamentoDiarioDTO
            {
                Data = data,
                ValorTotal = valoresPorDia.GetValueOrDefault(data)
            })
            .ToList();
    }

    private static List<TarefaPendenteDTO> CriarTarefas(DashboardViewModel dados)
    {
        var tarefas = new List<TarefaPendenteDTO>();

        if (dados.CaixasAbertosPendentes > 0)
        {
            tarefas.Add(new TarefaPendenteDTO
            {
                Descricao = $"Fechar {dados.CaixasAbertosPendentes} caixa(s) em aberto",
                TipoAcao = "Caixa",
                Link = "/Caixa"
            });
        }

        if (dados.NotasRejeitadas > 0)
        {
            tarefas.Add(new TarefaPendenteDTO
            {
                Descricao = $"Revisar {dados.NotasRejeitadas} nota(s) rejeitada(s)",
                TipoAcao = "Fiscal",
                Link = "/Fiscal/NotasFiscais"
            });
        }

        if (dados.PedidosAtrasados > 0)
        {
            tarefas.Add(new TarefaPendenteDTO
            {
                Descricao = $"Verificar {dados.PedidosAtrasados} pedido(s) atrasado(s)",
                TipoAcao = "Compras",
                Link = "/Fornecedores"
            });
        }

        return tarefas;
    }
}
