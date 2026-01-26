// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

namespace JulesSdk.Options;

/// <summary>
/// Configuration for rate limit retry behavior.
/// </summary>
public class RateLimitRetryConfig
{
    /// <summary>
    /// Maximum time in milliseconds to keep retrying before throwing JulesRateLimitException.
    /// </summary>
    public int MaxRetryTimeMs { get; set; } = 300000; // 5 minutes
    
    /// <summary>
    /// Base delay in milliseconds for exponential backoff.
    /// </summary>
    public int BaseDelayMs { get; set; } = 1000;
    
    /// <summary>
    /// Maximum delay in milliseconds between retry attempts.
    /// </summary>
    public int MaxDelayMs { get; set; } = 30000;
}

/// <summary>
/// Proxy configuration for browser/client environments.
/// </summary>
public class ProxyConfig
{
    /// <summary>
    /// The full URL to your proxy endpoint.
    /// </summary>
    public required string Url { get; init; }
    
    /// <summary>
    /// Async callback to retrieve the User Identity Token (e.g., Firebase ID Token).
    /// Or a static string for "Shared Secret" mode.
    /// </summary>
    public Func<Task<string>>? AuthProvider { get; init; }
}

/// <summary>
/// Configuration options for the Jules SDK client.
/// </summary>
public class JulesOptions
{
    /// <summary>
    /// The API key used for authentication.
    /// If not provided, the SDK will attempt to read it from the JULES_API_KEY environment variable.
    /// </summary>
    public string? ApiKey { get; set; }
    
    /// <summary>
    /// Proxy configuration for browser/client environments.
    /// </summary>
    public ProxyConfig? Proxy { get; set; }
    
    /// <summary>
    /// The interval in milliseconds to poll for session and activity updates.
    /// </summary>
    public int PollingIntervalMs { get; set; } = 5000;
    
    /// <summary>
    /// The timeout in milliseconds for individual HTTP requests.
    /// </summary>
    public int RequestTimeoutMs { get; set; } = 30000;
    
    /// <summary>
    /// Configuration for 429 rate limit retry behavior.
    /// </summary>
    public RateLimitRetryConfig RateLimitRetry { get; set; } = new();
    
    /// <summary>
    /// The directory path for local caching. 
    /// If null, in-memory storage is used.
    /// </summary>
    public string? CacheDir { get; set; }
}
