// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using JulesSdk.Exceptions;
using Xunit;

namespace JulesSdk.Tests;

public class ExceptionTests
{
    [Fact]
    public void JulesException_HasCorrectMessage()
    {
        var ex = new JulesException("test error");
        Assert.Equal("test error", ex.Message);
    }
    
    [Fact]
    public void JulesNetworkException_IncludesUrl()
    {
        var ex = new JulesNetworkException("https://example.com/api");
        Assert.Equal("https://example.com/api", ex.Url);
        Assert.Contains("https://example.com/api", ex.Message);
    }
    
    [Fact]
    public void JulesApiException_HasStatusInfo()
    {
        var ex = new JulesApiException("https://api.example.com", 500, "Internal Server Error");
        Assert.Equal(500, ex.StatusCode);
        Assert.Equal("Internal Server Error", ex.StatusText);
    }
    
    [Fact]
    public void JulesAuthenticationException_IndicatesAuthFailure()
    {
        var ex = new JulesAuthenticationException("https://api.example.com", 401, "Unauthorized");
        Assert.Equal(401, ex.StatusCode);
        Assert.Contains("Authentication", ex.Message);
        Assert.Contains("API key", ex.Message);
    }
    
    [Fact]
    public void JulesRateLimitException_IndicatesRateLimit()
    {
        var ex = new JulesRateLimitException("https://api.example.com", 429, "Too Many Requests");
        Assert.Equal(429, ex.StatusCode);
        Assert.Contains("rate limit", ex.Message.ToLower());
    }
    
    [Fact]
    public void MissingApiKeyException_HasHelpfulMessage()
    {
        var ex = new MissingApiKeyException();
        Assert.Contains("JULES_API_KEY", ex.Message);
    }
    
    [Fact]
    public void SourceNotFoundException_IncludesSourceId()
    {
        var ex = new SourceNotFoundException("owner/repo");
        Assert.Equal("owner/repo", ex.SourceIdentifier);
        Assert.Contains("owner/repo", ex.Message);
    }
    
    [Fact]
    public void AutomatedSessionFailedException_IncludesReason()
    {
        var ex = new AutomatedSessionFailedException("Test failure reason");
        Assert.Contains("Test failure reason", ex.Message);
    }
    
    [Fact]
    public void SyncInProgressException_HasMessage()
    {
        var ex = new SyncInProgressException();
        Assert.Contains("sync", ex.Message.ToLower());
        Assert.Contains("already in progress", ex.Message.ToLower());
    }
    
    [Fact]
    public void InvalidStateException_PreservesMessage()
    {
        var ex = new InvalidStateException("Cannot approve: invalid state");
        Assert.Equal("Cannot approve: invalid state", ex.Message);
    }
    
    [Fact]
    public void AllExceptions_InheritFromJulesException()
    {
        Assert.True(typeof(JulesNetworkException).IsSubclassOf(typeof(JulesException)));
        Assert.True(typeof(JulesApiException).IsSubclassOf(typeof(JulesException)));
        Assert.True(typeof(JulesAuthenticationException).IsSubclassOf(typeof(JulesApiException)));
        Assert.True(typeof(JulesRateLimitException).IsSubclassOf(typeof(JulesApiException)));
        Assert.True(typeof(MissingApiKeyException).IsSubclassOf(typeof(JulesException)));
        Assert.True(typeof(SourceNotFoundException).IsSubclassOf(typeof(JulesException)));
        Assert.True(typeof(AutomatedSessionFailedException).IsSubclassOf(typeof(JulesException)));
        Assert.True(typeof(SyncInProgressException).IsSubclassOf(typeof(JulesException)));
        Assert.True(typeof(InvalidStateException).IsSubclassOf(typeof(JulesException)));
    }
}
