// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;
using JulesSdk.Models;

namespace JulesSdk.Mcp.Types;

/// <summary>
/// Semantic status indicating the session's current operational state.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SessionStatus>))]
public enum SessionStatus
{
    /// <summary>Jules is actively working. Data is volatile.</summary>
    Busy,
    /// <summary>Work is paused. Safe to review code changes.</summary>
    Stable,
    /// <summary>Session encountered an error and cannot continue.</summary>
    Failed
}

/// <summary>
/// The last activity in the session.
/// </summary>
public class LastActivity
{
    /// <summary>Activity ID.</summary>
    [JsonPropertyName("activityId")]
    public required string ActivityId { get; init; }
    
    /// <summary>Activity type (e.g., 'agentMessaged', 'sessionCompleted').</summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }
    
    /// <summary>When the activity occurred.</summary>
    [JsonPropertyName("timestamp")]
    public required string Timestamp { get; init; }
}

/// <summary>
/// The last message sent by Jules to the user.
/// </summary>
public class LastAgentMessage
{
    /// <summary>Activity ID containing this message.</summary>
    [JsonPropertyName("activityId")]
    public required string ActivityId { get; init; }
    
    /// <summary>The message content.</summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }
    
    /// <summary>When the message was sent.</summary>
    [JsonPropertyName("timestamp")]
    public required string Timestamp { get; init; }
}

/// <summary>
/// A step in a pending plan.
/// </summary>
public class PlanStepSummary
{
    /// <summary>Step title.</summary>
    [JsonPropertyName("title")]
    public required string Title { get; init; }
    
    /// <summary>Step description.</summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }
}

/// <summary>
/// A plan awaiting approval.
/// </summary>
public class PendingPlan
{
    /// <summary>Activity ID that generated this plan.</summary>
    [JsonPropertyName("activityId")]
    public required string ActivityId { get; init; }
    
    /// <summary>Plan ID (use this when approving).</summary>
    [JsonPropertyName("planId")]
    public required string PlanId { get; init; }
    
    /// <summary>The steps in the plan.</summary>
    [JsonPropertyName("steps")]
    public required IReadOnlyList<PlanStepSummary> Steps { get; init; }
}

/// <summary>
/// Result of get_session_state tool.
/// </summary>
public class SessionStateResult
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }
    
    [JsonPropertyName("status")]
    public required SessionStatus Status { get; init; }
    
    [JsonPropertyName("url")]
    public required string Url { get; init; }
    
    [JsonPropertyName("title")]
    public required string Title { get; init; }
    
    [JsonPropertyName("prompt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Prompt { get; init; }
    
    [JsonPropertyName("pr")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PrInfo? Pr { get; init; }
    
    [JsonPropertyName("lastActivity")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LastActivity? LastActivity { get; init; }
    
    [JsonPropertyName("lastAgentMessage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LastAgentMessage? LastAgentMessage { get; init; }
    
    [JsonPropertyName("pendingPlan")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PendingPlan? PendingPlan { get; init; }
}

/// <summary>
/// Pull request info.
/// </summary>
public class PrInfo
{
    [JsonPropertyName("url")]
    public required string Url { get; init; }
    
    [JsonPropertyName("title")]
    public required string Title { get; init; }
}

/// <summary>
/// Result of create_session tool.
/// </summary>
public class CreateSessionResult
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }
}

/// <summary>
/// Result of interact (send_reply) tool.
/// </summary>
public class InteractResult
{
    [JsonPropertyName("success")]
    public required bool Success { get; init; }
    
    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; init; }
    
    [JsonPropertyName("reply")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Reply { get; init; }
}

/// <summary>
/// File change info for code review.
/// </summary>
public class FileChange
{
    [JsonPropertyName("path")]
    public required string Path { get; init; }
    
    [JsonPropertyName("changeType")]
    public required string ChangeType { get; init; }
    
    [JsonPropertyName("activityIds")]
    public required IReadOnlyList<string> ActivityIds { get; init; }
    
    [JsonPropertyName("additions")]
    public required int Additions { get; init; }
    
    [JsonPropertyName("deletions")]
    public required int Deletions { get; init; }
}

/// <summary>
/// Summary of file changes.
/// </summary>
public class FilesSummary
{
    [JsonPropertyName("totalFiles")]
    public required int TotalFiles { get; init; }
    
    [JsonPropertyName("created")]
    public required int Created { get; init; }
    
    [JsonPropertyName("modified")]
    public required int Modified { get; init; }
    
    [JsonPropertyName("deleted")]
    public required int Deleted { get; init; }
}

/// <summary>
/// Result of code review tool.
/// </summary>
public class ReviewChangesResult
{
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }
    
    [JsonPropertyName("title")]
    public required string Title { get; init; }
    
    [JsonPropertyName("state")]
    public required string State { get; init; }
    
    [JsonPropertyName("status")]
    public required SessionStatus Status { get; init; }
    
    [JsonPropertyName("url")]
    public required string Url { get; init; }
    
    [JsonPropertyName("files")]
    public required IReadOnlyList<FileChange> Files { get; init; }
    
    [JsonPropertyName("summary")]
    public required FilesSummary Summary { get; init; }
    
    [JsonPropertyName("formatted")]
    public required string Formatted { get; init; }
    
    [JsonPropertyName("hasStableHistory")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? HasStableHistory { get; init; }
    
    [JsonPropertyName("warning")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Warning { get; init; }
    
    [JsonPropertyName("pr")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PrInfo? Pr { get; init; }
}

/// <summary>
/// Result of show diff tool.
/// </summary>
public class ShowDiffResult
{
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }
    
    [JsonPropertyName("activityId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ActivityId { get; init; }
    
    [JsonPropertyName("file")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? File { get; init; }
    
    [JsonPropertyName("unidiffPatch")]
    public required string UnidiffPatch { get; init; }
    
    [JsonPropertyName("files")]
    public required IReadOnlyList<FileChangeDetail> Files { get; init; }
    
    [JsonPropertyName("summary")]
    public required FilesSummary Summary { get; init; }
}

/// <summary>
/// File change detail for diffs.
/// </summary>
public class FileChangeDetail
{
    [JsonPropertyName("path")]
    public required string Path { get; init; }
    
    [JsonPropertyName("changeType")]
    public required string ChangeType { get; init; }
    
    [JsonPropertyName("additions")]
    public required int Additions { get; init; }
    
    [JsonPropertyName("deletions")]
    public required int Deletions { get; init; }
}
