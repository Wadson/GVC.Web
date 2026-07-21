using System.Text.Json;
using FluentAssertions;
using GVC.Web.Models;
using GVC.Web.Services;
using Xunit;

namespace GVC.Web.Tests;

public sealed class VariacaoPersistenceContractTests
{
    [Fact]
    public void JsonDoPdv_DeveVincularVariacaoIdAoContratoDaVenda()
    {
        const string json = """
            { "produtoId": 10, "quantidade": 2, "desconto": 0, "variacaoId": 42 }
            """;

        var item = JsonSerializer.Deserialize<VendaItemInput>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        item.Should().NotBeNull();
        item!.VariacaoID.Should().Be(42);
    }

    [Fact]
    public async Task DbContext_DeveBloquearMovimentoDeProdutoComGradeSemVariacaoId()
    {
        await using var database = await TestDbContextFactory.CreateAsync();
        await database.SeedCoreAsync();

        await using var db = database.CreateContext();
        var dados = await IntegrationTestData.SeedProdutoComVariacoesAsync(db);
        db.MovimentacoesEstoque.Add(new MovimentacaoEstoque
        {
            EmpresaId = 1,
            ProdutoId = dados.ProdutoId,
            VariacaoID = null,
            TipoMovimentacao = "SAIDA",
            Quantidade = 1,
            EstoqueAnterior = 10,
            EstoqueAtual = 9,
            Origem = "TESTE",
            DataMovimentacao = DateTime.Now
        });

        var salvar = () => db.SaveChangesAsync();

        await salvar.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*VariacaoID*");
    }
}
