using System.ComponentModel.DataAnnotations;

namespace GVC.Web.DTOs;

public class ParcelaEntradaDTO
{
    [DataType(DataType.Date)]
    public DateTime DataVencimento { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999")]
    public decimal Valor { get; set; }
}
