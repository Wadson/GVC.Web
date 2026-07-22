using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using GVC.Web.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GVC.Web.Tests.Unit.Services;

public sealed class EmailServiceTests
{
    [Fact]
    public async Task SendAsync_SemConfiguracaoSmtp_DeveFalharELogarAviso()
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        var logger = fixture.Freeze<Mock<ILogger<EmailService>>>();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var service = new EmailService(configuration, logger.Object);

        Func<Task> action = () => service.SendAsync(
            fixture.Create<string>() + "@example.test",
            fixture.Create<string>(),
            fixture.Create<string>());

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Email:Host*Email:FromAddress*");
        bool possuiAviso = logger.Invocations.Any(invocation =>
            invocation.Arguments.Count >= 3 &&
            invocation.Arguments[0] is LogLevel.Warning &&
            invocation.Arguments[2]?.ToString()?.Contains("SMTP", StringComparison.OrdinalIgnoreCase) == true);
        possuiAviso.Should().BeTrue();
    }
}
