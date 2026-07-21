using FluentAssertions;
using GVC.Web.Models;
using GVC.Web.Pages.Produtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GVC.Web.Tests;

public sealed class ProdutoListagemIntegrationTests
{
    [Fact]
    public async Task PesquisaPorSkuFilho_DeveRetornarPaiComVariacoesDaEmpresaAtiva()
    {
        await using var database = await TestDbContextFactory.CreateAsync();
        await database.SeedCoreAsync();

        await using (var seed = database.CreateContext())
        {
            seed.Produtos.AddRange(
                CriarProdutoComGrade(1, "Camiseta", "SKU-AZUL-G", "SKU-AZUL-P"),
                CriarProdutoComGrade(2, "Produto de outra empresa", "SKU-AZUL-G", "SKU-OUTRO"));
            await seed.SaveChangesAsync();
        }

        await using var db = database.CreateContext();
        var page = new IndexModel(db, Mock.Of<IWebHostEnvironment>());
        TestDbContextFactory.ConfigurePage(page, empresaId: 1);
        var metadataProvider = new EmptyModelMetadataProvider();
        page.HttpContext.RequestServices = new ServiceCollection()
            .AddSingleton<IModelMetadataProvider>(metadataProvider)
            .BuildServiceProvider();
        page.PageContext.ViewData = new ViewDataDictionary(
            metadataProvider,
            new ModelStateDictionary());

        PartialViewResult result = await page.OnGetPesquisarAsync("SKU-AZUL-G", CancellationToken.None);
        var produtos = result.ViewData.Model.Should()
            .BeAssignableTo<IReadOnlyList<Produto>>()
            .Subject;

        result.ViewName.Should().Be("_ListaProdutos");
        produtos.Should().ContainSingle();
        produtos[0].EmpresaId.Should().Be(1);
        produtos[0].NomeProduto.Should().Be("Camiseta");
        produtos[0].Variacoes.Should().HaveCount(2);
        produtos[0].EstoqueTotal.Should().Be(15);
    }

    private static Produto CriarProdutoComGrade(
        int empresaId,
        string nome,
        string primeiroSku,
        string segundoSku) => new()
    {
        EmpresaId = empresaId,
        NomeProduto = nome,
        TemVariacao = true,
        Status = "Ativo",
        DataDeEntrada = DateTime.Now,
        PrecoCusto = 10m,
        PrecoDeVenda = 20m,
        Variacoes =
        [
            new ProdutoVariacao
            {
                Sku = primeiroSku,
                Estoque = 10,
                Status = "Ativo",
                Atributos =
                [
                    new ProdutoVariacaoAtributo
                    {
                        NomeAtributo = "Cor",
                        ValorAtributo = "Azul"
                    }
                ]
            },
            new ProdutoVariacao
            {
                Sku = segundoSku,
                Estoque = 5,
                Status = "Ativo",
                Atributos =
                [
                    new ProdutoVariacaoAtributo
                    {
                        NomeAtributo = "Tamanho",
                        ValorAtributo = "P"
                    }
                ]
            }
        ]
    };
}
