using System.Security.Claims;
using GVC.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Middleware;

public sealed class PermissionAuthorizationMiddleware(
    RequestDelegate next,
    ILogger<PermissionAuthorizationMiddleware> logger)
{
    private static readonly Dictionary<string, string> ModulePrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/Clientes"] = "Cadastros",
        ["/Fornecedores"] = "Cadastros",
        ["/Produtos"] = "Cadastros",
        ["/Catalogo/Gerenciar"] = "Cadastros",
        ["/Catalogo/AlternarProduto"] = "Cadastros",
        ["/Vendedores"] = "Cadastros",
        ["/Cidades"] = "Cadastros",
        ["/Estoque"] = "Estoque",
        ["/EntradaEstoque"] = "Estoque",
        ["/Relatorios/Estoque"] = "Estoque",
        ["/Relatorios"] = "Faturamento",
        ["/Vendas"] = "Faturamento",
        ["/Venda"] = "Faturamento",
        ["/Caixa"] = "Faturamento",
        ["/Relatorios/Vendas"] = "Faturamento",
        ["/Fiscal"] = "Fiscal",
        ["/Financeiro"] = "Financeiro",
        ["/ContasAPagar"] = "Financeiro",
        ["/ContasAReceber"] = "Financeiro",
        ["/Comissao"] = "Financeiro",
        ["/PlanoContas"] = "Financeiro",
        ["/Usuarios"] = "Segurança",
        ["/Configuracoes"] = "Segurança"
    };

    public async Task InvokeAsync(HttpContext context, ErpDbContext db)
    {
        if (!DeveValidar(context, out string? modulo, out PermissionAction action))
        {
            await next(context);
            return;
        }

        if (!TryGetClaim(context.User, "UsuarioID", out int usuarioId) ||
            !TryGetClaim(context.User, "EmpresaID", out int empresaId))
        {
            logger.LogWarning(
                "Autorização rejeitada por claims ausentes. Caminho {Path}, correlationId {CorrelationId}",
                context.Request.Path, context.TraceIdentifier);
            await context.ForbidAsync();
            return;
        }

        var permissao = await db.PermissoesUsuario
            .AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId && x.EmpresaId == empresaId && x.Modulo == modulo)
            .Select(x => new
            {
                x.PodeVisualizar,
                x.PodeCriar,
                x.PodeEditar,
                x.PodeExcluir
            })
            .FirstOrDefaultAsync(context.RequestAborted);

        bool autorizado = permissao is not null && action switch
        {
            PermissionAction.Visualizar => permissao.PodeVisualizar,
            PermissionAction.Criar => permissao.PodeCriar,
            PermissionAction.Editar => permissao.PodeEditar,
            PermissionAction.Excluir => permissao.PodeExcluir,
            _ => false
        };

        if (!autorizado)
        {
            logger.LogWarning(
                "Acesso negado. Empresa {EmpresaId}, usuário {UsuarioId}, módulo {Modulo}, ação {Acao}, caminho {Path}",
                empresaId, usuarioId, modulo, action, context.Request.Path);
            await context.ForbidAsync();
            return;
        }

        await next(context);
    }

    private static bool DeveValidar(
        HttpContext context,
        out string? modulo,
        out PermissionAction action)
    {
        modulo = null;
        action = PermissionAction.Visualizar;

        if (context.User.Identity?.IsAuthenticated != true ||
            context.User.IsInRole("Administrador") ||
            context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            return false;
        }

        string path = context.Request.Path.Value ?? string.Empty;
        modulo = ModulePrefixes
            .Where(x => path.StartsWith(x.Key, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Key.Length)
            .Select(x => x.Value)
            .FirstOrDefault();

        if (modulo is null)
        {
            return false;
        }

        action = ResolverAcao(context, path);
        return true;
    }

    private static PermissionAction ResolverAcao(HttpContext context, string path)
    {
        if (HttpMethods.IsDelete(context.Request.Method))
        {
            return PermissionAction.Excluir;
        }

        if (HttpMethods.IsPut(context.Request.Method) || HttpMethods.IsPatch(context.Request.Method))
        {
            return PermissionAction.Editar;
        }

        string handler = context.Request.RouteValues["handler"]?.ToString()
            ?? context.Request.Query["handler"].ToString();

        if (handler.Contains("Excluir", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/Excluir", StringComparison.OrdinalIgnoreCase))
        {
            return PermissionAction.Excluir;
        }

        if (path.Contains("/Create", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/Criar", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/EntradaEstoque/Nova", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/EntradaEstoque/UploadXml", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/EntradaEstoque/Salvar", StringComparison.OrdinalIgnoreCase) ||
            handler.Contains("Criar", StringComparison.OrdinalIgnoreCase) ||
            handler.Contains("Finalizar", StringComparison.OrdinalIgnoreCase) ||
            handler.Contains("Abrir", StringComparison.OrdinalIgnoreCase))
        {
            return PermissionAction.Criar;
        }

        if (!HttpMethods.IsGet(context.Request.Method) ||
            path.Contains("/Edit", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/Editar", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/AjusteManual", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/Receber", StringComparison.OrdinalIgnoreCase) ||
            handler.Contains("Editar", StringComparison.OrdinalIgnoreCase) ||
            handler.Contains("Estornar", StringComparison.OrdinalIgnoreCase) ||
            handler.Contains("Fechar", StringComparison.OrdinalIgnoreCase) ||
            handler.Contains("Salvar", StringComparison.OrdinalIgnoreCase))
        {
            return PermissionAction.Editar;
        }

        return PermissionAction.Visualizar;
    }

    private static bool TryGetClaim(ClaimsPrincipal user, string claimName, out int value) =>
        int.TryParse(user.FindFirstValue(claimName), out value) && value > 0;

    private enum PermissionAction
    {
        Visualizar,
        Criar,
        Editar,
        Excluir
    }
}
