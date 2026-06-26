using Dotin.DotNetForum.LogAgent.Contracts;

namespace Dotin.DotNetForum.LogAgent.AgentClient.Models;

public sealed record McpLogsResult(bool IsSuccess, IReadOnlyList<LogEntry> Logs, string? ErrorMessage)
{
    public static McpLogsResult Success(IReadOnlyList<LogEntry> logs) => new(true, logs, null);

    public static McpLogsResult Failure(string errorMessage) => new(false, [], errorMessage);
}
