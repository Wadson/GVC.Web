using System.ComponentModel.DataAnnotations;
using GVC.Web.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace GVC.Web.ViewModels;

public sealed class BaixarContaViewModel
{
    [Range(1, int.MaxValue)]
    public int ContasAPagarId { get; set; }

    [Required, DataType(DataType.Date)]
    [Display(Name = "Data do pagamento")]
    public DateTime DataPagamento { get; set; } = DateTime.Today;

    [ModelBinder(BinderType = typeof(FlexibleDecimalModelBinder))]
    [Display(Name = "Valor pago")]
    public decimal ValorPago { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecione a forma de pagamento.")]
    [Display(Name = "Forma de pagamento")]
    public int FormaPgtoId { get; set; }

    [StringLength(1000)]
    [Display(Name = "Observações")]
    public string? Observacoes { get; set; }
}
