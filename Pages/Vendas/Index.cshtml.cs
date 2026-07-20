using GVC.Web.Data;
using GVC.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Vendas;

public class IndexModel(ErpDbContext db) : BasePageModel
{
    public IReadOnlyList<Venda> Vendas { get; private set; } = [];

    public async Task OnGetAsync() => Vendas = await db.Vendas.AsNoTracking().Include(x => x.Cliente).Where(x => x.EmpresaId == EmpresaId).OrderByDescending(x => x.DataVenda).Take(200).ToListAsync();
}