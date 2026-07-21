using System.ComponentModel.DataAnnotations;

namespace GVC.Web.ViewModels;

public sealed class FornecedorViewModel
{
    public int FornecedorId { get; set; }

    [Required(ErrorMessage = "Informe o nome ou a razão social.")]
    [StringLength(100)]
    [Display(Name = "Nome / Razão social")]
    public string Nome { get; set; } = string.Empty;

    [StringLength(18)]
    [Display(Name = "CNPJ")]
    public string? Cnpj { get; set; }

    [StringLength(20)]
    [Display(Name = "Inscrição estadual")]
    public string? Ie { get; set; }

    [StringLength(20)]
    public string? Telefone { get; set; }

    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(100)]
    [Display(Name = "E-mail")]
    public string? Email { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecione uma cidade válida.")]
    [Display(Name = "Cidade")]
    public int CidadeId { get; set; }

    [StringLength(150)]
    public string? Logradouro { get; set; }

    [StringLength(10)]
    [Display(Name = "Número")]
    public string? Numero { get; set; }

    [StringLength(100)]
    public string? Bairro { get; set; }

    [StringLength(10)]
    [Display(Name = "CEP")]
    public string? Cep { get; set; }

    [Display(Name = "Observações")]
    public string? Observacoes { get; set; }
}
