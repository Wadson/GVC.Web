using System.Security.Claims;
using GVC.Web.Data;
using GVC.Web.Models;
using GVC.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Controllers;

[Authorize]
[Route("PlanoContas")]
public class PlanoContasController(ErpDbContext db) : Controller
{
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryGetEmpresaId(out int empresaId))
            return Forbid();

        var planos = await db.PlanosContas.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId)
            .OrderBy(x => x.CodigoClassificacao)
            .ToListAsync(cancellationToken);
        return View(planos);
    }

    [HttpGet("Criar")]
    public IActionResult Criar()
    {
        if (!TryGetEmpresaId(out _))
            return Forbid();

        return View("Form", new PlanoContasFormViewModel());
    }

    [HttpPost("Criar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(PlanoContasFormViewModel viewModel, CancellationToken cancellationToken)
    {
        if (!TryGetEmpresaId(out int empresaId))
            return Forbid();

        Normalizar(viewModel);
        await ValidarCodigoUnicoAsync(viewModel, empresaId, cancellationToken);
        if (!ModelState.IsValid)
            return View("Form", viewModel);

        db.PlanosContas.Add(new PlanoContas
        {
            EmpresaId = empresaId,
            CodigoClassificacao = viewModel.CodigoClassificacao,
            Descricao = viewModel.Descricao,
            Tipo = viewModel.Tipo
        });
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Conta cadastrada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Editar/{id:int}")]
    public async Task<IActionResult> Editar(int id, CancellationToken cancellationToken)
    {
        if (!TryGetEmpresaId(out int empresaId))
            return Forbid();

        var conta = await db.PlanosContas.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PlanoContasId == id && x.EmpresaId == empresaId, cancellationToken);
        if (conta is null)
            return NotFound();

        return View("Form", new PlanoContasFormViewModel
        {
            PlanoContasId = conta.PlanoContasId,
            CodigoClassificacao = conta.CodigoClassificacao,
            Descricao = conta.Descricao,
            Tipo = conta.Tipo
        });
    }

    [HttpPost("Editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(PlanoContasFormViewModel viewModel, CancellationToken cancellationToken)
    {
        if (!TryGetEmpresaId(out int empresaId))
            return Forbid();

        Normalizar(viewModel);
        await ValidarCodigoUnicoAsync(viewModel, empresaId, cancellationToken);
        if (!ModelState.IsValid)
            return View("Form", viewModel);

        var conta = await db.PlanosContas
            .SingleOrDefaultAsync(x => x.PlanoContasId == viewModel.PlanoContasId && x.EmpresaId == empresaId, cancellationToken);
        if (conta is null)
            return NotFound();

        conta.CodigoClassificacao = viewModel.CodigoClassificacao;
        conta.Descricao = viewModel.Descricao;
        conta.Tipo = viewModel.Tipo;
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Conta alterada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Excluir/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Excluir(int id, CancellationToken cancellationToken)
    {
        if (!TryGetEmpresaId(out int empresaId))
            return Forbid();

        var conta = await db.PlanosContas
            .SingleOrDefaultAsync(x => x.PlanoContasId == id && x.EmpresaId == empresaId, cancellationToken);
        if (conta is null)
            return NotFound();

        bool possuiLancamentos = await db.ContasAPagar.AsNoTracking()
            .AnyAsync(x => x.EmpresaId == empresaId && x.PlanoContasId == id, cancellationToken);
        if (possuiLancamentos)
        {
            TempData["Error"] = "Esta conta possui lançamentos financeiros e não pode ser excluída.";
            return RedirectToAction(nameof(Index));
        }

        db.PlanosContas.Remove(conta);
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = "Conta excluída com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidarCodigoUnicoAsync(
        PlanoContasFormViewModel viewModel,
        int empresaId,
        CancellationToken cancellationToken)
    {
        if (await db.PlanosContas.AsNoTracking().AnyAsync(x =>
                x.EmpresaId == empresaId &&
                x.CodigoClassificacao == viewModel.CodigoClassificacao &&
                x.PlanoContasId != viewModel.PlanoContasId,
            cancellationToken))
        {
            ModelState.AddModelError(nameof(viewModel.CodigoClassificacao), "Já existe uma conta com este código.");
        }
    }

    private static void Normalizar(PlanoContasFormViewModel viewModel)
    {
        viewModel.CodigoClassificacao = viewModel.CodigoClassificacao?.Trim() ?? string.Empty;
        viewModel.Descricao = viewModel.Descricao?.Trim() ?? string.Empty;
        viewModel.Tipo = viewModel.Tipo?.Trim().ToUpperInvariant() ?? string.Empty;
    }

    private bool TryGetEmpresaId(out int empresaId) =>
        int.TryParse(User.FindFirstValue("EmpresaID"), out empresaId) && empresaId > 0;
}
