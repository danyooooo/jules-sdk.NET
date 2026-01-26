// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using JulesSdk.Models;
using JulesSdk.Storage;
using Xunit;

namespace JulesSdk.Tests.Storage;

public class FileStorageTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FileStorage _storage;

    public FileStorageTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "jules_test_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
        _storage = new FileStorage(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task Session_Persists_To_Disk()
    {
        var session = new SessionResource { Id = "sessions/123", CreateTime = DateTime.UtcNow };

        // Insert
        await _storage.UpsertSessionAsync(session);

        // Verify file exists
        // The ID "sessions/123" should be sanitized to "sessions_123"
        var sanitizedId = "sessions_123";
        var sessionFile = Path.Combine(_tempDir, "sessions", $"{sanitizedId}.json"); 
        
        Assert.True(File.Exists(sessionFile), "Session file should exist on disk");
        
        // Re-read from new instance to ensure persistence
        var storage2 = new FileStorage(_tempDir);
        var retrieved = await storage2.GetSessionAsync("sessions/123");
        
        Assert.NotNull(retrieved);
        Assert.Equal(session.Id, retrieved!.Id);
    }
}
