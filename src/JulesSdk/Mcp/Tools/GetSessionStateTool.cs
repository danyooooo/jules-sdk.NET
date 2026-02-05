// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using JulesSdk.Mcp.Functions;

namespace JulesSdk.Mcp.Tools;

/// <summary>
/// Tool to get the current state of a Jules session.
/// </summary>
public class GetSessionStateTool : IMcpTool
{
    public string Name => "get_session_state";
    
    public string Description => @"Get the current status of a Jules session. Acts as a dashboard to determine if Jules is busy, waiting, or failed.

RETURNS: id, status, url, title, prompt, pr (if created), lastActivity, lastAgentMessage (if any), pendingPlan (if awaiting approval)

STATUS (use this to decide what action to take):
- ""busy"": Jules is actively working. Peek with get_code_review_context if needed.
- ""stable"": Work is paused. Safe to review code, send messages, or check outputs.
- ""failed"": System-level failure. Session cannot continue.

NEXT ACTIONS:
- busy → Wait for completion, or peek with get_code_review_context
- stable + pendingPlan → Review the plan steps, then approve or send feedback
- stable + lastAgentMessage → Read message, respond if Jules asked something
- stable + no message → Review PR or code changes
- failed → Report to user. Session is unrecoverable.

IMPORTANT: You can send messages to ANY session regardless of status.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            sessionId = new
            {
                type = "string",
                description = "The session ID (numeric string)"
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
        
        if (string.IsNullOrEmpty(sessionId))
            return McpToolResult.Error("sessionId is required");

        var result = await SessionStateFunctions.GetSessionStateAsync(client, sessionId, ct);
        return McpToolResult.Json(result);
    }
}
