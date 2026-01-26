// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace JulesSdk.Models;

/// <summary>
/// Context specific to GitHub repos.
/// </summary>
public record GitHubRepoContext(
    [property: JsonPropertyName("startingBranch")] string StartingBranch
);

/// <summary>
/// Represents the context used when the session was created.
/// </summary>
public class SourceContext
{
    /// <summary>
    /// The name of the source (e.g., "sources/github/owner/repo").
    /// </summary>
    [JsonPropertyName("source")]
    public required string Source { get; init; }
    
    /// <summary>
    /// Context specific to GitHub repos.
    /// </summary>
    [JsonPropertyName("githubRepoContext")]
    public GitHubRepoContext? GitHubRepoContext { get; init; }
}
