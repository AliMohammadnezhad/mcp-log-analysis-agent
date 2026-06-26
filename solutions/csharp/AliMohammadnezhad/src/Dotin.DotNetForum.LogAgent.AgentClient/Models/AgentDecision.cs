using Dotin.DotNetForum.LogAgent.Contracts;

namespace Dotin.DotNetForum.LogAgent.AgentClient.Models;

public abstract record AgentDecision
{
    public sealed record CallGetLogs(GetLogsArguments Arguments) : AgentDecision;

    public sealed record AskForClarification(string Question) : AgentDecision;
}
