using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("ContasAPagar")]
public class ContaAPagar
{
    [Key, Column("ContasAPagarID")]
    public int ContasAPagarId
    {
        get; set;
    }

    public int EmpresaId
    {
        get; set;
    }

    public int? FornecedorId
    {
        get; set;
    }

    public int? PedidoId
    {
        get; set;
    }

    [Column("DocumentoEntradaID")]
    public int? DocumentoEntradaId { get; set; }

    public int PlanoContasId
    {
        get; set;
    }

    [Required, StringLength(200)]
    public string Descricao { get; set; } = string.Empty;

    [StringLength(50)]
    public string? NumeroDocumento
    {
        get; set;
    }

    [Column(TypeName = "date")]
    public DateTime DataEmissao
    {
        get; set;
    }

    [Column(TypeName = "date")]
    public DateTime DataVencimento
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Valor
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ValorPago
    {
        get; set;
    }

    [Column(TypeName = "date")]
    public DateTime? DataPagamento
    {
        get; set;
    }

    [Required, StringLength(20)]
    public string Status { get; set; } = "Pendente";

    public int? FormaPgtoId
    {
        get; set;
    }

    public string? Observacoes
    {
        get; set;
    }

    public Empresa Empresa { get; set; } = null!;

    public Fornecedor? Fornecedor
    {
        get; set;
    }

    public Pedido? Pedido
    {
        get; set;
    }

    public DocumentoEntrada? DocumentoEntrada { get; set; }

    public PlanoContas PlanoContas { get; set; } = null!;

    public FormaPagamento? FormaPagamento
    {
        get; set;
    }
}
