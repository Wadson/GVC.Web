using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("Empresa")]
public class Empresa
{
    [Key]
    public int EmpresaId
    {
        get; set;
    }

    [Required, StringLength(150)]
    public string RazaoSocial { get; set; } = string.Empty;

    [StringLength(150)]
    public string? NomeFantasia
    {
        get; set;
    }

    [Required, StringLength(14)]
    public string Cnpj { get; set; } = string.Empty;

    [StringLength(20)]
    public string? InscricaoEstadual
    {
        get; set;
    }

    [StringLength(20)]
    public string? InscricaoMunicipal
    {
        get; set;
    }

    [StringLength(10)]
    public string? Cnae
    {
        get; set;
    }

    public int AmbienteSefaz { get; set; } = 2;

    public int RegimeTributario { get; set; } = 1;

    [StringLength(200)]
    public string? CertificadoDigital
    {
        get; set;
    }

    [StringLength(100)]
    public string? CertificadoSenha
    {
        get; set;
    }

    [Required, StringLength(150)]
    public string Logradouro { get; set; } = string.Empty;

    [StringLength(10)]
    public string? Numero
    {
        get; set;
    }

    [Required, StringLength(100)]
    public string Bairro { get; set; } = string.Empty;

    [Required, StringLength(10)]
    public string Cep { get; set; } = string.Empty;

    public int CidadeId
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

    [StringLength(100)]
    public string? Site
    {
        get; set;
    }

    [StringLength(100)]
    public string? Responsavel
    {
        get; set;
    }

    public byte[]? Logo
    {
        get; set;
    }

    public byte[]? FundoTelaImagem
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

    public Cidade Cidade { get; set; } = null!;
}