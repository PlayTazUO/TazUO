using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClassicUO.Game;
using ClassicUO.Game.UI.ImGuiControls;
using ImGuiNET;
using System.Numerics;
using System.Threading;
using ClassicUO.Game.UI;
using ClassicUO.Game.UI.ImGuiControls.Legion;

namespace ClassicUO.LegionScripting;

public class ScriptBrowser : SingletonImGuiWindow<ScriptBrowser>
{
    private readonly ConcurrentQueue<Action> _mainThreadActions = new();
    private const string REPO = "PlayTazUO/PublicLegionScripts";

    private readonly GitHubContentCache _cache;
    private readonly Dictionary<string, DirectoryNode> _directoryCache = new();
    private bool _isInitialLoading = false;
    private string _errorMessage = "";

    private ScriptBrowser() : base("Public Script Browser")
    {
        _cache = new GitHubContentCache(REPO);
        WindowFlags = ImGuiWindowFlags.None;

        // Start loading root directory
        LoadDirectoryAsync("");
    }

    public override void DrawContent()
    {
        // Show loading state
        if (_isInitialLoading)
        {
            ImGui.Text("Loading repository contents...");
            return;
        }

        // Show error message if any
        if (!string.IsNullOrEmpty(_errorMessage))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.4f, 0.4f, 1.0f));
            ImGui.TextWrapped(_errorMessage);
            ImGui.PopStyleColor();

            if (ImGui.Button("Retry"))
            {
                _errorMessage = "";
                LoadDirectoryAsync("");
            }
            return;
        }

        // Draw the tree view
        if (ImGui.BeginChild("ScriptTreeView", new Vector2(0, 0), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar))
        {
            DrawDirectoryTree("", 0);
        }
        ImGui.EndChild();
    }

    public override void Update()
    {
        base.Update();

        // Process main thread actions
        int processedCount = 0;
        while (_mainThreadActions.TryDequeue(out Action action) && processedCount < 10)
        {
            try
            {
                action();
                processedCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing main thread action: {ex.Message}");
            }
        }
    }

    private void DrawDirectoryTree(string path, int depth)
    {
        // Get or create directory node
        if (!_directoryCache.TryGetValue(path, out DirectoryNode node))
        {
            node = new DirectoryNode { Path = path, IsLoaded = false };
            _directoryCache[path] = node;
        }

        // Load directory if not loaded
        if (!node.IsLoaded && !node.IsLoading)
        {
            LoadDirectoryAsync(path);
            return;
        }

        // Show loading state
        if (node.IsLoading)
        {
            ImGui.Text("Loading...");
            return;
        }

        // Draw directories
        var directories = node.Contents.Where(f => f.Type == "dir").OrderBy(f => f.Name).ToList();
        foreach (GhFileObject dir in directories)
        {
            ImGui.PushID(dir.Path);

            // Check if this directory is expanded
            bool isExpanded = _directoryCache.TryGetValue(dir.Path, out DirectoryNode childNode) && childNode.IsExpanded;

            // Draw tree node
            ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;

            bool nodeOpen = ImGui.TreeNodeEx($"{dir.Name}", flags);

            // Update expansion state
            if (nodeOpen != isExpanded)
            {
                if (!_directoryCache.ContainsKey(dir.Path))
                    _directoryCache[dir.Path] = new DirectoryNode { Path = dir.Path, IsLoaded = false };
                _directoryCache[dir.Path].IsExpanded = nodeOpen;
            }

            if (nodeOpen)
            {
                // Draw subdirectory contents
                DrawDirectoryTree(dir.Path, depth + 1);
                ImGui.TreePop();
            }

            ImGui.PopID();
        }

        // Draw script files
        var scriptFiles = node.Contents.Where(f => f.Type == "file" && (f.Name.EndsWith(".py"))).OrderBy(f => f.Name).ToList();
        foreach (GhFileObject file in scriptFiles)
        {
            ImGui.PushID(file.Path);

            // Draw file as selectable
            if (ImGui.Selectable($"    {file.Name}"))
            {
                DownloadAndOpenScript(file);
            }

            // Tooltip
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip($"Click to download and open\n{file.Path}");
            }

            ImGui.PopID();
        }
    }

    private void LoadDirectoryAsync(string path)
    {
        if (!_directoryCache.TryGetValue(path, out DirectoryNode node))
        {
            node = new DirectoryNode { Path = path };
            _directoryCache[path] = node;
        }

        if (node.IsLoading || node.IsLoaded) return;

        node.IsLoading = true;
        if (string.IsNullOrEmpty(path))
            _isInitialLoading = true;

        Task.Run(async () =>
        {
            try
            {
                List<GhFileObject> files = await _cache.GetDirectoryContentsAsync(path);
                _mainThreadActions.Enqueue(() =>
                {
                    node.Contents = files;
                    node.IsLoaded = true;
                    node.IsLoading = false;
                    if (string.IsNullOrEmpty(path))
                    {
                        _isInitialLoading = false;
                        node.IsExpanded = true; // Auto-expand root
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading directory {path}: {ex.Message}");
                _mainThreadActions.Enqueue(() =>
                {
                    node.IsLoading = false;
                    if (string.IsNullOrEmpty(path))
                    {
                        _isInitialLoading = false;
                        _errorMessage = $"Failed to load scripts: {ex.Message}";
                    }
                });
            }
        });
    }

    private void DownloadAndOpenScript(GhFileObject file) => Task.Run(async () =>
    {
        try
        {
            string content = await _cache.GetFileContentAsync(file.DownloadUrl);
            _mainThreadActions.Enqueue(() =>
            {
                try
                {
                    // Validate and sanitize the filename to prevent path traversal
                    string sanitizedFileName = Path.GetFileName(file.Name);

                    // Reject names that contain path separators, relative navigation, or are empty
                    if (string.IsNullOrWhiteSpace(sanitizedFileName) ||
                        sanitizedFileName != file.Name ||
                        sanitizedFileName.Contains("\\") ||
                        sanitizedFileName.Contains("/") ||
                        sanitizedFileName.Contains("..") ||
                        sanitizedFileName == "." ||
                        sanitizedFileName == "..")
                    {
                        GameActions.Print(World.Instance, $"Invalid script filename: {file.Name}. Filename contains invalid characters or path separators.", 32);
                        Console.WriteLine($"Security: Rejected invalid filename: {file.Name}");
                        return;
                    }

                    // Check for invalid filename characters
                    char[] invalidChars = Path.GetInvalidFileNameChars();
                    if (sanitizedFileName.IndexOfAny(invalidChars) >= 0)
                    {
                        GameActions.Print(World.Instance, $"Invalid script filename: {file.Name}. Filename contains invalid characters.", 32);
                        Console.WriteLine($"Security: Rejected filename with invalid characters: {file.Name}");
                        return;
                    }

                    // Ensure the script directory exists
                    if (!Directory.Exists(LegionScripting.ScriptPath))
                    {
                        Directory.CreateDirectory(LegionScripting.ScriptPath);
                    }

                    // Create the full file path
                    string filePath = Path.Combine(LegionScripting.ScriptPath, sanitizedFileName);

                    // Resolve to full path and verify it's within the scripts directory
                    string fullFilePath = Path.GetFullPath(filePath);
                    string fullScriptPath = Path.GetFullPath(LegionScripting.ScriptPath);

                    // Verify the resolved path starts with the scripts root directory
                    if (!fullFilePath.StartsWith(fullScriptPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                        !fullFilePath.Equals(fullScriptPath, StringComparison.OrdinalIgnoreCase))
                    {
                        GameActions.Print(World.Instance, $"Security error: Script path must be within the scripts directory.", 32);
                        Console.WriteLine($"Security: Path traversal attempt blocked. File: {file.Name}, Resolved: {fullFilePath}");
                        return;
                    }

                    // Handle duplicate files by appending a number
                    string finalFileName = sanitizedFileName;
                    string finalFilePath = fullFilePath;

                    if (File.Exists(fullFilePath))
                    {
                        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sanitizedFileName);
                        string extension = Path.GetExtension(sanitizedFileName);
                        int counter = 1;

                        do
                        {
                            finalFileName = $"{fileNameWithoutExtension} ({counter}){extension}";
                            finalFilePath = Path.Combine(LegionScripting.ScriptPath, finalFileName);

                            // Re-validate the new path
                            string fullFinalPath = Path.GetFullPath(finalFilePath);
                            if (!fullFinalPath.StartsWith(fullScriptPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                                !fullFinalPath.Equals(fullScriptPath, StringComparison.OrdinalIgnoreCase))
                            {
                                GameActions.Print(World.Instance, $"Security error: Generated path is invalid.", 32);
                                return;
                            }

                            finalFilePath = fullFinalPath;
                            counter++;
                        } while (File.Exists(finalFilePath) && counter < 1000); // Limit to prevent infinite loop

                        if (counter >= 1000)
                        {
                            GameActions.Print(World.Instance, $"Too many duplicate files. Please clean up your scripts directory.", 32);
                            return;
                        }
                    }

                    // Write the content to disk
                    File.WriteAllText(finalFilePath, content, Encoding.UTF8);

                    // Create ScriptFile object pointing to the saved file
                    var f = new ScriptFile(World.Instance, LegionScripting.ScriptPath, finalFileName);
                    ImGuiManager.AddWindow(new ScriptEditorWindow(f));

                    GameActions.Print(World.Instance, $"Downloaded script: {finalFileName}");

                    // Refresh script manager if open
                    ScriptManagerWindow.Instance?.Update();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating script file: {ex.Message}");
                    GameActions.Print(World.Instance, $"Error saving script: {file.Name} - {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading file: {ex.Message}");
            _mainThreadActions.Enqueue(() =>
            {
                GameActions.Print(World.Instance, $"Error loading script: {file.Name}");
            });
        }
    });

    public override void Dispose()
    {
        _cache?.Dispose();
        base.Dispose();
    }

    private class DirectoryNode
    {
        public string Path { get; set; }
        public List<GhFileObject> Contents { get; set; } = new();
        public bool IsLoaded { get; set; }
        public bool IsLoading { get; set; }
        public bool IsExpanded { get; set; }
    }

    public class GhFileObject
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Sha { get; set; }
        public int Size { get; set; }
        public string Url { get; set; }
        public string HtmlUrl { get; set; }
        public string GitUrl { get; set; }
        public string DownloadUrl { get; set; }
        public string Type { get; set; }
        public Links Links { get; set; }
    }

    public class Links
    {
        public string Self { get; set; }
        public string Git { get; set; }
        public string Html { get; set; }
    }
}

/// <summary>
/// Caches GitHub repository content using WebClient for Mono compatibility
/// </summary>
internal class GitHubContentCache : IDisposable
{
    private readonly string _repository;
    private readonly string _baseUrl;
    private readonly Dictionary<string, List<ScriptBrowser.GhFileObject>> _directoryCache;
    private readonly Dictionary<string, string> _fileContentCache;
    private readonly Dictionary<string, DateTime> _cacheTimestamps;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(10);
    private DateTime _lastApiCallTime = DateTime.MinValue;
    private readonly Lock _rateLimitLock = new Lock();
    private const int MIN_MS_BETWEEN_REQUESTS = 1000; // 1 second between requests

    public GitHubContentCache(string repo)
    {
        _repository = repo;
        _baseUrl = $"https://api.github.com/repos/{_repository}/contents";
        _directoryCache = new Dictionary<string, List<ScriptBrowser.GhFileObject>>();
        _fileContentCache = new Dictionary<string, string>();
        _cacheTimestamps = new Dictionary<string, DateTime>();
    }

    /// <summary>
    /// Get directory contents, using cache if available and not expired
    /// </summary>
    public async Task<List<ScriptBrowser.GhFileObject>> GetDirectoryContentsAsync(string path = "")
    {
        string cacheKey = string.IsNullOrEmpty(path) ? "ROOT" : path;

        // Check if we have cached data that's still valid
        if (_directoryCache.ContainsKey(cacheKey) &&
            _cacheTimestamps.ContainsKey(cacheKey) &&
            DateTime.Now - _cacheTimestamps[cacheKey] < _cacheExpiration)
        {
            return _directoryCache[cacheKey];
        }

        // Fetch from API
        List<ScriptBrowser.GhFileObject> contents = await FetchDirectoryFromApi(path);

        // Cache the results
        _directoryCache[cacheKey] = contents;
        _cacheTimestamps[cacheKey] = DateTime.Now;

        // Pre-cache subdirectories in background for faster navigation
        // Process sequentially to respect rate limiting (1 request per second)
        _ = Task.Run(async () =>
        {
            IEnumerable<ScriptBrowser.GhFileObject> directories = contents.Where(f => f.Type == "dir").Take(3); // Reduced from 5 to 3 to minimize initial load time
            foreach (ScriptBrowser.GhFileObject dir in directories)
            {
                try
                {
                    if (!_directoryCache.ContainsKey(dir.Path))
                    {
                        await GetDirectoryContentsAsync(dir.Path); // Rate limiting is enforced in DownloadStringAsync
                    }
                }
                catch
                {
                    // Ignore errors in background pre-caching
                }
            }
        });

        return contents;
    }

    /// <summary>
    /// Get file content using WebClient, with caching
    /// </summary>
    public async Task<string> GetFileContentAsync(string downloadUrl)
    {
        if (_fileContentCache.ContainsKey(downloadUrl))
        {
            return _fileContentCache[downloadUrl];
        }

        string content = await DownloadStringAsync(downloadUrl);
        _fileContentCache[downloadUrl] = content;

        return content;
    }

    /// <summary>
    /// Fetch directory contents from GitHub API using WebClient
    /// </summary>
    private async Task<List<ScriptBrowser.GhFileObject>> FetchDirectoryFromApi(string path)
    {
        try
        {
            string url = string.IsNullOrEmpty(path) ? _baseUrl : $"{_baseUrl}/{path}";
            string response = await DownloadStringAsync(url);

            if (string.IsNullOrEmpty(response))
            {
                return new List<ScriptBrowser.GhFileObject>();
            }

            List<ScriptBrowser.GhFileObject> files = JsonSerializer.Deserialize<List<ScriptBrowser.GhFileObject>>(response);
            return files ?? new List<ScriptBrowser.GhFileObject>();
        }
        catch (WebException webEx)
        {
            Console.WriteLine($"Web error fetching directory {path}: {webEx.Message}");
            if (webEx.Response is HttpWebResponse httpResponse)
            {
                Console.WriteLine($"HTTP Status: {httpResponse.StatusCode}");
            }
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

    /// <summary>
    /// Enforce rate limiting to ensure minimum delay between API calls
    /// </summary>
    private async Task EnforceRateLimitAsync()
    {
        int delayNeeded = 0;

        lock (_rateLimitLock)
        {
            int timeSinceLastCall = (int)(DateTime.Now - _lastApiCallTime).TotalMilliseconds;
            if (timeSinceLastCall < MIN_MS_BETWEEN_REQUESTS)
            {
                delayNeeded = MIN_MS_BETWEEN_REQUESTS - timeSinceLastCall;
            }
            _lastApiCallTime = DateTime.Now.AddMilliseconds(delayNeeded);
        }

        if (delayNeeded > 0)
        {
            await Task.Delay(delayNeeded);
        }
    }

    /// <summary>
    /// Download string content using WebClient with proper async handling and timeout
    /// </summary>
    private async Task<string> DownloadStringAsync(string url)
    {
        // Enforce rate limiting before making the request
        await EnforceRateLimitAsync();

        var tcs = new TaskCompletionSource<string>();

        var webClient = new WebClient();
        webClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        webClient.Encoding = Encoding.UTF8;

        // Add timeout handling
        var timer = new System.Threading.Timer((_) =>
        {
            if (!tcs.Task.IsCompleted)
            {
                webClient.CancelAsync();
                tcs.TrySetException(new TimeoutException("Request timed out"));
            }
        }, null, TimeSpan.FromSeconds(30), TimeSpan.FromMilliseconds(-1));

        webClient.DownloadStringCompleted += (sender, e) =>
        {
            timer.Dispose();
            try
            {
                if (e.Error != null)
                {
                    tcs.TrySetException(e.Error);
                }
                else if (e.Cancelled)
                {
                    tcs.TrySetCanceled();
                }
                else
                {
                    tcs.TrySetResult(e.Result);
                }
            }
            finally
            {
                webClient.Dispose();
            }
        };

        try
        {
            webClient.DownloadStringAsync(new Uri(url));
        }
        catch (Exception ex)
        {
            timer.Dispose();
            webClient.Dispose();
            tcs.TrySetException(ex);
        }

        return tcs.Task.Result;
    }

    /// <summary>
    /// Clear all cached data
    /// </summary>
    public void ClearCache()
    {
        _directoryCache.Clear();
        _fileContentCache.Clear();
        _cacheTimestamps.Clear();
    }

    /// <summary>
    /// Clear expired cache entries
    /// </summary>
    public void ClearExpiredCache()
    {
        DateTime now = DateTime.Now;
        var expiredKeys = _cacheTimestamps
            .Where(kvp => now - kvp.Value >= _cacheExpiration)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (string key in expiredKeys)
        {
            _directoryCache.Remove(key);
            _cacheTimestamps.Remove(key);
        }
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public (int Directories, int Files, int Expired) GetCacheStats()
    {
        DateTime now = DateTime.Now;
        int expired = _cacheTimestamps.Count(kvp => now - kvp.Value >= _cacheExpiration);

        return (_directoryCache.Count, _fileContentCache.Count, expired);
    }

    public void Dispose() => ClearCache();
}
