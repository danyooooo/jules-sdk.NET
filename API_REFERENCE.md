# Jules SDK for .NET - API Reference

Complete reference for all public APIs in the JulesSdk library.

---

## Table of Contents

- [Static Factory](#static-factory)
- [Client Interfaces](#client-interfaces)
  - [IJulesClient](#ijulesclient)
  - [ISessionClient](#isessionclient)
  - [IAutomatedSession](#iautomatedsession)
  - [ISourceManager](#isourcemanager)
- [Configuration](#configuration)
- [Models](#models)
  - [SessionConfig](#sessionconfig)
  - [SessionResource](#sessionresource)
  - [Outcome](#outcome)
  - [Activity Types](#activity-types)
  - [Artifact Types](#artifact-types)
  - [Other Models](#other-models)
- [Storage](#storage)
- [Exceptions](#exceptions)
- [Dependency Injection](#dependency-injection)

---

## Static Factory

### `Jules` Class

Static entry point for quick SDK access.

```csharp
namespace JulesSdk;

public static class Jules
{
    // Pre-initialized client (reads JULES_API_KEY from environment)
    public static IJulesClient Client { get; }
    
    // Create client with custom options
    public static IJulesClient Connect(JulesOptions options);
    
    // Create client with API key
    public static IJulesClient Connect(string apiKey);
}
```

**Example:**
```csharp
// Using environment variable
var client = Jules.Client;

// Using API key
var client = Jules.Connect("your-api-key");

// Using options
var client = Jules.Connect(new JulesOptions { ApiKey = "key", PollingIntervalMs = 3000 });
```

---

## Client Interfaces

### IJulesClient

The main client interface for interacting with the Jules API.

```csharp
public interface IJulesClient
{
    // Create an automated session
    Task<IAutomatedSession> RunAsync(SessionConfig config, CancellationToken ct = default);
    
    // Create an interactive session
    Task<ISessionClient> SessionAsync(SessionConfig config, CancellationToken ct = default);
    
    // Rehydrate existing session by ID
    ISessionClient Session(string sessionId);
    
    // List sessions with pagination
    IAsyncEnumerable<SessionResource> SessionsAsync(ListSessionsOptions? options = null, CancellationToken ct = default);
    
    // Batch run multiple sessions
    Task<IReadOnlyList<IAutomatedSession>> AllAsync<T>(
        IEnumerable<T> items,
        Func<T, SessionConfig> mapper,
        BatchOptions? options = null,
        CancellationToken ct = default);
    
    // Sync sessions to local cache
    Task<SyncStats> SyncAsync(SyncOptions? options = null, CancellationToken ct = default);
    
    // Create new client with different options
    IJulesClient With(JulesOptions options);
    
    // Properties
    ISourceManager Sources { get; }
    ISessionStorage Storage { get; }
}
```

---

### ISessionClient

Interactive session with full control over the agent.

```csharp
public interface ISessionClient
{
    string Id { get; }
    
    // Stream activities in real-time
    IAsyncEnumerable<Activity> StreamAsync(StreamOptions? options = null, CancellationToken ct = default);
    
    // Get past activities from cache
    IAsyncEnumerable<Activity> HistoryAsync(CancellationToken ct = default);
    
    // Get only future activities
    IAsyncEnumerable<Activity> UpdatesAsync(CancellationToken ct = default);
    
    // Approve pending plan
    Task ApproveAsync(CancellationToken ct = default);
    
    // Send message to agent
    Task SendAsync(string prompt, CancellationToken ct = default);
    
    // Send message and wait for reply
    Task<AgentMessagedActivity> AskAsync(string prompt, CancellationToken ct = default);
    
    // Wait for session completion
    Task<Outcome> ResultAsync(CancellationToken ct = default);
    
    // Wait for specific state
    Task WaitForAsync(SessionState state, CancellationToken ct = default);
    
    // Get session info
    Task<SessionResource> InfoAsync(CancellationToken ct = default);
    
    // Get a specific activity by ID
    Task<Activity> GetActivityAsync(string activityId, CancellationToken ct = default);
}
```

---

### IAutomatedSession

Handle for automated (hands-off) sessions.

```csharp
public interface IAutomatedSession
{
    string Id { get; }
    
    // Stream activities
    IAsyncEnumerable<Activity> StreamAsync(CancellationToken ct = default);
    
    // Wait for completion and get result
    Task<Outcome> ResultAsync(CancellationToken ct = default);
}
```

---

### ISourceManager

Manages GitHub repository connections.

```csharp
public interface ISourceManager
{
    // List all connected sources
    IAsyncEnumerable<Source> ListAsync(CancellationToken ct = default);
    
    // Get source by GitHub identifier (e.g., "owner/repo")
    Task<Source?> GetAsync(string github, CancellationToken ct = default);
    
    // Get source directly by resource name
    Task<Source> GetByNameAsync(string sourceName, CancellationToken ct = default);
}
```

---

## Configuration

### JulesOptions

```csharp
public class JulesOptions
{
    string? ApiKey { get; set; }              // API key (or use JULES_API_KEY env var)
    int PollingIntervalMs { get; set; }       // Default: 5000
    int RequestTimeoutMs { get; set; }        // Default: 30000
    string? CacheDir { get; set; }            // File-based cache directory (optional)
    ProxyConfig? Proxy { get; set; }          // Proxy configuration (optional)
    RateLimitRetryConfig RateLimitRetry { get; set; }
}
```

> **Note:** The API base URL is hardcoded to `https://jules.googleapis.com/v1alpha` and cannot be configured.
```

### ListSessionsOptions

```csharp
public class ListSessionsOptions
{
    int PageSize { get; init; } = 20;         // Sessions per page
    int? Limit { get; init; }                 // Max total sessions
    string? PageToken { get; init; }          // Pagination token
}
```

### BatchOptions

```csharp
public class BatchOptions
{
    int Concurrency { get; init; } = 4;       // Max concurrent sessions
    bool StopOnError { get; init; } = true;   // Stop on first failure
    int DelayMs { get; init; } = 0;           // Delay between starts
}
```

### SyncOptions

```csharp
public class SyncOptions
{
    string? SessionId { get; init; }          // Sync specific session only
    int Limit { get; init; } = 100;           // Max sessions to sync
    SyncDepth Depth { get; init; }            // Metadata or Activities
    bool Incremental { get; init; } = true;   // Stop at cached records
    int Concurrency { get; init; } = 3;       // Parallel hydration
}

public enum SyncDepth { Metadata, Activities }
```

### StreamOptions

```csharp
public class StreamOptions
{
    Origin? ExcludeOriginator { get; init; }  // Filter by originator
}
```

---

## Models

### SessionConfig

Configuration for creating a new session.

```csharp
public class SessionConfig
{
    required string Prompt { get; init; }     // Task description (required)
    SourceInput? Source { get; init; }        // GitHub repo (optional = repoless)
    string? Title { get; init; }              // Session title
    bool? RequireApproval { get; init; }      // Wait for plan approval
    bool? AutoPr { get; init; }               // Auto-create PR on completion
    string? OwnerId { get; init; }            // Owner ID (for proxy auth)
}
```

### SourceInput

```csharp
public record SourceInput(string Github, string? BaseBranch = null);
```

**Example:**
```csharp
new SourceInput("owner/repo", "main")
```

---

### SessionResource

The session data returned by the API.

```csharp
public class SessionResource
{
    string Name { get; }                      // Full resource name
    string Id { get; }                        // Computed short ID
    string Prompt { get; }                    // Original prompt
    string Title { get; }                     // Session title
    SessionState State { get; }               // Current state
    string CreateTime { get; }                // RFC 3339 timestamp
    string UpdateTime { get; }                // RFC 3339 timestamp
    string? Url { get; }                      // Web UI URL
    SourceContext? SourceContext { get; }     // Repo context
    Source? Source { get; }                   // Connected source
    IReadOnlyList<SessionOutput>? Outputs { get; }
    SessionOutcome? Outcome { get; }
}
```

### SessionState

```csharp
public enum SessionState
{
    Unspecified,
    Starting,
    AwaitingInput,
    AwaitingPlanApproval,
    Working,
    Completed,
    Failed
}
```

---

### Outcome

Final result of a completed session.

```csharp
public class Outcome
{
    string SessionId { get; }
    string Title { get; }
    SessionState State { get; }
    PullRequest? PullRequest { get; }
    IReadOnlyList<SessionOutput> Outputs { get; }
    
    // Get generated files from changeset artifacts
    GeneratedFiles GeneratedFiles();
}
```

### PullRequest

```csharp
public record PullRequest(
    string Url,
    string Title,
    int Number,
    string State
);
```

### GeneratedFiles / GeneratedFile

```csharp
public class GeneratedFiles
{
    IReadOnlyList<GeneratedFile> Files { get; }
    GeneratedFile? Get(string path);
}

public record GeneratedFile(
    string Path,
    string Content,
    ChangeType ChangeType
);

public enum ChangeType { Added, Modified, Deleted }
```

---

### Activity Types

All activities inherit from the abstract `Activity` base class:

```csharp
public abstract class Activity
{
    string Type { get; }                      // Discriminator
    string Name { get; }                      // Full resource name
    string Id { get; }                        // Computed short ID
    string CreateTime { get; }                // RFC 3339 timestamp
    Origin Originator { get; }                // AGENT, USER, or SYSTEM
    IReadOnlyList<Artifact>? Artifacts { get; }
}
```

| Type | Class | Key Properties |
|------|-------|----------------|
| `agentMessaged` | `AgentMessagedActivity` | `Message` |
| `userMessaged` | `UserMessagedActivity` | `Message` |
| `planGenerated` | `PlanGeneratedActivity` | `Plan` |
| `planApproved` | `PlanApprovedActivity` | `PlanId` |
| `progressUpdated` | `ProgressUpdatedActivity` | `Title`, `Description` |
| `sessionCompleted` | `SessionCompletedActivity` | — |
| `sessionFailed` | `SessionFailedActivity` | `Reason` |

**Usage with pattern matching:**
```csharp
await foreach (var activity in session.StreamAsync())
{
    switch (activity)
    {
        case AgentMessagedActivity msg:
            Console.WriteLine($"Agent: {msg.Message}");
            break;
        case PlanGeneratedActivity plan:
            Console.WriteLine($"Plan with {plan.Plan.Steps.Count} steps");
            break;
        case SessionCompletedActivity:
            Console.WriteLine("Done!");
            break;
    }
}
```

---

### Artifact Types

All artifacts inherit from the abstract `Artifact` base class.

#### ChangeSetArtifact

```csharp
public class ChangeSetArtifact : Artifact
{
    string Source { get; }                    // Source identifier
    GitPatch GitPatch { get; }                // Contains UnidiffPatch
    
    ParsedChangeSet Parse();                  // Parse the diff
}
```

#### MediaArtifact

```csharp
public class MediaArtifact : Artifact
{
    string Data { get; }                      // Base64-encoded data
    string Format { get; }                    // MIME type (e.g., "image/png")
    
    Task SaveAsync(string filepath);          // Save to file
    string ToDataUrl();                       // Convert to data URL
}
```

#### BashArtifact

```csharp
public class BashArtifact : Artifact
{
    string Command { get; }                   // Executed command
    string? Stdout { get; }                   // Standard output
    string? Stderr { get; }                   // Standard error
    int? ExitCode { get; }                    // Exit code
    
    string ToString();                        // Formatted terminal output
}
```

---

### Other Models

#### Plan

```csharp
public record Plan(
    string Id,
    IReadOnlyList<PlanStep> Steps
);

public record PlanStep(
    string Id,
    string Title,
    string? Description
);
```

#### Source

```csharp
public class Source
{
    string Name { get; }                      // Resource name
    string Id { get; }                        // Short ID
    string? RepositoryResource { get; }       // Repository reference
    SourceState State { get; }                // CONNECTED, DISCONNECTED, etc.
}
```

#### Origin

```csharp
public enum Origin { Unspecified, Agent, User, System }
```

---

## Storage

### ISessionStorage

Interface for local caching of sessions and activities.

```csharp
public interface ISessionStorage
{
    // Sessions
    Task<SessionResource?> GetSessionAsync(string sessionId, CancellationToken ct = default);
    Task UpsertSessionAsync(SessionResource session, CancellationToken ct = default);
    Task DeleteSessionAsync(string sessionId, CancellationToken ct = default);
    IAsyncEnumerable<SessionResource> ListSessionsAsync(CancellationToken ct = default);
    
    // Activities
    IAsyncEnumerable<Activity> ListActivitiesAsync(string sessionId, CancellationToken ct = default);
    Task UpsertActivityAsync(string sessionId, Activity activity, CancellationToken ct = default);
    Task DeleteActivityAsync(string sessionId, string activityId, CancellationToken ct = default);
}
```

### Implementations

| Class | Description |
|-------|-------------|
| `MemoryStorage` | In-memory (default, non-persistent) |
| `FileStorage` | JSON files in specified directory |

**Usage:**
```csharp
var client = Jules.Connect(new JulesOptions { CacheDir = "./cache" });
var cached = await client.Storage.GetSessionAsync("sessions/123");
```

---

## Exceptions

All exceptions inherit from `JulesException`.

| Exception | Description | Key Properties |
|-----------|-------------|----------------|
| `JulesException` | Base exception | — |
| `JulesNetworkException` | Network/timeout failure | `Url` |
| `JulesApiException` | Non-2xx API response | `Url`, `StatusCode`, `StatusText` |
| `JulesAuthenticationException` | 401/403 response | `Url`, `StatusCode` |
| `JulesRateLimitException` | 429 response | `Url`, `StatusCode` |
| `MissingApiKeyException` | No API key provided | — |
| `SourceNotFoundException` | Source not found | `SourceIdentifier` |
| `AutomatedSessionFailedException` | Run() ended in FAILED state | — |
| `SyncInProgressException` | Sync already running | — |
| `InvalidStateException` | Invalid operation for current state | — |

---

## Dependency Injection

### ServiceCollectionExtensions

```csharp
using JulesSdk.Extensions;

// Basic registration
services.AddJulesSdk(options =>
{
    options.ApiKey = "your-api-key";
    options.PollingIntervalMs = 3000;
});

// Shorthand with API key
services.AddJulesSdk("your-api-key");
```

**Inject and use:**
```csharp
public class MyService(IJulesClient client)
{
    public async Task DoWork()
    {
        var session = await client.RunAsync(new SessionConfig { Prompt = "..." });
        var result = await session.ResultAsync();
    }
}
```

---

## Response Types Summary

| Method | Returns |
|--------|---------|
| `RunAsync()` | `IAutomatedSession` |
| `SessionAsync()` | `ISessionClient` |
| `Session()` | `ISessionClient` |
| `SessionsAsync()` | `IAsyncEnumerable<SessionResource>` |
| `AllAsync()` | `IReadOnlyList<IAutomatedSession>` |
| `SyncAsync()` | `SyncStats` |
| `StreamAsync()` | `IAsyncEnumerable<Activity>` |
| `ResultAsync()` | `Outcome` |
| `InfoAsync()` | `SessionResource` |
| `AskAsync()` | `AgentMessagedActivity` |
| `Sources.ListAsync()` | `IAsyncEnumerable<Source>` |
| `Sources.GetAsync()` | `Source?` |
| `Storage.GetSessionAsync()` | `SessionResource?` |

---

*This SDK is not officially supported by Google. Apache-2.0 License.*
