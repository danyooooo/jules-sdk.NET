// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using JulesSdk.Client;
using JulesSdk.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JulesSdk.Extensions;

/// <summary>
/// Extension methods for adding Jules SDK to dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Jules SDK to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddJulesSdk(
        this IServiceCollection services, 
        Action<JulesOptions>? configure = null)
    {
        services.AddOptions<JulesOptions>();
        
        if (configure != null)
        {
            services.Configure(configure);
        }
        
        services.AddHttpClient<IJulesClient, JulesClientImpl>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<JulesOptions>>().Value;
            httpClient.Timeout = TimeSpan.FromMilliseconds(options.RequestTimeoutMs);
        });
        
        // Register concrete implementation
        services.AddTransient<IJulesClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(IJulesClient));
            var options = sp.GetRequiredService<IOptions<JulesOptions>>().Value;
            return new JulesClientImpl(httpClient, options);
        });
        
        return services;
    }
    
    /// <summary>
    /// Adds the Jules SDK with an API key.
    /// </summary>
    public static IServiceCollection AddJulesSdk(
        this IServiceCollection services,
        string apiKey)
    {
        return services.AddJulesSdk(options => options.ApiKey = apiKey);
    }
}
