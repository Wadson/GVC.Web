using System.Security.Cryptography;
using System.Text;

namespace GVC.Web.Services;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

        return $"pbkdf2-sha256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public bool Verify(string password, string encodedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(encodedHash))
            return false;

        if (IsLegacySha256(encodedHash))
        {
            var actual = SHA256.HashData(Encoding.UTF8.GetBytes(password));

            var expected = Convert.FromHexString(encodedHash);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }

        try
        {
            var parts = encodedHash.Split('$');

            if (parts.Length != 4 || parts[0] != "pbkdf2-sha256")
                return false;

            var iterations = int.Parse(parts[1]);

            var salt = Convert.FromBase64String(parts[2]);

            var expected = Convert.FromBase64String(parts[3]);

            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    public bool NeedsRehash(string encodedHash) => !encodedHash.StartsWith("pbkdf2-sha256$", StringComparison.Ordinal);

    private static bool IsLegacySha256(string value) =>
        value.Length == 64 && value.All(Uri.IsHexDigit);
}