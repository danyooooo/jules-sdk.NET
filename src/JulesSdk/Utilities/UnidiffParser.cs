// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;
using JulesSdk.Models;

namespace JulesSdk.Utilities;

/// <summary>
/// Parses unified diff patches to extract file information.
/// </summary>
public static partial class UnidiffParser
{
    /// <summary>
    /// Parses a unified diff string and extracts file information.
    /// </summary>
    public static ParsedChangeSet Parse(string? patch)
    {
        if (string.IsNullOrEmpty(patch))
        {
            return new ParsedChangeSet
            {
                Files = Array.Empty<ParsedFile>(),
                Summary = new ChangeSetSummary(0, 0, 0, 0)
            };
        }

        var files = new List<ParsedFile>();
        var diffSections = DiffHeaderRegex().Split(patch).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

        foreach (var section in diffSections)
        {
            var lines = section.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var (path, changeType) = ExtractPathAndChangeType(lines);
            
            if (string.IsNullOrEmpty(path)) continue;

            var (additions, deletions) = CountChanges(lines);
            files.Add(new ParsedFile(path, changeType, additions, deletions));
        }

        var summary = new ChangeSetSummary(
            files.Count,
            files.Count(f => f.ChangeType == ChangeType.Created),
            files.Count(f => f.ChangeType == ChangeType.Modified),
            files.Count(f => f.ChangeType == ChangeType.Deleted)
        );

        return new ParsedChangeSet { Files = files, Summary = summary };
    }

    /// <summary>
    /// Parses a unified diff and extracts file information including content.
    /// </summary>
    public static IReadOnlyList<GeneratedFile> ParseWithContent(string? patch)
    {
        if (string.IsNullOrEmpty(patch))
        {
            return Array.Empty<GeneratedFile>();
        }

        var files = new List<GeneratedFile>();
        var diffSections = DiffHeaderRegex().Split(patch).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

        foreach (var section in diffSections)
        {
            var lines = section.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var (path, changeType) = ExtractPathAndChangeType(lines);
            
            if (string.IsNullOrEmpty(path)) continue;

            var (additions, deletions, contentLines) = CountChangesWithContent(lines);
            var content = changeType == ChangeType.Deleted ? "" : string.Join("\n", contentLines);
            
            files.Add(new GeneratedFile(path, changeType, content, additions, deletions));
        }

        return files;
    }

    private static (string path, ChangeType changeType) ExtractPathAndChangeType(string[] lines)
    {
        string fromPath = "";
        string toPath = "";

        foreach (var line in lines)
        {
            if (line.StartsWith("--- "))
            {
                fromPath = line[4..].Trim().Replace("a/", "").Replace("/dev/null", "");
            }
            else if (line.StartsWith("+++ "))
            {
                toPath = line[4..].Trim().Replace("b/", "").Replace("/dev/null", "");
            }
        }

        ChangeType changeType;
        string path;

        if (string.IsNullOrEmpty(fromPath) || lines.Any(l => l.StartsWith("--- /dev/null")))
        {
            changeType = ChangeType.Created;
            path = toPath;
        }
        else if (string.IsNullOrEmpty(toPath) || lines.Any(l => l.StartsWith("+++ /dev/null")))
        {
            changeType = ChangeType.Deleted;
            path = fromPath;
        }
        else
        {
            changeType = ChangeType.Modified;
            path = toPath;
        }

        return (path, changeType);
    }

    private static (int additions, int deletions) CountChanges(string[] lines)
    {
        int additions = 0;
        int deletions = 0;
        bool inHunk = false;

        foreach (var line in lines)
        {
            if (line.StartsWith("@@"))
            {
                inHunk = true;
                continue;
            }
            if (inHunk)
            {
                if (line.StartsWith('+') && !line.StartsWith("+++"))
                    additions++;
                else if (line.StartsWith('-') && !line.StartsWith("---"))
                    deletions++;
            }
        }

        return (additions, deletions);
    }

    private static (int additions, int deletions, List<string> contentLines) CountChangesWithContent(string[] lines)
    {
        int additions = 0;
        int deletions = 0;
        var contentLines = new List<string>();
        bool inHunk = false;

        foreach (var line in lines)
        {
            if (line.StartsWith("@@"))
            {
                inHunk = true;
                continue;
            }
            if (inHunk)
            {
                if (line.StartsWith('+') && !line.StartsWith("+++"))
                {
                    additions++;
                    contentLines.Add(line[1..]);
                }
                else if (line.StartsWith('-') && !line.StartsWith("---"))
                {
                    deletions++;
                }
            }
        }

        return (additions, deletions, contentLines);
    }

    [GeneratedRegex(@"^diff --git ", RegexOptions.Multiline)]
    private static partial Regex DiffHeaderRegex();
}
