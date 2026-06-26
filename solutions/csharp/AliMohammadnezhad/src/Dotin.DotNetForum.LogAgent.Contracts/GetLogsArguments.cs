using System.Text.Json.Serialization;

namespace Dotin.DotNetForum.LogAgent.Contracts;

public sealed record GetLogsArguments(
    [property: JsonPropertyName("serviceName")] string ServiceName,
    [property: JsonPropertyName("minutesAgo")] int MinutesAgo);
