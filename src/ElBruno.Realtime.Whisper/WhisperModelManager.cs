using Whisper.net;
using Whisper.net.Ggml;

namespace ElBruno.Realtime.Whisper;

/// <summary>
/// Manages downloading and caching of Whisper GGML models.
/// </summary>
public static class WhisperModelManager
{
    private static readonly string DefaultCacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ElBruno", "PersonaPlex", "whisper-models");

    /// <summary>Known Whisper model mappings from friendly name to GgmlType.</summary>
    private static readonly Dictionary<string, GgmlType> ModelMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["whisper-tiny.en"] = GgmlType.TinyEn,
        ["whisper-tiny"] = GgmlType.Tiny,
        ["whisper-base.en"] = GgmlType.BaseEn,
        ["whisper-base"] = GgmlType.Base,
        ["whisper-small.en"] = GgmlType.SmallEn,
        ["whisper-small"] = GgmlType.Small,
        ["whisper-medium.en"] = GgmlType.MediumEn,
        ["whisper-medium"] = GgmlType.Medium,
        ["whisper-large-v3"] = GgmlType.LargeV3,
    };

    /// <summary>
    /// Ensures the specified Whisper model is downloaded and returns the path.
    /// </summary>
    /// <param name="modelId">Model identifier (e.g., "whisper-tiny.en", "whisper-base.en").</param>
    /// <param name="cacheDir">Optional cache directory. Uses default if null.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Full path to the downloaded model file.</returns>
    public static async Task<string> EnsureModelAsync(
        string modelId = "whisper-tiny.en",
        string? cacheDir = null,
        CancellationToken cancellationToken = default)
    {
        var targetDir = Path.GetFullPath(cacheDir ?? DefaultCacheDir);
        Directory.CreateDirectory(targetDir);

        if (!ModelMap.TryGetValue(modelId, out var ggmlType))
            throw new ArgumentException($"Unknown Whisper model: '{modelId}'. Supported: {string.Join(", ", ModelMap.Keys)}", nameof(modelId));

        var fileName = $"ggml-{modelId.Replace("whisper-", "")}.bin";
        var modelPath = Path.GetFullPath(Path.Combine(targetDir, fileName));
        // Ensure resolved path is still under targetDir (prevent path traversal)
        if (!modelPath.StartsWith(targetDir, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Invalid model ID or cache directory.", nameof(modelId));

        if (File.Exists(modelPath))
            return modelPath;

        using var modelStream = await WhisperGgmlDownloader.Default
            .GetGgmlModelAsync(ggmlType);

        using var fileStream = File.Create(modelPath);
        await modelStream.CopyToAsync(fileStream, cancellationToken);

        return modelPath;
    }

    /// <summary>Gets the list of supported model identifiers.</summary>
    public static IReadOnlyCollection<string> SupportedModels => ModelMap.Keys;
}
