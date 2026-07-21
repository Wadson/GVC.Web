using FluentAssertions;
using GVC.Web.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GVC.Web.Tests;

public sealed class StatusDomainTests
{
    [Fact]
    public void Enums_DevemManterCodigosOficiais()
    {
        ((int)StatusVenda.Aberta).Should().Be(1);
        ((int)StatusVenda.Concluida).Should().Be(2);
        ((int)StatusVenda.AguardandoPagamento).Should().Be(3);
        ((int)StatusVenda.Cancelada).Should().Be(4);

        ((int)StatusParcela.Pendente).Should().Be(1);
        ((int)StatusParcela.Pago).Should().Be(2);
        ((int)StatusParcela.ParcialmentePago).Should().Be(3);
        ((int)StatusParcela.Atrasada).Should().Be(4);
        ((int)StatusParcela.Cancelada).Should().Be(5);
    }

    [Fact]
    public void ParcelaPendenteVencida_DeveExibirAtrasada_SemAlterarStatusPersistido()
    {
        var parcela = new Parcela
        {
            Status = StatusParcela.Pendente,
            DataVencimento = DateTime.Today.AddDays(-1)
        };

        parcela.StatusAtual.Should().Be(StatusParcela.Atrasada);
        parcela.Status.Should().Be(StatusParcela.Pendente);

        parcela.Status = StatusParcela.ParcialmentePago;
        parcela.StatusAtual.Should().Be(StatusParcela.ParcialmentePago);
    }

    [Fact]
    public async Task ConversoresEf_DevemPreservarFormatoAtualDoSqlELerAliasesLegados()
    {
        await using var database = await TestDbContextFactory.CreateAsync();
        await using var db = database.CreateContext();

        var vendaConverter = db.Model.FindEntityType(typeof(Venda))!
            .FindProperty(nameof(Venda.StatusVenda))!.GetValueConverter()!;
        var parcelaConverter = db.Model.FindEntityType(typeof(Parcela))!
            .FindProperty(nameof(Parcela.Status))!.GetValueConverter()!;

        vendaConverter.ConvertToProvider(StatusVenda.Concluida).Should().Be("Concluída");
        vendaConverter.ConvertFromProvider("Finalizada").Should().Be(StatusVenda.Concluida);
        parcelaConverter.ConvertFromProvider("Pago Parcial").Should().Be(StatusParcela.ParcialmentePago);
        parcelaConverter.ConvertFromProvider("Cancelado").Should().Be(StatusParcela.Cancelada);
    }
}
