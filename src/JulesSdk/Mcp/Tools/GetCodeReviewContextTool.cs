// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using JulesSdk.Mcp.Functions;
using JulesSdk.Mcp.Types;
using JulesSdk.Models;
using JulesSdk.Utilities;

namespace JulesSdk.Mcp.Tools;

/// <summary>
/// Tool to get code review context for a session.
/// </summary>
public class GetCodeReviewContextTool : IMcpTool
{
    public string Name => "get_code_review_context";
    
    public string Description => @"Get a summary of session changes with file list and metadata. Use this to understand what Jules has changed before reviewing the full diff.

Returns session info, file changes summary, and formatted output.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            sessionId = new
            {
                type = "string",
                description = "The session ID"
            },
            format = new
            {
                type = "string",
                @enum = new[] { "summary", "tree", "detailed", "markdown" },
                description = "Output format. Default: summary"
            },
            filter = new
            {
                type = "string",
                @enum = new[] { "all", "created", "modified", "deleted" },
                description = "Filter files by change type. Default: all"
            }
        },
        required = new[] { "sessionId" }
    };

    public async Task<McpToolResult> ExecuteAsync(
        IJulesClient client,
        Dictionary<string, object?> args,
        CancellationToken ct = default)
    {
        var sessionId = args.TryGetValue("sessionId", out var id) ? id?.ToString() : null;
        var format = args.TryGetValue("format", out var f) ? f?.ToString() ?? "summary" : "summary";
        var filter = args.TryGetValue("filter", out var fl) ? fl?.ToString() ?? "all" : "all";
        
        if (string.IsNullOrEmpty(sessionId))
            return McpToolResult.Error("sessionId is required");

        var session = client.Session(sessionId);
        var snapshot = await session.SnapshotAsync(null, ct);
        
        // Ensure activities is safe
        var activities = snapshot.Activities ?? [];

        var status = SessionStateFunctions.DeriveStatus(snapshot.State);
        var hasStableHistory = SessionStateFunctions.HasStableHistory(activities);
        var isBusy = status == SessionStatus.Busy;
        
        string? warning = isBusy && hasStableHistory 
            ? "⚠️ Session was previously stable. Current changes may modify earlier work." 
            : null;
        
        // Get files from changeSet
        var changeSet = snapshot.ChangeSet();
        var files = new List<FileChange>();
        
        if (changeSet != null)
        {
            var parsed = UnidiffParser.Parse(changeSet!.GitPatch?.UnidiffPatch ?? "");
            foreach (var file in parsed.Files)
            {
                if (filter != "all" && !file.ChangeType.ToString().Equals(filter, StringComparison.OrdinalIgnoreCase))
                    continue;
                    
                files.Add(new FileChange
                {
                    Path = file.Path,
                    ChangeType = file.ChangeType.ToString().ToLowerInvariant(),
                    ActivityIds = ["outcome"],
                    Additions = file.Additions,
                    Deletions = file.Deletions
                });
            }
        }

        var summary = new FilesSummary
        {
            TotalFiles = files.Count,
            Created = files.Count(f => f.ChangeType == "created"),
            Modified = files.Count(f => f.ChangeType == "modified"),
            Deleted = files.Count(f => f.ChangeType == "deleted")
        };

        // Format output based on format option
        var formatted = format switch
        {
            "markdown" => snapshot.ToMarkdown(),
            "tree" => FormatAsTree(files),
            "detailed" => FormatAsDetailed(files),
            _ => FormatAsSummary(snapshot.Title ?? "", snapshot.State.ToString(), status, snapshot.Url ?? "", files, summary, warning)
        };

        var pr = snapshot.PullRequest;
        var result = new ReviewChangesResult
        {
            SessionId = snapshot.Id,
            Title = snapshot.Title ?? "",
            State = snapshot.State.ToString(),
            Status = status,
            Url = snapshot.Url ?? "",
            Files = files,
            Summary = summary,
            Formatted = formatted,
            HasStableHistory = hasStableHistory,
            Warning = warning,
            Pr = pr != null ? new PrInfo { Url = pr.Url ?? "", Title = pr.Title ?? "" } : null
        };

        return McpToolResult.Json(result);
    }

    private static string FormatAsTree(IReadOnlyList<FileChange> files)
    {
        var lines = new List<string>();
        var byDir = files.GroupBy(f => 
        {
            var parts = f.Path.Split('/');
            return parts.Length > 1 ? string.Join("/", parts[..^1]) : ".";
        }).OrderBy(g => g.Key);

        foreach (var group in byDir)
        {
            lines.Add($"{group.Key}/");
            foreach (var file in group)
            {
                var basename = file.Path.Split('/').Last();
                var icon = file.ChangeType switch
                {
                    "created" => "🟢",
                    "deleted" => "🔴",
                    _ => "🟡"
                };
                var stats = file.ChangeType == "deleted"
                    ? $"(-{file.Deletions})"
                    : $"(+{file.Additions}{(file.Deletions > 0 ? $" / -{file.Deletions}" : "")})";
                lines.Add($"  {icon} {basename} {stats}");
            }
        }
        return string.Join("\n", lines);
    }

    private static string FormatAsDetailed(IReadOnlyList<FileChange> files)
    {
        var lines = new List<string>();
        foreach (var file in files)
        {
            var icon = file.ChangeType switch
            {
                "created" => "🟢",
                "deleted" => "🔴",
                _ => "🟡"
            };
            lines.Add($"{icon} {file.Path}");
            lines.Add($"   Type: {file.ChangeType}");
            lines.Add($"   Lines: +{file.Additions} / -{file.Deletions}");
        }
        return string.Join("\n", lines);
    }

    private static string FormatAsSummary(string title, string state, SessionStatus status, string url, 
        IReadOnlyList<FileChange> files, FilesSummary summary, string? warning)
    {
        var lines = new List<string>
        {
            $"Session: \"{title}\" ({state})",
            $"URL: {url}",
            ""
        };
        
        // Warning for busy sessions with stable history
        if (!string.IsNullOrEmpty(warning))
        {
            lines.Add(warning);
            lines.Add("");
        }

        var totalAdditions = files.Sum(f => f.Additions);
        var totalDeletions = files.Sum(f => f.Deletions);
        var inProgressMarker = status == SessionStatus.Busy ? " (in progress)" : "";

        lines.Add($"📊 Summary: {summary.TotalFiles} files changed (+{totalAdditions} / -{totalDeletions}){inProgressMarker}");
        if (summary.Created > 0) lines.Add($"  🟢 {summary.Created} created");
        if (summary.Modified > 0) lines.Add($"  🟡 {summary.Modified} modified");
        if (summary.Deleted > 0) lines.Add($"  🔴 {summary.Deleted} deleted");
        lines.Add("");
        
        lines.Add("📁 Changes:");
        lines.Add(FormatAsTree(files));
        
        return string.Join("\n", lines);
    }
}
