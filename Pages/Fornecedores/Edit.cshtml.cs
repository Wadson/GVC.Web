using GVC.Web.Data;
using GVC.Web.Extensions;
using GVC.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Fornecedores;

public class EditModel(ErpDbContext db) : BasePageModel
{
    [BindProperty]
    public FornecedorViewModel Item { get; set; } = new();

    public string? CidadeNome
    {
        get; private set;
    }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await db.Fornecedores
            .AsNoTracking()
            .Include(x => x.Cidade)
            .ThenInclude(x => x.Estado)
            .SingleOrDefaultAsync(
                x => x.FornecedorId == id && x.EmpresaId == EmpresaId,
                cancellationToken);

        if (entity is null)
            return NotFound();

        Item = new FornecedorViewModel
        {
            FornecedorId = entity.FornecedorId,
            Nome = entity.Nome,
            Cnpj = entity.Cnpj,
            Ie = entity.Ie,
            Telefone = entity.Telefone,
            Email = entity.Email,
            CidadeId = entity.CidadeId,
            Logradouro = entity.Logradouro,
            Numero = entity.Numero,
            Bairro = entity.Bairro,
            Cep = entity.Cep,
            Observacoes = entity.Observacoes
        };

        CidadeNome = $"{entity.Cidade.Nome} - {entity.Cidade.Estado.Uf}";

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var entity = await db.Fornecedores.SingleOrDefaultAsync(
            x => x.FornecedorId == Item.FornecedorId && x.EmpresaId == EmpresaId,
            cancellationToken);

        if (entity is null)
            return NotFound();

        Item.Cnpj = Item.Cnpj.OnlyDigits();

        if (Item.Cnpj.Length > 0 && Item.Cnpj.Length != 14)
            ModelState.AddModelError("Item.Cnpj", "CNPJ inválido.");

        if (!await db.Cidades.AnyAsync(x => x.CidadeId == Item.CidadeId, cancellationToken))
            ModelState.AddModelError("Item.CidadeId", "Selecione uma cidade válida.");

        if (!ModelState.IsValid)
        {
            await CarregarCidadeNomeAsync(cancellationToken);
            return Page();
        }

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

        await db.SaveChangesAsync(cancellationToken);

        TempData["Success"] = "Fornecedor atualizado com sucesso!";

        return RedirectToPage("Index");
    }

    private async Task CarregarCidadeNomeAsync(CancellationToken cancellationToken)
    {
        CidadeNome = await db.Cidades
            .AsNoTracking()
            .Where(x => x.CidadeId == Item.CidadeId)
            .Select(x => x.Nome + " - " + x.Estado.Uf)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
