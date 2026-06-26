using System.ComponentModel;
using Dotin.DotNetForum.LogAgent.Contracts;
using Dotin.DotNetForum.LogAgent.McpServer.Services;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Dotin.DotNetForum.LogAgent.McpServer.Tools;

[McpServerToolType]
public static class GetLogsTool
{
    private const int MaxMinutesAgo = 7 * 24 * 60;

    [McpServerTool(Name = "GetLogs")]
    [Description("Retrieves simulated Elasticsearch-style structured logs for a given service within a time window, including any critical errors.")]
    public static async Task<IReadOnlyList<LogEntry>> GetLogs(
        ILogProvider logProvider,
        [Description("Name of the service to query, e.g. PaymentService")] string serviceName,
        [Description("How many minutes back from now to search, e.g. 15")] int minutesAgo,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            throw new McpException("serviceName must not be empty.");
        }

        if (minutesAgo <= 0 || minutesAgo > MaxMinutesAgo)
        {
            throw new McpException($"minutesAgo must be a positive integer no greater than {MaxMinutesAgo}.");
        }

        return await logProvider.GetLogsAsync(serviceName, minutesAgo, cancellationToken);
    }
}
