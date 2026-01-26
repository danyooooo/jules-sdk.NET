// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace JulesSdk.Models;

/// <summary>
/// Details of a GitHub repository connected to Jules.
/// </summary>
public record GitHubRepo(
    [property: JsonPropertyName("owner")] string Owner,
    [property: JsonPropertyName("repo")] string Repo,
    [property: JsonPropertyName("isPrivate")] bool IsPrivate
);

/// <summary>
/// An input source of data for a session (e.g., a GitHub repository).
/// </summary>
public class Source
{
    /// <summary>
    /// The full resource name (e.g., "sources/github/owner/repo").
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    
    /// <summary>
    /// The short identifier of the source (e.g., "github/owner/repo").
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }
    
    /// <summary>
    /// The type of source (currently only "githubRepo").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "githubRepo";
    
    /// <summary>
    /// GitHub repository details.
    /// </summary>
    [JsonPropertyName("githubRepo")]
    public GitHubRepo? GitHubRepo { get; init; }
}
