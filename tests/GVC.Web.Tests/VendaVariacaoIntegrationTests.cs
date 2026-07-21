using FluentAssertions;
using GVC.Web.Models;
using GVC.Web.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GVC.Web.Tests;

public sealed class VendaVariacaoIntegrationTests
{
    [Fact]
    public async Task VendaPorSku_DeveBaixarSomenteVariacaoSelecionada_ERegistrarHistorico()
    {
        await using var database = await TestDbContextFactory.CreateAsync();
        await database.SeedCoreAsync();
        int clienteId, produtoId, variacaoGId, variacaoPId;
        await using (var seed = database.CreateContext())
            (clienteId, produtoId, variacaoGId, variacaoPId, _) =
                await IntegrationTestData.SeedProdutoComVariacoesAsync(seed);

        await using (var db = database.CreateContext())
        {
            var service = new VendaService(db);
            await service.FinalizarAsync(1, 1, new FinalizarVendaInput(
                clienteId, null, 1, 0, false,
                [new VendaItemInput(produtoId, 2, 0, variacaoGId)],
                [new ParcelaVendaInput(1, DateTime.Today.AddDays(30), 100m)]),
                CancellationToken.None);
        }

        await using var assertDb = database.CreateContext();
        var produto = await assertDb.Produtos.Include(x => x.Variacoes)
            .SingleAsync(x => x.ProdutoId == produtoId);
        produto.Variacoes.Single(x => x.VariacaoId == variacaoGId).Estoque.Should().Be(8);
        produto.Variacoes.Single(x => x.VariacaoId == variacaoPId).Estoque.Should().Be(5);
        produto.Estoque.Should().Be(0);
        produto.EstoqueTotal.Should().Be(13);

        var movimento = await assertDb.MovimentacoesEstoque.SingleAsync();
        movimento.VariacaoID.Should().Be(variacaoGId);
        movimento.TipoMovimentacao.Should().Be("SAIDA");
        movimento.Origem.Should().Be("VENDA");

        (await assertDb.Vendas.SingleAsync()).StatusVenda.Should().Be(StatusVenda.Concluida);
        (await assertDb.Parcelas.SingleAsync()).Status.Should().Be(StatusParcela.Pendente);
    }
}
