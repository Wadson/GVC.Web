using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GVC.Web.Data;

[Table("DataVersion")]
public sealed class DataVersion
{
    [Key, StringLength(100)]
    public string Version { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}
