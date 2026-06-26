using Dotin.DotNetForum.LogAgent.AgentClient.Mcp;
using Dotin.DotNetForum.LogAgent.AgentClient.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dotin.DotNetForum.LogAgent.AgentClient.Agent;

public sealed class LogAnalysisAgent(
    IAgentModel model,
    IMcpLogClient mcpClient,
    ILogger<LogAnalysisAgent>? logger = null)
{
    private readonly ILogger<LogAnalysisAgent> _logger = logger ?? NullLogger<LogAnalysisAgent>.Instance;

    public async Task<string> InvestigateAsync(string userQuestion, CancellationToken cancellationToken = default)
    {
        AgentDecision decision;
        try
        {
            decision = await model.DecideAsync(userQuestion, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent model failed to decide on a tool call for question: {Question}", userQuestion);
            return $"I couldn't analyze your question due to an internal error: {ex.Message}";
        }

        if (decision is AgentDecision.AskForClarification clarification)
        {
            _logger.LogInformation("Agent requested clarification instead of calling a tool: {Question}", clarification.Question);
            return clarification.Question;
        }

        var callGetLogs = (AgentDecision.CallGetLogs)decision;
        _logger.LogInformation(
            "Tool call decided: GetLogs(serviceName={ServiceName}, minutesAgo={MinutesAgo})",
            callGetLogs.Arguments.ServiceName,
            callGetLogs.Arguments.MinutesAgo);

        var result = await mcpClient.GetLogsAsync(
            callGetLogs.Arguments.ServiceName,
            callGetLogs.Arguments.MinutesAgo,
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("GetLogs call failed: {ErrorMessage}", result.ErrorMessage);
            return $"I could not complete the investigation: {result.ErrorMessage}";
        }

        _logger.LogInformation("GetLogs returned {LogCount} entries", result.Logs.Count);
        return await model.GenerateReportAsync(userQuestion, callGetLogs.Arguments, result.Logs, cancellationToken);
    }
}
