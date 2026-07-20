using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("PermissaoUsuario")]
public class PermissaoUsuario
{
    [Key]
    [Column("PermissaoID")]
    public int PermissaoId { get; set; }

    [Column("UsuarioID")]
    public int UsuarioId { get; set; }

    [Column("EmpresaID")]
    public int EmpresaId { get; set; }

    [Required]
    [StringLength(50)]
    public string Modulo { get; set; } = string.Empty;

    public bool PodeVisualizar { get; set; }

    public bool PodeCriar { get; set; }

    public bool PodeEditar { get; set; }

    public bool PodeExcluir { get; set; }

    public Usuario Usuario { get; set; } = null!;

    public Empresa Empresa { get; set; } = null!;
}
