# .NET Challenge: Building an Intelligent Log Analysis Agent with MCP

[![Difficulty](https://img.shields.io/badge/difficulty-medium-orange)]()
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)]()
[![MCP](https://img.shields.io/badge/MCP-Official%20SDK-informational)]()
[![Deadline](https://img.shields.io/badge/deadline-2026--06--28-critical)]()

> Build an MCP Server and an AI Agent Client with .NET 10. The agent must select the appropriate tool, retrieve service logs, and report the root cause of a Production error in clear, understandable language.

---

## Table of Contents

- [Prerequisites](#prerequisites)
- [Problem Description](#problem-description)
- [Expected Architecture](#expected-architecture)
- [Part One: MCP Server](#part-one-mcp-server)
- [Part Two: AI Agent Client](#part-two-ai-agent-client)
- [Language Model Policy](#language-model-policy)
- [Rules and Constraints](#rules-and-constraints)
- [Acceptance Criteria](#acceptance-criteria)
- [Core Scenarios](#core-scenarios)
- [Sample Input and Output](#sample-input-and-output)
- [Suggested Project Structure](#suggested-project-structure)
- [How to Run and Test](#how-to-run-and-test)
- [How to Submit](#how-to-submit)
- [Evaluation Criteria](#evaluation-criteria)
- [Timeline](#timeline)
- [Contact](#contact)

---

## Prerequisites

- Programming language: **C#**
- Platform: **.NET 10**
- Application types:
  - MCP Server
  - Console Application for the Agent Client
- Using the **official MCP SDK** is allowed.
- Recommended transport: **Stdio**
- Connecting to a real language model is not required.
- Operating system: Windows, Linux, or macOS

---

## Problem Description

One of our services in the Production environment has failed, and users are receiving HTTP 500 errors while using it.

The goal of this challenge is to build an intelligent log analysis agent that can behave like a senior DevOps engineer:

1. Receive a question from the user.
2. Determine which tool is required to answer the question.
3. Extract the appropriate tool arguments from the user's request.
4. Connect to the server through the MCP protocol.
5. Retrieve the required logs.
6. Identify the root cause of the error.
7. Report the result to the user in simple, clear, and understandable language.

To complete the challenge, you must implement two main components with .NET 10:

- **MCP Server**
- **AI Agent Client**

---

## Expected Architecture

```text
User Question
      │
      ▼
AI Agent Client
      │
      ├── Agent / Model Abstraction
      │      ├── Mock or Fake Model
      │      └── Real LLM Provider (Optional)
      │
      ▼
Tool Selection and Argument Extraction
      │
      ▼
MCP Client
      │
      ▼
MCP Server
      │
      ▼
GetLogs Tool
      │
      ▼
Simulated Elasticsearch Logs
```

Expected execution flow:

```text
Console App
    → Agent
    → Tool Selection
    → MCP Client
    → MCP Server
    → GetLogs
    → Log Analysis
    → Final Report
```

Calling the `GetLogs` method directly from the Console Application without passing through the Agent and MCP layers is not acceptable.

---

## Part One: MCP Server

Create a .NET application that acts as an MCP Server and exposes the following tool to the Agent.

### `GetLogs` Tool

#### Tool Name

```text
GetLogs
```

#### Suggested Tool Description

```text
Returns application logs for a specified service within a requested time range.
```

#### Inputs

| Parameter | Type | Required | Description |
|---|---:|:---:|---|
| `serviceName` | `string` | Yes | Name of the service whose logs must be inspected |
| `minutesAgo` | `int` | Yes | Number of previous minutes to search for logs |

Sample input:

```json
{
  "serviceName": "PaymentService",
  "minutesAgo": 15
}
```

#### Expected Output

The tool must return simulated Elasticsearch data containing at least the following error:

```text
[ERROR] - PaymentService - Database connection timeout after 30000ms on SQL-Server-01
```

The output may be plain text or a structured model, but it must contain the following information:

- Log level: `ERROR`
- Service name: `PaymentService`
- Error type: `Database connection timeout`
- Timeout duration: `30000ms`
- Target server: `SQL-Server-01`

### MCP Server Requirements

- The `GetLogs` tool must be discoverable and callable through MCP.
- Using the official MCP SDK is allowed.
- Communication should preferably use `stdio`.
- Invalid inputs must be handled without crashing the application.
- An understandable error must be returned when `serviceName` is empty or `minutesAgo` is invalid.
- A real Elasticsearch connection is not required.
- Log data may be stored in memory, in a file, or behind a simulated Provider.
- The simulated data-generation logic should preferably be placed behind an abstraction such as `ILogProvider`.

---

## Part Two: AI Agent Client

Create a .NET 10 Console Application that acts as the Agent Client.

The user asks the following question:

```text
Why has the payment service been returning HTTP 500 errors during the last 15 minutes?
```

The Agent must analyze the request and determine that the `GetLogs` tool should be called with the following arguments:

```json
{
  "name": "GetLogs",
  "arguments": {
    "serviceName": "PaymentService",
    "minutesAgo": 15
  }
}
```

After receiving the logs from the MCP Server, the Agent must extract the root cause and produce an understandable report.

### Agent Client Responsibilities

- Receive the user's question
- Access the MCP Server's available tool list
- Select the appropriate tool
- Extract the required arguments from the user's request
- Create a Tool Call
- Invoke the tool through MCP
- Receive the tool result
- Analyze the logs
- Produce the final response
- Handle communication errors and incomplete input

---

## Language Model Policy

Connecting to a real language model such as one of the following is not required:

- OpenAI
- Azure OpenAI
- Ollama
- GitHub Models
- Any other provider

Participants may use a `Fake` or `Mock Model` to keep the project offline, testable, and free from mandatory external dependencies.

However, all of the following conditions are required:

1. The model or Agent Provider must be placed behind an Interface or another suitable abstraction.
2. Replacing the Mock implementation with a real model must not require changes to the application's core Agent logic.
3. The Agent's output must represent a Tool Call.
4. The Agent must be responsible for selecting the tool and producing its arguments.
5. The Console Application must not directly decide to invoke `GetLogs` with fixed values.
6. The final answer must not be stored as a predefined constant string in the code.

Suggested abstraction:

```csharp
public interface IAgentModel
{
    Task<AgentDecision> DecideAsync(
        string userMessage,
        IReadOnlyCollection<AvailableTool> tools,
        CancellationToken cancellationToken);
}
```

The Interface and model names are optional. Participants may use a different design as long as it satisfies the requirements.

### Acceptable Implementations

- Connecting to a real LLM and using Tool Calling
- Using a Mock Model that produces a standard Tool Call
- Using a general-purpose, extensible parser to analyze the request and create a Tool Call
- Using Configuration or Metadata to map a human-readable service name to its technical service identifier

### Unacceptable Implementations

The following example is considered hard-coded:

```csharp
if (userMessage.Contains("payment"))
{
    await GetLogs("PaymentService", 15);
}
```

The following approaches are also unacceptable:

- Calling `GetLogs` directly from the Console Application
- Storing the complete Tool Call as a fixed value for the single sample question
- Storing the final response as a fixed string in the code
- Bypassing MCP and calling an MCP Server class directly
- Coupling the entire application to one Provider without a replacement mechanism

---

## Rules and Constraints

- The project must be implemented with **.NET 10**.
- Using the official MCP SDK is allowed.
- Using a real LLM is optional.
- The project must be reviewable and runnable without requiring an API key.
- Committing Secrets or API keys to the Repository is prohibited.
- Elasticsearch data must be simulated; a real connection is not required.
- Communication between the Agent and the tool must occur through MCP.
- Hard-coding the Tool Call or final answer is prohibited.
- The application must handle invalid input and communication errors without crashing.
- All projects must build with a standard `dotnet build` command.
- Naming, internal structure, and implementation details are open, provided that all challenge requirements are satisfied.

---

## Acceptance Criteria

A solution is considered complete only when all of the following conditions are satisfied:

- [ ] The Solution builds successfully with .NET 10.
- [ ] The MCP Server can be started.
- [ ] The `GetLogs` tool is registered by the MCP Server and is discoverable.
- [ ] The tool accepts the `serviceName` and `minutesAgo` parameters.
- [ ] The Agent Client connects to the Server through MCP.
- [ ] The Agent selects `GetLogs` for the sample question.
- [ ] The Agent extracts `PaymentService` and `15`.
- [ ] The Tool Call is visible in logs or Debug output.
- [ ] The MCP Server returns the expected simulated log.
- [ ] The Agent identifies the database timeout as the root cause.
- [ ] The final response refers to `SQL-Server-01` and the 30-second timeout.
- [ ] The Tool Call is not hard-coded directly in the Console Application.
- [ ] The final answer is not stored as a fixed string in the code.
- [ ] The Mock/Fake Model is replaceable through an abstraction.
- [ ] Incomplete or invalid input is handled without crashing.
- [ ] The solution README explains the architecture and how to run the project.

---

## Core Scenarios

### Scenario 1: Successful Analysis of the Payment Service Error

**Input:**

```text
Why has the payment service been returning HTTP 500 errors during the last 15 minutes?
```

**Expected behavior:**

1. The Agent selects the `GetLogs` tool.
2. The following arguments are produced:

```json
{
  "serviceName": "PaymentService",
  "minutesAgo": 15
}
```

3. The tool is invoked through MCP.
4. The timeout error log is received.
5. The root cause is explained in the final response.

---

### Scenario 2: Incomplete Request

**Sample input:**

```text
Why is the payment service returning errors?
```

The time range is missing from this request.

**Acceptable behavior:**

- The Agent asks the user to provide a time range; or
- The Agent uses a documented default value and clearly states that value.

Silently selecting an undocumented fixed value is not acceptable.

---

### Scenario 3: Invalid Tool Input

Example:

```json
{
  "serviceName": "",
  "minutesAgo": 0
}
```

**Expected behavior:**

- The MCP Server must not crash.
- It must return an understandable and structured error.

---

### Scenario 4: MCP Server Is Unavailable

**Expected behavior:**

- The Agent Client handles the connection failure.
- A raw technical error or Stack Trace must not be displayed as the only user-facing response.
- The Agent must clearly explain why the investigation could not be completed.

---

## Sample Input and Output

### User Input

```text
Why has the payment service been returning HTTP 500 errors during the last 15 minutes?
```

### Agent Decision

```json
{
  "name": "GetLogs",
  "arguments": {
    "serviceName": "PaymentService",
    "minutesAgo": 15
  }
}
```

### MCP Tool Output

```text
[ERROR] - PaymentService - Database connection timeout after 30000ms on SQL-Server-01
```

### Sample Final Response

```text
The logs from the last 15 minutes show that the payment service has been returning HTTP 500 errors because its connection to the SQL-Server-01 database timed out. The database server did not respond within 30 seconds.
```

The wording may differ, but the response must identify and clearly explain the correct root cause.

---

## Suggested Project Structure

The following structure is only a recommendation:

```text
Dotin.DotNetForum.LogAgent/
├── src/
│   ├── Dotin.DotNetForum.LogAgent.McpServer/
│   │   ├── Tools/
│   │   ├── Services/
│   │   └── Program.cs
│   │
│   ├── Dotin.DotNetForum.LogAgent.AgentClient/
│   │   ├── Agent/
│   │   ├── Models/
│   │   ├── Mcp/
│   │   └── Program.cs
│   │
│   └── Dotin.DotNetForum.LogAgent.Contracts/
│
├── tests/
│   ├── Dotin.DotNetForum.LogAgent.McpServer.Tests/
│   └── Dotin.DotNetForum.LogAgent.AgentClient.Tests/
│
├── README.md
└── Dotin.DotNetForum.LogAgent.slnx
```

Both `.sln` and `.slnx` files are allowed.

---

## How to Run and Test

### Clone the Repository

```bash
git clone https://github.com/<organization>/<repository>.git
cd <repository>
```

### Check the .NET Version

```bash
dotnet --version
```

Expected version:

```text
10.x
```

### Restore and Build

```bash
dotnet restore
dotnet build
```

### Run the Agent Client

The participant must document the exact execution command in the solution README.

Example:

```bash
dotnet run --project src/Dotin.DotNetForum.LogAgent.AgentClient
```

The Agent Client may start the MCP Server as a Child Process through `stdio`.

Example Server command:

```bash
dotnet run --project src/Dotin.DotNetForum.LogAgent.McpServer
```

### Run the Tests

```bash
dotnet test
```

Suggested minimum test coverage:

- `GetLogs` input validation
- Correct log output for `PaymentService`
- Correct tool selection by the Agent
- Extraction of `minutesAgo`
- Correct timeout error analysis
- Handling an unavailable MCP Server
- Verification that the model Provider is replaceable

---

## How to Submit

1. Fork the challenge Repository.
2. Create a new Branch:

```bash
git checkout -b solution/<github-username>
```

3. Add your solution under:

```text
solutions/csharp/<github-username>/
```

Example:

```text
solutions/csharp/<github-username>/
├── src/
├── tests/
├── README.md
└── Dotin.DotNetForum.LogAgent.slnx
```

4. Commit and push your changes.
5. Open a Pull Request with the following title:

```text
[Solution] MCP Log Analysis Agent - <github-username>
```

### The Solution README Must Include

- Architecture description
- Instructions for running the MCP Server and Agent Client
- Instructions for running tests
- The type of Agent Provider used
- An explanation of how the Mock implementation can be replaced with a real LLM
- Limitations and assumptions
- A sample execution output

---

## Evaluation Criteria

Total score: **100 points**

| Criterion | Points |
|---|---:|
| Functional Correctness and Completion of the Main Scenario | 30 |
| MCP Server and `GetLogs` Tool Implementation | 20 |
| Agent Design, Tool Selection, and Prevention of Hard-Coding | 20 |
| Architecture, Code Quality, and Testability | 15 |
| Tests, Validation, and Error Handling | 10 |
| Documentation and Submission Quality | 5 |
| **Total** | **100** |

### 1. Functional Correctness and Completion of the Main Scenario — 30 Points

- Complete execution of the user-question scenario
- Correct extraction of the service name
- Correct extraction of the time range
- Retrieval of logs from the MCP Server
- Correct root-cause identification
- Clear and understandable final report

### 2. MCP Server and `GetLogs` Tool Implementation — 20 Points

- Correct tool registration
- Appropriate tool name, description, and parameter definitions
- Actual communication through MCP
- Correct use of `stdio` or another valid Transport
- Input validation
- Appropriate separation of the Log Provider from the MCP Tool

### 3. Agent Design, Tool Selection, and Prevention of Hard-Coding — 20 Points

- Presence of an Agent or Model abstraction
- Tool selection within the Agent layer
- Tool Call generation
- Ability to replace the Mock with a real LLM
- No direct tool invocation from the Console Application
- No hard-coded final response or final argument set

> Connecting to a real LLM does not provide separate bonus points. A correctly designed Mock Model can receive full points for this section.

### 4. Architecture, Code Quality, and Testability — 15 Points

- Separation of Concerns
- Appropriate naming
- Readable and simple code
- Proper use of Dependency Injection
- Correct use of `async/await`
- Use of Cancellation Tokens where appropriate
- Extensibility and replaceability of components

### 5. Tests, Validation, and Error Handling — 10 Points

- Effective Unit Tests or Integration Tests
- Tool Call testing
- Invalid-input testing
- Log-analysis testing
- Server connection failure handling
- No raw Stack Trace presented to the end user

### 6. Documentation and Submission Quality — 5 Points

- Complete README
- Correct run commands
- Architecture explanation
- Documented assumptions
- Correct folder structure
- Clean and reviewable Pull Request

### Effect of Hard-Coding on Evaluation

- Calling `GetLogs` directly from the Console Application results in zero points for the Agent section.
- Bypassing MCP means the main scenario is considered incomplete.
- A fixed final response receives no points for log analysis and final reporting.
- Fixed arguments created specifically for the sample question receive no points for argument extraction.

---

## Optional Enhancements

The following items are not required, but they may positively affect the relevant quality criteria:

- Structured Logging
- OpenTelemetry
- Retry or Timeout Policy
- Dockerfile
- Integration Test for MCP communication
- Support for multiple services
- Support for multiple tools
- Optional connection to a real LLM
- Structured output for the analysis report
- Tracing for Tool Selection and Tool Execution

These enhancements do not provide a score above 100 points.

---

## Timeline

- **Challenge Start:** 31 Khordad 1405 — June 21, 2026
- **Pull Request Submission Deadline:** 7 Tir 1405 — June 28, 2026

The Pull Request creation time is used as the official submission time.

---

## Contact

Use one of the following channels to ask questions or report ambiguities:

- GitHub Issues in the challenge Repository
- .NET Community Group

Questions that result in changes or clarifications to the challenge specification must be answered publicly so that all participants have access to the same information.
