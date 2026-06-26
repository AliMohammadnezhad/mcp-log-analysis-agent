using Dotin.DotNetForum.LogAgent.Contracts;
using Dotin.DotNetForum.LogAgent.McpServer.Services;
using Dotin.DotNetForum.LogAgent.McpServer.Tools;
using FluentAssertions;
using Moq;
using ModelContextProtocol;
using Xunit;

namespace Dotin.DotNetForum.LogAgent.McpServer.Tests;

public class GetLogsToolTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetLogs_ThrowsMcpException_WhenServiceNameIsEmpty(string serviceName)
    {
        var logProvider = Mock.Of<ILogProvider>();

        var act = () => GetLogsTool.GetLogs(logProvider, serviceName, 15, CancellationToken.None);

        await act.Should().ThrowAsync<McpException>()
            .WithMessage("*serviceName*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(20_000)]
    public async Task GetLogs_ThrowsMcpException_WhenMinutesAgoIsOutOfRange(int minutesAgo)
    {
        var logProvider = Mock.Of<ILogProvider>();

        var act = () => GetLogsTool.GetLogs(logProvider, "PaymentService", minutesAgo, CancellationToken.None);

        await act.Should().ThrowAsync<McpException>()
            .WithMessage("*minutesAgo*");
    }

    [Fact]
    public async Task GetLogs_ReturnsLogsFromProvider_WhenInputIsValid()
    {
        var expectedLogs = new[]
        {
            new LogEntry(DateTimeOffset.UtcNow, LogSeverity.Error, "PaymentService", "Database connection timeout after 30000ms on SQL-Server-01")
        };
        var providerMock = new Mock<ILogProvider>();
        providerMock
            .Setup(p => p.GetLogsAsync("PaymentService", 15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedLogs);

        var result = await GetLogsTool.GetLogs(providerMock.Object, "PaymentService", 15, CancellationToken.None);

        result.Should().BeEquivalentTo(expectedLogs);
    }
}
