// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using JulesSdk.Client;
using JulesSdk.Options;

namespace JulesSdk;

/// <summary>
/// Static factory for quick access to the Jules SDK.
/// </summary>
public static class Jules
{
    private static readonly Lazy<IJulesClient> _defaultClient = new(() =>
    {
        var httpClient = new HttpClient();
        return new JulesClientImpl(httpClient, new JulesOptions());
    });
    
    /// <summary>
    /// The default pre-initialized client that reads API keys from environment variables.
    /// </summary>
    public static IJulesClient Client => _defaultClient.Value;
    
    /// <summary>
    /// Creates a new client instance with custom configuration.
    /// </summary>
    /// <param name="options">The configuration options.</param>
    /// <returns>A new JulesClient instance.</returns>
    public static IJulesClient Connect(JulesOptions options)
    {
        var httpClient = new HttpClient();
        return new JulesClientImpl(httpClient, options);
    }
    
    /// <summary>
    /// Creates a new client instance with just an API key.
    /// </summary>
    /// <param name="apiKey">The API key.</param>
    /// <returns>A new JulesClient instance.</returns>
    public static IJulesClient Connect(string apiKey)
    {
        return Connect(new JulesOptions { ApiKey = apiKey });
    }
}
