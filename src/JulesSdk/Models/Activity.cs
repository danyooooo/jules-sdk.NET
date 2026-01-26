// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace JulesSdk.Models;

/// <summary>
/// Base class for all activities.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(AgentMessagedActivity), "agentMessaged")]
[JsonDerivedType(typeof(UserMessagedActivity), "userMessaged")]
[JsonDerivedType(typeof(PlanGeneratedActivity), "planGenerated")]
[JsonDerivedType(typeof(PlanApprovedActivity), "planApproved")]
[JsonDerivedType(typeof(ProgressUpdatedActivity), "progressUpdated")]
[JsonDerivedType(typeof(SessionCompletedActivity), "sessionCompleted")]
[JsonDerivedType(typeof(SessionFailedActivity), "sessionFailed")]
public abstract class Activity
{
    /// <summary>
    /// The activity type discriminator.
    /// </summary>
    [JsonPropertyName("type")]
    public abstract string Type { get; }
    
    /// <summary>
    /// The full resource name (e.g., "sessions/{session}/activities/{activity}").
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    
    /// <summary>
    /// The unique ID of the activity.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id => Name.Split('/').LastOrDefault() ?? Name;
    
    /// <summary>
    /// The time at which this activity was created (RFC 3339 timestamp).
    /// </summary>
    [JsonPropertyName("createTime")]
    public required string CreateTime { get; init; }
    
    /// <summary>
    /// The entity that this activity originated from.
    /// </summary>
    [JsonPropertyName("originator")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Origin Originator { get; init; }
    
    /// <summary>
    /// A description of this activity.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }
    
    /// <summary>
    /// The artifacts produced by this activity.
    /// </summary>
    [JsonPropertyName("artifacts")]
    public IReadOnlyList<Artifact>? Artifacts { get; init; }
}

/// <summary>
/// An activity representing a message from the agent.
/// </summary>
public class AgentMessagedActivity : Activity
{
    /// <inheritdoc/>
    public override string Type => "agentMessaged";
    
    /// <summary>
    /// The message the agent posted.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }
}

/// <summary>
/// An activity representing a message from the user.
/// </summary>
public class UserMessagedActivity : Activity
{
    /// <inheritdoc/>
    public override string Type => "userMessaged";
    
    /// <summary>
    /// The message the user posted.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }
}

/// <summary>
/// An activity representing a newly generated plan.
/// </summary>
public class PlanGeneratedActivity : Activity
{
    /// <inheritdoc/>
    public override string Type => "planGenerated";
    
    /// <summary>
    /// The plan that was generated.
    /// </summary>
    [JsonPropertyName("plan")]
    public required Plan Plan { get; init; }
}

/// <summary>
/// An activity representing the approval of a plan.
/// </summary>
public class PlanApprovedActivity : Activity
{
    /// <inheritdoc/>
    public override string Type => "planApproved";
    
    /// <summary>
    /// The ID of the plan that was approved.
    /// </summary>
    [JsonPropertyName("planId")]
    public required string PlanId { get; init; }
}

/// <summary>
/// An activity representing a progress update from the agent.
/// </summary>
public class ProgressUpdatedActivity : Activity
{
    /// <inheritdoc/>
    public override string Type => "progressUpdated";
    
    /// <summary>
    /// The title of the progress update.
    /// </summary>
    [JsonPropertyName("title")]
    public required string Title { get; init; }
}

/// <summary>
/// An activity signifying the successful completion of a session.
/// </summary>
public class SessionCompletedActivity : Activity
{
    /// <inheritdoc/>
    public override string Type => "sessionCompleted";
}

/// <summary>
/// An activity signifying the failure of a session.
/// </summary>
public class SessionFailedActivity : Activity
{
    /// <inheritdoc/>
    public override string Type => "sessionFailed";
    
    /// <summary>
    /// The reason the session failed.
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }
}
