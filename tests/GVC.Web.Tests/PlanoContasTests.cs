using System.Security.Claims;
using GVC.Web.Controllers;
using GVC.Web.Data;
using GVC.Web.Models;
using GVC.Web.Services;
using GVC.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace GVC.Web.Tests;

public class PlanoContasTests
{
    [Fact]
    public async Task SeedInicial_CriaEstruturaPadrao_ParaNovaEmpresa()
    {
        await using ErpDbContext db = CriarDbContext();

        int registros = await DbInitializer.SeedPlanoContasAsync(db, 37);

        Assert.Equal(8, registros);
        Assert.Equal(8, await db.PlanosContas.CountAsync(x => x.EmpresaId == 37));
        Assert.True(await db.PlanosContas.AnyAsync(x =>
            x.EmpresaId == 37 &&
            x.CodigoClassificacao == "2.01" &&
            x.Tipo == "D"));
    }

    [Fact]
    public async Task EntradaEstoque_Nova_ListaSomenteContasDeDespesa()
    {
        await using ErpDbContext db = CriarDbContext();
        db.PlanosContas.AddRange(
            new PlanoContas { EmpresaId = 10, CodigoClassificacao = "1.01", Descricao = "Venda", Tipo = "R" },
            new PlanoContas { EmpresaId = 10, CodigoClassificacao = "2.01", Descricao = "Compras", Tipo = "D" },
            new PlanoContas { EmpresaId = 10, CodigoClassificacao = "2.02", Descricao = "Pessoal", Tipo = "D" },
            new PlanoContas { EmpresaId = 99, CodigoClassificacao = "2.01", Descricao = "Outra empresa", Tipo = "D" });
        await db.SaveChangesAsync();

        var controller = new EntradaEstoqueController(db, Mock.Of<IEntradaEstoqueService>());
        ConfigurarUsuario(controller, 10, 5);

        var result = Assert.IsType<ViewResult>(await controller.Nova(CancellationToken.None));
        var model = Assert.IsType<EntradaEstoqueViewModel>(result.Model);

        Assert.Equal(2, model.PlanosContas.Count);
        Assert.All(model.PlanosContas, item => Assert.StartsWith("2.", item.Descricao));
        Assert.Equal(
            model.PlanosContas.Single(x => x.Descricao.StartsWith("2.01")).Id,
            model.PlanoContasId);
    }

    [Fact]
    public async Task Excluir_ContaEmUso_ImpedeExclusao()
    {
        await using ErpDbContext db = CriarDbContext();
        var plano = new PlanoContas
        {
            EmpresaId = 20,
            CodigoClassificacao = "2.01",
            Descricao = "Compras",
            Tipo = "D"
        };
        db.PlanosContas.Add(plano);
        await db.SaveChangesAsync();
        db.ContasAPagar.Add(new ContaAPagar
        {
            EmpresaId = 20,
            PlanoContasId = plano.PlanoContasId,
            Descricao = "Compra vinculada",
            DataEmissao = DateTime.Today,
            DataVencimento = DateTime.Today.AddDays(30),
            Valor = 100,
            Status = "Pendente"
        });
        await db.SaveChangesAsync();

        var controller = new PlanoContasController(db);
        ConfigurarUsuario(controller, 20, 8);
        var tempDataProvider = new Mock<ITempDataProvider>();
        tempDataProvider
            .Setup(x => x.LoadTempData(It.IsAny<HttpContext>()))
            .Returns(new Dictionary<string, object>());
        controller.TempData = new TempDataDictionary(
            controller.HttpContext,
            tempDataProvider.Object);

        Assert.IsType<RedirectToActionResult>(await controller.Excluir(plano.PlanoContasId, CancellationToken.None));

        Assert.True(await db.PlanosContas.AnyAsync(x => x.PlanoContasId == plano.PlanoContasId));
        Assert.Contains("não pode ser excluída", controller.TempData["Error"]?.ToString());
    }

    private static ErpDbContext CriarDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ErpDbContext(options);
    }

    private static void ConfigurarUsuario(Controller controller, int empresaId, int usuarioId)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("EmpresaID", empresaId.ToString()),
            new Claim("UsuarioID", usuarioId.ToString()),
            new Claim(ClaimTypes.Name, "Usuário de teste")
        ], "Test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }
}
