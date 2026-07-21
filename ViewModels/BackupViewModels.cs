using System.ComponentModel.DataAnnotations;
using GVC.Web.Services;

namespace GVC.Web.ViewModels;

public sealed class BackupIndexViewModel
{
    public string DirectoryPath { get; set; } = string.Empty;
    public IReadOnlyList<BackupFileInfo> Files { get; set; } = [];
    public DateTime? LastBackupAt => Files.FirstOrDefault()?.CreatedAt;
    public long TotalSizeBytes => Files.Sum(x => x.SizeBytes);
    public decimal TotalSizeMb => Math.Round(TotalSizeBytes / 1024m / 1024m, 2);
}

public sealed class RestoreBackupViewModel
{
    [Required]
    public string FileName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Digite CONFIRMAR para prosseguir.")]
    public string Confirmacao { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe sua senha atual.")]
    [DataType(DataType.Password)]
    public string SenhaAtual { get; set; } = string.Empty;
}
