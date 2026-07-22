using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using GVC.Web.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GVC.Web.Tests.Unit.Services;

public sealed class VendaServiceTests
{
    [Fact]
    public async Task FinalizarAsync_ComClienteInvalido_DeveRejeitarELogarAviso()
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        var logger = fixture.Freeze<Mock<ILogger<VendaService>>>();
        await using var database = await TestDbContextFactory.CreateAsync();
        await database.SeedCoreAsync();
        await using var db = database.CreateContext();
        var service = new VendaService(db, logger.Object);
        int clienteInexistente = fixture.Create<int>() & int.MaxValue;
        if (clienteInexistente is 0 or 1) clienteInexistente = int.MaxValue;

        var input = new FinalizarVendaInput(
            clienteInexistente,
            null,
            1,
            0,
            false,
            [new VendaItemInput(1, 1, 0)],
            [new ParcelaVendaInput(1, DateTime.Today, 10m)]);

        Func<Task> action = () => service.FinalizarAsync(1, 1, input, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cliente inválido.");
        DeveTerLog(logger, LogLevel.Warning, "cliente", clienteInexistente.ToString());
    }

    [Fact]
    public async Task FinalizarAsync_SemCliente_DeveFalharAntesDeConsultarBanco()
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        var logger = fixture.Freeze<Mock<ILogger<VendaService>>>();
        await using var database = await TestDbContextFactory.CreateAsync();
        await using var db = database.CreateContext();
        var service = new VendaService(db, logger.Object);
        var input = new FinalizarVendaInput(
            null,
            null,
            1,
            0,
            false,
            [new VendaItemInput(1, 1, 0)],
            [new ParcelaVendaInput(1, DateTime.Today, 10m)]);

        Func<Task> action = () => service.FinalizarAsync(1, 1, input, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Informe o cliente.");
        DeveTerLog(logger, LogLevel.Warning, "cliente não informado");
    }

    private static void DeveTerLog(
        Mock<ILogger<VendaService>> logger,
        LogLevel level,
        params string[] trechos)
    {
        bool encontrado = logger.Invocations.Any(invocation =>
            invocation.Arguments.Count >= 3 &&
            invocation.Arguments[0] is LogLevel logLevel && logLevel == level &&
            trechos.All(trecho => invocation.Arguments[2]?.ToString()?.Contains(
                trecho, StringComparison.OrdinalIgnoreCase) == true));

        encontrado.Should().BeTrue($"deveria existir log {level} contendo {string.Join(", ", trechos)}");
    }
}
