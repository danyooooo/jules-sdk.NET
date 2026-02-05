// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using JulesSdk.Mcp.Types;
using JulesSdk.Models;
using JulesSdk.Utilities;

namespace JulesSdk.Mcp.Tools;

/// <summary>
/// Tool to show the actual code diff for a session.
/// </summary>
public class ShowCodeDiffTool : IMcpTool
{
    public string Name => "show_code_diff";
    
    public string Description => @"Get the full unidiff patch for a session's code changes. Use this to see the actual code that was modified.

Optionally filter to a specific file.";

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
            activityId = new
            {
                type = "string",
                description = "Optional activity ID to get diff from a specific activity"
            },
            file = new
            {
                type = "string",
                description = "Optional file path to filter to a specific file's diff"
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
        var activityId = args.TryGetValue("activityId", out var aid) ? aid?.ToString() : null;
        var file = args.TryGetValue("file", out var f) ? f?.ToString() : null;
        
        if (string.IsNullOrEmpty(sessionId))
            return McpToolResult.Error("sessionId is required");

        var session = client.Session(sessionId);
        var snapshot = await session.SnapshotAsync(null, ct);
        
        // Ensure activities is safe
        var activities = snapshot.Activities ?? [];
        
        ChangeSetData? changeSet = null;
        
        if (!string.IsNullOrEmpty(activityId))
        {
            var activity = activities.FirstOrDefault(a => a.Id == activityId);
            if (activity == null)
            {
                // Activity not found, return empty result
                return McpToolResult.Json(new ShowDiffResult
                {
                    SessionId = snapshot.Id,
                    ActivityId = activityId,
                    File = file,
                    UnidiffPatch = "",
                    Files = [],
                    Summary = new FilesSummary { TotalFiles = 0, Created = 0, Modified = 0, Deleted = 0 }
                });
            }
            
            var artifact = activity.Artifacts?.FirstOrDefault(a => a.ChangeSet != null);
            changeSet = artifact?.ChangeSet;
        }
        else
        {
            changeSet = snapshot.ChangeSet();
        }

        if (changeSet?.GitPatch?.UnidiffPatch == null)
        {
            return McpToolResult.Json(new ShowDiffResult
            {
                SessionId = snapshot.Id,
                ActivityId = activityId,
                File = file,
                UnidiffPatch = "",
                Files = [],
                Summary = new FilesSummary { TotalFiles = 0, Created = 0, Modified = 0, Deleted = 0 }
            });
        }

        var unidiffPatch = changeSet!.GitPatch!.UnidiffPatch!;
        var parsed = UnidiffParser.Parse(unidiffPatch);
        
        var files = parsed.Files.Select(pf => new FileChangeDetail
        {
            Path = pf.Path,
            ChangeType = pf.ChangeType.ToString().ToLowerInvariant(),
            Additions = pf.Additions,
            Deletions = pf.Deletions
        }).ToList();

        // Filter to specific file if requested
        if (!string.IsNullOrEmpty(file))
        {
            unidiffPatch = ExtractFileDiff(unidiffPatch, file);
            files = files.Where(pf => pf.Path == file).ToList();
        }

        var summary = new FilesSummary
        {
            TotalFiles = files.Count,
            Created = files.Count(pf => pf.ChangeType == "created"),
            Modified = files.Count(pf => pf.ChangeType == "modified"),
            Deleted = files.Count(pf => pf.ChangeType == "deleted")
        };

        return McpToolResult.Json(new ShowDiffResult
        {
            SessionId = snapshot.Id,
            ActivityId = activityId,
            File = file,
            UnidiffPatch = unidiffPatch,
            Files = files,
            Summary = summary
        });
    }

    /// <summary>
    /// Extract a specific file's diff from a unidiff patch.
    /// </summary>
    private static string ExtractFileDiff(string unidiffPatch, string filePath)
    {
        if (string.IsNullOrEmpty(unidiffPatch)) return "";
        
        // Add a leading newline to handle the first entry correctly
        var patches = ("\n" + unidiffPatch).Split("\ndiff --git ");
        var targetHeader = $"a/{filePath} ";
        var patch = patches.FirstOrDefault(p => p.StartsWith(targetHeader));

        return patch != null ? $"diff --git {patch}".Trim() : "";
    }
}
