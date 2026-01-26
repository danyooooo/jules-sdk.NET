// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;
using JulesSdk.Utilities;

namespace JulesSdk.Models;

/// <summary>
/// Base class for all artifacts produced by activities.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ChangeSetArtifact), "changeSet")]
[JsonDerivedType(typeof(MediaArtifact), "media")]
[JsonDerivedType(typeof(BashArtifact), "bashOutput")]
public abstract class Artifact
{
    /// <summary>
    /// The artifact type discriminator.
    /// </summary>
    [JsonPropertyName("type")]
    public abstract string Type { get; }
}

/// <summary>
/// A set of code changes with helper methods to parse the diff.
/// </summary>
public class ChangeSetArtifact : Artifact
{
    /// <inheritdoc/>
    public override string Type => "changeSet";
    
    /// <summary>
    /// The source this changeset applies to.
    /// </summary>
    [JsonPropertyName("source")]
    public required string Source { get; init; }
    
    /// <summary>
    /// The git patch containing the unified diff.
    /// </summary>
    [JsonPropertyName("gitPatch")]
    public required GitPatch GitPatch { get; init; }
    
    /// <summary>
    /// Parses the unified diff and returns structured file change information.
    /// </summary>
    public ParsedChangeSet Parse() => UnidiffParser.Parse(GitPatch.UnidiffPatch);
}

/// <summary>
/// A media artifact (e.g., an image) with helper methods.
/// </summary>
public class MediaArtifact : Artifact
{
    /// <inheritdoc/>
    public override string Type => "media";
    
    /// <summary>
    /// The base64-encoded media data.
    /// </summary>
    [JsonPropertyName("data")]
    public required string Data { get; init; }
    
    /// <summary>
    /// The format of the media (e.g., "image/png").
    /// </summary>
    [JsonPropertyName("format")]
    public required string Format { get; init; }
    
    /// <summary>
    /// Saves the media artifact to a file.
    /// </summary>
    public async Task SaveAsync(string filepath, CancellationToken cancellationToken = default)
    {
        var bytes = Convert.FromBase64String(Data);
        await File.WriteAllBytesAsync(filepath, bytes, cancellationToken);
    }
    
    /// <summary>
    /// Converts the media artifact to a data URL.
    /// </summary>
    public string ToDataUrl() => $"data:{Format};base64,{Data}";
}

/// <summary>
/// Output from a bash command execution.
/// </summary>
public class BashArtifact : Artifact
{
    /// <inheritdoc/>
    public override string Type => "bashOutput";
    
    /// <summary>
    /// The command that was executed.
    /// </summary>
    [JsonPropertyName("command")]
    public required string Command { get; init; }
    
    /// <summary>
    /// The standard output.
    /// </summary>
    [JsonPropertyName("stdout")]
    public string? Stdout { get; init; }
    
    /// <summary>
    /// The standard error.
    /// </summary>
    [JsonPropertyName("stderr")]
    public string? Stderr { get; init; }
    
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
        var output = string.Join("", new[] { Stdout, Stderr }.Where(s => !string.IsNullOrEmpty(s)));
        var outputLine = !string.IsNullOrEmpty(output) ? $"{output}\n" : "";
        return $"$ {Command}\n{outputLine}[exit code: {ExitCode?.ToString() ?? "N/A"}]";
    }
}
