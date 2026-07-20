namespace GVC.Web.Services;

public static class CompanyImageStorage
{
    private const long MaxLogoSize = 5 * 1024 * 1024;
    private const long MaxBackgroundSize = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    public static string? ValidateLogo(IFormFile? file) =>
        Validate(file, MaxLogoSize, "A logo", "5 MB");

    public static string? ValidateBackground(IFormFile? file) =>
        Validate(file, MaxBackgroundSize, "A imagem de fundo", "10 MB");

    public static async Task<byte[]?> ReadAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        return stream.ToArray();
    }

    public static string GetDataUrl(byte[] image)
    {
        string contentType = image.Length > 3 && image[0] == 0x89 && image[1] == 0x50
            ? "image/png"
            : image.Length > 2 && image[0] == 0xFF && image[1] == 0xD8
                ? "image/jpeg"
                : "image/webp";

        return $"data:{contentType};base64,{Convert.ToBase64String(image)}";
    }

    private static string? Validate(IFormFile? file, long maxSize, string label, string maxSizeLabel)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        if (file.Length > maxSize)
        {
            return $"{label} deve ter no máximo {maxSizeLabel}.";
        }

        string extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension) ||
            !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return $"{label} deve ser uma imagem JPG, PNG ou WEBP válida.";
        }

        return null;
    }
}
