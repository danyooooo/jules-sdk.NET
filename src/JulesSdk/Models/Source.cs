// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace JulesSdk.Models;

/// <summary>
/// A GitHub branch.
/// </summary>
public class GitHubBranch
{
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }
}

/// <summary>
/// Details of a GitHub repository connected to Jules.
/// </summary>
public class GitHubRepo
{
    [JsonPropertyName("owner")]
    public string? Owner { get; init; }
    
    [JsonPropertyName("repo")]
    public string? Repo { get; init; }
    
    [JsonPropertyName("isPrivate")]
    public bool? IsPrivate { get; init; }
    
    [JsonPropertyName("defaultBranch")]
    public GitHubBranch? DefaultBranch { get; init; }
    
    [JsonPropertyName("branches")]
    public IReadOnlyList<GitHubBranch>? Branches { get; init; }
}

/// <summary>
/// An input source of data for a session (e.g., a GitHub repository).
/// </summary>
public class Source
{
    /// <summary>
    /// The full resource name (e.g., "sources/{source}").
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }
    
    /// <summary>
    /// The short identifier of the source.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }
    
    /// <summary>
    /// GitHub repository details.
    /// </summary>
    [JsonPropertyName("githubRepo")]
    public GitHubRepo? GitHubRepo { get; init; }
}


