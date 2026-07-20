using GVC.Web.DTOs;

namespace GVC.Web.ViewModels;

public class DashboardViewModel
{
    public decimal VendasHoje { get; set; }

    public decimal ContasAPagarHoje { get; set; }

    public decimal MargemMediaMes { get; set; }

    public int CaixasAbertosPendentes { get; set; }

    public int NotasRejeitadas { get; set; }

    public int PedidosAtrasados { get; set; }

    public List<FaturamentoDiarioDTO> FaturamentoUltimos30Dias { get; set; } = [];

    public List<TarefaPendenteDTO> TarefasPendentes { get; set; } = [];
}
