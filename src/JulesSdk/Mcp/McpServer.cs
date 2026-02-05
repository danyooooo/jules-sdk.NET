// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using System.Text.Json.Serialization;
using StreamJsonRpc;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JulesSdk.Mcp;

/// <summary>
/// MCP Server that exposes Jules SDK functionality via JSON-RPC over stdio.
/// </summary>
public class McpServer
{
    private readonly IJulesClient _client;
    private readonly ILogger<McpServer> _logger;
    private readonly Dictionary<string, IMcpTool> _tools = new();
    private readonly JsonSerializerOptions _jsonOptions;
    
    /// <summary>
    /// Server name for MCP protocol.
    /// </summary>
    public string Name { get; } = "jules-mcp";
    
    /// <summary>
    /// Server version for MCP protocol.
    /// </summary>
    public string Version { get; } = "1.3.0";

    /// <summary>
    /// Creates a new MCP server with the given Jules client.
    /// </summary>
    public McpServer(IJulesClient client, ILogger<McpServer>? logger = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? NullLogger<McpServer>.Instance;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Register a tool with the server.
    /// </summary>
    public McpServer RegisterTool(IMcpTool tool)
    {
        _tools[tool.Name] = tool;
        _logger.LogDebug("Registered tool: {ToolName}", tool.Name);
        return this;
    }

    /// <summary>
    /// Register multiple tools with the server.
    /// </summary>
    public McpServer RegisterTools(IEnumerable<IMcpTool> tools)
    {
        foreach (var tool in tools)
        {
            RegisterTool(tool);
        }
        return this;
    }

    /// <summary>
    /// Run the MCP server on stdio, blocking until cancelled.
    /// </summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        var stdin = Console.OpenStandardInput();
        var stdout = Console.OpenStandardOutput();
        
        // Use header-delimited JSON messaging (Content-Length headers)
        var handler = new HeaderDelimitedMessageHandler(stdout, stdin);
        
        using var rpc = new JsonRpc(handler);
        rpc.AddLocalRpcTarget(new McpRpcTarget(this, _client, _tools), new JsonRpcTargetOptions
        {
            MethodNameTransform = n => n // Keep method names as-is
        });
        
        rpc.StartListening();
        
        _logger.LogInformation("Jules MCP Server running on stdio");
        
        try
        {
            await rpc.Completion.WaitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
            _logger.LogInformation("MCP Server stopping...");
        }
    }

    /// <summary>
    /// Get list of registered tools.
    /// </summary>
    internal IReadOnlyDictionary<string, IMcpTool> Tools => _tools;
}

/// <summary>
/// RPC target class that handles MCP protocol methods.
/// </summary>
internal class McpRpcTarget
{
    private readonly McpServer _server;
    private readonly IJulesClient _client;
    private readonly IReadOnlyDictionary<string, IMcpTool> _tools;
    
    public McpRpcTarget(McpServer server, IJulesClient client, IReadOnlyDictionary<string, IMcpTool> tools)
    {
        _server = server;
        _client = client;
        _tools = tools;
    }

    /// <summary>
    /// MCP initialize handshake.
    /// </summary>
    [JsonRpcMethod("initialize")]
    public object Initialize(JsonElement @params)
    {
        return new
        {
            protocolVersion = "2024-11-05",
            serverInfo = new
            {
                name = _server.Name,
                version = _server.Version
            },
            capabilities = new
            {
                tools = new { }
            }
        };
    }

    /// <summary>
    /// List available tools.
    /// </summary>
    [JsonRpcMethod("tools/list")]
    public object ListTools()
    {
        var tools = _tools.Values.Select(t => new
        {
            name = t.Name,
            description = t.Description,
            inputSchema = t.InputSchema
        }).ToList();
        
        return new { tools };
    }

    /// <summary>
    /// Call a tool.
    /// </summary>
    [JsonRpcMethod("tools/call")]
    public async Task<object> CallToolAsync(JsonElement @params, CancellationToken ct)
    {
        var name = @params.GetProperty("name").GetString() 
            ?? throw new ArgumentException("Tool name is required");
        
        if (!_tools.TryGetValue(name, out var tool))
        {
            return new McpToolResult
            {
                Content = [new McpContentBlock { Type = "text", Text = $"Tool not found: {name}" }],
                IsError = true
            };
        }

        try
        {
            var args = new Dictionary<string, object?>();
            
            if (@params.TryGetProperty("arguments", out var argsElement))
            {
                foreach (var prop in argsElement.EnumerateObject())
                {
                    args[prop.Name] = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => prop.Value.GetString(),
                        JsonValueKind.Number => prop.Value.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Null => null,
                        _ => prop.Value.ToString()
                    };
                }
            }

            return await tool.ExecuteAsync(_client, args, ct);
        }
        catch (Exception ex)
        {
            return McpToolResult.Error($"Error executing {name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Ping/pong for connection testing.
    /// </summary>
    [JsonRpcMethod("ping")]
    public object Ping() => new { };

    /// <summary>
    /// Notifications/initialized - acknowledge client ready.
    /// </summary>
    [JsonRpcMethod("notifications/initialized")]
    public void NotificationsInitialized() { }
}
