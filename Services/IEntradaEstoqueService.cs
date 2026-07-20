using GVC.Web.ViewModels;

namespace GVC.Web.Services;

public interface IEntradaEstoqueService
{
    Task<EntradaEstoqueViewModel> ProcessarXmlAsync(
        int empresaId,
        Stream xmlStream,
        CancellationToken cancellationToken);

    Task<int> SalvarAsync(
        int empresaId,
        int usuarioId,
        string usuarioNome,
        EntradaEstoqueViewModel input,
        CancellationToken cancellationToken);
}
