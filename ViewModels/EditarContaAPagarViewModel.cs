using System.ComponentModel.DataAnnotations;
using GVC.Web.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace GVC.Web.ViewModels;

public sealed class EditarContaAPagarViewModel
{
    public int ContasAPagarId { get; set; }

    [Required, StringLength(200)]
    [Display(Name = "Descrição")]
    public string Descricao { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Número do documento")]
    public string? NumeroDocumento { get; set; }

    [Display(Name = "Fornecedor")]
    public int? FornecedorId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecione uma conta de despesa.")]
    [Display(Name = "Plano de contas")]
    public int PlanoContasId { get; set; }

    [Required, DataType(DataType.Date)]
    [Display(Name = "Data de emissão")]
    public DateTime DataEmissao { get; set; }

    [Required, DataType(DataType.Date)]
    [Display(Name = "Data de vencimento")]
    public DateTime DataVencimento { get; set; }

    [ModelBinder(BinderType = typeof(FlexibleDecimalModelBinder))]
    public decimal Valor { get; set; }

    [StringLength(2000)]
    [Display(Name = "Observações")]
    public string? Observacoes { get; set; }

    [ValidateNever]
    public IReadOnlyList<SelectListItem> Fornecedores { get; set; } = [];

    [ValidateNever]
    public IReadOnlyList<SelectListItem> PlanosContas { get; set; } = [];
}
