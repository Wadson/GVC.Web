using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("Vendedores")]
public class Vendedor
{
    [Key]
    public int VendedorId
    {
        get; set;
    }

    [Required, StringLength(100)]
    public string Nome { get; set; } = string.Empty;

    [StringLength(11)]
    public string? Cpf
    {
        get; set;
    }

    [StringLength(20)]
    public string? Rg
    {
        get; set;
    }

    [StringLength(20)]
    public string? Telefone
    {
        get; set;
    }

    [StringLength(100)]
    public string? Email
    {
        get; set;
    }

    [Column(TypeName = "date")]
    public DateTime? DataNascimento
    {
        get; set;
    }

    [Column(TypeName = "date")]
    public DateTime? DataContratacao
    {
        get; set;
    }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? Comissao
    {
        get; set;
    }

    public int? CidadeId
    {
        get; set;
    }

    [StringLength(200)]
    public string? Endereco
    {
        get; set;
    }

    [StringLength(10)]
    public string? Numero
    {
        get; set;
    }

    [StringLength(100)]
    public string? Bairro
    {
        get; set;
    }

    [StringLength(2), Column(TypeName = "char(2)")]
    public string? Uf
    {
        get; set;
    }

    [StringLength(10)]
    public string? Cep
    {
        get; set;
    }

    public string? Observacoes
    {
        get; set;
    }

    public int Status { get; set; } = 1;

    public int EmpresaId
    {
        get; set;
    }

    public DateTime DataCriacao
    {
        get; set;
    }

    public DateTime? DataAtualizacao
    {
        get; set;
    }

    [StringLength(50)]
    public string? UsuarioCriacao
    {
        get; set;
    }

    [StringLength(50)]
    public string? UsuarioAtualizacao
    {
        get; set;
    }

    public Cidade? Cidade
    {
        get; set;
    }

    public Empresa Empresa { get; set; } = null!;
}