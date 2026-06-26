using Dotin.DotNetForum.LogAgent.Contracts;

namespace Dotin.DotNetForum.LogAgent.McpServer.Services;

public interface ILogProvider
{
    Task<IReadOnlyList<LogEntry>> GetLogsAsync(string serviceName, int minutesAgo, CancellationToken cancellationToken = default);
}
