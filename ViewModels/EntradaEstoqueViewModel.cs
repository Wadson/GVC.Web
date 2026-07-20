using System.ComponentModel.DataAnnotations;
using GVC.Web.DTOs;

namespace GVC.Web.ViewModels;

public class EntradaEstoqueViewModel
{
    public bool OrigemXml { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecione o fornecedor.")]
    public int FornecedorId { get; set; }

    public string? FornecedorXml { get; set; }

    public int? PedidoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecione o plano de contas.")]
    public int PlanoContasId { get; set; }

    [Required, StringLength(50)]
    public string NumeroDocumento { get; set; } = string.Empty;

    [StringLength(10)]
    public string? Serie { get; set; }

    [StringLength(44)]
    public string? ChaveAcesso { get; set; }

    [DataType(DataType.Date)]
    public DateTime DataEmissao { get; set; } = DateTime.Today;

    public decimal ValorTotalProdutos { get; set; }

    public decimal ValorFrete { get; set; }

    public decimal ValorDesconto { get; set; }

    public decimal ValorTotalNota { get; set; }

    public string? XmlConteudo { get; set; }

    public string? Observacao { get; set; }

    public List<EntradaEstoqueItemDTO> Itens { get; set; } = [];

    public List<ParcelaEntradaDTO> Parcelas { get; set; } = [];

    public IReadOnlyList<EntradaEstoqueOptionDTO> Fornecedores { get; set; } = [];

    public IReadOnlyList<EntradaEstoqueOptionDTO> Pedidos { get; set; } = [];

    public IReadOnlyList<EntradaEstoqueOptionDTO> PlanosContas { get; set; } = [];

    public IReadOnlyList<EntradaEstoqueProdutoOptionDTO> Produtos { get; set; } = [];
}
