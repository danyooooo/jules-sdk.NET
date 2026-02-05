// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using JulesSdk.Models;

namespace JulesSdk.Mcp.Tools;

/// <summary>
/// Tool to create a new Jules session.
/// </summary>
public class CreateSessionTool : IMcpTool
{
    public string Name => "create_session";
    
    public string Description => @"Creates a new Jules session or automated run to perform code tasks. If repo and branch are omitted, creates a ""repoless"" session where the user provides their own context in the prompt and Jules will perform code tasks based on that context instead of a GitHub repo.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            prompt = new
            {
                type = "string",
                description = "The task for the agent."
            },
            repo = new
            {
                type = "string",
                description = "GitHub repository (owner/repo). Optional for repoless sessions."
            },
            branch = new
            {
                type = "string",
                description = "Target branch. Optional for repoless sessions."
            },
            interactive = new
            {
                type = "boolean",
                description = "If true, waits for plan approval. Defaults to false (automated run)."
            },
            autoPr = new
            {
                type = "boolean",
                description = "Automatically create a PR on completion. Defaults to true."
            }
        },
        required = new[] { "prompt" }
    };

    public async Task<McpToolResult> ExecuteAsync(
        IJulesClient client,
        Dictionary<string, object?> args,
        CancellationToken ct = default)
    {
        var prompt = args.TryGetValue("prompt", out var p) ? p?.ToString() : null;
        
        if (string.IsNullOrEmpty(prompt))
            return McpToolResult.Error("prompt is required");

        var repo = args.TryGetValue("repo", out var r) ? r?.ToString() : null;
        var branch = args.TryGetValue("branch", out var b) ? b?.ToString() : null;
        var interactive = args.TryGetValue("interactive", out var i) && i is true;
        var autoPr = !args.TryGetValue("autoPr", out var ap) || ap is not false; // default true

        var config = new SessionConfig
        {
            Prompt = prompt,
            Source = !string.IsNullOrEmpty(repo) ? new SourceInput(repo, branch) : null,
            RequireApproval = interactive,
            AutoPr = autoPr
        };

        if (interactive)
        {
            var session = await client.SessionAsync(config, ct);
            return McpToolResult.Text($"Session created. ID: {session.Id}");
        }
        else
        {
            var session = await client.RunAsync(config, ct);
            return McpToolResult.Text($"Session created. ID: {session.Id}");
        }
    }
}
