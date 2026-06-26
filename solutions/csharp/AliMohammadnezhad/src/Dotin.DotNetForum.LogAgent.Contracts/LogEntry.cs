namespace Dotin.DotNetForum.LogAgent.Contracts;

public sealed record LogEntry(
    DateTimeOffset Timestamp,
    LogSeverity Severity,
    string ServiceName,
    string Message)
{
    public override string ToString() =>
        $"[{Severity.ToString().ToUpperInvariant()}] - {ServiceName} - {Message}";
}
