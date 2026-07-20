using GVC.Web.Data;
using GVC.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Financeiro.Recebiveis;

public class IndexModel(ErpDbContext db) : BasePageModel
{
    public IReadOnlyList<Parcela> Itens { get; private set; } = [];

    public IReadOnlyList<string> StatusDisponiveis { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Cliente
    {
        get; set;
    }

    [BindProperty(SupportsGet = true)]
    public int? VendaId
    {
        get; set;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime? Inicio
    {
        get; set;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime? Fim
    {
        get; set;
    }

    [BindProperty(SupportsGet = true)]
    public List<string> StatusSelecionados { get; set; } = ["Pendente", "Atrasada", "ParcialmentePago"];

    [BindProperty(SupportsGet = true)]
    public string? Situacao
    {
        get; set;
    }

    public decimal TotalParcelas
    {
        get; private set;
    }

    public decimal TotalRecebido
    {
        get; private set;
    }

    public decimal SaldoAberto => TotalParcelas - TotalRecebido;

    public int Quantidade => Itens.Count;

    public int Vencidas => Itens.Count(x => x.DataVencimento < DateTime.Today && x.Status != "Pago" && x.Status != "Cancelada");

    public int Pendentes => Itens.Count(x => x.Status == "Pendente");

    public int ParcialmentePagas => Itens.Count(x => x.Status == "ParcialmentePago");

    public int Pagas => Itens.Count(x => x.Status == "Pago");

    public async Task OnGetAsync()
    {
        StatusDisponiveis = await db.Parcelas.AsNoTracking().Where(x => x.EmpresaId == EmpresaId)
            .Select(x => x.Status).Distinct().OrderBy(x => x).ToListAsync();

        var query = db.Parcelas.AsNoTracking().Include(x => x.Venda).ThenInclude(x => x.Cliente)
            .Where(x => x.EmpresaId == EmpresaId && x.Venda.EmpresaId == EmpresaId && x.Venda.Cliente.EmpresaId == EmpresaId);

        var cliente = Cliente?.Trim();

        if (!string.IsNullOrWhiteSpace(cliente))
            query = query.Where(x => x.Venda.Cliente.Nome.Contains(cliente));

        if (VendaId.HasValue)
            query = query.Where(x => x.VendaId == VendaId.Value);

        if (Inicio.HasValue)
            query = query.Where(x => x.DataVencimento >= Inicio.Value.Date);

        if (Fim.HasValue)
            query = query.Where(x => x.DataVencimento <= Fim.Value.Date);

        var statusValidos = StatusSelecionados.Intersect(StatusDisponiveis, StringComparer.OrdinalIgnoreCase).ToArray();

        if (statusValidos.Length > 0)
            query = query.Where(x => statusValidos.Contains(x.Status));

        if (Situacao == "Vencidas")
            query = query.Where(x => x.DataVencimento < DateTime.Today && x.Status != "Pago" && x.Status != "Cancelada");

        if (Situacao == "AVencer")
            query = query.Where(x => x.DataVencimento >= DateTime.Today && x.Status != "Pago" && x.Status != "Cancelada");

        Itens = await query.OrderBy(x => x.DataVencimento).ThenBy(x => x.VendaId).ThenBy(x => x.NumeroParcela).ToListAsync();

        TotalParcelas = Itens.Sum(x => x.ValorParcela);

        TotalRecebido = Itens.Sum(x => x.ValorRecebido ?? 0);
    }
}