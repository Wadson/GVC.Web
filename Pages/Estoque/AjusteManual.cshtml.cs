using GVC.Web.Data;
using GVC.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Estoque;

public class AjusteManualModel(ErpDbContext db) : BasePageModel
{
    [BindProperty]
    public int ProdutoId
    {
        get; set;
    }

    [BindProperty]
    public int NovoEstoque
    {
        get; set;
    }

    [BindProperty]
    public string? Observacao
    {
        get; set;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var p = await db.Produtos.SingleOrDefaultAsync(x => x.ProdutoId == ProdutoId && x.EmpresaId == EmpresaId);

        if (p is null)
            return NotFound();

        if (NovoEstoque < 0)
        {
            ModelState.AddModelError(nameof(NovoEstoque), "O estoque não pode ser negativo.");

            return Page();
        }

        var anterior = p.Estoque;

        p.Estoque = NovoEstoque;

        db.MovimentacoesEstoque.Add(new MovimentacaoEstoque { EmpresaId = EmpresaId, ProdutoId = p.ProdutoId, TipoMovimentacao = "AJUSTE", Quantidade = Math.Abs(NovoEstoque - anterior), EstoqueAnterior = anterior, EstoqueAtual = NovoEstoque, Origem = "Ajuste Manual", Observacao = Observacao, Usuario = UsuarioId.ToString(), DataMovimentacao = DateTime.Now });

        await db.SaveChangesAsync();

        return RedirectToPage("Extrato");
    }
}