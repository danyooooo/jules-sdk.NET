// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

namespace JulesSdk.Models;

/// <summary>
/// Computed analytics and insights derived from the session's activities.
/// </summary>
public class SessionInsights
{
    /// <summary>
    /// Number of times the session reached a completed state.
    /// </summary>
    public int CompletionAttempts { get; init; }
    
    /// <summary>
    /// Number of times a plan was regenerated.
    /// </summary>
    public int PlanRegenerations { get; init; }
    
    /// <summary>
    /// Number of user interventions (messages sent by the user).
    /// </summary>
    public int UserInterventions { get; init; }
    
    /// <summary>
    /// Activities containing bash commands that failed (non-zero exit code).
    /// </summary>
    public IReadOnlyList<Activity> FailedCommands { get; init; } = [];
}
