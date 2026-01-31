// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using JulesSdk.Exceptions;
using JulesSdk.Options;

namespace JulesSdk.Http;

/// <summary>
/// Request options for the API client.
/// </summary>
public class ApiRequestOptions
{
    public HttpMethod Method { get; init; } = HttpMethod.Get;
    public object? Body { get; init; }
    public Dictionary<string, string>? Query { get; init; }
    public Dictionary<string, string>? Headers { get; init; }
}

/// <summary>
/// Internal API client to handle HTTP requests to the Jules API.
/// </summary>
internal class ApiClient
{
    private const string BaseUrl = "https://jules.googleapis.com/v1alpha";
    
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly RateLimitRetryConfig _rateLimitConfig;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public ApiClient(HttpClient httpClient, JulesOptions options)
    {
        _httpClient = httpClient;
        _apiKey = options.ApiKey ?? Environment.GetEnvironmentVariable("JULES_API_KEY");
        _rateLimitConfig = options.RateLimitRetry;
        
        _httpClient.Timeout = TimeSpan.FromMilliseconds(options.RequestTimeoutMs);
    }

    public async Task<T> RequestAsync<T>(
        string endpoint, 
        ApiRequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new ApiRequestOptions();
        var url = BuildUrl(endpoint, options.Query);
        
        using var request = new HttpRequestMessage(options.Method, url);
        
        // Add authentication
        AddAuthentication(request);
        
        // Add custom headers
        if (options.Headers != null)
        {
            foreach (var (key, value) in options.Headers)
            {
                request.Headers.TryAddWithoutValidation(key, value);
            }
        }
        
        // Add body
        if (options.Body != null)
        {
            request.Content = JsonContent.Create(options.Body, options: JsonOptions);
        }
        
        return await ExecuteWithRetryAsync<T>(request, url, cancellationToken);
    }

    private void AddAuthentication(HttpRequestMessage request)
    {
        if (!string.IsNullOrEmpty(_apiKey))
        {
            request.Headers.Add("X-Goog-Api-Key", _apiKey);
        }
        else
        {
            throw new MissingApiKeyException();
        }
    }

    private async Task<T> ExecuteWithRetryAsync<T>(
        HttpRequestMessage request, 
        string url, 
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var retryCount = 0;
        
        while (true)
        {
            try
            {
                // We need to clone the request for retries since it can only be sent once
                using var clonedRequest = await CloneRequestAsync(request);
                var response = await _httpClient.SendAsync(clonedRequest, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    if (string.IsNullOrEmpty(content))
                        return default!;
                    return JsonSerializer.Deserialize<T>(content, JsonOptions)!;
                }
                
                // Handle rate limiting
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    if (elapsed < _rateLimitConfig.MaxRetryTimeMs)
                    {
                        var delay = Math.Min(
                            _rateLimitConfig.BaseDelayMs * Math.Pow(2, retryCount),
                            _rateLimitConfig.MaxDelayMs);
                        await Task.Delay((int)delay, cancellationToken);
                        retryCount++;
                        continue;
                    }
                    
                    throw new JulesRateLimitException(url, (int)response.StatusCode, response.ReasonPhrase ?? "Too Many Requests");
                }
                
                // Handle authentication errors
                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    throw new JulesAuthenticationException(url, (int)response.StatusCode, response.ReasonPhrase ?? "Unauthorized");
                }
                
                // Handle other errors
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new JulesApiException(url, (int)response.StatusCode, response.ReasonPhrase ?? "Error", 
                    $"[{(int)response.StatusCode} {response.ReasonPhrase}] {request.Method} {url} - {errorBody}");
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new JulesNetworkException(url);
            }
            catch (HttpRequestException ex)
            {
                throw new JulesNetworkException(url, ex);
            }
        }
    }

    private static string BuildUrl(string endpoint, Dictionary<string, string>? query)
    {
        var url = $"{BaseUrl}/{endpoint}";
        
        if (query != null && query.Count > 0)
        {
            var queryString = string.Join("&", query.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
            url += $"?{queryString}";
        }
        
        return url;
    }
    
    private async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        
        if (request.Content != null)
        {
            var content = await request.Content.ReadAsStringAsync();
            clone.Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
        }
        
        return clone;
    }
}

