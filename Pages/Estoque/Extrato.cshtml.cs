using GVC.Web.Data;
using GVC.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Estoque;

public class ExtratoModel(ErpDbContext db) : BasePageModel
{
    public IReadOnlyList<MovimentacaoEstoque> Movimentos { get; private set; } = [];

    public async Task OnGetAsync() => Movimentos = await db.MovimentacoesEstoque.AsNoTracking()
        .Include(x => x.Produto).Include(x => x.Variacao)
        .Where(x => x.EmpresaId == EmpresaId)
        .OrderByDescending(x => x.DataMovimentacao).Take(500).ToListAsync();
}
