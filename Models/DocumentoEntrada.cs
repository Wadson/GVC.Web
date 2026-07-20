using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("DocumentoEntrada")]
public class DocumentoEntrada
{
    [Key, Column("DocumentoEntradaID")]
    public int DocumentoEntradaId { get; set; }

    [Column("EmpresaID")]
    public int EmpresaId { get; set; }

    [Column("FornecedorID")]
    public int FornecedorId { get; set; }

    [Column("PedidoID")]
    public int? PedidoId { get; set; }

    [Required, StringLength(20)]
    public string TipoEntrada { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string NumeroDocumento { get; set; } = string.Empty;

    [StringLength(10)]
    public string? Serie { get; set; }

    [StringLength(44), Column(TypeName = "char(44)")]
    public string? ChaveAcesso { get; set; }

    [Column(TypeName = "date")]
    public DateTime DataEmissao { get; set; }

    public DateTime DataEntrada { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ValorTotalProdutos { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ValorFrete { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ValorDesconto { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ValorTotalNota { get; set; }

    public string? XmlConteudo { get; set; }

    public string? Observacao { get; set; }

    [Column("UsuarioID")]
    public int UsuarioId { get; set; }

    public Empresa Empresa { get; set; } = null!;

    public Fornecedor Fornecedor { get; set; } = null!;

    public ICollection<DocumentoEntradaItem> Itens { get; set; } = [];
}
