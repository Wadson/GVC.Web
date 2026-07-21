using FluentAssertions;
using GVC.Web.Controllers;
using GVC.Web.Models;
using GVC.Web.ViewModels;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GVC.Web.Tests;

public sealed class ConcorrenciaFinanceiraIntegrationTests
{
    [Fact]
    public async Task DuplaBaixaConcorrente_DeveGerarUmaUnicaSaidaNoCaixa()
    {
        await using var database = await TestDbContextFactory.CreateAsync();
        await database.SeedCoreAsync();
        int contaId = await SeedContaAsync(database);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task<bool> primeira = BaixarAsync(database, contaId, gate.Task);
        Task<bool> segunda = BaixarAsync(database, contaId, gate.Task);
        gate.SetResult();
        bool[] resultados = await Task.WhenAll(primeira, segunda);

        resultados.Count(x => x).Should().Be(1);
        await using var assertDb = database.CreateContext();
        (await assertDb.ContasAPagar.SingleAsync(x => x.ContasAPagarId == contaId)).Status.Should().Be("Pago");
        (await assertDb.CaixaMovimentos.CountAsync(x =>
            x.Origem == "ContasAPagar" && x.ReferenciaId == contaId && x.Tipo == "SAIDA"))
            .Should().Be(1);
    }

    [Fact]
    public async Task Estorno_DeveRestaurarTitulo_EGerarContrapartidaNoCaixa()
    {
        await using var database = await TestDbContextFactory.CreateAsync();
        await database.SeedCoreAsync();
        int contaId = await SeedContaAsync(database);
        (await BaixarAsync(database, contaId, Task.CompletedTask)).Should().BeTrue();

        await using (var db = database.CreateContext())
        {
            var controller = new ContasAPagarController(db);
            TestDbContextFactory.ConfigureController(controller, 1, 1);
            await controller.Estornar(contaId, CancellationToken.None);
        }

        await using var assertDb = database.CreateContext();
        var conta = await assertDb.ContasAPagar.SingleAsync(x => x.ContasAPagarId == contaId);
        conta.Status.Should().Be("Pendente");
        conta.ValorPago.Should().Be(0);
        conta.DataPagamento.Should().BeNull();
        conta.FormaPgtoId.Should().BeNull();
        var movimentos = await assertDb.CaixaMovimentos.Where(x => x.ReferenciaId == contaId).ToListAsync();
        movimentos.Should().ContainSingle(x => x.Origem == "ContasAPagarEstornada" && x.Tipo == "SAIDA");
        movimentos.Should().ContainSingle(x => x.Origem == "EstornoContasAPagar" && x.Tipo == "ENTRADA" && x.Valor == 100m);
    }

    private static async Task<int> SeedContaAsync(TestDbContextFactory database)
    {
        await using var db = database.CreateContext();
        var plano = IntegrationTestData.NovoPlanoDespesa();
        db.PlanosContas.Add(plano);
        db.Caixas.Add(new Caixa
        {
            EmpresaId = 1, UsuarioAberturaId = 1, DataCaixa = DateTime.Today,
            DataAbertura = DateTime.Now, Status = "Aberto", SaldoInicial = 500m
        });
        await db.SaveChangesAsync();
        var conta = new ContaAPagar
        {
            EmpresaId = 1, PlanoContasId = plano.PlanoContasId, Descricao = "Conta concorrente",
            DataEmissao = DateTime.Today, DataVencimento = DateTime.Today.AddDays(10),
            Valor = 100m, Status = "Pendente"
        };
        db.ContasAPagar.Add(conta);
        await db.SaveChangesAsync();
        return conta.ContasAPagarId;
    }

    private static async Task<bool> BaixarAsync(
        TestDbContextFactory database,
        int contaId,
        Task gate)
    {
        await gate;
        try
        {
            await using var db = database.CreateContext();
            var controller = new ContasAPagarController(db);
            TestDbContextFactory.ConfigureController(controller, 1, 1);
            await controller.Baixar(new BaixarContaViewModel
            {
                ContasAPagarId = contaId, DataPagamento = DateTime.Today,
                ValorPago = 100m, FormaPgtoId = 1
            }, CancellationToken.None);
            return controller.TempData["Success"] is not null;
        }
        catch (DbUpdateException)
        {
            return false;
        }
        catch (Microsoft.Data.Sqlite.SqliteException)
        {
            return false;
        }
    }
}
