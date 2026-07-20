using System.Security.Claims;
using GVC.Web.Data;
using GVC.Web.DTOs;
using GVC.Web.Models;
using GVC.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Controllers;

[Authorize(Roles = "Administrador")]
[Route("Configuracoes")]
public class ConfiguracoesController(ErpDbContext db) : Controller
{
    private static readonly string[] ModulosDisponiveis =
    [
        "Cadastros",
        "Estoque",
        "Faturamento",
        "Fiscal",
        "Financeiro",
        "Segurança"
    ];

    [HttpGet("Permissoes/{usuarioId:int}")]
    public async Task<IActionResult> Permissoes(int usuarioId, CancellationToken cancellationToken)
    {
        if (!TryGetEmpresaId(out int empresaId))
        {
            return Forbid();
        }

        var usuario = await db.Usuarios
            .AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId && x.EmpresaId == empresaId)
            .Select(x => new { x.UsuarioId, x.NomeUsuario })
            .SingleOrDefaultAsync(cancellationToken);

        if (usuario is null)
        {
            return NotFound();
        }

        var permissoesCadastradas = await db.PermissoesUsuario
            .AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId && x.EmpresaId == empresaId)
            .ToListAsync(cancellationToken);

        var cadastradas = permissoesCadastradas
            .GroupBy(x => x.Modulo, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        var viewModel = new GerenciarPermissoesViewModel
        {
            UsuarioId = usuario.UsuarioId,
            NomeUsuario = usuario.NomeUsuario,
            Permissoes = ModulosDisponiveis.Select(modulo =>
            {
                if (cadastradas.TryGetValue(modulo, out PermissaoUsuario? permissao))
                {
                    return MapearDTO(permissao);
                }

                return CriarPermissaoPadrao(usuario.UsuarioId, empresaId, modulo);
            }).ToList()
        };

        return View(viewModel);
    }

    [HttpPost("SalvarPermissoes")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarPermissoes(
        GerenciarPermissoesViewModel viewModel,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmpresaId(out int empresaId))
        {
            return Forbid();
        }

        var usuario = await db.Usuarios
            .AsNoTracking()
            .Where(x => x.UsuarioId == viewModel.UsuarioId && x.EmpresaId == empresaId)
            .Select(x => new { x.UsuarioId, x.NomeUsuario })
            .SingleOrDefaultAsync(cancellationToken);

        if (usuario is null)
        {
            return NotFound();
        }

        var permissoesPostadas = viewModel.Permissoes
            .Where(x => ModulosDisponiveis.Contains(x.Modulo, StringComparer.OrdinalIgnoreCase))
            .GroupBy(x => x.Modulo, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var permissoesExistentes = await db.PermissoesUsuario
            .Where(x => x.UsuarioId == usuario.UsuarioId && x.EmpresaId == empresaId)
            .ToListAsync(cancellationToken);

        db.PermissoesUsuario.RemoveRange(permissoesExistentes);

        var novasPermissoes = ModulosDisponiveis.Select(modulo =>
        {
            permissoesPostadas.TryGetValue(modulo, out PermissaoUsuarioDTO? permissao);

            return new PermissaoUsuario
            {
                UsuarioId = usuario.UsuarioId,
                EmpresaId = empresaId,
                Modulo = modulo,
                PodeVisualizar = permissao?.PodeVisualizar ?? true,
                PodeCriar = permissao?.PodeCriar ?? false,
                PodeEditar = permissao?.PodeEditar ?? false,
                PodeExcluir = permissao?.PodeExcluir ?? false
            };
        });

        await db.PermissoesUsuario.AddRangeAsync(novasPermissoes, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        TempData["Success"] = $"Permissões de {usuario.NomeUsuario} atualizadas com sucesso.";

        return RedirectToAction(nameof(Permissoes), new { usuarioId = usuario.UsuarioId });
    }

    private bool TryGetEmpresaId(out int empresaId) =>
        int.TryParse(User.FindFirstValue("EmpresaID"), out empresaId) && empresaId > 0;

    private static PermissaoUsuarioDTO MapearDTO(PermissaoUsuario permissao) => new()
    {
        PermissaoId = permissao.PermissaoId,
        UsuarioId = permissao.UsuarioId,
        EmpresaId = permissao.EmpresaId,
        Modulo = permissao.Modulo,
        PodeVisualizar = permissao.PodeVisualizar,
        PodeCriar = permissao.PodeCriar,
        PodeEditar = permissao.PodeEditar,
        PodeExcluir = permissao.PodeExcluir
    };

    private static PermissaoUsuarioDTO CriarPermissaoPadrao(int usuarioId, int empresaId, string modulo) => new()
    {
        UsuarioId = usuarioId,
        EmpresaId = empresaId,
        Modulo = modulo,
        PodeVisualizar = true
    };
}
