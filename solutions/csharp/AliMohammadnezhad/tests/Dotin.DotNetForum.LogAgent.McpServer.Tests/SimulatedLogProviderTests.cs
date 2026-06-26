using Dotin.DotNetForum.LogAgent.Contracts;
using Dotin.DotNetForum.LogAgent.McpServer.Services;
using FluentAssertions;
using Xunit;

namespace Dotin.DotNetForum.LogAgent.McpServer.Tests;

public class SimulatedLogProviderTests
{
    private readonly SimulatedLogProvider _provider = new();

    [Fact]
    public async Task GetLogsAsync_ReturnsDatabaseTimeoutError_ForPaymentService()
    {
        var logs = await _provider.GetLogsAsync("PaymentService", 15);

        logs.Should().Contain(l =>
            l.Severity == LogSeverity.Error &&
            l.Message.Contains("SQL-Server-01") &&
            l.Message.Contains("30000ms"));
    }

    [Fact]
    public async Task GetLogsAsync_IsCaseInsensitiveOnServiceName()
    {
        var logs = await _provider.GetLogsAsync("paymentservice", 15);

        logs.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetLogsAsync_ExcludesEntriesOutsideTheTimeWindow()
    {
        var wideWindow = await _provider.GetLogsAsync("PaymentService", 15);
        var narrowWindow = await _provider.GetLogsAsync("PaymentService", 1);

        wideWindow.Should().NotBeEmpty();
        narrowWindow.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLogsAsync_ReturnsHealthyLog_ForUnknownService()
    {
        var logs = await _provider.GetLogsAsync("SomeUnknownService", 15);

        logs.Should().ContainSingle();
        logs.Single().Severity.Should().Be(LogSeverity.Info);
    }
}
