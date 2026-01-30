// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace JulesSdk.Models;

/// <summary>
/// A pull request created by the session.
/// </summary>
public class PullRequest
{
    [JsonPropertyName("url")]
    public string? Url { get; init; }
    
    [JsonPropertyName("title")]
    public string? Title { get; init; }
    
    [JsonPropertyName("description")]
    public string? Description { get; init; }
}

/// <summary>
/// An output of a session. This is a union type - only one of pullRequest will be set.
/// </summary>
public class SessionOutput
{
    /// <summary>
    /// A pull request created by the session, if applicable.
    /// </summary>
    [JsonPropertyName("pullRequest")]
    public PullRequest? PullRequest { get; init; }
}

/// <summary>
/// A patch in Git's unidiff format.
/// </summary>
public class GitPatch
{
    [JsonPropertyName("unidiffPatch")]
    public string? UnidiffPatch { get; init; }
    
    [JsonPropertyName("baseCommitId")]
    public string? BaseCommitId { get; init; }
    
    [JsonPropertyName("suggestedCommitMessage")]
    public string? SuggestedCommitMessage { get; init; }
}

/// <summary>
/// A set of changes to be applied to a source. Used in Artifact.changeSet.
/// </summary>
public class ChangeSet
{
    [JsonPropertyName("source")]
    public string? Source { get; init; }
    
    [JsonPropertyName("gitPatch")]
    public GitPatch? GitPatch { get; init; }
}

