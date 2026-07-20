using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Models;

[Table("FormaPagamento")]
public class FormaPagamento
{
    [Key, Column("FormaPgtoID")]
    public int FormaPgtoId
    {
        get; set;
    }

    [Required, StringLength(50)]
    public string NomeFormaPagamento { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;
}