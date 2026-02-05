// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using JulesSdk.Mcp.Tools;

using Microsoft.Extensions.Logging;

namespace JulesSdk.Mcp;

/// <summary>
/// Extension methods for MCP server setup.
/// </summary>
public static class McpServerExtensions
{
    /// <summary>
    /// Creates a new MCP server with all default tools registered.
    /// </summary>
    public static McpServer CreateMcpServer(this IJulesClient client, ILoggerFactory? loggerFactory = null)
    {
        var logger = loggerFactory?.CreateLogger<McpServer>();
        return new McpServer(client, logger)
            .RegisterTools(GetDefaultTools());
    }

    /// <summary>
    /// Gets the default set of MCP tools.
    /// </summary>
    public static IEnumerable<IMcpTool> GetDefaultTools()
    {
        yield return new CreateSessionTool();
        yield return new ListSessionsTool();
        yield return new GetSessionStateTool();
        yield return new SendReplyTool();
        yield return new GetCodeReviewContextTool();
        yield return new ShowCodeDiffTool();
    }
}
