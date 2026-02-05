// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace JulesSdk.Models;

/// <summary>
/// The underlying data structure representing a Session resource from the REST API.
/// </summary>
public class SessionResource
{
    /// <summary>
    /// The full resource name (e.g., "sessions/314159...").
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }
    
    /// <summary>
    /// The unique ID of the session.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id => Name?.Replace("sessions/", "") ?? "";
    
    /// <summary>
    /// The initial prompt for the session.
    /// </summary>
    [JsonPropertyName("prompt")]
    public string? Prompt { get; init; }
    
    /// <summary>
    /// The source context for the session.
    /// </summary>
    [JsonPropertyName("sourceContext")]
    public SourceContext? SourceContext { get; init; }
    
    /// <summary>
    /// The source associated with this session.
    /// </summary>
    [JsonPropertyName("source")]
    public Source? Source { get; init; }
    
    /// <summary>
    /// The title of the session.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }
    
    /// <summary>
    /// Whether plan approval is required before execution.
    /// </summary>
    [JsonPropertyName("requirePlanApproval")]
    public bool? RequirePlanApproval { get; init; }
    
    /// <summary>
    /// The automation mode of the session (e.g., "AUTO_CREATE_PR").
    /// </summary>
    [JsonPropertyName("automationMode")]
    public string? AutomationMode { get; init; }
    
    /// <summary>
    /// The time the session was created (RFC 3339 timestamp).
    /// </summary>
    [JsonPropertyName("createTime")]
    public string? CreateTime { get; init; }
    
    /// <summary>
    /// The time the session was last updated (RFC 3339 timestamp).
    /// </summary>
    [JsonPropertyName("updateTime")]
    public string? UpdateTime { get; init; }
    
    /// <summary>
    /// The current state of the session.
    /// </summary>
    [JsonPropertyName("state")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SessionState State { get; init; }
    
    /// <summary>
    /// The URL to view the session in the Jules web app.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; init; }
    
    /// <summary>
    /// The outputs of the session, if any.
    /// </summary>
    [JsonPropertyName("outputs")]
    public IReadOnlyList<SessionOutput>? Outputs { get; init; }
    
    /// <summary>
    /// The outcome of the session, if completed.
    /// </summary>
    [JsonPropertyName("outcome")]
    public SessionOutcomeData? Outcome { get; set; }

    /// <summary>
    /// Activities associated with the session.
    /// Only populated when fetched with activities.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<Activity>? Activities { get; set; }
}

/// <summary>
/// The outcome of a session (DTO).
/// </summary>
public class SessionOutcomeData
{
    /// <summary>
    /// The pull request created by the session.
    /// </summary>
    [JsonPropertyName("pullRequest")]
    public PullRequest? PullRequest { get; init; }
    
    /// <summary>
    /// The changeset produced by the session.
    /// </summary>
    [JsonPropertyName("changeSet")]
    public ChangeSetData? ChangeSet { get; init; }
}

