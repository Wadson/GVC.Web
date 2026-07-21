using GVC.Web.Data;
using GVC.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Pages.Financeiro.Recebiveis;

public class IndexModel(ErpDbContext db) : BasePageModel
{
    public IReadOnlyList<Parcela> Itens { get; private set; } = [];

    public IReadOnlyList<StatusParcela> StatusDisponiveis { get; private set; } =
        Enum.GetValues<StatusParcela>();

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
    public List<StatusParcela> StatusSelecionados { get; set; } =
        [StatusParcela.Pendente, StatusParcela.Atrasada, StatusParcela.ParcialmentePago];

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

    public int Vencidas => Itens.Count(x => x.StatusAtual == StatusParcela.Atrasada);

    public int Pendentes => Itens.Count(x => x.StatusAtual == StatusParcela.Pendente);

    public int ParcialmentePagas => Itens.Count(x => x.Status == StatusParcela.ParcialmentePago);

    public int Pagas => Itens.Count(x => x.Status == StatusParcela.Pago);

    public async Task OnGetAsync()
    {
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

        var statusValidos = StatusSelecionados.Intersect(StatusDisponiveis).ToArray();

        if (statusValidos.Length > 0)
        {
            bool pendente = statusValidos.Contains(StatusParcela.Pendente);
            bool atrasada = statusValidos.Contains(StatusParcela.Atrasada);
            bool parcial = statusValidos.Contains(StatusParcela.ParcialmentePago);
            bool paga = statusValidos.Contains(StatusParcela.Pago);
            bool cancelada = statusValidos.Contains(StatusParcela.Cancelada);
            DateTime hoje = DateTime.Today;

            query = query.Where(x =>
                (pendente && x.Status == StatusParcela.Pendente && x.DataVencimento >= hoje) ||
                (atrasada && ((x.Status == StatusParcela.Pendente && x.DataVencimento < hoje) || x.Status == StatusParcela.Atrasada)) ||
                (parcial && x.Status == StatusParcela.ParcialmentePago) ||
                (paga && x.Status == StatusParcela.Pago) ||
                (cancelada && x.Status == StatusParcela.Cancelada));
        }

        if (Situacao == "Vencidas")
            query = query.Where(x => x.DataVencimento < DateTime.Today &&
                (x.Status == StatusParcela.Pendente || x.Status == StatusParcela.Atrasada || x.Status == StatusParcela.ParcialmentePago));

        if (Situacao == "AVencer")
            query = query.Where(x => x.DataVencimento >= DateTime.Today &&
                (x.Status == StatusParcela.Pendente || x.Status == StatusParcela.ParcialmentePago));

        Itens = await query.OrderBy(x => x.DataVencimento).ThenBy(x => x.VendaId).ThenBy(x => x.NumeroParcela).ToListAsync();

        TotalParcelas = Itens.Sum(x => x.ValorParcela);

        TotalRecebido = Itens.Sum(x => x.ValorRecebido ?? 0);
    }
}
