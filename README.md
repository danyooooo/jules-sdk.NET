# Jules SDK for .NET

Unofficial .NET SDK for interacting with Google Jules (AI Agent).

> **Note:** This is not an officially supported Google product.

## Installation

```bash
dotnet add package JulesSdk
```

## Quick Start

```csharp
using JulesSdk;
using JulesSdk.Models;

// Set JULES_API_KEY environment variable or pass it directly
var client = Jules.Client;

// Create an automated session
var session = await client.RunAsync(new SessionConfig
{
    Prompt = "Fix the login button bug",
    Source = new SourceInput("owner/repo", "main"),
    AutoPr = true
});

// Stream activities as they happen
await foreach (var activity in session.StreamAsync())
{
    switch (activity)
    {
        case PlanGeneratedActivity plan:
            Console.WriteLine($"Plan: {plan.Plan.Steps.Count} steps");
            break;
        case AgentMessagedActivity msg:
            Console.WriteLine($"Agent: {msg.Message}");
            break;
        case SessionCompletedActivity:
            Console.WriteLine("Session complete!");
            break;
    }
}

// Get the result
var outcome = await session.ResultAsync();
if (outcome.PullRequest != null)
{
    Console.WriteLine($"PR: {outcome.PullRequest.Url}");
}
```

## Interactive Sessions

For workflows requiring human oversight:

```csharp
var session = await client.SessionAsync(new SessionConfig
{
    Prompt = "Refactor the authentication module",
    Source = new SourceInput("owner/repo", "develop")
});

// Wait for plan approval
await session.WaitForAsync(SessionState.AwaitingPlanApproval);
Console.WriteLine("Plan ready. Approving...");
await session.ApproveAsync();

// Ask questions
var reply = await session.AskAsync("Start with the first step");
Console.WriteLine($"Agent: {reply.Message}");

// Get final result
var outcome = await session.ResultAsync();
```

## Repoless Sessions

Run sessions without a GitHub repository:

```csharp
var session = await client.RunAsync(new SessionConfig
{
    Prompt = "Generate a Python script that calculates fibonacci numbers"
});

var result = await session.ResultAsync();
var files = result.GeneratedFiles();
var script = files.Get("fibonacci.py");
Console.WriteLine(script?.Content);
```

## Batch Processing

Process multiple tasks in parallel:

```csharp
var tasks = new[] { "Fix login bug", "Update README", "Add tests" };

var sessions = await client.AllAsync(tasks, task => new SessionConfig
{
    Prompt = task,
    Source = new SourceInput("owner/repo", "main")
}, new BatchOptions { Concurrency = 3 });
```

## Dependency Injection

```csharp
// Basic setup
services.AddJulesSdk(options =>
{
    options.ApiKey = "your-api-key";
    options.PollingIntervalMs = 3000;
});

// Or with just an API key
services.AddJulesSdk("your-api-key");

// Then inject and use
public class MyService(IJulesClient julesClient)
{
    public async Task DoWorkAsync()
    {
        var session = await julesClient.RunAsync(new SessionConfig { ... });
    }
}
```

## Local Storage & Caching

Cache session data locally for offline access and faster lookups:

```csharp
// File-based storage
var client = Jules.Connect(new JulesOptions
{
    ApiKey = "your-api-key",
    CacheDir = "./jules-cache"
});

// Sync sessions to local cache
var stats = await client.SyncAsync(new SyncOptions
{
    Limit = 100,
    Depth = SyncDepth.Activities
});
Console.WriteLine($"Synced {stats.SessionsIngested} sessions");

// Access cached data directly
var cachedSession = await client.Storage.GetSessionAsync("sessions/123");
await foreach (var activity in client.Storage.ListActivitiesAsync("sessions/123"))
{
    Console.WriteLine(activity.Type);
}
```

## Configuration

```csharp
var client = Jules.Connect(new JulesOptions
{
    ApiKey = "your-api-key",
    PollingIntervalMs = 5000,
    RequestTimeoutMs = 30000,
    CacheDir = "./cache"  // Optional: enables file-based caching
});
```

> **Note:** The API endpoint is hardcoded to `https://jules.googleapis.com/v1alpha`.

## Artifacts

Handle code changes, media, and bash output:

```csharp
await foreach (var activity in session.StreamAsync())
{
    foreach (var artifact in activity.Artifacts ?? [])
    {
        switch (artifact)
        {
            case ChangeSetArtifact changeSet:
                var parsed = changeSet.Parse();
                Console.WriteLine($"Changed {parsed.Summary.TotalFiles} files");
                foreach (var file in parsed.Files)
                {
                    Console.WriteLine($"  {file.ChangeType}: {file.Path}");
                }
                break;
                
            case MediaArtifact media when media.Format.StartsWith("image/"):
                await media.SaveAsync($"./screenshot-{activity.Id}.png");
                break;
                
            case BashArtifact bash:
                Console.WriteLine(bash.ToString());
                break;
        }
    }
}
```

## Requirements

- .NET 8.0, 9.0, or 10.0
- Valid Jules API key

## License

Apache-2.0
