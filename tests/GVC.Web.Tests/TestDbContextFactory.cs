using System.Security.Claims;
using GVC.Web.Data;
using GVC.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GVC.Web.Tests;

public sealed class TestDbContextFactory : IAsyncDisposable
{
    private readonly SqliteConnection keeper;

    private TestDbContextFactory(SqliteConnection keeper) => this.keeper = keeper;

    public static async Task<TestDbContextFactory> CreateAsync()
    {
        string connectionString = $"Data Source=GvcTests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        var keeper = new SqliteConnection(connectionString);
        await keeper.OpenAsync();
        var factory = new TestDbContextFactory(keeper) { ConnectionString = connectionString };
        await using ErpDbContext db = factory.CreateContext();
        await db.Database.EnsureCreatedAsync();
        return factory;
    }

    private string ConnectionString { get; set; } = string.Empty;

    public ErpDbContext CreateContext(IHttpContextAccessor? accessor = null)
    {
        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseSqlite(ConnectionString)
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging()
            .Options;
        return new ErpDbContext(options, accessor);
    }

    public static DefaultHttpContext CreateHttpContext(int empresaId = 1, int usuarioId = 1)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("EmpresaID", empresaId.ToString()),
            new Claim("UsuarioID", usuarioId.ToString()),
            new Claim(ClaimTypes.Name, "Usuário de integração"),
            new Claim(ClaimTypes.Role, "Administrador")
        ], "IntegrationTest");
        return new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
    }

    public static IHttpContextAccessor CreateAccessor(int empresaId = 1, int usuarioId = 1) =>
        new HttpContextAccessor { HttpContext = CreateHttpContext(empresaId, usuarioId) };

    public static void ConfigureController(Controller controller, int empresaId = 1, int usuarioId = 1)
    {
        controller.ControllerContext = new ControllerContext { HttpContext = CreateHttpContext(empresaId, usuarioId) };
        var provider = new Mock<ITempDataProvider>();
        provider.Setup(x => x.LoadTempData(It.IsAny<HttpContext>())).Returns(new Dictionary<string, object>());
        controller.TempData = new TempDataDictionary(controller.HttpContext, provider.Object);
    }

    public static void ConfigurePage(PageModel page, int empresaId = 1, int usuarioId = 1)
    {
        var http = CreateHttpContext(empresaId, usuarioId);
        page.PageContext = new PageContext { HttpContext = http };
        var provider = new Mock<ITempDataProvider>();
        provider.Setup(x => x.LoadTempData(It.IsAny<HttpContext>())).Returns(new Dictionary<string, object>());
        page.TempData = new TempDataDictionary(http, provider.Object);
    }

    public async Task SeedCoreAsync()
    {
        await using ErpDbContext db = CreateContext();
        var estado = new Estado { EstadoId = 1, Nome = "São Paulo", Uf = "SP" };
        var cidade = new Cidade { CidadeId = 1, Nome = "São Paulo", EstadoId = 1 };
        db.AddRange(estado, cidade);
        db.Empresas.AddRange(CreateEmpresa(1, 1), CreateEmpresa(2, 1));
        db.Usuarios.AddRange(CreateUsuario(1, 1), CreateUsuario(2, 2));
        db.FormasPagamento.Add(new FormaPagamento { FormaPgtoId = 1, NomeFormaPagamento = "Dinheiro", Ativo = true });
        await db.SaveChangesAsync();
    }

    public static Empresa CreateEmpresa(int id, int cidadeId) => new()
    {
        EmpresaId = id, RazaoSocial = $"Empresa {id}", NomeFantasia = $"Empresa {id}",
        Cnpj = id.ToString().PadLeft(14, '0'), Logradouro = "Rua Teste", Bairro = "Centro",
        Cep = "01001000", CidadeId = cidadeId, DataCriacao = DateTime.Now
    };

    public static Usuario CreateUsuario(int id, int empresaId) => new()
    {
        UsuarioId = id, EmpresaId = empresaId, TipoUsuario = "Administrador",
        NomeCompleto = $"Usuário {id}", NomeUsuario = $"usuario{id}",
        Cpf = id.ToString().PadLeft(11, '0'), Email = $"usuario{id}@teste.local",
        Senha = "hash", DataNascimento = new DateTime(1990, 1, 1), DataCriacao = DateTime.Now
    };

    public async ValueTask DisposeAsync() => await keeper.DisposeAsync();
}
