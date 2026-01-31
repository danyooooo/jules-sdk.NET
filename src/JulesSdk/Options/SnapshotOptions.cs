// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

namespace JulesSdk.Options;

/// <summary>
/// Options for creating a session snapshot.
/// </summary>
public class SnapshotOptions
{
    /// <summary>
    /// Whether to include activities in the snapshot. Defaults to true.
    /// </summary>
    public bool IncludeActivities { get; init; } = true;
}

/// <summary>
/// Options for serializing a snapshot to JSON with field masking.
/// </summary>
public class SnapshotSerializeOptions
{
    /// <summary>
    /// Fields to include in the output. If specified, only these fields are returned.
    /// Takes precedence over <see cref="Exclude"/> if both are provided.
    /// </summary>
    public IReadOnlyList<string>? Include { get; init; }
    
    /// <summary>
    /// Fields to exclude from the output. Ignored if <see cref="Include"/> is specified.
    /// </summary>
    public IReadOnlyList<string>? Exclude { get; init; }
}
