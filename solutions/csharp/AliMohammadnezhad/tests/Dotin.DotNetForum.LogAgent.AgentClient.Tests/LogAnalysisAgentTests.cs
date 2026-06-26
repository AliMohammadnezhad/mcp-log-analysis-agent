using Dotin.DotNetForum.LogAgent.AgentClient.Agent;
using Dotin.DotNetForum.LogAgent.AgentClient.Mcp;
using Dotin.DotNetForum.LogAgent.AgentClient.Models;
using Dotin.DotNetForum.LogAgent.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace Dotin.DotNetForum.LogAgent.AgentClient.Tests;

public class LogAnalysisAgentTests
{
    [Fact]
    public async Task InvestigateAsync_ReturnsClarification_WithoutCallingMcpClient_WhenModelAsksForClarification()
    {
        var modelMock = new Mock<IAgentModel>();
        modelMock
            .Setup(m => m.DecideAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentDecision.AskForClarification("Which service?"));
        var mcpClientMock = new Mock<IMcpLogClient>();

        var agent = new LogAnalysisAgent(modelMock.Object, mcpClientMock.Object);

        var result = await agent.InvestigateAsync("vague question");

        result.Should().Be("Which service?");
        mcpClientMock.Verify(
            c => c.GetLogsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task InvestigateAsync_CallsMcpClientWithExtractedArguments_AndReturnsModelReport()
    {
        var arguments = new GetLogsArguments("PaymentService", 15);
        var logs = new[] { new LogEntry(DateTimeOffset.UtcNow, LogSeverity.Error, "PaymentService", "boom") };

        var modelMock = new Mock<IAgentModel>();
        modelMock
            .Setup(m => m.DecideAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentDecision.CallGetLogs(arguments));
        modelMock
            .Setup(m => m.GenerateReportAsync("question", arguments, logs, It.IsAny<CancellationToken>()))
            .ReturnsAsync("final report");

        var mcpClientMock = new Mock<IMcpLogClient>();
        mcpClientMock
            .Setup(c => c.GetLogsAsync("PaymentService", 15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpLogsResult.Success(logs));

        var agent = new LogAnalysisAgent(modelMock.Object, mcpClientMock.Object);

        var result = await agent.InvestigateAsync("question");

        result.Should().Be("final report");
        mcpClientMock.Verify(c => c.GetLogsAsync("PaymentService", 15, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvestigateAsync_ReturnsGracefulMessage_WhenMcpServerIsUnavailable()
    {
        var arguments = new GetLogsArguments("PaymentService", 15);

        var modelMock = new Mock<IAgentModel>();
        modelMock
            .Setup(m => m.DecideAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentDecision.CallGetLogs(arguments));

        var mcpClientMock = new Mock<IMcpLogClient>();
        mcpClientMock
            .Setup(c => c.GetLogsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpLogsResult.Failure("connection refused"));

        var agent = new LogAnalysisAgent(modelMock.Object, mcpClientMock.Object);

        var result = await agent.InvestigateAsync("question");

        result.Should().Contain("could not complete").And.Contain("connection refused");
    }

    [Fact]
    public async Task InvestigateAsync_IsAgentModelAgnostic_AnyIAgentModelImplementationCanBeSubstituted()
    {
        var stubModel = new StubAlwaysHealthyAgentModel();
        var mcpClientMock = new Mock<IMcpLogClient>();
        mcpClientMock
            .Setup(c => c.GetLogsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(McpLogsResult.Success(Array.Empty<LogEntry>()));

        var agent = new LogAnalysisAgent(stubModel, mcpClientMock.Object);

        var result = await agent.InvestigateAsync("anything");

        result.Should().Be("stub report");
    }

    private sealed class StubAlwaysHealthyAgentModel : IAgentModel
    {
        public Task<AgentDecision> DecideAsync(string userQuestion, CancellationToken cancellationToken = default) =>
            Task.FromResult<AgentDecision>(new AgentDecision.CallGetLogs(new GetLogsArguments("AnyService", 5)));

        public Task<string> GenerateReportAsync(
            string userQuestion, GetLogsArguments queryArguments, IReadOnlyList<LogEntry> logs, CancellationToken cancellationToken = default) =>
            Task.FromResult("stub report");
    }
}
