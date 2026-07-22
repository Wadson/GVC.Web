using GVC.Web.Data;
using GVC.Web.Extensions;
using GVC.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Clientes;

public class CreateModel(ErpDbContext db) : BasePageModel
{
    [BindProperty]
    public Cliente Cliente { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Cliente.TipoCliente = Cliente.TipoCliente is "PJ" ? "PJ" : "PF";

        Cliente.Cpf = Cliente.Cpf.OnlyDigits();

        Cliente.Cnpj = Cliente.Cnpj.OnlyDigits();

        if (Cliente.TipoCliente == "PF" && Cliente.Cpf.Length > 0 && Cliente.Cpf.Length != 11)
            ModelState.AddModelError("Cliente.Cpf", "CPF inválido.");

        if (Cliente.TipoCliente == "PJ" && Cliente.Cnpj.Length > 0 && Cliente.Cnpj.Length != 14)
            ModelState.AddModelError("Cliente.Cnpj", "CNPJ inválido.");

        Cliente.EmpresaId = EmpresaId;

        if (Cliente.CidadeId.HasValue && !await db.Cidades.AnyAsync(x => x.CidadeId == Cliente.CidadeId))
            ModelState.AddModelError("Cliente.CidadeId", "Selecione uma cidade válida.");

        if (!ModelState.IsValid)
            return Page();

        Cliente.UsuarioCriacao = User.Identity?.Name;

        Cliente.DataCriacao = DateTime.Now;

        if (Cliente.TipoCliente == "PF")
        {
            Cliente.Cpf = string.IsNullOrEmpty(Cliente.Cpf) ? null : Cliente.Cpf;

            Cliente.Cnpj = null;

            Cliente.Ie = null;
        }
        else
        {
            Cliente.Cnpj = string.IsNullOrEmpty(Cliente.Cnpj) ? null : Cliente.Cnpj;

            Cliente.Cpf = null;

            Cliente.DataNascimento = null;
        }

        db.Clientes.Add(Cliente);

        await db.SaveChangesAsync();

        TempData["Success"] = "Cliente cadastrado com sucesso.";

        return RedirectToPage("Index");
    }
}
