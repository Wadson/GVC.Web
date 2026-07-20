using GVC.Web.Data;
using GVC.Web.Extensions;
using GVC.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Clientes;

public class EditModel(ErpDbContext db) : BasePageModel
{
    [BindProperty]
    public Cliente Cliente { get; set; } = null!;

    public string? CidadeNome
    {
        get; private set;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Cliente = await db.Clientes.AsNoTracking().Include(x => x.Cidade).ThenInclude(x => x!.Estado).SingleOrDefaultAsync(x => x.ClienteId == id && x.EmpresaId == EmpresaId) ?? null!;

        if (Cliente is null)
            return NotFound();

        CidadeNome = Cliente.Cidade is null ? null : $"{Cliente.Cidade.Nome} - {Cliente.Cidade.Estado.Uf}";

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var item = await db.Clientes.SingleOrDefaultAsync(x => x.ClienteId == Cliente.ClienteId && x.EmpresaId == EmpresaId);

        if (item is null)
            return NotFound();

        Cliente.TipoCliente = Cliente.TipoCliente is "PJ" ? "PJ" : "PF";

        Cliente.Cpf = Cliente.Cpf.OnlyDigits();

        Cliente.Cnpj = Cliente.Cnpj.OnlyDigits();

        if (Cliente.TipoCliente == "PF" && Cliente.Cpf.Length != 11)
            ModelState.AddModelError("Cliente.Cpf", "CPF inválido.");

        if (Cliente.TipoCliente == "PJ" && Cliente.Cnpj.Length != 14)
            ModelState.AddModelError("Cliente.Cnpj", "CNPJ inválido.");

        if (Cliente.CidadeId.HasValue && !await db.Cidades.AnyAsync(x => x.CidadeId == Cliente.CidadeId))
            ModelState.AddModelError("Cliente.CidadeId", "Selecione uma cidade válida.");

        if (!ModelState.IsValid)
            return Page();

        item.Nome = Cliente.Nome;

        item.TipoCliente = Cliente.TipoCliente;

        item.Cpf = Cliente.TipoCliente == "PF" ? Cliente.Cpf : null;

        item.Rg = Cliente.TipoCliente == "PF" ? Cliente.Rg : null;

        item.OrgaoExpedidorRg = Cliente.TipoCliente == "PF" ? Cliente.OrgaoExpedidorRg : null;

        item.DataNascimento = Cliente.TipoCliente == "PF" ? Cliente.DataNascimento : null;

        item.Cnpj = Cliente.TipoCliente == "PJ" ? Cliente.Cnpj : null;

        item.Ie = Cliente.TipoCliente == "PJ" ? Cliente.Ie : null;

        item.Telefone = Cliente.Telefone;

        item.Email = Cliente.Email;

        item.CidadeId = Cliente.CidadeId;

        item.Logradouro = Cliente.Logradouro;

        item.Numero = Cliente.Numero;

        item.Bairro = Cliente.Bairro;

        item.Cep = Cliente.Cep;

        item.LimiteCredito = Cliente.LimiteCredito;

        item.Status = Cliente.Status;

        item.Observacoes = Cliente.Observacoes;

        item.DataAtualizacao = DateTime.Now;

        item.UsuarioAtualizacao = User.Identity?.Name;

        await db.SaveChangesAsync();

        TempData["Success"] = "Cliente alterado com sucesso.";

        return RedirectToPage("Index");
    }
}