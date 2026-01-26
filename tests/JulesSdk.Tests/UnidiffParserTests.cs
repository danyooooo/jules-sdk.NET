// Copyright 2026 Google LLC
// SPDX-License-Identifier: Apache-2.0

using JulesSdk.Models;
using JulesSdk.Utilities;
using Xunit;

namespace JulesSdk.Tests;

public class UnidiffParserTests
{
    private const string SampleDiff = @"diff --git a/README.md b/README.md
new file mode 100644
index 0000000..e69de29
--- /dev/null
+++ b/README.md
@@ -0,0 +1,3 @@
+# Hello World
+
+This is a test file.
diff --git a/src/main.py b/src/main.py
index abc1234..def5678 100644
--- a/src/main.py
+++ b/src/main.py
@@ -1,5 +1,7 @@
 def main():
-    print(""Hello"")
+    print(""Hello World"")
+    print(""Goodbye"")
     return 0
 
 if __name__ == ""__main__"":
";

    [Fact]
    public void Parse_WithValidDiff_ReturnsCorrectFiles()
    {
        // Act
        var result = UnidiffParser.Parse(SampleDiff);
        
        // Assert
        Assert.Equal(2, result.Files.Count);
        Assert.Equal(2, result.Summary.TotalFiles);
    }
    
    [Fact]
    public void Parse_WithNewFile_IdentifiesAsCreated()
    {
        // Act
        var result = UnidiffParser.Parse(SampleDiff);
        
        // Assert
        var newFile = result.Files.First(f => f.Path == "README.md");
        Assert.Equal(ChangeType.Created, newFile.ChangeType);
        Assert.Equal(3, newFile.Additions);
        Assert.Equal(0, newFile.Deletions);
    }
    
    [Fact]
    public void Parse_WithModifiedFile_IdentifiesAsModified()
    {
        // Act
        var result = UnidiffParser.Parse(SampleDiff);
        
        // Assert
        var modifiedFile = result.Files.First(f => f.Path == "src/main.py");
        Assert.Equal(ChangeType.Modified, modifiedFile.ChangeType);
        Assert.Equal(2, modifiedFile.Additions);
        Assert.Equal(1, modifiedFile.Deletions);
    }
    
    [Fact]
    public void Parse_WithNullPatch_ReturnsEmptyResult()
    {
        // Act
        var result = UnidiffParser.Parse(null);
        
        // Assert
        Assert.Empty(result.Files);
        Assert.Equal(0, result.Summary.TotalFiles);
    }
    
    [Fact]
    public void Parse_SummaryCountsAreCorrect()
    {
        // Act
        var result = UnidiffParser.Parse(SampleDiff);
        
        // Assert
        Assert.Equal(1, result.Summary.Created);
        Assert.Equal(1, result.Summary.Modified);
        Assert.Equal(0, result.Summary.Deleted);
    }
    
    [Fact]
    public void ParseWithContent_ExtractsFileContent()
    {
        // Act
        var files = UnidiffParser.ParseWithContent(SampleDiff);
        
        // Assert
        var readme = files.First(f => f.Path == "README.md");
        Assert.Contains("Hello World", readme.Content);
        Assert.Contains("This is a test file", readme.Content);
    }
}
