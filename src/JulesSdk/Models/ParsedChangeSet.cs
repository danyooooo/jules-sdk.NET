// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

namespace JulesSdk.Models;

/// <summary>
/// A single file change extracted from a unified diff.
/// </summary>
public record ParsedFile(
    string Path,
    ChangeType ChangeType,
    int Additions,
    int Deletions
);

/// <summary>
/// The type of file change.
/// </summary>
public enum ChangeType
{
    Created,
    Modified,
    Deleted
}

/// <summary>
/// Parsed representation of a ChangeSet's unified diff.
/// </summary>
public class ParsedChangeSet
{
    /// <summary>
    /// Individual file changes.
    /// </summary>
    public required IReadOnlyList<ParsedFile> Files { get; init; }
    
    /// <summary>
    /// Summary counts.
    /// </summary>
    public required ChangeSetSummary Summary { get; init; }
}

/// <summary>
/// Summary counts for a changeset.
/// </summary>
public record ChangeSetSummary(
    int TotalFiles,
    int Created,
    int Modified,
    int Deleted
);
