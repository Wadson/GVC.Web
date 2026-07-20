
namespace GVC.Web.Services;

public static class ProductImageStorage
{
    private const long MaxFileSize = 2 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    public static string? Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return null;

        if (file.Length > MaxFileSize)
            return "A imagem deve ter no máximo 2 MB.";

        var extension = Path.GetExtension(file.FileName);

        if (!AllowedExtensions.Contains(extension))
            return "Use uma imagem JPG, JPEG, PNG ou WEBP.";

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return "O arquivo selecionado não é uma imagem válida.";

        return null;
    }

    public static async Task<string> SaveAsync(IFormFile file, IWebHostEnvironment environment, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        var directory = Path.Combine(environment.WebRootPath, "uploads", "produtos");

        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid():N}{extension}";

        await using var stream = new FileStream(Path.Combine(directory, fileName), FileMode.CreateNew);

        await file.CopyToAsync(stream, cancellationToken);

        return $"/uploads/produtos/{fileName}";
    }

    public static void Delete(string? relativePath, IWebHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;

        string uploadsRoot = Path.GetFullPath(Path.Combine(environment.WebRootPath, "uploads", "produtos"));
        string relativeFile = relativePath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
        string absolutePath = Path.GetFullPath(Path.Combine(environment.WebRootPath, relativeFile));

        if (!absolutePath.StartsWith(uploadsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return;

        if (File.Exists(absolutePath)) File.Delete(absolutePath);
    }
}
