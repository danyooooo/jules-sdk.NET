// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace JulesSdk.Mcp;

/// <summary>
/// MCP tool definition for registration with the server.
/// </summary>
public interface IMcpTool
{
    /// <summary>
    /// Unique name of the tool (e.g., "create_session").
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Human-readable description of what the tool does.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// JSON Schema for the tool's input parameters.
    /// </summary>
    object InputSchema { get; }
    
    /// <summary>
    /// Execute the tool with the given arguments.
    /// </summary>
    Task<McpToolResult> ExecuteAsync(IJulesClient client, Dictionary<string, object?> args, CancellationToken ct = default);
}

/// <summary>
/// Result returned by an MCP tool execution.
/// </summary>
public class McpToolResult
{
    /// <summary>
    /// Content blocks to return to the caller.
    /// </summary>
    [JsonPropertyName("content")]
    public required IReadOnlyList<McpContentBlock> Content { get; init; }
    
    /// <summary>
    /// Whether the tool execution resulted in an error.
    /// </summary>
    [JsonPropertyName("isError")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsError { get; init; }
    
    /// <summary>
    /// Creates a successful text result.
    /// </summary>
    public static McpToolResult Text(string text) => new()
    {
        Content = [new McpContentBlock { Type = "text", Text = text }]
    };
    
    /// <summary>
    /// Creates a successful JSON result.
    /// </summary>
    public static McpToolResult Json(object data) => new()
    {
        Content = [new McpContentBlock 
        { 
            Type = "text", 
            Text = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            }) 
        }]
    };
    
    /// <summary>
    /// Creates an error result.
    /// </summary>
    public static McpToolResult Error(string message) => new()
    {
        Content = [new McpContentBlock { Type = "text", Text = message }],
        IsError = true
    };
}

/// <summary>
/// A content block in an MCP response.
/// </summary>
public class McpContentBlock
{
    /// <summary>
    /// Type of content (e.g., "text", "image").
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }
    
    /// <summary>
    /// Text content (for type="text").
    /// </summary>
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; init; }
}
