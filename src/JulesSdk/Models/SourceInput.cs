// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

namespace JulesSdk.Models;

/// <summary>
/// Ergonomic definition for specifying a source context when creating a session.
/// </summary>
/// <param name="Github">The GitHub repository identifier in the format 'owner/repo'.</param>
/// <param name="BaseBranch">The base branch that Jules will branch off of when starting the session.</param>
public record SourceInput(string Github, string BaseBranch);
