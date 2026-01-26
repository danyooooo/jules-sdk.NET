// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace JulesSdk.Models;

/// <summary>
/// A pull request created by the session.
/// </summary>
public record PullRequest(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description
);

/// <summary>
/// An output of a session, such as a pull request or changeset.
/// </summary>
public class SessionOutput
{
    /// <summary>
    /// The type of output ("pullRequest" or "changeSet").
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }
    
    /// <summary>
    /// The pull request, if type is "pullRequest".
    /// </summary>
    [JsonPropertyName("pullRequest")]
    public PullRequest? PullRequest { get; init; }
    
    /// <summary>
    /// The change set, if type is "changeSet".
    /// </summary>
    [JsonPropertyName("changeSet")]
    public ChangeSet? ChangeSet { get; init; }
}

/// <summary>
/// A patch in Git's unidiff format.
/// </summary>
public record GitPatch(
    [property: JsonPropertyName("unidiffPatch")] string UnidiffPatch,
    [property: JsonPropertyName("baseCommitId")] string BaseCommitId,
    [property: JsonPropertyName("suggestedCommitMessage")] string SuggestedCommitMessage
);

/// <summary>
/// A set of changes to be applied to a source.
/// </summary>
public record ChangeSet(
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("gitPatch")] GitPatch GitPatch
);
