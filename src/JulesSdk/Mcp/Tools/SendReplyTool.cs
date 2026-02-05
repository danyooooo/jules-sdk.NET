// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using JulesSdk.Mcp.Functions;

namespace JulesSdk.Mcp.Tools;

/// <summary>
/// Tool to send a message or approve a plan in a Jules session.
/// </summary>
public class SendReplyTool : IMcpTool
{
    public string Name => "send_reply_to_session";
    
    public string Description => @"Send a message to a Jules session or approve a pending plan.

ACTIONS:
- ""approve"": Approve the pending plan. No message required.
- ""send"": Send a message to Jules. Message required.
- ""ask"": Send a message and wait for Jules to reply. Message required.

Use this after checking get_session_state to respond to Jules or approve plans.";

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
            action = new
            {
                type = "string",
                @enum = new[] { "approve", "send", "ask" },
                description = "The action to perform"
            },
            message = new
            {
                type = "string",
                description = "Message to send (required for 'send' and 'ask' actions)"
            }
        },
        required = new[] { "sessionId", "action" }
    };

    public async Task<McpToolResult> ExecuteAsync(
        IJulesClient client,
        Dictionary<string, object?> args,
        CancellationToken ct = default)
    {
        var sessionId = args.TryGetValue("sessionId", out var id) ? id?.ToString() : null;
        var actionStr = args.TryGetValue("action", out var a) ? a?.ToString() : null;
        var message = args.TryGetValue("message", out var m) ? m?.ToString() : null;
        
        if (string.IsNullOrEmpty(sessionId))
            return McpToolResult.Error("sessionId is required");
        
        if (string.IsNullOrEmpty(actionStr))
            return McpToolResult.Error("action is required");

        if (!Enum.TryParse<InteractAction>(actionStr, ignoreCase: true, out var action))
            return McpToolResult.Error($"Invalid action: {actionStr}. Must be 'approve', 'send', or 'ask'");

        var result = await InteractFunctions.InteractAsync(client, sessionId, action, message, ct);
        return McpToolResult.Json(result);
    }
}
