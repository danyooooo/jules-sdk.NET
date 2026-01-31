// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

namespace JulesSdk.Models;

/// <summary>
/// An entry in the computed session timeline, representing a single activity.
/// </summary>
public record TimelineEntry(
    /// <summary>The time the activity occurred (RFC 3339 timestamp).</summary>
    string Time,
    /// <summary>The type of the activity.</summary>
    string Type,
    /// <summary>A human-readable summary of the activity.</summary>
    string Summary
);
