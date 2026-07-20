using GVC.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Relatorios;

public class VendasModel(ErpDbContext db) : BasePageModel
{
    public decimal Total
    {
        get; private set;
    }

    public int Quantidade
    {
        get; private set;
    }

    public DateTime Inicio
    {
        get; private set;
    }

    public DateTime Fim
    {
        get; private set;
    }

    public async Task OnGetAsync(DateTime? inicio, DateTime? fim)
    {
        Inicio = inicio ?? DateTime.Today.AddDays(-30);

        Fim = fim ?? DateTime.Today;

        var q = db.Vendas.Where(x => x.EmpresaId == EmpresaId && x.DataVenda >= Inicio && x.DataVenda < Fim.AddDays(1) && x.StatusVenda == "Finalizada");

        Quantidade = await q.CountAsync();

        Total = await q.SumAsync(x => (decimal?)x.TotalLiquido) ?? 0;
    }
}