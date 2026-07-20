using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("TokensRedefinicao")]
public class TokenRedefinicao
{
    [Key]
    public int TokenId
    {
        get; set;
    }

    public int UsuarioId
    {
        get; set;
    }

    [Required, StringLength(500)]
    public string Token { get; set; } = string.Empty;

    public DateTime DataExpiracao
    {
        get; set;
    }

    public Usuario Usuario { get; set; } = null!;
}