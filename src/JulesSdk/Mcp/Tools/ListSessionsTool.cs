// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using JulesSdk.Models;

namespace JulesSdk.Mcp.Tools;

/// <summary>
/// Tool to list recent Jules sessions.
/// </summary>
public class ListSessionsTool : IMcpTool
{
    public string Name => "list_sessions";
    
    public string Description => "List recent Jules sessions with optional pagination.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            pageSize = new
            {
                type = "integer",
                description = "Number of sessions to return. Default: 10"
            },
            pageToken = new
            {
                type = "string",
                description = "Pagination token for next page"
            }
        }
    };

    public async Task<McpToolResult> ExecuteAsync(
        IJulesClient client,
        Dictionary<string, object?> args,
        CancellationToken ct = default)
    {
        var pageSize = args.TryGetValue("pageSize", out var ps) && ps != null 
            ? Convert.ToInt32(ps) 
            : 10;
        var pageToken = args.TryGetValue("pageToken", out var pt) ? pt?.ToString() : null;

        var options = new ListSessionsOptions
        {
            PageSize = pageSize,
            PageToken = pageToken
        };

        var sessions = new List<object>();
        await foreach (var session in client.SessionsAsync(options, ct))
        {
            sessions.Add(new
            {
                id = session.Id,
                title = session.Title,
                state = session.State.ToString(),
                url = session.Url
            });
            
            if (sessions.Count >= pageSize) break;
        }

        return McpToolResult.Json(new { sessions });
    }
}
