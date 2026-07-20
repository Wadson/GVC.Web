using GVC.Web.Data;
using GVC.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Configuracoes.Empresa;

public class IndexModel(ErpDbContext db) : BasePageModel
{
    public Models.Empresa Item { get; private set; } = null!;

    public async Task OnGetAsync() => Item = await db.Empresas.AsNoTracking().SingleAsync(x => x.EmpresaId == EmpresaId);
}