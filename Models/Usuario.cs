using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("Usuarios")]
public class Usuario
{
    [Key]
    public int UsuarioId
    {
        get; set;
    }

    [Required, StringLength(20)]
    public string TipoUsuario { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string NomeCompleto { get; set; } = string.Empty;

    [Required, StringLength(14)]
    public string Cpf { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime DataNascimento
    {
        get; set;
    }

    [Required, StringLength(50)]
    public string NomeUsuario { get; set; } = string.Empty;

    [Required, StringLength(100), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(255)]
    public string Senha { get; set; } = string.Empty;

    public DateTime DataCriacao
    {
        get; set;
    }

    public int EmpresaId
    {
        get; set;
    }

    public Empresa Empresa { get; set; } = null!;
}