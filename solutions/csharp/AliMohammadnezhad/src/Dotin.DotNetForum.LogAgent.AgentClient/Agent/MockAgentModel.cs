using System.Text;
using System.Text.RegularExpressions;
using Dotin.DotNetForum.LogAgent.AgentClient.Models;
using Dotin.DotNetForum.LogAgent.Contracts;

namespace Dotin.DotNetForum.LogAgent.AgentClient.Agent;

public sealed class MockAgentModel : IAgentModel
{
    private static readonly Regex CamelCaseServiceRegex = new(@"\b[A-Z][A-Za-z0-9]*Service\b", RegexOptions.Compiled);

    private static readonly Regex PhraseServiceRegex = new(@"\b([A-Za-z]+)\s+service\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex TimeWindowRegex = new(
        @"(?:(?:last|past)\s+(?<num1>\d+)\s*(?<unit1>minutes?|mins?|hours?|hrs?))|(?:(?<num2>\d+)\s*(?<unit2>minutes?|mins?|hours?|hrs?)\s+ago)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex TimeoutMessageRegex = new(@"timeout after (?<ms>\d+)\s*ms", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ServerNameRegex = new(@"\bon\s+([A-Za-z0-9\-_.]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly HashSet<string> ServiceNameStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "this", "that", "our", "my", "a", "an", "which", "what", "your", "any", "such"
    };

    public Task<AgentDecision> DecideAsync(string userQuestion, CancellationToken cancellationToken = default)
    {
        var serviceName = ExtractServiceName(userQuestion);
        if (serviceName is null)
        {
            return Task.FromResult<AgentDecision>(new AgentDecision.AskForClarification(
                "Which service should I investigate? Please name it explicitly, e.g. \"PaymentService\"."));
        }

        var minutesAgo = ExtractMinutesAgo(userQuestion);
        if (minutesAgo is null)
        {
            return Task.FromResult<AgentDecision>(new AgentDecision.AskForClarification(
                $"Over what time window should I check {serviceName}'s logs? Please specify, e.g. \"in the last 15 minutes\"."));
        }

        return Task.FromResult<AgentDecision>(new AgentDecision.CallGetLogs(new GetLogsArguments(serviceName, minutesAgo.Value)));
    }

    public Task<string> GenerateReportAsync(
        string userQuestion,
        GetLogsArguments queryArguments,
        IReadOnlyList<LogEntry> logs,
        CancellationToken cancellationToken = default)
    {
        var errors = logs.Where(l => l.Severity is LogSeverity.Error or LogSeverity.Critical).ToList();

        if (errors.Count == 0)
        {
            return Task.FromResult(
                $"No errors were found for {queryArguments.ServiceName} in the last {queryArguments.MinutesAgo} minute(s). " +
                $"{logs.Count} log entr{(logs.Count == 1 ? "y" : "ies")} reviewed; the service appears healthy.");
        }

        var primary = errors[0];
        var report = new StringBuilder();
        report.Append($"The logs from the last {queryArguments.MinutesAgo} minute(s) show that {queryArguments.ServiceName} has been returning errors. ");

        var timeoutMatch = TimeoutMessageRegex.Match(primary.Message);
        if (timeoutMatch.Success)
        {
            var seconds = int.Parse(timeoutMatch.Groups["ms"].Value) / 1000.0;
            report.Append($"The root cause is a database connection timeout: the database did not respond within {seconds:0.#} seconds");

            var serverMatch = ServerNameRegex.Match(primary.Message);
            if (serverMatch.Success)
            {
                report.Append($" on {serverMatch.Groups[1].Value}");
            }

            report.Append(". ");
        }
        else
        {
            report.Append($"Root cause: {primary.Message} ");
        }

        if (errors.Count > 1)
        {
            report.Append($"{errors.Count} related error entries were observed in this window. ");
        }

        report.Append("Recommended next step: verify database connectivity and connection pool capacity on the affected server.");

        return Task.FromResult(report.ToString());
    }

    private static string? ExtractServiceName(string question)
    {
        var camelMatch = CamelCaseServiceRegex.Match(question);
        if (camelMatch.Success)
        {
            return camelMatch.Value;
        }

        var phraseMatch = PhraseServiceRegex.Match(question);
        if (phraseMatch.Success && !ServiceNameStopWords.Contains(phraseMatch.Groups[1].Value))
        {
            var word = phraseMatch.Groups[1].Value;
            return char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant() + "Service";
        }

        return null;
    }

    private static int? ExtractMinutesAgo(string question)
    {
        var match = TimeWindowRegex.Match(question);
        if (!match.Success)
        {
            return null;
        }

        var numberGroup = match.Groups["num1"].Success ? match.Groups["num1"] : match.Groups["num2"];
        var unitGroup = match.Groups["unit1"].Success ? match.Groups["unit1"] : match.Groups["unit2"];

        var value = int.Parse(numberGroup.Value);
        var isHours = unitGroup.Value.StartsWith('h') || unitGroup.Value.StartsWith('H');
        return isHours ? value * 60 : value;
    }
}
