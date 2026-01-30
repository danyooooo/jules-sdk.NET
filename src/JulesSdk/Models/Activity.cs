// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace JulesSdk.Models;

/// <summary>
/// An activity in a session. This is a union type - only ONE of the activity type fields
/// will be populated (agentMessaged, userMessaged, planGenerated, etc.).
/// </summary>
public class Activity
{
    /// <summary>
    /// The full resource name (e.g., "sessions/{session}/activities/{activity}").
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }
    
    /// <summary>
    /// The unique ID of the activity.
    /// </summary>
    [JsonIgnore]
    public string Id => Name?.Split('/').LastOrDefault() ?? Name ?? string.Empty;
    
    /// <summary>
    /// The time at which this activity was created (RFC 3339 timestamp).
    /// </summary>
    [JsonPropertyName("createTime")]
    public string? CreateTime { get; init; }
    
    /// <summary>
    /// The entity that this activity originated from.
    /// </summary>
    [JsonPropertyName("originator")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Origin Originator { get; init; }
    
    // =====================
    // Union Fields - Only ONE of these will be populated per activity
    // =====================
    
    /// <summary>
    /// Agent message activity data. Present when the agent posts a message.
    /// </summary>
    [JsonPropertyName("agentMessaged")]
    public AgentMessagedData? AgentMessaged { get; init; }
    
    /// <summary>
    /// User message activity data. Present when the user posts a message.
    /// </summary>
    [JsonPropertyName("userMessaged")]
    public UserMessagedData? UserMessaged { get; init; }
    
    /// <summary>
    /// Plan generated activity data. Present when a plan is generated.
    /// </summary>
    [JsonPropertyName("planGenerated")]
    public PlanGeneratedData? PlanGenerated { get; init; }
    
    /// <summary>
    /// Artifacts produced by this activity (code changes, bash output, media).
    /// </summary>
    [JsonPropertyName("artifacts")]
    public IReadOnlyList<Artifact>? Artifacts { get; init; }
    
    /// <summary>
    /// Plan approved activity data.
    /// </summary>
    [JsonPropertyName("planApproved")]
    public PlanApprovedData? PlanApproved { get; init; }
    
    /// <summary>
    /// Progress update activity data.
    /// </summary>
    [JsonPropertyName("progressUpdated")]
    public ProgressUpdatedData? ProgressUpdated { get; init; }
    
    /// <summary>
    /// Session completed activity data.
    /// </summary>
    [JsonPropertyName("sessionCompleted")]
    public SessionCompletedData? SessionCompleted { get; init; }
    
    /// <summary>
    /// Session failed activity data.
    /// </summary>
    [JsonPropertyName("sessionFailed")]
    public SessionFailedData? SessionFailed { get; init; }
    
    // =====================
    // Helper Properties
    // =====================
    
    /// <summary>
    /// Gets the type of this activity based on which field is populated.
    /// </summary>
    [JsonIgnore]
    public string Type => AgentMessaged != null ? "agentMessaged"
        : UserMessaged != null ? "userMessaged"
        : PlanGenerated != null ? "planGenerated"
        : PlanApproved != null ? "planApproved"
        : ProgressUpdated != null ? "progressUpdated"
        : SessionCompleted != null ? "sessionCompleted"
        : SessionFailed != null ? "sessionFailed"
        : Artifacts != null ? "artifacts"
        : "unknown";
    
    /// <summary>Returns true if this is an agent messaged activity.</summary>
    [JsonIgnore]
    public bool IsAgentMessaged => AgentMessaged != null;
    
    /// <summary>Returns true if this is a user messaged activity.</summary>
    [JsonIgnore]
    public bool IsUserMessaged => UserMessaged != null;
    
    /// <summary>Returns true if this is a plan generated activity.</summary>
    [JsonIgnore]
    public bool IsPlanGenerated => PlanGenerated != null;
    
    /// <summary>Returns true if this is a plan approved activity.</summary>
    [JsonIgnore]
    public bool IsPlanApproved => PlanApproved != null;
    
    /// <summary>Returns true if this activity has artifacts.</summary>
    [JsonIgnore]
    public bool HasArtifacts => Artifacts != null && Artifacts.Count > 0;
    
    /// <summary>Returns true if this is a session completed activity.</summary>
    [JsonIgnore]
    public bool IsSessionCompleted => SessionCompleted != null;
    
    /// <summary>Returns true if this is a session failed activity.</summary>
    [JsonIgnore]
    public bool IsSessionFailed => SessionFailed != null;
    
    /// <summary>
    /// Gets the message content if this is an agent or user messaged activity.
    /// </summary>
    [JsonIgnore]
    public string? Message => AgentMessaged?.AgentMessage ?? UserMessaged?.UserMessage;
}

