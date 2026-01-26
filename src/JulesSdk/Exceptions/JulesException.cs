// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

namespace JulesSdk.Exceptions;

/// <summary>
/// Base class for all SDK-specific exceptions.
/// </summary>
public class JulesException : Exception
{
    public JulesException(string message) : base(message) { }
    public JulesException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown for fundamental network issues like fetch failures or timeouts.
/// </summary>
public class JulesNetworkException : JulesException
{
    public string Url { get; }
    
    public JulesNetworkException(string url, Exception? innerException = null)
        : base($"Network request to {url} failed", innerException!)
    {
        Url = url;
    }
}

/// <summary>
/// A generic wrapper for non-2xx API responses.
/// </summary>
public class JulesApiException : JulesException
{
    public string Url { get; }
    public int StatusCode { get; }
    public string StatusText { get; }
    
    public JulesApiException(string url, int statusCode, string statusText, string? message = null)
        : base(message ?? $"[{statusCode} {statusText}] Request to {url} failed")
    {
        Url = url;
        StatusCode = statusCode;
        StatusText = statusText;
    }
}

/// <summary>
/// Thrown for 401 Unauthorized or 403 Forbidden API responses.
/// </summary>
public class JulesAuthenticationException : JulesApiException
{
    public JulesAuthenticationException(string url, int statusCode, string statusText)
        : base(url, statusCode, statusText, 
            $"[{statusCode} {statusText}] Authentication to {url} failed. Ensure your API key is correct.")
    { }
}

/// <summary>
/// Thrown for 429 Too Many Requests API responses.
/// </summary>
public class JulesRateLimitException : JulesApiException
{
    public JulesRateLimitException(string url, int statusCode, string statusText)
        : base(url, statusCode, statusText,
            $"[{statusCode} {statusText}] API rate limit exceeded for {url}.")
    { }
}

/// <summary>
/// Thrown when an API key is required but not provided.
/// </summary>
public class MissingApiKeyException : JulesException
{
    public MissingApiKeyException()
        : base("Jules API key is missing. Pass it to the constructor or set the JULES_API_KEY environment variable.")
    { }
}

/// <summary>
/// Thrown when a requested source cannot be found.
/// </summary>
public class SourceNotFoundException : JulesException
{
    public string SourceIdentifier { get; }
    
    public SourceNotFoundException(string sourceIdentifier)
        : base($"Could not get source '{sourceIdentifier}'")
    {
        SourceIdentifier = sourceIdentifier;
    }
}

/// <summary>
/// Thrown when a jules.Run() operation terminates in a FAILED state.
/// </summary>
public class AutomatedSessionFailedException : JulesException
{
    public AutomatedSessionFailedException(string? reason = null)
        : base(BuildMessage(reason))
    { }
    
    private static string BuildMessage(string? reason)
    {
        var message = "The Jules automated session terminated with a FAILED state.";
        if (!string.IsNullOrEmpty(reason))
            message += $" Reason: {reason}";
        return message;
    }
}

/// <summary>
/// Thrown when attempting to start a sync while another sync is already in progress.
/// </summary>
public class SyncInProgressException : JulesException
{
    public SyncInProgressException()
        : base("A sync operation is already in progress. Wait for it to complete before starting another.")
    { }
}

/// <summary>
/// Thrown when an operation is attempted on a session that is not in a valid state.
/// </summary>
public class InvalidStateException : JulesException
{
    public InvalidStateException(string message) : base(message) { }
}
