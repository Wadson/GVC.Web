namespace GVC.Web.DTOs;

public sealed record EntradaEstoqueOptionDTO(int Id, string Descricao);

public sealed record EntradaEstoqueProdutoOptionDTO(
    int Id,
    string Descricao,
    decimal PrecoCompra,
    decimal PrecoCusto);
