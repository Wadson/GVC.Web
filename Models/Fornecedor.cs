using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("Fornecedor")]
public class Fornecedor
{
    [Key]
    public int FornecedorId
    {
        get; set;
    }

    [Required, StringLength(100)]
    public string Nome { get; set; } = string.Empty;

    [StringLength(14)]
    public string? Cnpj
    {
        get; set;
    }

    [StringLength(20)]
    public string? Ie
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

    public int CidadeId
    {
        get; set;
    }

    [StringLength(150)]
    public string? Logradouro
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

    [StringLength(10)]
    public string? Cep
    {
        get; set;
    }

    public string? Observacoes
    {
        get; set;
    }

    public int EmpresaId
    {
        get; set;
    }

    public DateTime? DataCriacao
    {
        get; set;
    }

    public Cidade Cidade { get; set; } = null!;

    public Empresa Empresa { get; set; } = null!;
}