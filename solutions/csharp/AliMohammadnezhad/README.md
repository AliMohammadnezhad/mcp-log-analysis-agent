# Dotin.DotNetForum.LogAgent

An AI Agent that investigates production incidents by querying logs through an MCP (Model
Context Protocol) server, identifying the root cause, and producing a plain-English report —
without ever calling the log tool directly from the console app.

## Architecture

```text
User Question
      │
      ▼
AgentClient (console app, Program.cs)
      │
      ▼
LogAnalysisAgent  (Agent/LogAnalysisAgent.cs — orchestrator)
      │
      ├─► IAgentModel.DecideAsync   (tool selection + argument extraction)
      │        └─ MockAgentModel: regex-based NLP, no LLM/API key required
      │
      ├─► IMcpLogClient.GetLogsAsync   (MCP Client, Mcp/McpLogClient.cs)
      │        └─ ModelContextProtocol SDK, stdio transport → spawns McpServer as a child process
      │
      ▼
McpServer (Tools/GetLogsTool.cs)
      │
      ▼
ILogProvider → SimulatedLogProvider   (simulated Elasticsearch-style log source)
      │
      ▼
IAgentModel.GenerateReportAsync   (root-cause analysis from the returned log content)
```

The console app holds no reference to `ILogProvider` or the `GetLogs` tool — every request
flows through `LogAnalysisAgent` → `IAgentModel` → `IMcpLogClient` → the MCP server process.

### Projects

| Project | Responsibility |
|---|---|
| `Dotin.DotNetForum.LogAgent.Contracts` | Shared types: `LogEntry`, `LogSeverity`, `GetLogsArguments` |
| `Dotin.DotNetForum.LogAgent.McpServer` | MCP server exposing the `GetLogs` tool over stdio |
| `Dotin.DotNetForum.LogAgent.AgentClient` | Console app: Agent, MCP client, reporting |
| `*.Tests` | xUnit test projects per component |

## Running

Requires **.NET 10 SDK**.

```bash
dotnet restore
dotnet build
```

Run the Agent Client (it spawns the MCP Server itself as a child process over stdio — no
separate terminal needed):

```bash
dotnet run --project src/Dotin.DotNetForum.LogAgent.AgentClient -- "Why has the payment service been returning HTTP 500 errors during the last 15 minutes?"
```

Omitting the question argument drops into an interactive loop instead — the MCP connection is
opened once and kept alive across multiple questions in the same session; type `exit` (or
press Enter on an empty line) to quit. Passing a question on the command line stays single-shot
(answer once, exit) for scripted/CI use. The MCP Server can also be run standalone for manual
protocol inspection:

```bash
dotnet run --project src/Dotin.DotNetForum.LogAgent.McpServer
```

## Testing

```bash
dotnet test
```

23 tests across both test projects cover:
- `GetLogs` input validation (empty `serviceName`, invalid `minutesAgo`) — `McpServer.Tests`
- Simulated log content and time-window filtering — `McpServer.Tests`
- Tool selection / argument extraction across varied phrasings and service names (not just the
  sample question) — `AgentClient.Tests`
- Clarification flow when service name or time window is missing — `AgentClient.Tests`
- Dynamic report generation from arbitrary log content — `AgentClient.Tests`
- Graceful handling when the MCP server is unavailable — `AgentClient.Tests`
- That `IAgentModel` is swappable (a stub model drives the same orchestrator) — `AgentClient.Tests`

## Agent Provider

The default and only provider wired up is **`MockAgentModel`** (`Agent/MockAgentModel.cs`) — no
LLM, no API key, fully deterministic. It performs genuine text parsing, not a special case for
the sample question:

- **Service name**: matches a PascalCase token ending in `Service` (e.g. `PaymentService`), or a
  lowercase phrase like `"payment service"` which it normalizes into `PaymentService`.
- **Time window**: matches `"(last|past) N (minutes|hours)"` or `"N minutes ago"`; hours are
  converted to minutes.
- If either piece is missing, the agent returns a clarification request instead of guessing
  (Scenario 2 in the challenge — no undocumented defaults).
- The report is built by scanning the returned `LogEntry` list for error/critical severities and
  extracting any `timeout after Nms` / `on <server>` patterns present in the actual message text —
  the wording changes with whatever logs come back, it is not a fixed string.

### Swapping in a real LLM

`IAgentModel` (`Agent/IAgentModel.cs`) is the only contract the orchestrator depends on:

```csharp
public interface IAgentModel
{
    Task<AgentDecision> DecideAsync(string userQuestion, CancellationToken cancellationToken = default);
    Task<string> GenerateReportAsync(string userQuestion, GetLogsArguments queryArguments, IReadOnlyList<LogEntry> logs, CancellationToken cancellationToken = default);
}
```

To use a real provider (OpenAI-compatible, Anthropic, or a local model via LM Studio), implement
this interface — `DecideAsync` prompts the LLM to return a tool call or a clarification question
in the same `AgentDecision` shape, and `GenerateReportAsync` prompts it to summarize the log
content — then swap the single line in `Program.cs`:

```csharp
var agent = new LogAnalysisAgent(new MockAgentModel(), mcpClient, agentLogger);
// becomes
var agent = new LogAnalysisAgent(new OpenAiAgentModel(apiKey), mcpClient, agentLogger);
```

No other layer changes.

## Observability & Resilience

- Structured logging via **Serilog** in both the server and the client; the server logs to
  stderr exclusively since stdout is reserved for the MCP protocol stream.
- The Agent logs every tool-call decision (`GetLogs(serviceName=..., minutesAgo=...)`), making
  tool selection visible in console output without inspecting the wire protocol.
- **Polly** wraps the MCP server connection with a retry policy (2 attempts, 1s delay) to absorb
  transient process-startup failures; a persistent failure (e.g. the server binary missing) is
  reported to the user as a plain-English message, never a raw stack trace.

## Limitations & Assumptions

- The log source is simulated (`SimulatedLogProvider`) — two services have a fixed canned
  incident scenario (`PaymentService`: DB timeout; `InventoryService`: slow upstream warning) so
  the documented sample question reproduces the documented sample answer; any other service name
  returns a generic healthy log. There is no real Elasticsearch connection.
- `MockAgentModel`'s extraction is regex-based and covers the phrasings exercised in the tests;
  it is not a general-purpose NLU engine. A real LLM provider would generalize further.
- `minutesAgo` is capped at 7 days (10,080 minutes) by the server as a basic sanity bound.
- The Agent Client locates the McpServer binary by assuming both projects are built side-by-side
  under the same solution (`src/<X>/bin/<Config>/<TFM>/`); building the solution as a whole
  before running the client is required.

## Sample Execution

```text
$ dotnet run --project src/Dotin.DotNetForum.LogAgent.AgentClient -- "Why has the payment service been returning HTTP 500 errors during the last 15 minutes?"

Connecting to the MCP Log Server...
[21:06:53 INF] Tool call decided: GetLogs(serviceName=PaymentService, minutesAgo=15)
[21:06:53 INF] GetLogs returned 4 entries

The logs from the last 15 minute(s) show that PaymentService has been returning errors. The root
cause is a database connection timeout: the database did not respond within 30 seconds on
SQL-Server-01. 2 related error entries were observed in this window. Recommended next step:
verify database connectivity and connection pool capacity on the affected server.
```
