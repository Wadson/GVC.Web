using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("ProdutoFornecedorMapeamento")]
public class ProdutoFornecedorMapeamento
{
    [Key, Column("MapeamentoID")]
    public int MapeamentoId { get; set; }

    [Column("EmpresaID")]
    public int EmpresaId { get; set; }

    [Column("FornecedorID")]
    public int FornecedorId { get; set; }

    [Required, StringLength(50)]
    public string CodigoNoFornecedor { get; set; } = string.Empty;

    [Column("ProdutoID")]
    public int ProdutoId { get; set; }

    public Empresa Empresa { get; set; } = null!;

    public Fornecedor Fornecedor { get; set; } = null!;

    public Produto Produto { get; set; } = null!;
}
