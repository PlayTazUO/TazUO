using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClassicUO.Game;
using ClassicUO.Game.UI.ImGuiControls;
using ClassicUO.Game.UI.MyraWindows;
using ImGuiNET;
using System.Numerics;
using System.Threading;
using ClassicUO.Game.UI;
using System.Text.Json.Serialization;
using ClassicUO.Utility.Platforms;

namespace ClassicUO.LegionScripting;

[JsonSerializable(typeof(ScriptBrowser.GhFileObject))]
[JsonSerializable(typeof(List<ScriptBrowser.GhFileObject>))]
[JsonSerializable(typeof(ScriptBrowser.Links))]
internal partial class ScriptBrowserJsonContext : JsonSerializerContext { }

public class ScriptBrowser : SingletonImGuiWindow<ScriptBrowser>
{
    private readonly ConcurrentQueue<Action> _mainThreadActions = new();
    private const string REPO = "PlayTazUO/PublicLegionScripts";

    private readonly GitHubContentCache _cache;
    private readonly Dictionary<string, DirectoryNode> _directoryCache = new();
    private bool _isInitialLoading = false;
    private string _errorMessage = "";

    private string _previewTitle = "";
    private string _previewContent = "";
    private bool _previewLoading = false;
    private bool _openPreviewPopup = false;

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
            if (ImGui.BeginTable("ScriptTable", 4, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.ScrollX))
            {
                ImGui.TableSetupColumn("Script", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("View", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Download", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Link", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableHeadersRow();

                DrawDirectoryTree("", 0);

                ImGui.EndTable();
            }
        }
        ImGui.EndChild();

        DrawPreviewPopup();
    }

    private void DrawPreviewPopup()
    {
        if (_openPreviewPopup)
        {
            ImGui.OpenPopup("ScriptPreview");
            _openPreviewPopup = false;
        }

        ImGui.SetNextWindowSize(new Vector2(700, 500), ImGuiCond.Appearing);
        if (ImGui.BeginPopupModal("ScriptPreview", ImGuiWindowFlags.None))
        {
            ImGui.Text(_previewTitle);
            ImGui.Separator();

            var contentSize = new Vector2(-1, ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing() - ImGui.GetStyle().ItemSpacing.Y);

            if (_previewLoading)
            {
                ImGui.Text("Loading...");
            }
            else
            {
                ImGui.InputTextMultiline("##preview", ref _previewContent, (uint)_previewContent.Length + 1, contentSize, ImGuiInputTextFlags.ReadOnly);
            }

            if (ImGui.Button("Close"))
                ImGui.CloseCurrentPopup();

            ImGui.EndPopup();
        }
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
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.Text("Loading...");
            return;
        }

        // Draw directories
        var directories = node.Contents.Where(f => f.Type == "dir").OrderBy(f => f.Name).ToList();
        foreach (GhFileObject dir in directories)
        {
            ImGui.PushID(dir.Path);
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);

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
        var scriptFiles = node.Contents.Where(f => f.Type == "file" && (f.Name.EndsWith(".py") || f.Name.EndsWith(".cs"))).OrderBy(f => f.Name).ToList();
        foreach (GhFileObject file in scriptFiles)
        {
            ImGui.PushID(file.Path);
            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            ImGui.Text(file.Name);

            ImGui.TableSetColumnIndex(1);
            if (ImGui.SmallButton("View"))
                ViewScript(file);

            ImGui.TableSetColumnIndex(2);
            if (ImGui.SmallButton("Download"))
                DownloadAndOpenScript(file);

            ImGui.TableSetColumnIndex(3);
            if (ImGui.SmallButton("Open Link"))
                PlatformHelper.LaunchBrowser(file.HtmlUrl);

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
                    new ScriptEditorWindow(f);

                    GameActions.Print(World.Instance, $"Downloaded script: {finalFileName}");

                    // Refresh script manager if open
                    ScriptManagerWindow.Instance?.Refresh();
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

    private void ViewScript(GhFileObject file) => Task.Run(async () =>
    {
        _mainThreadActions.Enqueue(() =>
        {
            _previewTitle = file.Name;
            _previewContent = "";
            _previewLoading = true;
            _openPreviewPopup = true;
        });

        try
        {
            string content = await _cache.GetFileContentAsync(file.DownloadUrl);
            _mainThreadActions.Enqueue(() =>
            {
                _previewContent = content;
                _previewLoading = false;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading file for preview: {ex.Message}");
            _mainThreadActions.Enqueue(() =>
            {
                _previewContent = $"Error loading file: {ex.Message}";
                _previewLoading = false;
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
    private const int MIN_MS_BETWEEN_REQUESTS = 1000; // 1 second between requests

    public GitHubContentCache(string repo)
    {
        _repository = repo;
        _baseUrl = $"https://api.github.com/repos/{_repository}/contents";
    }

    /// <summary>
    /// Get directory contents, using cache if available and not expired
    /// </summary>
    public async Task<List<ScriptBrowser.GhFileObject>> GetDirectoryContentsAsync(string path = "")
    {
        string cacheKey = string.IsNullOrEmpty(path) ? "ROOT" : path;

        // Check if we have cached data that's still valid
        if (_directoryCache.TryGetValue(cacheKey, out List<ScriptBrowser.GhFileObject> cached) &&
            _cacheTimestamps.TryGetValue(cacheKey, out DateTime timestamp) &&
            DateTime.Now - timestamp < _cacheExpiration)
        {
            return cached;
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
        if (_fileContentCache.TryGetValue(downloadUrl, out string cachedContent))
        {
            return cachedContent;
        }

        string content = await DownloadStringAsync(downloadUrl);
        _fileContentCache[downloadUrl] = content;

        return content;
    }

    /// <summary>
    /// Fetch directory contents from GitHub API
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

            List<ScriptBrowser.GhFileObject> files = JsonSerializer.Deserialize(response, ScriptBrowserJsonContext.Default.ListGhFileObject);
            return files ?? new List<ScriptBrowser.GhFileObject>();
        }
        catch (HttpRequestException httpEx)
        {
            Console.WriteLine($"HTTP error fetching directory {path}: {httpEx.Message}");
            if (httpEx.StatusCode.HasValue)
            {
                Console.WriteLine($"HTTP Status: {httpEx.StatusCode}");
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
    /// Download string content using HttpClient with rate limiting
    /// </summary>
    private async Task<string> DownloadStringAsync(string url)
    {
        await EnforceRateLimitAsync();
        return await _httpClient.GetStringAsync(url);
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
            _directoryCache.TryRemove(key, out _);
            _cacheTimestamps.TryRemove(key, out _);
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
