using GVC.Web.Data;
using GVC.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Usuarios;

public class IndexModel(ErpDbContext db) : BasePageModel
{
    public IReadOnlyList<Usuario> Usuarios { get; private set; } = [];

    public async Task OnGetAsync() => Usuarios = await db.Usuarios.AsNoTracking().Where(x => x.EmpresaId == EmpresaId).OrderBy(x => x.NomeCompleto).ToListAsync();

    public async Task<IActionResult> OnPostExcluirAsync(int id)
    {
        if (id == UsuarioId)
        {
            TempData["Error"] = "Você não pode excluir o usuário conectado.";

            return RedirectToPage();
        }

        var item = await db.Usuarios.SingleOrDefaultAsync(x => x.UsuarioId == id && x.EmpresaId == EmpresaId);

        if (item is null)
            return NotFound();

        try
        {
            db.Usuarios.Remove(item);

            await db.SaveChangesAsync();

            TempData["Success"] = "Usuário excluído.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "O usuário possui movimentações e não pode ser excluído.";
        }

        return RedirectToPage();
    }
}