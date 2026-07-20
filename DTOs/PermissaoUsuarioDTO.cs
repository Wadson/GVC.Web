using System.ComponentModel.DataAnnotations;

namespace GVC.Web.DTOs;

public class PermissaoUsuarioDTO
{
    public int PermissaoId { get; set; }

    public int UsuarioId { get; set; }

    public int EmpresaId { get; set; }

    [Required]
    [StringLength(50)]
    public string Modulo { get; set; } = string.Empty;

    public bool PodeVisualizar { get; set; }

    public bool PodeCriar { get; set; }

    public bool PodeEditar { get; set; }

    public bool PodeExcluir { get; set; }
}
