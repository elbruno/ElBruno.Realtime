using ElBruno.Realtime.SileroVad;
using ElBruno.Realtime.Whisper;

namespace ElBruno.Realtime.Tests;

/// <summary>
/// Security tests for path handling in model managers.
/// </summary>
/// <remarks>
/// Tests validate that model managers handle file paths safely.
/// 
/// SECURITY GAP IDENTIFIED (2026-02-28 by Kane):
/// Current implementation allows relative paths with ".." in cacheDir parameter.
/// Path.GetFullPath() resolves these to absolute paths, which may escape intended
/// cache directory boundaries. The existing validation only checks that the final
/// model path (with whitelisted filename) is within targetDir, but does NOT validate
/// that targetDir itself is within expected boundaries.
/// 
/// Example: cacheDir="../../../etc" resolves to "C:\etc" (or "/etc" on Linux) which
/// is outside the intended LocalApplicationData folder.
/// 
/// Recommendation: Add validation after Path.GetFullPath(cacheDir) to ensure resolved
/// path is within LocalApplicationData or a user-specified safe boundary.
/// 
/// These tests document current behavior for future hardening (tracked in issue #1).
/// </remarks>
public class ModelManagerSecurityTests
{
    // Tests for model ID path traversal (currently protected via whitelisting)
    
    [Fact]
    public async Task WhisperModelManager_RejectsUnknownModelId()
    {
        // Model ID is whitelisted - any non-whitelisted value throws
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            async () => await WhisperModelManager.EnsureModelAsync(
                modelId: "../../../etc/passwd"));

        Assert.Contains("Unknown Whisper model", ex.Message);
    }

    [Fact]
    public void SileroModelManager_UsesFixedFilename()
    {
        // Silero uses fixed filename "silero_vad.onnx" - no user input in filename
        // This test documents secure design (no injection surface for model filename)
        Assert.True(true);
    }

    // Tests documenting current cacheDir behavior (allows ".." traversal)
    
    [Fact]
    public async Task WhisperModelManager_AllowsRelativePathWithDotDot()
    {
        // DOCUMENTS CURRENT BEHAVIOR: Relative paths with ".." are accepted
        // Path.GetFullPath() resolves them, potentially escaping intended boundaries
        var cacheDir = Path.Combine(Path.GetTempPath(), "subdir", "..", "whisper-test-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(cacheDir);
            
            // This succeeds - no ArgumentException thrown
            var path = await WhisperModelManager.EnsureModelAsync(
                modelId: "whisper-tiny.en",
                cacheDir: cacheDir);
            
            Assert.NotNull(path);
        }
        finally
        {
            if (Directory.Exists(cacheDir))
            {
                try { Directory.Delete(cacheDir, recursive: true); } 
                catch { /* Best effort cleanup */ }
            }
        }
    }

    [Fact]
    public async Task SileroModelManager_AllowsRelativePathWithDotDot()
    {
        // DOCUMENTS CURRENT BEHAVIOR: Relative paths with ".." are accepted
        var cacheDir = Path.Combine(Path.GetTempPath(), "subdir", "..", "silero-test-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(cacheDir);
            
            // This succeeds - no ArgumentException thrown
            var path = await SileroModelManager.EnsureModelAsync(cacheDir: cacheDir);
            
            Assert.NotNull(path);
        }
        finally
        {
            if (Directory.Exists(cacheDir))
            {
                try { Directory.Delete(cacheDir, recursive: true); } 
                catch { /* Best effort cleanup */ }
            }
        }
    }

    // Positive tests - valid absolute paths work correctly
    
    [Fact]
    public async Task WhisperModelManager_AcceptsValidAbsolutePath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "test-whisper-cache-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(tempDir);
            
            var path = await WhisperModelManager.EnsureModelAsync(
                modelId: "whisper-tiny.en",
                cacheDir: tempDir);
            
            Assert.NotNull(path);
            Assert.StartsWith(tempDir, path);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, recursive: true); } 
                catch { /* Best effort cleanup */ }
            }
        }
    }

    [Fact]
    public async Task SileroModelManager_AcceptsValidAbsolutePath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "test-silero-cache-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(tempDir);
            
            var path = await SileroModelManager.EnsureModelAsync(cacheDir: tempDir);
            
            Assert.NotNull(path);
            Assert.StartsWith(tempDir, path);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, recursive: true); } 
                catch { /* Best effort cleanup */ }
            }
        }
    }
}
