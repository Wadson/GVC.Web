using GVC.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Cidades;

public class BuscarModel(ErpDbContext db) : PageModel
{
    public async Task<IActionResult> OnGetAsync(string termo)
    {
        termo = (termo ?? string.Empty).Trim();

        if (termo.Length < 2)
            return new JsonResult(Array.Empty<object>());

        var cidades = await db.Cidades.AsNoTracking().Include(x => x.Estado)
            .Where(x => x.Nome.Contains(termo) || x.Estado.Uf == termo)
            .OrderBy(x => x.Nome).ThenBy(x => x.Estado.Uf).Take(20)
            .Select(x => new { id = x.CidadeId, nome = x.Nome, uf = x.Estado.Uf }).ToListAsync();

        return new JsonResult(cidades);
    }
}