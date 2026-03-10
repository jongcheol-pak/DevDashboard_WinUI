using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DevDashboard.Infrastructure.Services;

/// <summary>
/// 아이콘 추출 및 LRU 캐싱 서비스.
/// 캐시 파일은 LocalState 폴더에 저장됩니다.
/// </summary>
internal static class IconCacheService
{
    private static readonly ConcurrentDictionary<string, (string path, DateTime lastAccess)> _cache = new();
    private static readonly SemaphoreSlim _saveLock = new(1, 1);
    private const int MaxCacheSize = 500;

    private static readonly string CacheFolder = Path.Combine(
        Windows.Storage.ApplicationData.Current.LocalFolder.Path, "LauncherIcons");
    private static readonly string CacheFilePath = Path.Combine(
        Windows.Storage.ApplicationData.Current.LocalFolder.Path, "launcher_icon_cache.json");

    static IconCacheService()
    {
        Directory.CreateDirectory(CacheFolder);
        LoadCache();
    }

    /// <summary>
    /// 파일에서 아이콘을 추출하여 캐시된 PNG 경로를 반환합니다.
    /// </summary>
    public static async Task<string?> GetIconPathAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return null;

        bool isUwpApp = filePath.StartsWith("shell:AppsFolder", StringComparison.OrdinalIgnoreCase);
        if (!isUwpApp && !File.Exists(filePath)) return null;

        string cacheKey = ComputeCacheKey(filePath);

        // 캐시 히트
        if (_cache.TryGetValue(cacheKey, out var entry) && File.Exists(entry.path))
        {
            _cache.TryUpdate(cacheKey, (entry.path, DateTime.Now), entry);
            return entry.path;
        }

        // 캐시 미스 — 추출
        _cache.TryRemove(cacheKey, out _);

        try
        {
            bool isProtectedPath = filePath.Contains("Program Files", StringComparison.OrdinalIgnoreCase);
            var timeout = isProtectedPath ? TimeSpan.FromSeconds(5) : TimeSpan.FromSeconds(3);

            string? extractedPath = await IconExtractor.ExtractIconAndSaveAsync(filePath, CacheFolder, 48, timeout);
            if (extractedPath is null || !File.Exists(extractedPath)) return null;

            // 캐시 크기 초과 시 정리 후 추가
            if (_cache.Count >= MaxCacheSize)
                await CleanupAsync();

            _cache[cacheKey] = (extractedPath, DateTime.Now);
            _ = SaveCacheAsync();
            return extractedPath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"아이콘 캐시 실패 ({filePath}): {ex.Message}");
            return null;
        }
    }

    /// <summary>이미지 파일을 BitmapImage로 로드합니다.</summary>
    public static async Task<BitmapImage?> LoadImageAsync(string filePath, int decodeSize = 36)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;

        try
        {
            var bmp = new BitmapImage
            {
                DecodePixelWidth = decodeSize,
                DecodePixelHeight = decodeSize,
                DecodePixelType = DecodePixelType.Logical
            };
            using var stream = File.OpenRead(filePath);
            using var ras = stream.AsRandomAccessStream();
            await bmp.SetSourceAsync(ras);
            return bmp;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"이미지 로드 실패: {ex.Message}");
            return null;
        }
    }

    private static string ComputeCacheKey(string filePath)
    {
        if (filePath.StartsWith("shell:AppsFolder", StringComparison.OrdinalIgnoreCase))
            return filePath;

        if (!File.Exists(filePath)) return filePath;

        var fi = new FileInfo(filePath);
        return $"{filePath}_{fi.LastWriteTimeUtc.Ticks}_{fi.Length}";
    }

    private static void LoadCache()
    {
        try
        {
            if (!File.Exists(CacheFilePath)) return;
            string json = File.ReadAllText(CacheFilePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (data is null) return;

            foreach (var kvp in data)
            {
                if (!string.IsNullOrEmpty(kvp.Value) && File.Exists(kvp.Value))
                    _cache.TryAdd(kvp.Key, (kvp.Value, DateTime.Now));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"아이콘 캐시 로드 실패: {ex.Message}");
        }
    }

    private static async Task SaveCacheAsync()
    {
        if (!await _saveLock.WaitAsync(TimeSpan.FromSeconds(2))) return;
        try
        {
            var snapshot = _cache.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.path);
            string json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(CacheFilePath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"아이콘 캐시 저장 실패: {ex.Message}");
        }
        finally
        {
            _saveLock.Release();
        }
    }

    private static async Task CleanupAsync()
    {
        var keysToRemove = _cache.Where(kvp => !File.Exists(kvp.Value.path)).Select(kvp => kvp.Key).ToList();
        foreach (var key in keysToRemove)
            _cache.TryRemove(key, out _);

        if (_cache.Count >= MaxCacheSize)
        {
            int removeCount = Math.Max(1, (int)(_cache.Count * 0.2));
            var oldest = _cache.OrderBy(kvp => kvp.Value.lastAccess).Take(removeCount).ToList();
            foreach (var entry in oldest)
                _cache.TryRemove(entry.Key, out _);
        }

        await SaveCacheAsync();
    }
}
