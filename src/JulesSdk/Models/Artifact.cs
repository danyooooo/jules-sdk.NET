// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;
using JulesSdk.Utilities;

namespace JulesSdk.Models;

/// <summary>
/// An artifact produced by an activity. This is a union type - only one of 
/// ChangeSet, Media, or BashOutput will be populated.
/// </summary>
public class Artifact
{
    /// <summary>
    /// A changeset artifact containing code changes.
    /// </summary>
    [JsonPropertyName("changeSet")]
    public ChangeSetData? ChangeSet { get; init; }
    
    /// <summary>
    /// A media artifact (e.g., an image).
    /// </summary>
    [JsonPropertyName("media")]
    public MediaData? Media { get; init; }
    
    /// <summary>
    /// A bash output artifact.
    /// </summary>
    [JsonPropertyName("bashOutput")]
    public BashOutputData? BashOutput { get; init; }
    
    /// <summary>
    /// Gets the type of this artifact based on which field is populated.
    /// </summary>
    [JsonIgnore]
    public string Type => ChangeSet != null ? "changeSet" 
        : Media != null ? "media" 
        : BashOutput != null ? "bashOutput" 
        : "unknown";
    
    /// <summary>
    /// Returns true if this is a changeset artifact.
    /// </summary>
    [JsonIgnore]
    public bool IsChangeSet => ChangeSet != null;
    
    /// <summary>
    /// Returns true if this is a media artifact.
    /// </summary>
    [JsonIgnore]
    public bool IsMedia => Media != null;
    
    /// <summary>
    /// Returns true if this is a bash output artifact.
    /// </summary>
    [JsonIgnore]
    public bool IsBashOutput => BashOutput != null;
}

/// <summary>
/// Data for a changeset artifact containing code changes.
/// </summary>
public class ChangeSetData
{
    /// <summary>
    /// The source this changeset applies to.
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; init; }
    
    /// <summary>
    /// The git patch containing the unified diff.
    /// </summary>
    [JsonPropertyName("gitPatch")]
    public GitPatch? GitPatch { get; init; }
    
    /// <summary>
    /// Parses the unified diff and returns structured file change information.
    /// </summary>
    public ParsedChangeSet Parse() => UnidiffParser.Parse(GitPatch?.UnidiffPatch ?? string.Empty);
}

/// <summary>
/// Data for a media artifact (e.g., an image).
/// </summary>
public class MediaData
{
    /// <summary>
    /// The base64-encoded media data.
    /// </summary>
    [JsonPropertyName("data")]
    public string? Data { get; init; }
    
    /// <summary>
    /// The format of the media (MIME type, e.g., "image/png").
    /// </summary>
    [JsonPropertyName("mimeType")]
    public string? MimeType { get; init; }
    
    /// <summary>
    /// Saves the media artifact to a file.
    /// </summary>
    public async Task SaveAsync(string filepath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(Data)) return;
        var bytes = Convert.FromBase64String(Data);
        await File.WriteAllBytesAsync(filepath, bytes, cancellationToken);
    }
    
    /// <summary>
    /// Converts the media artifact to a data URL.
    /// </summary>
    public string ToDataUrl() => $"data:{MimeType ?? "application/octet-stream"};base64,{Data ?? ""}";
}

/// <summary>
/// Data for a bash command execution output.
/// </summary>
public class BashOutputData
{
    /// <summary>
    /// The command that was executed.
    /// </summary>
    [JsonPropertyName("command")]
    public string? Command { get; init; }
    
    /// <summary>
    /// The combined output (stdout and stderr).
    /// </summary>
    [JsonPropertyName("output")]
    public string? Output { get; init; }
    
    /// <summary>
    /// The exit code of the command.
    /// </summary>
    [JsonPropertyName("exitCode")]
    public int? ExitCode { get; init; }
    
    /// <summary>
    /// Formats the bash output as a string, mimicking a terminal session.
    /// </summary>
    public override string ToString()
    {
        var outputLine = !string.IsNullOrEmpty(Output) ? $"{Output}\n" : "";
        return $"$ {Command ?? "[no command]"}\n{outputLine}[exit code: {ExitCode?.ToString() ?? "N/A"}]";
    }
}

// Legacy type aliases for backwards compatibility
/// <summary>
/// Alias for ChangeSetData for backwards compatibility.
/// </summary>
public class ChangeSetArtifact : Artifact
{
    /// <summary>
    /// The source this changeset applies to.
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; init; }
    
    /// <summary>
    /// The git patch containing the unified diff.
    /// </summary>
    [JsonPropertyName("gitPatch")]
    public GitPatch? GitPatch { get; init; }
    
    /// <summary>
    /// Parses the unified diff and returns structured file change information.
    /// </summary>
    public ParsedChangeSet Parse() => UnidiffParser.Parse(GitPatch?.UnidiffPatch ?? string.Empty);
}

/// <summary>
/// Alias for MediaData for backwards compatibility.
/// </summary>
public class MediaArtifact : Artifact
{
    /// <summary>
    /// The base64-encoded media data.
    /// </summary>
    [JsonPropertyName("data")]
    public string? Data { get; init; }
    
    /// <summary>
    /// The format of the media (MIME type, e.g., "image/png").
    /// </summary>
    [JsonPropertyName("mimeType")]
    public string? MimeType { get; init; }
    
    /// <summary>
    /// Saves the media artifact to a file.
    /// </summary>
    public async Task SaveAsync(string filepath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(Data)) return;
        var bytes = Convert.FromBase64String(Data);
        await File.WriteAllBytesAsync(filepath, bytes, cancellationToken);
    }
    
    /// <summary>
    /// Converts the media artifact to a data URL.
    /// </summary>
    public string ToDataUrl() => $"data:{MimeType ?? "application/octet-stream"};base64,{Data ?? ""}";
}

/// <summary>
/// Alias for BashOutputData for backwards compatibility.
/// </summary>
public class BashArtifact : Artifact
{
    /// <summary>
    /// The command that was executed.
    /// </summary>
    [JsonPropertyName("command")]
    public string? Command { get; init; }
    
    /// <summary>
    /// The combined output (stdout and stderr).
    /// </summary>
    [JsonPropertyName("output")]
    public string? Output { get; init; }
    
    /// <summary>
    /// The exit code of the command.
    /// </summary>
    [JsonPropertyName("exitCode")]
    public int? ExitCode { get; init; }
    
    /// <summary>
    /// Formats the bash output as a string, mimicking a terminal session.
    /// </summary>
    public override string ToString()
    {
        var outputLine = !string.IsNullOrEmpty(Output) ? $"{Output}\n" : "";
        return $"$ {Command ?? "[no command]"}\n{outputLine}[exit code: {ExitCode?.ToString() ?? "N/A"}]";
    }
}


