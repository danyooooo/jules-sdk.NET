// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

namespace JulesSdk.Models;

/// <summary>
/// Configuration options for starting a new session or run.
/// </summary>
public class SessionConfig
{
    /// <summary>
    /// The initial instruction or task description for the agent.
    /// Required.
    /// </summary>
    public required string Prompt { get; init; }
    
    /// <summary>
    /// The source code context for the session.
    /// Optional. If omitted, creates a "repoless" session not attached to any repository.
    /// </summary>
    public SourceInput? Source { get; init; }
    
    /// <summary>
    /// Optional title for the session. If not provided, the system will generate one.
    /// </summary>
    public string? Title { get; init; }
    
    /// <summary>
    /// If true, the agent will pause and wait for explicit approval before executing any generated plan.
    /// Defaults to false for Run(), true for Session().
    /// </summary>
    public bool? RequireApproval { get; init; }
    
    /// <summary>
    /// If true, the agent will automatically create a Pull Request when the task is completed.
    /// Defaults to true for Run().
    /// </summary>
    public bool? AutoPr { get; init; }
    
    /// <summary>
    /// The ID of the user who owns this session.
    /// Primarily used by the Proxy/Authorization layer.
    /// </summary>
    public string? OwnerId { get; init; }
}
