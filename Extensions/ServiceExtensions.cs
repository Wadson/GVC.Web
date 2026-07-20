
namespace GVC.Web.Extensions;

public static class ServiceExtensions
{
    public static string OnlyDigits(this string? value) => new((value ?? string.Empty).Where(char.IsDigit).ToArray());
}