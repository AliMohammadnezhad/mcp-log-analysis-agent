using Dotin.DotNetForum.LogAgent.AgentClient.Agent;
using Dotin.DotNetForum.LogAgent.AgentClient.Mcp;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Serilog;

using var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog(new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger()));
var logger = loggerFactory.CreateLogger<Program>();
var agentLogger = loggerFactory.CreateLogger<LogAnalysisAgent>();

Console.WriteLine("Connecting to the MCP Log Server...");

var retryPolicy = new ResiliencePipelineBuilder().AddRetry(new RetryStrategyOptions
{
    MaxRetryAttempts = 2,
    Delay = TimeSpan.FromSeconds(1),
    OnRetry = args => { logger.LogWarning("Retrying MCP server connection (attempt {Attempt})", args.AttemptNumber + 1); return default; }
}).Build();

IMcpLogClient mcpClient;
try
{
    mcpClient = await retryPolicy.ExecuteAsync(async cancellationToken =>
    {
        var serverDllPath = ResolveServerDllPath();
        return await McpLogClient.ConnectAsync("dotnet", [serverDllPath], cancellationToken);
    });
}
catch (Exception ex)
{
    logger.LogError(ex, "Could not start or connect to the MCP server");
    Console.WriteLine($"Could not start or connect to the MCP server: {ex.Message}");
    Console.WriteLine("The investigation could not be completed because the log server is unavailable.");
    return;
}

await using (mcpClient)
{
    var agent = new LogAnalysisAgent(new MockAgentModel(), mcpClient, agentLogger);

    if (args.Length > 0)
    {
        var report = await agent.InvestigateAsync(string.Join(' ', args));
        Console.WriteLine();
        Console.WriteLine(report);
        return;
    }

    Console.WriteLine("Connected. Ask about a service's recent errors (type \"exit\" to quit).");
    while (true)
    {
        Console.Write("\n> ");
        var question = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(question) || question.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            break;
        }

        var report = await agent.InvestigateAsync(question);
        Console.WriteLine();
        Console.WriteLine(report);
    }
}

return;

static string ResolveServerDllPath()
{
    var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    var serverBaseDir = baseDir.Replace(
        "Dotin.DotNetForum.LogAgent.AgentClient",
        "Dotin.DotNetForum.LogAgent.McpServer");
    var dllPath = Path.Combine(serverBaseDir, "Dotin.DotNetForum.LogAgent.McpServer.dll");

    if (!File.Exists(dllPath))
    {
        throw new FileNotFoundException(
            $"MCP Server executable not found at '{dllPath}'. Build the solution first with 'dotnet build'.");
    }

    return dllPath;
}
