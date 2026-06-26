using Dotin.DotNetForum.LogAgent.Contracts;

namespace Dotin.DotNetForum.LogAgent.McpServer.Services;

public sealed class SimulatedLogProvider : ILogProvider
{
    private static readonly IReadOnlyDictionary<string, Func<DateTimeOffset, string, IReadOnlyList<LogEntry>>> Scenarios =
        new Dictionary<string, Func<DateTimeOffset, string, IReadOnlyList<LogEntry>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["PaymentService"] = (now, service) =>
            [
                new LogEntry(now.AddMinutes(-12), LogSeverity.Info, service, "Received checkout request for order #48213."),
                new LogEntry(now.AddMinutes(-9), LogSeverity.Warning, service, "Connection pool to SQL-Server-01 nearing capacity (18/20 connections)."),
                new LogEntry(now.AddMinutes(-6), LogSeverity.Error, service, "Database connection timeout after 30000ms on SQL-Server-01"),
                new LogEntry(now.AddMinutes(-6), LogSeverity.Error, service, "HTTP 500 returned to caller: unable to complete checkout.")
            ],
            ["InventoryService"] = (now, service) =>
            [
                new LogEntry(now.AddMinutes(-10), LogSeverity.Info, service, "Stock reconciliation job started."),
                new LogEntry(now.AddMinutes(-4), LogSeverity.Warning, service, "Upstream WarehouseApi responded slowly (2400ms).")
            ],
        };

    public Task<IReadOnlyList<LogEntry>> GetLogsAsync(string serviceName, int minutesAgo, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var allEntries = Scenarios.TryGetValue(serviceName, out var scenario)
            ? scenario(now, serviceName)
            : [new LogEntry(now.AddMinutes(-2), LogSeverity.Info, serviceName, "Service is healthy. No anomalies detected.")
            ];

        var windowStart = now.AddMinutes(-minutesAgo);
        IReadOnlyList<LogEntry> result = allEntries.Where(e => e.Timestamp >= windowStart).ToList();

        return Task.FromResult(result);
    }
}
