using GVC.Web.Data;
using GVC.Web.Extensions;
using GVC.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Vendedores;

public class EditModel(ErpDbContext db) : BasePageModel
{
    [BindProperty]
    public Vendedor Item { get; set; } = null!;

    public string? CidadeNome
    {
        get; private set;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Item = await db.Vendedores.AsNoTracking().Include(x => x.Cidade).ThenInclude(x => x!.Estado).SingleOrDefaultAsync(x => x.VendedorId == id && x.EmpresaId == EmpresaId) ?? null!;

        if (Item is null)
            return NotFound();

        CidadeNome = Item.Cidade is null ? null : $"{Item.Cidade.Nome} - {Item.Cidade.Estado.Uf}";

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var entity = await db.Vendedores.SingleOrDefaultAsync(x => x.VendedorId == Item.VendedorId && x.EmpresaId == EmpresaId);

        if (entity is null)
            return NotFound();

        Item.Cpf = Item.Cpf.OnlyDigits();

        if (Item.Cpf.Length > 0 && Item.Cpf.Length != 11)
            ModelState.AddModelError("Item.Cpf", "CPF inválido.");

        if (Item.CidadeId.HasValue && !await db.Cidades.AnyAsync(x => x.CidadeId == Item.CidadeId))
            ModelState.AddModelError("Item.CidadeId", "Selecione uma cidade válida.");

        if (Item.Comissao is < 0 or > 100)
            ModelState.AddModelError("Item.Comissao", "A comissão deve estar entre 0 e 100.");

        if (!ModelState.IsValid)
            return Page();

        var id = entity.VendedorId;

        var empresa = entity.EmpresaId;

        var criado = entity.DataCriacao;

        var usuarioCriacao = entity.UsuarioCriacao;

        db.Entry(entity).CurrentValues.SetValues(Item);

        entity.VendedorId = id;

        entity.EmpresaId = empresa;

        entity.DataCriacao = criado;

        entity.UsuarioCriacao = usuarioCriacao;

        entity.DataAtualizacao = DateTime.Now;

        entity.UsuarioAtualizacao = User.Identity?.Name;

        await db.SaveChangesAsync();

        TempData["Success"] = "Vendedor alterado.";

        return RedirectToPage("Index");
    }
}