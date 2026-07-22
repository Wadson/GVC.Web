using System.Security.Cryptography;
using System.Text;
using AutoFixture;
using FluentAssertions;
using GVC.Web.Services;
using Xunit;

namespace GVC.Web.Tests.Unit.Services;

public sealed class PasswordHasherTests
{
    private readonly Fixture fixture = new();
    private readonly Pbkdf2PasswordHasher hasher = new();

    [Fact]
    public void Hash_DeveGerarSaltUnicoEValidarSomenteSenhaCorreta()
    {
        string password = fixture.Create<string>();

        string primeiroHash = hasher.Hash(password);
        string segundoHash = hasher.Hash(password);

        primeiroHash.Should().NotBe(segundoHash);
        hasher.Verify(password, primeiroHash).Should().BeTrue();
        hasher.Verify(password + "incorreta", primeiroHash).Should().BeFalse();
        hasher.NeedsRehash(primeiroHash).Should().BeFalse();
    }

    [Fact]
    public void Verify_DeveAceitarHashSha256LegadoESinalizarRehash()
    {
        string password = fixture.Create<string>();
        string legacyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));

        hasher.Verify(password, legacyHash).Should().BeTrue();
        hasher.NeedsRehash(legacyHash).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("formato-invalido")]
    [InlineData("pbkdf2-sha256$abc$salt$hash")]
    public void Verify_ComHashInvalido_DeveRetornarFalse(string encodedHash)
    {
        hasher.Verify("senha", encodedHash).Should().BeFalse();
    }
}
