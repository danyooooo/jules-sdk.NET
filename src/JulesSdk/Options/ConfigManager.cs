// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;

namespace JulesSdk.Options;

/// <summary>
/// Manages persistent configuration for Jules SDK CLI.
/// </summary>
public static class ConfigManager
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
        ".jules");
        
    private static readonly string ConfigFile = Path.Combine(ConfigDir, "config.json");
    
    private static readonly JsonSerializerOptions JsonOptions = new() 
    { 
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Loads configuration from file, or returns empty config if none exists.
    /// </summary>
    public static Dictionary<string, string> Load()
    {
        if (!File.Exists(ConfigFile))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
        try
        {
            var json = File.ReadAllText(ConfigFile);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions) 
                   ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Sets a configuration value and saves to file.
    /// </summary>
    public static void Set(string key, string value)
    {
        var config = Load();
        config[key] = value;
        Save(config);
    }
    
    /// <summary>
    /// Gets a configuration value.
    /// </summary>
    public static string? Get(string key)
    {
        var config = Load();
        return config.TryGetValue(key, out var val) ? val : null;
    }

    private static void Save(Dictionary<string, string> config)
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(ConfigFile, json);
    }
    
    /// <summary>
    /// Applies configuration values to JulesOptions.
    /// </summary>
    public static void ApplyTo(JulesOptions options)
    {
        var config = Load();
        
        if (config.TryGetValue("api-key", out var apiKey) && string.IsNullOrEmpty(options.ApiKey))
        {
            options.ApiKey = apiKey;
        }
        
        if (config.TryGetValue("cache-dir", out var cacheDir) && string.IsNullOrEmpty(options.CacheDir))
        {
            options.CacheDir = cacheDir;
        }
        
        if (config.TryGetValue("polling-interval", out var polling) && int.TryParse(polling, out var interval))
        {
            options.PollingIntervalMs = interval;
        }
    }
}
