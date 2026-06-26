using Dotin.DotNetForum.LogAgent.AgentClient.Agent;
using Dotin.DotNetForum.LogAgent.AgentClient.Models;
using Dotin.DotNetForum.LogAgent.Contracts;
using FluentAssertions;
using Xunit;

namespace Dotin.DotNetForum.LogAgent.AgentClient.Tests;

public class MockAgentModelTests
{
    private readonly MockAgentModel _model = new();

    [Theory]
    [InlineData("Why has the payment service been returning HTTP 500 errors during the last 15 minutes?", "PaymentService", 15)]
    [InlineData("Investigate inventory service errors in the past 20 minutes", "InventoryService", 20)]
    [InlineData("What's wrong with OrderService in the last 5 minutes?", "OrderService", 5)]
    [InlineData("Check NotificationService logs 30 minutes ago", "NotificationService", 30)]
    [InlineData("How is the billing service doing in the last 2 hours?", "BillingService", 120)]
    public async Task DecideAsync_ExtractsServiceNameAndMinutes_FromVariedPhrasing(
        string question, string expectedService, int expectedMinutes)
    {
        var decision = await _model.DecideAsync(question);

        var callDecision = decision.Should().BeOfType<AgentDecision.CallGetLogs>().Subject;
        callDecision.Arguments.ServiceName.Should().Be(expectedService);
        callDecision.Arguments.MinutesAgo.Should().Be(expectedMinutes);
    }

    [Fact]
    public async Task DecideAsync_AsksForClarification_WhenServiceNameIsMissing()
    {
        var decision = await _model.DecideAsync("Why are we seeing errors in the last 15 minutes?");

        decision.Should().BeOfType<AgentDecision.AskForClarification>();
    }

    [Fact]
    public async Task DecideAsync_AsksForClarification_WhenTimeWindowIsMissing()
    {
        var decision = await _model.DecideAsync("Why is the payment service returning errors?");

        decision.Should().BeOfType<AgentDecision.AskForClarification>();
    }

    [Fact]
    public async Task GenerateReportAsync_ExtractsTimeoutAndServer_FromArbitraryLogContent()
    {
        var args = new GetLogsArguments("BillingService", 10);
        var logs = new[]
        {
            new LogEntry(DateTimeOffset.UtcNow, LogSeverity.Error, "BillingService", "Database connection timeout after 45000ms on SQL-Replica-09")
        };

        var report = await _model.GenerateReportAsync("question", args, logs);

        report.Should().Contain("45").And.Contain("SQL-Replica-09");
    }

    [Fact]
    public async Task GenerateReportAsync_ReportsHealthy_WhenNoErrorsPresent()
    {
        var args = new GetLogsArguments("OrderService", 15);
        var logs = new[]
        {
            new LogEntry(DateTimeOffset.UtcNow, LogSeverity.Info, "OrderService", "Service is healthy. No anomalies detected.")
        };

        var report = await _model.GenerateReportAsync("question", args, logs);

        report.Should().Contain("No errors").And.Contain("OrderService");
    }
}
