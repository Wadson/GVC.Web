using GVC.Web.DTOs;

namespace GVC.Web.ViewModels;

public class GerenciarPermissoesViewModel
{
    public int UsuarioId { get; set; }

    public string NomeUsuario { get; set; } = string.Empty;

    public List<PermissaoUsuarioDTO> Permissoes { get; set; } = [];
}
