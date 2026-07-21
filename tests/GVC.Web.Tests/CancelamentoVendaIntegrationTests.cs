using FluentAssertions;
using GVC.Web.Models;
using GVC.Web.Pages.Vendas;
using GVC.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GVC.Web.Tests;

public sealed class CancelamentoVendaIntegrationTests
{
    [Fact]
    public async Task Cancelamento_DeveDevolverSku_RegistrarEntrada_EEstornarCaixa()
    {
        await using var database = await TestDbContextFactory.CreateAsync();
        await database.SeedCoreAsync();
        int clienteId, produtoId, variacaoId;
        long vendaId;
        await using (var db = database.CreateContext())
        {
            (clienteId, produtoId, variacaoId, _, _) =
                await IntegrationTestData.SeedProdutoComVariacoesAsync(db);
            vendaId = await new VendaService(db).FinalizarAsync(1, 1, new FinalizarVendaInput(
                clienteId, null, 1, 0, true,
                [new VendaItemInput(produtoId, 2, 0, variacaoId)],
                [new ParcelaVendaInput(1, DateTime.Today, 100m)]), CancellationToken.None);
        }

        await using (var db = database.CreateContext())
        {
            var page = new DetailsModel(db);
            TestDbContextFactory.ConfigurePage(page, 1, 1);
            IActionResult result = await page.OnPostCancelarAsync((int)vendaId, CancellationToken.None);
            result.Should().BeOfType<RedirectToPageResult>();
        }

        await using var assertDb = database.CreateContext();
        (await assertDb.ProdutosVariacoes.SingleAsync(x => x.VariacaoId == variacaoId))
            .Estoque.Should().Be(10);
        var movimento = await assertDb.MovimentacoesEstoque.SingleAsync(x => x.Origem == "CANCELAMENTO_VENDA");
        movimento.TipoMovimentacao.Should().Be("ENTRADA");
        movimento.VariacaoID.Should().Be(variacaoId);

        var caixa = await assertDb.CaixaMovimentos.Where(x => x.ReferenciaId == vendaId).ToListAsync();
        caixa.Should().Contain(x => x.Tipo == "ENTRADA" && x.Origem == "Venda" && x.Valor == 100m);
        caixa.Should().Contain(x => x.Tipo == "SAIDA" && x.Origem == "CANCELAMENTO_VENDA" && x.Valor == 100m);
        (await assertDb.Vendas.SingleAsync(x => x.VendaId == vendaId)).StatusVenda.Should().Be(StatusVenda.Cancelada);
    }

    [Fact]
    public async Task CancelamentoDeVendaAberta_NaoDeveGerarEntradaNemAlterarEstoque()
    {
        await using var database = await TestDbContextFactory.CreateAsync();
        await database.SeedCoreAsync();
        int variacaoId;
        int vendaId;

        await using (var db = database.CreateContext())
        {
            var dados = await IntegrationTestData.SeedProdutoComVariacoesAsync(db);
            variacaoId = dados.VariacaoGId;
            var venda = new Venda
            {
                EmpresaId = 1,
                ClienteId = dados.ClienteId,
                FormaPgtoId = 1,
                DataVenda = DateTime.Now,
                StatusVenda = StatusVenda.Aberta
            };
            venda.Itens.Add(new ItemVenda
            {
                EmpresaId = 1,
                ProdutoId = dados.ProdutoId,
                VariacaoID = variacaoId,
                Quantidade = 2,
                PrecoUnitario = 50m
            });
            db.Vendas.Add(venda);
            await db.SaveChangesAsync();
            vendaId = venda.VendaId;
        }

        await using (var db = database.CreateContext())
        {
            var page = new DetailsModel(db);
            TestDbContextFactory.ConfigurePage(page, 1, 1);
            (await page.OnPostCancelarAsync(vendaId, CancellationToken.None))
                .Should().BeOfType<RedirectToPageResult>();
        }

        await using var assertDb = database.CreateContext();
        (await assertDb.ProdutosVariacoes.SingleAsync(x => x.VariacaoId == variacaoId))
            .Estoque.Should().Be(10);
        (await assertDb.MovimentacoesEstoque.AnyAsync(x => x.Documento == vendaId.ToString()))
            .Should().BeFalse();
        (await assertDb.Vendas.SingleAsync(x => x.VendaId == vendaId))
            .StatusVenda.Should().Be(StatusVenda.Cancelada);
    }
}
