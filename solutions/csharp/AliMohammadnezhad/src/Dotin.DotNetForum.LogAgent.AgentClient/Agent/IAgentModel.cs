using Dotin.DotNetForum.LogAgent.AgentClient.Models;
using Dotin.DotNetForum.LogAgent.Contracts;

namespace Dotin.DotNetForum.LogAgent.AgentClient.Agent;

public interface IAgentModel
{
    Task<AgentDecision> DecideAsync(string userQuestion, CancellationToken cancellationToken = default);

    Task<string> GenerateReportAsync(
        string userQuestion,
        GetLogsArguments queryArguments,
        IReadOnlyList<LogEntry> logs,
        CancellationToken cancellationToken = default);
}
