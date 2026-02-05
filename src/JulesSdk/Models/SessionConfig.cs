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
    /// A unique key to identify this session for later resumption.
    /// If provided, the session ID will be stored locally under this key.
    /// Use client.ResumeAsync(key) to recover the session handle.
    /// </summary>
    public string? ResumeKey { get; init; }
}

