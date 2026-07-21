using GVC.Web.Data;
using GVC.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Financeiro.ContasPagar;

public class IndexModel(ErpDbContext db) : BasePageModel
{
    public IReadOnlyList<ContaAPagar> Itens { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken) => Itens = await db.ContasAPagar
        .AsNoTracking()
        .Include(x => x.Fornecedor)
        .Include(x => x.PlanoContas)
        .Where(x => x.EmpresaId == EmpresaId)
        .OrderBy(x => x.DataVencimento)
        .ToListAsync(cancellationToken);
}
