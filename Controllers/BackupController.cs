using System.Security.Claims;
using GVC.Web.Data;
using GVC.Web.Services;
using GVC.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GVC.Web.Controllers;

[Authorize(Policy = "Administradores")]
[Route("Admin/Backup")]
public class BackupController(
    IBackupService backupService,
    ErpDbContext db,
    IPasswordHasher passwordHasher,
    ILogger<BackupController> logger) : Controller
{
    private const long UploadLimit = 5L * 1024 * 1024 * 1024;

    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index() => View(
        "~/Views/Admin/Backup/Index.cshtml",
        new BackupIndexViewModel
        {
            DirectoryPath = backupService.DirectoryPath,
            Files = backupService.List()
        });

    [HttpPost("Criar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(CancellationToken cancellationToken)
    {
        try
        {
            BackupFileInfo file = await backupService.CreateAsync(cancellationToken);
            TempData["Success"] = $"Backup '{file.FileName}' criado com sucesso!";
        }
        catch (Exception exception) when (IsOperationalException(exception))
        {
            logger.LogError(exception, "Falha ao criar backup manual do banco erp_gvc.");
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Download/{fileName}")]
    public IActionResult Download(string fileName)
    {
        try
        {
            string path = backupService.GetFilePath(fileName);
            if (!System.IO.File.Exists(path))
                return NotFound();
            return PhysicalFile(path, "application/octet-stream", fileName, enableRangeProcessing: true);
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }

    [HttpPost("Upload")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(UploadLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = UploadLimit)]
    public async Task<IActionResult> Upload(IFormFile? arquivo, CancellationToken cancellationToken)
    {
        if (arquivo is null)
        {
            TempData["Error"] = "Selecione um arquivo .bak.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            BackupFileInfo saved = await backupService.SaveUploadAsync(arquivo, cancellationToken);
            TempData["Success"] = $"Arquivo '{saved.FileName}' enviado. Verifique-o antes de restaurar.";
        }
        catch (Exception exception) when (IsOperationalException(exception))
        {
            logger.LogWarning(exception, "Falha no upload de um backup externo.");
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Excluir")]
    [ValidateAntiForgeryToken]
    public IActionResult Excluir(string fileName)
    {
        try
        {
            backupService.Delete(fileName);
            TempData["Success"] = "Backup excluído com sucesso.";
        }
        catch (Exception exception) when (IsOperationalException(exception))
        {
            logger.LogWarning(exception, "Falha ao excluir o backup {BackupFile}.", fileName);
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Restaurar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restaurar(
        RestoreBackupViewModel viewModel,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(viewModel.Confirmacao?.Trim(), "CONFIRMAR", StringComparison.Ordinal))
            ModelState.AddModelError(nameof(viewModel.Confirmacao), "Digite exatamente CONFIRMAR.");

        if (!int.TryParse(User.FindFirstValue("UsuarioID"), out int usuarioId))
            return Forbid();

        var usuario = await db.Usuarios.AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId)
            .Select(x => new { x.Senha })
            .SingleOrDefaultAsync(cancellationToken);

        if (usuario is null || !passwordHasher.Verify(viewModel.SenhaAtual ?? string.Empty, usuario.Senha))
            ModelState.AddModelError(nameof(viewModel.SenhaAtual), "Senha atual inválida.");

        if (!ModelState.IsValid)
        {
            TempData["Error"] = ModelState.Values.SelectMany(x => x.Errors)
                .Select(x => x.ErrorMessage).FirstOrDefault() ?? "Confirmação inválida.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await backupService.RestoreAsync(viewModel.FileName, cancellationToken);
            TempData["Success"] = "Banco erp_gvc restaurado com sucesso. Revise os dados e autentique-se novamente se necessário.";
        }
        catch (Exception exception) when (IsOperationalException(exception))
        {
            logger.LogCritical(exception, "Falha ao restaurar o banco erp_gvc com {BackupFile}.", viewModel.FileName);
            TempData["Error"] = "A restauração falhou. " + exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private static bool IsOperationalException(Exception exception) => exception is
        ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException or Microsoft.Data.SqlClient.SqlException;
}
