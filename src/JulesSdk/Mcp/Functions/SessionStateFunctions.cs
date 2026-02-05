// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using JulesSdk.Mcp.Types;
using JulesSdk.Models;

namespace JulesSdk.Mcp.Functions;

/// <summary>
/// Pure functions for session state operations.
/// </summary>
public static class SessionStateFunctions
{
    private static readonly HashSet<string> BusyStates = new(StringComparer.OrdinalIgnoreCase)
    {
        "queued", "QUEUED",
        "planning", "PLANNING", 
        "inProgress", "IN_PROGRESS", "in_progress",
        "Starting", "Working"
    };

    private static readonly HashSet<string> FailedStates = new(StringComparer.OrdinalIgnoreCase)
    {
        "failed", "FAILED"
    };

    /// <summary>
    /// Derives semantic status from technical session state.
    /// </summary>
    public static SessionStatus DeriveStatus(string state)
    {
        if (FailedStates.Contains(state)) return SessionStatus.Failed;
        if (BusyStates.Contains(state)) return SessionStatus.Busy;
        return SessionStatus.Stable;
    }
    
    /// <summary>
    /// Derives semantic status from SessionState enum.
    /// </summary>
    public static SessionStatus DeriveStatus(SessionState state) => state switch
    {
        SessionState.Queued => SessionStatus.Busy,
        SessionState.Planning => SessionStatus.Busy,
        SessionState.InProgress => SessionStatus.Busy,
        SessionState.Failed => SessionStatus.Failed,
        _ => SessionStatus.Stable
    };

    /// <summary>
    /// Find the last activity from the activities list.
    /// </summary>
    public static LastActivity? FindLastActivity(IReadOnlyList<Activity> activities)
    {
        if (activities.Count == 0) return null;

        var sorted = activities
            .Where(a => !string.IsNullOrEmpty(a.CreateTime))
            .OrderByDescending(a => DateTime.TryParse(a.CreateTime, out var dt) ? dt : DateTime.MinValue)
            .ToList();

        var last = sorted.FirstOrDefault();
        if (last == null) return null;

        return new LastActivity
        {
            ActivityId = last.Id,
            Type = last.Type,
            Timestamp = last.CreateTime ?? ""
        };
    }

    /// <summary>
    /// Find the last agent message from activities.
    /// </summary>
    public static LastAgentMessage? FindLastAgentMessage(IReadOnlyList<Activity> activities)
    {
        var sorted = activities
            .Where(a => a.IsAgentMessaged && !string.IsNullOrEmpty(a.CreateTime))
            .OrderByDescending(a => DateTime.TryParse(a.CreateTime, out var dt) ? dt : DateTime.MinValue)
            .ToList();

        var lastMessage = sorted.FirstOrDefault();
        if (lastMessage == null) return null;

        var content = lastMessage.Message;
        if (string.IsNullOrEmpty(content)) return null;

        return new LastAgentMessage
        {
            ActivityId = lastMessage.Id,
            Content = content,
            Timestamp = lastMessage.CreateTime ?? ""
        };
    }

    /// <summary>
    /// Find a pending plan from activities.
    /// </summary>
    public static PendingPlan? FindPendingPlan(IReadOnlyList<Activity> activities)
    {
        var sorted = activities
            .Where(a => !string.IsNullOrEmpty(a.CreateTime))
            .OrderByDescending(a => DateTime.TryParse(a.CreateTime, out var dt) ? dt : DateTime.MinValue)
            .ToList();

        // Find the most recent planGenerated
        var planActivity = sorted.FirstOrDefault(a => a.IsPlanGenerated);
        if (planActivity == null) return null;

        var planTime = DateTime.TryParse(planActivity.CreateTime, out var pt) ? pt : DateTime.MinValue;

        // Check if there's a planApproved after this planGenerated
        var planApproved = sorted.FirstOrDefault(a =>
            a.IsPlanApproved &&
            DateTime.TryParse(a.CreateTime, out var approvedTime) &&
            approvedTime > planTime);

        // If plan was approved, it's not pending
        if (planApproved != null) return null;

        var plan = planActivity.PlanGenerated?.Plan;
        if (plan == null) return null;

        return new PendingPlan
        {
            ActivityId = planActivity.Id,
            PlanId = plan.Id ?? "",
            Steps = plan.Steps?.Select(s => new PlanStepSummary
            {
                Title = s.Title ?? "",
                Description = s.Description
            }).ToList() ?? []
        };
    }

    /// <summary>
    /// Check if session has ever been in a stable state before.
    /// This is detected by looking for sessionCompleted or planApproved activities.
    /// </summary>
    public static bool HasStableHistory(IReadOnlyList<Activity> activities)
    {
        return activities.Any(a => a.IsSessionCompleted || a.IsPlanApproved);
    }

    /// <summary>
    /// Get the current state of a Jules session.
    /// </summary>
    public static async Task<SessionStateResult> GetSessionStateAsync(
        IJulesClient client,
        string sessionId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(sessionId))
            throw new ArgumentException("sessionId is required", nameof(sessionId));

        var session = client.Session(sessionId);
        var snapshot = await session.SnapshotAsync(null, ct);

        // Ensure activities is safe
        var activities = snapshot.Activities ?? [];

        var pr = snapshot.PullRequest;
        var lastActivity = FindLastActivity(activities);
        var lastAgentMessage = FindLastAgentMessage(activities);
        var pendingPlan = FindPendingPlan(activities);

        return new SessionStateResult
        {
            Id = snapshot.Id,
            Status = DeriveStatus(snapshot.State),
            Url = snapshot.Url ?? "",
            Title = snapshot.Title ?? "",
            Prompt = snapshot.Prompt,
            Pr = pr != null ? new PrInfo { Url = pr.Url ?? "", Title = pr.Title ?? "" } : null,
            LastActivity = lastActivity,
            LastAgentMessage = lastAgentMessage,
            PendingPlan = pendingPlan
        };
    }
}
