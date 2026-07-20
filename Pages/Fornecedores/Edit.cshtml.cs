using GVC.Web.Data;
using GVC.Web.Extensions;
using GVC.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Fornecedores;

public class EditModel(ErpDbContext db) : BasePageModel
{
    [BindProperty]
    public Fornecedor Item { get; set; } = null!;

    public string? CidadeNome
    {
        get; private set;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Item = await db.Fornecedores.AsNoTracking().Include(x => x.Cidade).ThenInclude(x => x.Estado).SingleOrDefaultAsync(x => x.FornecedorId == id && x.EmpresaId == EmpresaId) ?? null!;

        if (Item is null)
            return NotFound();

        CidadeNome = $"{Item.Cidade.Nome} - {Item.Cidade.Estado.Uf}";

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var entity = await db.Fornecedores.SingleOrDefaultAsync(x => x.FornecedorId == Item.FornecedorId && x.EmpresaId == EmpresaId);

        if (entity is null)
            return NotFound();

        Item.Cnpj = Item.Cnpj.OnlyDigits();

        if (Item.Cnpj.Length > 0 && Item.Cnpj.Length != 14)
            ModelState.AddModelError("Item.Cnpj", "CNPJ inválido.");

        if (!await db.Cidades.AnyAsync(x => x.CidadeId == Item.CidadeId))
            ModelState.AddModelError("Item.CidadeId", "Selecione uma cidade válida.");

        if (!ModelState.IsValid)
            return Page();

        entity.Nome = Item.Nome;

        entity.Cnpj = string.IsNullOrEmpty(Item.Cnpj) ? null : Item.Cnpj;

        entity.Ie = Item.Ie;

        entity.Telefone = Item.Telefone;

        entity.Email = Item.Email;

        entity.CidadeId = Item.CidadeId;

        entity.Logradouro = Item.Logradouro;

        entity.Numero = Item.Numero;

        entity.Bairro = Item.Bairro;

        entity.Cep = Item.Cep;

        entity.Observacoes = Item.Observacoes;

        await db.SaveChangesAsync();

        TempData["Success"] = "Fornecedor alterado.";

        return RedirectToPage("Index");
    }
}