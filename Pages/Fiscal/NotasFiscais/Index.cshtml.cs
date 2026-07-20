using GVC.Web.Data;
using GVC.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Fiscal.NotasFiscais;

public class IndexModel(ErpDbContext db) : BasePageModel
{
    public IReadOnlyList<FiscalDocumento> Itens { get; private set; } = [];

    public async Task OnGetAsync() => Itens = await db.DocumentosFiscais.AsNoTracking().Where(x => x.EmpresaId == EmpresaId).OrderByDescending(x => x.DataEmissao).ToListAsync();
}