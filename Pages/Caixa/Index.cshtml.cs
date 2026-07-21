using GVC.Web.Data;
using GVC.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace GVC.Web.Pages.Caixa;

public class IndexModel(ErpDbContext db) : BasePageModel
{
    public Models.Caixa? CaixaAtual
    {
        get; private set;
    }

    public decimal Saldo
    {
        get; private set;
    }

    [BindProperty]
    public decimal SaldoInicial
    {
        get; set;
    }

    public async Task OnGetAsync() => await Load();

    public async Task<IActionResult> OnPostAbrirAsync()
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (await db.Caixas.AnyAsync(x => x.EmpresaId == EmpresaId &&
            x.UsuarioAberturaId == UsuarioId && x.DataCaixa == DateTime.Today && x.Status == "Aberto"))
        {
            ModelState.AddModelError(string.Empty, "Já existe um caixa aberto.");

            await Load();

            return Page();
        }

        db.Caixas.Add(new Models.Caixa { EmpresaId = EmpresaId, UsuarioAberturaId = UsuarioId, DataCaixa = DateTime.Today, DataAbertura = DateTime.Now, SaldoInicial = SaldoInicial });

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostFecharAsync(decimal saldoInformado)
    {
        var caixa = await db.Caixas.SingleOrDefaultAsync(x => x.EmpresaId == EmpresaId &&
            x.UsuarioAberturaId == UsuarioId && x.DataCaixa == DateTime.Today && x.Status == "Aberto");

        if (caixa is null)
            return RedirectToPage();

        var mov = await db.CaixaMovimentos.Where(x => x.CaixaId == caixa.CaixaId && x.EmpresaId == EmpresaId)
            .SumAsync(x => (decimal?)(x.Tipo == "ENTRADA" ? x.Valor : -x.Valor)) ?? 0;

        caixa.SaldoFinalSistema = caixa.SaldoInicial + mov;

        caixa.SaldoFinalInformado = saldoInformado;

        caixa.Diferenca = saldoInformado - caixa.SaldoFinalSistema;

        caixa.Status = "Fechado";

        caixa.DataFechamento = DateTime.Now;

        caixa.UsuarioFechamentoId = UsuarioId;

        await db.SaveChangesAsync();

        return RedirectToPage();
    }

    private async Task Load()
    {
        CaixaAtual = await db.Caixas.AsNoTracking().SingleOrDefaultAsync(x => x.EmpresaId == EmpresaId &&
            x.UsuarioAberturaId == UsuarioId && x.DataCaixa == DateTime.Today && x.Status == "Aberto");

        if (CaixaAtual is not null)
            Saldo = CaixaAtual.SaldoInicial + (await db.CaixaMovimentos
                .Where(x => x.CaixaId == CaixaAtual.CaixaId && x.EmpresaId == EmpresaId)
                .SumAsync(x => (decimal?)(x.Tipo == "ENTRADA" ? x.Valor : -x.Valor)) ?? 0);
    }
}
