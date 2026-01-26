// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

namespace JulesSdk.Models;

/// <summary>
/// Represents the possible states of a session.
/// </summary>
public enum SessionState
{
    /// <summary>Unspecified state.</summary>
    Unspecified,
    
    /// <summary>Session is queued for processing.</summary>
    Queued,
    
    /// <summary>Agent is planning the task.</summary>
    Planning,
    
    /// <summary>Agent is waiting for plan approval. Call ApproveAsync().</summary>
    AwaitingPlanApproval,
    
    /// <summary>Agent is waiting for user feedback.</summary>
    AwaitingUserFeedback,
    
    /// <summary>Session is actively being worked on.</summary>
    InProgress,
    
    /// <summary>Session is paused.</summary>
    Paused,
    
    /// <summary>Session has failed.</summary>
    Failed,
    
    /// <summary>Session completed successfully.</summary>
    Completed
}
