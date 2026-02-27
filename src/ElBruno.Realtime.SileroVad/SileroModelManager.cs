using ElBruno.HuggingFace;

namespace ElBruno.Realtime.SileroVad;

/// <summary>
/// Manages downloading and caching of the Silero VAD ONNX model.
/// </summary>
public static class SileroModelManager
{
    private const string DefaultRepoId = "onnx-community/silero-vad";
    private const string ModelFileName = "onnx/model.onnx";

    private static readonly string DefaultCacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ElBruno", "PersonaPlex", "silero-vad");

    /// <summary>
    /// Ensures the Silero VAD model is downloaded and returns the path.
    /// </summary>
    /// <param name="cacheDir">Optional cache directory. Uses default if null.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Full path to the downloaded model file.</returns>
    public static async Task<string> EnsureModelAsync(
        string? cacheDir = null,
        CancellationToken cancellationToken = default)
    {
        var targetDir = Path.GetFullPath(cacheDir ?? DefaultCacheDir);
        Directory.CreateDirectory(targetDir);

        var modelPath = Path.GetFullPath(Path.Combine(targetDir, "silero_vad.onnx"));
        // Ensure resolved path is still under targetDir (prevent path traversal)
        if (!modelPath.StartsWith(targetDir, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Invalid cache directory path.", nameof(cacheDir));

        if (File.Exists(modelPath))
            return modelPath;

        // Download from HuggingFace
        using var downloader = new HuggingFaceDownloader();
        await downloader.DownloadFilesAsync(new DownloadRequest
        {
            RepoId = DefaultRepoId,
            LocalDirectory = targetDir,
            RequiredFiles = [ModelFileName],
        }, cancellationToken);

        // Move from nested onnx/ directory to flat path
        var downloadedPath = Path.Combine(targetDir, "onnx", "model.onnx");
        if (File.Exists(downloadedPath) && !File.Exists(modelPath))
        {
            File.Move(downloadedPath, modelPath);
            // Clean up onnx directory
            var onnxDir = Path.Combine(targetDir, "onnx");
            if (Directory.Exists(onnxDir) && !Directory.EnumerateFileSystemEntries(onnxDir).Any())
                Directory.Delete(onnxDir);
        }

        return modelPath;
    }
}
