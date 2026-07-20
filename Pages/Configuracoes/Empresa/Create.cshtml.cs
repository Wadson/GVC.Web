using GVC.Web.Data;
using GVC.Web.Extensions;
using GVC.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Configuracoes.Empresa;

public class CreateModel(ErpDbContext db) : BasePageModel
{
    [BindProperty]
    public Models.Empresa Item { get; set; } = new();

    [BindProperty]
    public IFormFile? LogoArquivo { get; set; }

    [BindProperty]
    public IFormFile? FundoTelaArquivo { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Item.Cnpj = Item.Cnpj.OnlyDigits();
        Item.Cep = Item.Cep.OnlyDigits();

        if (Item.Cnpj.Length != 14)
        {
            ModelState.AddModelError("Item.Cnpj", "CNPJ inválido.");
        }
        else if (await db.Empresas.AnyAsync(x => x.Cnpj == Item.Cnpj, cancellationToken))
        {
            ModelState.AddModelError("Item.Cnpj", "Já existe uma empresa cadastrada com este CNPJ.");
        }

        if (!await db.Cidades.AnyAsync(x => x.CidadeId == Item.CidadeId, cancellationToken))
        {
            ModelState.AddModelError("Item.CidadeId", "Selecione uma cidade válida.");
        }

        string? logoError = CompanyImageStorage.ValidateLogo(LogoArquivo);
        if (logoError is not null)
        {
            ModelState.AddModelError(nameof(LogoArquivo), logoError);
        }

        string? fundoError = CompanyImageStorage.ValidateBackground(FundoTelaArquivo);
        if (fundoError is not null)
        {
            ModelState.AddModelError(nameof(FundoTelaArquivo), fundoError);
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        Item.DataCriacao = DateTime.Now;
        Item.UsuarioCriacao = User.Identity?.Name;
        Item.DataAtualizacao = null;
        Item.UsuarioAtualizacao = null;
        Item.Logo = await CompanyImageStorage.ReadAsync(LogoArquivo, cancellationToken);
        Item.FundoTelaImagem = await CompanyImageStorage.ReadAsync(FundoTelaArquivo, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Empresas.Add(Item);
        await db.SaveChangesAsync(cancellationToken);
        await DbInitializer.SeedPlanoContasAsync(db, Item.EmpresaId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        TempData["Success"] = $"Empresa {Item.NomeFantasia ?? Item.RazaoSocial} cadastrada com sucesso.";

        return RedirectToPage("Index");
    }
}
