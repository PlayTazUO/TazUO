using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Game.UI.MyraWindows;

namespace ClassicUO.LegionScripting;

[JsonSerializable(typeof(ScriptBrowser.GhFileObject))]
[JsonSerializable(typeof(List<ScriptBrowser.GhFileObject>))]
[JsonSerializable(typeof(ScriptBrowser.Links))]
internal partial class ScriptBrowserJsonContext : JsonSerializerContext { }

public static class ScriptBrowser
{
    public const string REPO = "PlayTazUO/PublicLegionScripts";

    public static void Show() => ScriptBrowserWindow.Show();

    public class GhFileObject
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("sha")]
        public string Sha { get; set; }

        [JsonPropertyName("size")]
        public int Size { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; }

        [JsonPropertyName("git_url")]
        public string GitUrl { get; set; }

        [JsonPropertyName("download_url")]
        public string DownloadUrl { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("_links")]
        public Links Links { get; set; }
    }

    public class Links
    {
        [JsonPropertyName("self")]
        public string Self { get; set; }

        [JsonPropertyName("git")]
        public string Git { get; set; }

        [JsonPropertyName("html")]
        public string Html { get; set; }
    }
}

/// <summary>
/// Caches GitHub repository content
/// </summary>
internal class GitHubContentCache : IDisposable
{
    private static readonly HttpClient _httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(30),
        DefaultRequestHeaders = { { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36" } }
    };

    private readonly string _repository;
    private readonly string _baseUrl;
    private readonly ConcurrentDictionary<string, List<ScriptBrowser.GhFileObject>> _directoryCache = new();
    private readonly ConcurrentDictionary<string, string> _fileContentCache = new();
    private readonly ConcurrentDictionary<string, DateTime> _cacheTimestamps = new();
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(10);
    private DateTime _lastApiCallTime = DateTime.MinValue;
    private readonly Lock _rateLimitLock = new Lock();
    private const int MIN_MS_BETWEEN_REQUESTS = 1000;

    public GitHubContentCache(string repo)
    {
        _repository = repo;
        _baseUrl = $"https://api.github.com/repos/{_repository}/contents";
    }

    public async Task<List<ScriptBrowser.GhFileObject>> GetDirectoryContentsAsync(string path = "")
    {
        string cacheKey = string.IsNullOrEmpty(path) ? "ROOT" : path;

        if (_directoryCache.TryGetValue(cacheKey, out List<ScriptBrowser.GhFileObject> cached) &&
            _cacheTimestamps.TryGetValue(cacheKey, out DateTime timestamp) &&
            DateTime.Now - timestamp < _cacheExpiration)
        {
            return cached;
        }

        List<ScriptBrowser.GhFileObject> contents = await FetchDirectoryFromApi(path);

        _directoryCache[cacheKey] = contents;
        _cacheTimestamps[cacheKey] = DateTime.Now;

        _ = Task.Run(async () =>
        {
            IEnumerable<ScriptBrowser.GhFileObject> directories = contents.Where(f => f.Type == "dir").Take(3);
            foreach (ScriptBrowser.GhFileObject dir in directories)
            {
                try
                {
                    if (!_directoryCache.ContainsKey(dir.Path))
                        await GetDirectoryContentsAsync(dir.Path);
                }
                catch { }
            }
        });

        return contents;
    }

    public async Task<string> GetFileContentAsync(string downloadUrl)
    {
        if (_fileContentCache.TryGetValue(downloadUrl, out string cachedContent))
            return cachedContent;

        string content = await DownloadStringAsync(downloadUrl);
        _fileContentCache[downloadUrl] = content;
        return content;
    }

    private async Task<List<ScriptBrowser.GhFileObject>> FetchDirectoryFromApi(string path)
    {
        try
        {
            string url = string.IsNullOrEmpty(path) ? _baseUrl : $"{_baseUrl}/{path}";
            string response = await DownloadStringAsync(url);

            if (string.IsNullOrEmpty(response))
                return new List<ScriptBrowser.GhFileObject>();

            List<ScriptBrowser.GhFileObject> files = JsonSerializer.Deserialize(response, ScriptBrowserJsonContext.Default.ListGhFileObject);
            return files ?? new List<ScriptBrowser.GhFileObject>();
        }
        catch (HttpRequestException httpEx)
        {
            Console.WriteLine($"HTTP error fetching directory {path}: {httpEx.Message}");
            if (httpEx.StatusCode.HasValue)
                Console.WriteLine($"HTTP Status: {httpEx.StatusCode}");
            throw;
        }
        catch (JsonException jsonEx)
        {
            Console.WriteLine($"JSON parsing error for directory {path}: {jsonEx.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching directory {path}: {ex.Message}");
            throw;
        }
    }

    private async Task EnforceRateLimitAsync()
    {
        int delayNeeded = 0;

        lock (_rateLimitLock)
        {
            int timeSinceLastCall = (int)(DateTime.Now - _lastApiCallTime).TotalMilliseconds;
            if (timeSinceLastCall < MIN_MS_BETWEEN_REQUESTS)
                delayNeeded = MIN_MS_BETWEEN_REQUESTS - timeSinceLastCall;
            _lastApiCallTime = DateTime.Now.AddMilliseconds(delayNeeded);
        }

        if (delayNeeded > 0)
            await Task.Delay(delayNeeded);
    }

    private async Task<string> DownloadStringAsync(string url)
    {
        await EnforceRateLimitAsync();
        return await _httpClient.GetStringAsync(url);
    }

    public void ClearCache()
    {
        _directoryCache.Clear();
        _fileContentCache.Clear();
        _cacheTimestamps.Clear();
    }

    public void Dispose() => ClearCache();
}
