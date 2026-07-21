using FluentAssertions;
using GVC.Web.DTOs;
using GVC.Web.Services;
using GVC.Web.ViewModels;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GVC.Web.Tests;

public sealed class DocumentoEntradaIntegrationTests
{
    [Fact]
    public async Task EntradaComVariacao_DeveIncrementarSku_EGravarVariacaoNoDocumentoEMovimento()
    {
        await using var database = await TestDbContextFactory.CreateAsync();
        await database.SeedCoreAsync();
        int produtoId, variacaoId, fornecedorId, planoId;
        await using (var seed = database.CreateContext())
        {
            (_, produtoId, variacaoId, _, _) = await IntegrationTestData.SeedProdutoComVariacoesAsync(seed);
            var fornecedor = IntegrationTestData.NovoFornecedor();
            var plano = IntegrationTestData.NovoPlanoDespesa();
            seed.AddRange(fornecedor, plano);
            await seed.SaveChangesAsync();
            fornecedorId = fornecedor.FornecedorId;
            planoId = plano.PlanoContasId;
        }

        await using (var db = database.CreateContext())
        {
            var input = new EntradaEstoqueViewModel
            {
                FornecedorId = fornecedorId,
                PlanoContasId = planoId,
                NumeroDocumento = "NF-001",
                Serie = "1",
                DataEmissao = DateTime.Today,
                ValorTotalProdutos = 150m,
                ValorTotalNota = 150m,
                Itens =
                [
                    new EntradaEstoqueItemDTO
                    {
                        ProdutoId = produtoId, VariacaoID = variacaoId, Quantidade = 15,
                        PrecoUnitarioCompra = 10m, PrecoCustoUnitario = 10m, ValorTotalItem = 150m
                    }
                ],
                Parcelas = [new ParcelaEntradaDTO { DataVencimento = DateTime.Today.AddDays(30), Valor = 150m }]
            };
            await new EntradaEstoqueService(db).SalvarAsync(1, 1, "teste", input, CancellationToken.None);
        }

        await using var assertDb = database.CreateContext();
        (await assertDb.ProdutosVariacoes.SingleAsync(x => x.VariacaoId == variacaoId))
            .Estoque.Should().Be(25);
        var item = await assertDb.DocumentosEntradaItens.SingleAsync();
        item.VariacaoID.Should().Be(variacaoId);
        var movimento = await assertDb.MovimentacoesEstoque.SingleAsync();
        movimento.TipoMovimentacao.Should().Be("ENTRADA");
        movimento.Origem.Should().Be("COMPRA");
        movimento.VariacaoID.Should().Be(variacaoId);
    }
}
