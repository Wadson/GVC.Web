namespace GVC.Web.DTOs;

public sealed record EntradaEstoqueOptionDTO(int Id, string Descricao);

public sealed record EntradaEstoqueProdutoOptionDTO(
    int Id,
    int? VariacaoID,
    string Descricao,
    decimal PrecoCompra,
    decimal PrecoCusto)
{
    public string Codigo => VariacaoID.HasValue ? $"V{VariacaoID}" : $"P{Id}";
}
