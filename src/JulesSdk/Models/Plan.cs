// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace JulesSdk.Models;

/// <summary>
/// A single step within an agent's plan.
/// </summary>
public class PlanStep
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }
    
    [JsonPropertyName("title")]
    public string? Title { get; init; }
    
    [JsonPropertyName("description")]
    public string? Description { get; init; }
    
    [JsonPropertyName("index")]
    public int? Index { get; init; }
}

/// <summary>
/// A sequence of steps that the agent will take to complete the task.
/// </summary>
public class Plan
{
    /// <summary>
    /// The unique identifier for the plan.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }
    
    /// <summary>
    /// The steps in the plan.
    /// </summary>
    [JsonPropertyName("steps")]
    public IReadOnlyList<PlanStep>? Steps { get; init; }
    
    /// <summary>
    /// The time the plan was created.
    /// </summary>
    [JsonPropertyName("createTime")]
    public string? CreateTime { get; init; }
}

