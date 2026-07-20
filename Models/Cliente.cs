using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("Clientes")]
public class Cliente
{
    [Key]
    public int ClienteId
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
    public string? OrgaoExpedidorRg
    {
        get; set;
    }

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

    [StringLength(100), EmailAddress]
    public string? Email
    {
        get; set;
    }

    public int? CidadeId
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

    [Column(TypeName = "date")]
    public DateTime? DataNascimento
    {
        get; set;
    }

    [StringLength(20)]
    public string? TipoCliente
    {
        get; set;
    }

    public int Status { get; set; } = 1;

    public string? Observacoes
    {
        get; set;
    }

    public DateTime? DataUltimaCompra
    {
        get; set;
    }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? LimiteCredito
    {
        get; set;
    }

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