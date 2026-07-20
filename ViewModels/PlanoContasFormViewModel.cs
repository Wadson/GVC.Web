using System.ComponentModel.DataAnnotations;

namespace GVC.Web.ViewModels;

public class PlanoContasFormViewModel
{
    public int PlanoContasId { get; set; }

    [Required(ErrorMessage = "Informe o código de classificação.")]
    [StringLength(20)]
    [Display(Name = "Código de classificação")]
    public string CodigoClassificacao { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a descrição.")]
    [StringLength(100)]
    public string Descricao { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecione o tipo.")]
    [RegularExpression("^[RD]$", ErrorMessage = "O tipo deve ser Receita ou Despesa.")]
    public string Tipo { get; set; } = "D";
}
