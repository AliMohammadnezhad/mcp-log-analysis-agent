using Dotin.DotNetForum.LogAgent.AgentClient.Models;

namespace Dotin.DotNetForum.LogAgent.AgentClient.Mcp;

public interface IMcpLogClient : IAsyncDisposable
{
    Task<McpLogsResult> GetLogsAsync(string serviceName, int minutesAgo, CancellationToken cancellationToken = default);
}
