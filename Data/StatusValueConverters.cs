using GVC.Web.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GVC.Web.Data;

internal static class StatusValueConverters
{
    public static readonly ValueConverter<StatusVenda, string> Venda = new(
        status => VendaParaBanco(status),
        value => VendaDoBanco(value));

    public static readonly ValueConverter<StatusParcela, string> Parcela = new(
        status => ParcelaParaBanco(status),
        value => ParcelaDoBanco(value));

    private static string VendaParaBanco(StatusVenda status) => status switch
    {
        StatusVenda.Aberta => "Aberta",
        StatusVenda.Concluida => "Concluída",
        StatusVenda.AguardandoPagamento => "AguardandoPagamento",
        StatusVenda.Cancelada => "Cancelada",
        _ => throw new InvalidOperationException($"Status de venda inválido: {(int)status}.")
    };

    private static StatusVenda VendaDoBanco(string value) => value.Trim() switch
    {
        "Aberta" => StatusVenda.Aberta,
        "Concluída" or "Concluida" or "Finalizada" => StatusVenda.Concluida,
        "AguardandoPagamento" or "Aguardando Pagamento" => StatusVenda.AguardandoPagamento,
        "Cancelada" or "Cancelado" => StatusVenda.Cancelada,
        _ => throw new InvalidOperationException($"Status de venda desconhecido no banco: '{value}'.")
    };

    private static string ParcelaParaBanco(StatusParcela status) => status switch
    {
        StatusParcela.Pendente => "Pendente",
        StatusParcela.Pago => "Pago",
        StatusParcela.ParcialmentePago => "ParcialmentePago",
        StatusParcela.Atrasada => "Atrasada",
        StatusParcela.Cancelada => "Cancelada",
        _ => throw new InvalidOperationException($"Status de parcela inválido: {(int)status}.")
    };

    private static StatusParcela ParcelaDoBanco(string value) => value.Trim() switch
    {
        "Pendente" => StatusParcela.Pendente,
        "Pago" or "Paga" => StatusParcela.Pago,
        "ParcialmentePago" or "Pago Parcial" => StatusParcela.ParcialmentePago,
        "Atrasada" or "Atrasado" or "Vencida" or "Vencido" => StatusParcela.Atrasada,
        "Cancelada" or "Cancelado" => StatusParcela.Cancelada,
        _ => throw new InvalidOperationException($"Status de parcela desconhecido no banco: '{value}'.")
    };
}
