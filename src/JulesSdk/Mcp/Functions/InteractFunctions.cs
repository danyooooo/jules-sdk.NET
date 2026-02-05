// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using JulesSdk.Mcp.Types;
using JulesSdk.Models;

namespace JulesSdk.Mcp.Functions;

/// <summary>
/// Interaction types for send_reply tool.
/// </summary>
public enum InteractAction
{
    /// <summary>Approve a pending plan.</summary>
    Approve,
    /// <summary>Send a message without waiting for reply.</summary>
    Send,
    /// <summary>Send a message and wait for agent reply.</summary>
    Ask
}

/// <summary>
/// Pure functions for session interaction operations.
/// </summary>
public static class InteractFunctions
{
    /// <summary>
    /// Interact with an active Jules session.
    /// </summary>
    public static async Task<InteractResult> InteractAsync(
        IJulesClient client,
        string sessionId,
        InteractAction action,
        string? message = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(sessionId))
            throw new ArgumentException("sessionId is required", nameof(sessionId));

        var session = client.Session(sessionId);

        switch (action)
        {
            case InteractAction.Approve:
                await session.ApproveAsync(ct);
                return new InteractResult { Success = true, Message = "Plan approved." };

            case InteractAction.Send:
                if (string.IsNullOrEmpty(message))
                    throw new ArgumentException("Message is required for 'send' action", nameof(message));
                await session.SendAsync(message, ct);
                return new InteractResult { Success = true, Message = "Message sent." };

            case InteractAction.Ask:
                if (string.IsNullOrEmpty(message))
                    throw new ArgumentException("Message is required for 'ask' action", nameof(message));
                var reply = await session.AskAsync(message, ct);
                return new InteractResult { Success = true, Reply = reply.Message };

            default:
                throw new ArgumentException($"Invalid action: {action}", nameof(action));
        }
    }
}
