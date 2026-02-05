// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using JulesSdk.Options;

namespace JulesSdk.Mcp;

/// <summary>
/// CLI entry point for the MCP server.
/// Can be invoked via: JulesSdk.dll --mcp or as a dotnet tool.
/// </summary>
public static class McpCli
{
    /// <summary>
    /// Run the MCP server with the given arguments.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <param name="apiKey">API key (optional, will use JULES_API_KEY environment variable if not provided)</param>
    /// <returns>Exit code (0 for success)</returns>
    public static async Task<int> RunAsync(string[] args, string? apiKey = null)
    {
        try
        {

            // Handle config commands
            if (args.Length > 0 && args[0] == "config")
            {
                if (args.Length == 3 && args[1] == "get")
                {
                    var val = ConfigManager.Get(args[2]);
                    Console.WriteLine(val ?? "");
                    return 0;
                }
                if (args.Length == 4 && args[1] == "set")
                {
                    ConfigManager.Set(args[2], args[3]);
                    Console.WriteLine($"Set {args[2]} = {args[3]}");
                    return 0;
                }
                Console.Error.WriteLine("Usage: config get <key> | config set <key> <value>");
                return 1;
            }

            // Resolve API key
            apiKey ??= Environment.GetEnvironmentVariable("JULES_API_KEY");
            
            // Allow loading from config
            var options = new JulesOptions { ApiKey = apiKey };
            ConfigManager.ApplyTo(options);
            
            if (string.IsNullOrEmpty(options.ApiKey))
            {
                Console.Error.WriteLine("Error: JULES_API_KEY environment variable or config setting 'api-key' is required");
                Console.Error.WriteLine("Set it with: $env:JULES_API_KEY = 'your-api-key'");
                Console.Error.WriteLine("Or: jules config set api-key <your-api-key>");
                return 1;
            }

            // Create client with API key
            var client = Jules.Connect(options);

            // Create and configure server
            var server = client.CreateMcpServer();

            // Handle Ctrl+C gracefully
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            // Run server
            Console.Error.WriteLine("Jules MCP Server starting (stdio mode)...");
            await server.RunAsync(cts.Token);
            
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("MCP Server shutdown");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Check if the given args indicate MCP mode.
    /// </summary>
    public static bool IsMcpMode(string[] args)
    {
        return args.Length > 0 && 
               (args[0] == "--mcp" || args[0] == "mcp" || args[0] == "serve");
    }
}
