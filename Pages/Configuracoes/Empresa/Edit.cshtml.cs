using GVC.Web.Data;
using GVC.Web.Extensions;
using GVC.Web.Models;
using GVC.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Configuracoes.Empresa;

public class EditModel(ErpDbContext db) : BasePageModel
{
    [BindProperty]
    public Models.Empresa Item { get; set; } = null!;

    [BindProperty]
    public IFormFile? LogoArquivo { get; set; }

    [BindProperty]
    public IFormFile? FundoTelaArquivo { get; set; }

    public string? CidadeNome
    {
        get; private set;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        Item = await db.Empresas.AsNoTracking().Include(x => x.Cidade).ThenInclude(x => x.Estado).SingleAsync(x => x.EmpresaId == EmpresaId);

        CidadeNome = $"{Item.Cidade.Nome} - {Item.Cidade.Estado.Uf}";

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var entity = await db.Empresas.SingleAsync(x => x.EmpresaId == EmpresaId);

        Item.Cnpj = Item.Cnpj.OnlyDigits();

        Item.Cep = Item.Cep.OnlyDigits();

        if (Item.Cnpj.Length != 14)
            ModelState.AddModelError("Item.Cnpj", "CNPJ inválido.");

        if (!await db.Cidades.AnyAsync(x => x.CidadeId == Item.CidadeId))
            ModelState.AddModelError("Item.CidadeId", "Selecione uma cidade válida.");

        string? logoError = CompanyImageStorage.ValidateLogo(LogoArquivo);
        if (logoError is not null)
            ModelState.AddModelError(nameof(LogoArquivo), logoError);

        string? fundoError = CompanyImageStorage.ValidateBackground(FundoTelaArquivo);
        if (fundoError is not null)
            ModelState.AddModelError(nameof(FundoTelaArquivo), fundoError);

        if (!ModelState.IsValid)
            return Page();

        entity.RazaoSocial = Item.RazaoSocial;

        entity.NomeFantasia = Item.NomeFantasia;

        entity.Cnpj = Item.Cnpj;

        entity.InscricaoEstadual = Item.InscricaoEstadual;

        entity.InscricaoMunicipal = Item.InscricaoMunicipal;

        entity.Cnae = Item.Cnae;

        entity.AmbienteSefaz = Item.AmbienteSefaz;

        entity.RegimeTributario = Item.RegimeTributario;

        entity.CertificadoDigital = Item.CertificadoDigital;

        if (!string.IsNullOrWhiteSpace(Item.CertificadoSenha))
            entity.CertificadoSenha = Item.CertificadoSenha;

        entity.Logradouro = Item.Logradouro;

        entity.Numero = Item.Numero;

        entity.Bairro = Item.Bairro;

        entity.Cep = Item.Cep;

        entity.CidadeId = Item.CidadeId;

        entity.Telefone = Item.Telefone;

        entity.Email = Item.Email;

        entity.Site = Item.Site;

        entity.Responsavel = Item.Responsavel;

        if (LogoArquivo is not null && LogoArquivo.Length > 0)
            entity.Logo = await CompanyImageStorage.ReadAsync(LogoArquivo, cancellationToken);

        if (FundoTelaArquivo is not null && FundoTelaArquivo.Length > 0)
            entity.FundoTelaImagem = await CompanyImageStorage.ReadAsync(FundoTelaArquivo, cancellationToken);

        entity.DataAtualizacao = DateTime.Now;

        entity.UsuarioAtualizacao = User.Identity?.Name;

        await db.SaveChangesAsync(cancellationToken);

        TempData["Success"] = "Empresa alterada.";

        return RedirectToPage("Index");
    }
}
