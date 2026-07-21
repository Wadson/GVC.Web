using FluentAssertions;
using GVC.Web.Models;
using GVC.Web.Pages.Clientes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GVC.Web.Tests;

public sealed class MultiTenantIntegrationTests
{
    [Fact]
    public async Task Listagem_DeveRetornarSomenteRegistrosDaEmpresaAtiva()
    {
        await using var database = await TestDbContextFactory.CreateAsync();
        await database.SeedCoreAsync();
        await using (var seed = database.CreateContext())
        {
            seed.Clientes.AddRange(
                NovoCliente("Cliente empresa 1", 1, "11111111111"),
                NovoCliente("Cliente empresa 2", 2, "22222222222"));
            await seed.SaveChangesAsync();
        }

        await using var db = database.CreateContext();
        var page = new IndexModel(db);
        TestDbContextFactory.ConfigurePage(page, empresaId: 1);

        await page.OnGetAsync();

        page.Clientes.Should().ContainSingle()
            .Which.Nome.Should().Be("Cliente empresa 1");
        page.Clientes.Should().OnlyContain(x => x.EmpresaId == 1);
    }

    [Fact]
    public async Task EdicaoPorId_DeOutraEmpresa_DeveRetornarNotFound()
    {
        await using var database = await TestDbContextFactory.CreateAsync();
        await database.SeedCoreAsync();
        int clienteEmpresa2;
        await using (var seed = database.CreateContext())
        {
            var cliente = NovoCliente("Cliente protegido", 2, "22222222222");
            seed.Clientes.Add(cliente);
            await seed.SaveChangesAsync();
            clienteEmpresa2 = cliente.ClienteId;
        }

        await using var db = database.CreateContext();
        var page = new EditModel(db);
        TestDbContextFactory.ConfigurePage(page, empresaId: 1);

        IActionResult result = await page.OnGetAsync(clienteEmpresa2);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Edicao_DeveCarregarNomeEUfDaCidadeAssociada()
    {
        await using var database = await TestDbContextFactory.CreateAsync();
        await database.SeedCoreAsync();
        int clienteId;
        await using (var seed = database.CreateContext())
        {
            var cliente = NovoCliente("Cliente com cidade", 1, "33333333333");
            cliente.CidadeId = 1;
            seed.Clientes.Add(cliente);
            await seed.SaveChangesAsync();
            clienteId = cliente.ClienteId;
        }

        await using var db = database.CreateContext();
        var page = new EditModel(db);
        TestDbContextFactory.ConfigurePage(page, empresaId: 1);

        IActionResult result = await page.OnGetAsync(clienteId);

        result.Should().BeOfType<PageResult>();
        page.Cliente.CidadeId.Should().Be(1);
        page.CidadeNome.Should().Be("São Paulo - SP");
    }

    [Fact]
    public async Task Insercao_DeveSobrescreverEmpresaIdPostado_ComEmpresaDaClaim()
    {
        await using var database = await TestDbContextFactory.CreateAsync();
        await database.SeedCoreAsync();
        await using var db = database.CreateContext(TestDbContextFactory.CreateAccessor(empresaId: 1));
        var page = new CreateModel(db)
        {
            Cliente = NovoCliente("Novo cliente", 999, "12345678901")
        };
        TestDbContextFactory.ConfigurePage(page, empresaId: 1);

        IActionResult result = await page.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        (await db.Clientes.SingleAsync(x => x.Nome == "Novo cliente"))
            .EmpresaId.Should().Be(1);
    }

    private static Cliente NovoCliente(string nome, int empresaId, string cpf) => new()
    {
        Nome = nome, EmpresaId = empresaId, TipoCliente = "PF", Cpf = cpf,
        Status = 1, DataCriacao = DateTime.Now
    };
}
