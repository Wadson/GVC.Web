namespace GVC.Web.Models;

public static class StatusExtensions
{
    public static string Descricao(this StatusVenda status) => status switch
    {
        StatusVenda.Aberta => "Aberta",
        StatusVenda.Concluida => "Concluída",
        StatusVenda.AguardandoPagamento => "Aguardando pagamento",
        StatusVenda.Cancelada => "Cancelada",
        _ => status.ToString()
    };

    public static string Descricao(this StatusParcela status) => status switch
    {
        StatusParcela.Pendente => "Pendente",
        StatusParcela.Pago => "Pago",
        StatusParcela.ParcialmentePago => "Parcialmente pago",
        StatusParcela.Atrasada => "Atrasada",
        StatusParcela.Cancelada => "Cancelada",
        _ => status.ToString()
    };
}