// =====================
// Activity Data Classes
// =====================

/// <summary>
/// Data for an agent messaged activity.
/// </summary>
public class AgentMessagedData
{
    /// <summary>
    /// The message the agent posted.
    /// </summary>
    [JsonPropertyName("agentMessage")]
    public string? AgentMessage { get; init; }
}

/// <summary>
/// Data for a user messaged activity.
/// </summary>
public class UserMessagedData
{
    /// <summary>
    /// The message the user posted.
    /// </summary>
    [JsonPropertyName("userMessage")]
    public string? UserMessage { get; init; }
}

/// <summary>
/// Data for a plan generated activity.
/// </summary>
public class PlanGeneratedData
{
    /// <summary>
    /// The plan that was generated.
    /// </summary>
    [JsonPropertyName("plan")]
    public Plan? Plan { get; init; }
}

/// <summary>
/// Data for a plan approved activity.
/// </summary>
public class PlanApprovedData
{
    /// <summary>
    /// The ID of the plan that was approved.
    /// </summary>
    [JsonPropertyName("planId")]
    public string? PlanId { get; init; }
}

/// <summary>
/// Data for a progress updated activity.
/// </summary>
public class ProgressUpdatedData
{
    /// <summary>
    /// The title of the progress update.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }
    
    /// <summary>
    /// The description of the progress update.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }
}

/// <summary>
/// Data for a session completed activity.
/// </summary>
public class SessionCompletedData
{
    /// <summary>
    /// Summary of the completed session.
    /// </summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; init; }
}

/// <summary>
/// Data for a session failed activity.
/// </summary>
public class SessionFailedData
{
    /// <summary>
    /// The reason the session failed.
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }
}

// =====================
// Legacy Type Aliases (for backwards compatibility)
// =====================

/// <summary>
/// Legacy alias for an agent messaged activity.
/// </summary>
public class AgentMessagedActivity : Activity
{
    /// <summary>
    /// The message the agent posted.
    /// </summary>
    [JsonIgnore]
    public string? AgentMessage => AgentMessaged?.AgentMessage;
}

/// <summary>
/// Legacy alias for a user messaged activity.
/// </summary>
public class UserMessagedActivity : Activity
{
    /// <summary>
    /// The message the user posted.
    /// </summary>
    [JsonIgnore]
    public string? UserMessage => UserMessaged?.UserMessage;
}

/// <summary>
/// Legacy alias for a plan generated activity.
/// </summary>
public class PlanGeneratedActivity : Activity
{
    /// <summary>
    /// The generated plan.
    /// </summary>
    [JsonIgnore]
    public Plan? Plan => PlanGenerated?.Plan;
}

/// <summary>
/// Legacy alias for a plan approved activity.
/// </summary>
public class PlanApprovedActivity : Activity
{
    /// <summary>
    /// The ID of the approved plan.
    /// </summary>
    [JsonIgnore]
    public string? PlanId => PlanApproved?.PlanId;
}

/// <summary>
/// Legacy alias for a progress updated activity.
/// </summary>
public class ProgressUpdatedActivity : Activity
{
    /// <summary>
    /// The progress update title.
    /// </summary>
    [JsonIgnore]
    public string? Title => ProgressUpdated?.Title;
}

/// <summary>
/// Legacy alias for a session completed activity.
/// </summary>
public class SessionCompletedActivity : Activity { }

/// <summary>
/// Legacy alias for a session failed activity.
/// </summary>
public class SessionFailedActivity : Activity
{
    /// <summary>
    /// The reason the session failed.
    /// </summary>
    [JsonIgnore]
    public string? Reason => SessionFailed?.Reason;
}


