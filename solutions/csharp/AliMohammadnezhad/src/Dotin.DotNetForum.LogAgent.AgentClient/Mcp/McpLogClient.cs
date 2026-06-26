using System.Text.Json;
using Dotin.DotNetForum.LogAgent.AgentClient.Models;
using Dotin.DotNetForum.LogAgent.Contracts;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Dotin.DotNetForum.LogAgent.AgentClient.Mcp;

public sealed class McpLogClient : IMcpLogClient
{
    private readonly McpClient _client;

    private McpLogClient(McpClient client) => _client = client;

    public static async Task<McpLogClient> ConnectAsync(
        string serverCommand,
        IReadOnlyList<string>? arguments = null,
        CancellationToken cancellationToken = default)
    {
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Command = serverCommand,
            Arguments = arguments?.ToList(),
            Name = "Dotin.DotNetForum.LogAgent.McpServer"
        });

        var client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
        return new McpLogClient(client);
    }

    public async Task<McpLogsResult> GetLogsAsync(string serviceName, int minutesAgo, CancellationToken cancellationToken = default)
    {
        try
        {
            var arguments = new Dictionary<string, object?>
            {
                ["serviceName"] = serviceName,
                ["minutesAgo"] = minutesAgo
            };

            var result = await _client.CallToolAsync(
                "GetLogs",
                arguments!,
                cancellationToken: cancellationToken);

            if (result.IsError == true)
            {
                return McpLogsResult.Failure(ExtractText(result) ?? "The GetLogs tool reported an error.");
            }

            return McpLogsResult.Success(ParseLogs(result));
        }
        catch (McpException ex)
        {
            return McpLogsResult.Failure($"The MCP server reported an error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return McpLogsResult.Failure($"Failed to communicate with the MCP server: {ex.Message}");
        }
    }

    private static string? ExtractText(CallToolResult result) =>
        result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text;

    private static IReadOnlyList<LogEntry> ParseLogs(CallToolResult result)
    {
        var json = ExtractText(result);
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<LogEntry>();
        }

        return JsonSerializer.Deserialize<List<LogEntry>>(json, McpJsonUtilities.DefaultOptions) ?? new List<LogEntry>();
    }

    public async ValueTask DisposeAsync() => await _client.DisposeAsync();
}
