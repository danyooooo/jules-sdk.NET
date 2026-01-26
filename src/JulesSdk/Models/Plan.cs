// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace JulesSdk.Models;

/// <summary>
/// A single step within an agent's plan.
/// </summary>
public record PlanStep(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("index")] int Index
);

/// <summary>
/// A sequence of steps that the agent will take to complete the task.
/// </summary>
public class Plan
{
    /// <summary>
    /// The unique identifier for the plan.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }
    
    /// <summary>
    /// The steps in the plan.
    /// </summary>
    [JsonPropertyName("steps")]
    public required IReadOnlyList<PlanStep> Steps { get; init; }
    
    /// <summary>
    /// The time the plan was created.
    /// </summary>
    [JsonPropertyName("createTime")]
    public required string CreateTime { get; init; }
}
