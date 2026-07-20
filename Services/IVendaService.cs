
namespace GVC.Web.Services;

public sealed record VendaItemInput(int ProdutoId, decimal Quantidade, decimal Desconto);

public sealed record ParcelaVendaInput(int Numero, DateTime DataVencimento, decimal Valor);

public sealed record FinalizarVendaInput(
    int? ClienteId,
    int? VendedorId,
    int FormaPagamentoId,
    decimal Desconto,
    bool RecebidoAgora,
    IReadOnlyCollection<VendaItemInput> Itens,
    IReadOnlyCollection<ParcelaVendaInput> Parcelas);

public interface IVendaService
{
    Task<long> FinalizarAsync(int empresaId, int usuarioId, FinalizarVendaInput input, CancellationToken cancellationToken);
}