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
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.ImGuiControls;
using ImGuiNET;
using System.Numerics;
using ClassicUO.Game.UI;
using ClassicUO.Game.UI.ImGuiControls.Legion;

namespace ClassicUO.LegionScripting
{
    using System.Text.Json.Serialization;

    [JsonSerializable(typeof(List<ScriptBrowser.GHFileObject>))]
    [JsonSerializable(typeof(ScriptBrowser.GHFileObject))]
    [JsonSerializable(typeof(ScriptBrowser._Links))]
    public partial class ScriptBrowserJsonContext : JsonSerializerContext
    {
    }

    public class ScriptBrowser : SingletonImGuiWindow<ScriptBrowser>
    {
        private readonly ConcurrentQueue<Action> _mainThreadActions = new();
        private const string REPO = "PlayTazUO/PublicLegionScripts";

        private readonly GitHubContentCache cache;
        private readonly Dictionary<string, DirectoryNode> directoryCache = new();
        private bool isInitialLoading = false;
        private string errorMessage = "";

        private ScriptBrowser() : base("Public Script Browser")
        {
            cache = new GitHubContentCache(REPO);
            WindowFlags = ImGuiWindowFlags.None;

            // Start loading root directory
            LoadDirectoryAsync("");
        }

        public override void DrawContent()
        {
            // Show loading state
            if (isInitialLoading)
            {
                ImGui.Text("Loading repository contents...");
                return;
            }

            // Show error message if any
            if (!string.IsNullOrEmpty(errorMessage))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.4f, 0.4f, 1.0f));
                ImGui.TextWrapped(errorMessage);
                ImGui.PopStyleColor();

                if (ImGui.Button("Retry"))
                {
                    errorMessage = "";
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
            if (!directoryCache.TryGetValue(path, out DirectoryNode node))
            {
                node = new DirectoryNode { Path = path, IsLoaded = false };
                directoryCache[path] = node;
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
            var directories = node.Contents.Where(f => f.type == "dir").OrderBy(f => f.name).ToList();
            foreach (GHFileObject dir in directories)
            {
                ImGui.PushID(dir.path);

                // Check if this directory is expanded
                bool isExpanded = directoryCache.TryGetValue(dir.path, out DirectoryNode childNode) && childNode.IsExpanded;

                // Draw tree node
                ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;

                bool nodeOpen = ImGui.TreeNodeEx($"{dir.name}", flags);

                // Update expansion state
                if (nodeOpen != isExpanded)
                {
                    if (!directoryCache.ContainsKey(dir.path))
                        directoryCache[dir.path] = new DirectoryNode { Path = dir.path, IsLoaded = false };
                    directoryCache[dir.path].IsExpanded = nodeOpen;
                }

                if (nodeOpen)
                {
                    // Draw subdirectory contents
                    DrawDirectoryTree(dir.path, depth + 1);
                    ImGui.TreePop();
                }

                ImGui.PopID();
            }

            // Draw script files
            var scriptFiles = node.Contents.Where(f => f.type == "file" && (f.name.EndsWith(".lscript") || f.name.EndsWith(".py"))).OrderBy(f => f.name).ToList();
            foreach (GHFileObject file in scriptFiles)
            {
                ImGui.PushID(file.path);

                // Draw file as selectable
                if (ImGui.Selectable($"    {file.name}"))
                {
                    DownloadAndOpenScript(file);
                }

                // Tooltip
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip($"Click to download and open\n{file.path}");
                }

                ImGui.PopID();
            }
        }

        private void LoadDirectoryAsync(string path)
        {
            if (!directoryCache.TryGetValue(path, out DirectoryNode node))
            {
                node = new DirectoryNode { Path = path };
                directoryCache[path] = node;
            }

            if (node.IsLoading || node.IsLoaded) return;

            node.IsLoading = true;
            if (string.IsNullOrEmpty(path))
                isInitialLoading = true;

            Task.Run(async () =>
            {
                try
                {
                    List<GHFileObject> files = await cache.GetDirectoryContentsAsync(path);
                    _mainThreadActions.Enqueue(() =>
                    {
                        node.Contents = files;
                        node.IsLoaded = true;
                        node.IsLoading = false;
                        if (string.IsNullOrEmpty(path))
                        {
                            isInitialLoading = false;
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
                            isInitialLoading = false;
                            errorMessage = $"Failed to load scripts: {ex.Message}";
                        }
                    });
                }
            });
        }

        private void DownloadAndOpenScript(GHFileObject file) => Task.Run(async () =>
                                                                          {
                                                                              try
                                                                              {
                                                                                  string content = await cache.GetFileContentAsync(file.download_url);
                                                                                  _mainThreadActions.Enqueue(() =>
                                                                                  {
                                                                                      try
                                                                                      {
                                                                                          // Validate and sanitize the filename to prevent path traversal
                                                                                          string sanitizedFileName = Path.GetFileName(file.name);

                                                                                          // Reject names that contain path separators, relative navigation, or are empty
                                                                                          if (string.IsNullOrWhiteSpace(sanitizedFileName) ||
                                                                                              sanitizedFileName != file.name ||
                                                                                              sanitizedFileName.Contains("\\") ||
                                                                                              sanitizedFileName.Contains("/") ||
                                                                                              sanitizedFileName.Contains("..") ||
                                                                                              sanitizedFileName == "." ||
                                                                                              sanitizedFileName == "..")
                                                                                          {
                                                                                              GameActions.Print(World.Instance, $"Invalid script filename: {file.name}. Filename contains invalid characters or path separators.", 32);
                                                                                              Console.WriteLine($"Security: Rejected invalid filename: {file.name}");
                                                                                              return;
                                                                                          }

                                                                                          // Check for invalid filename characters
                                                                                          char[] invalidChars = Path.GetInvalidFileNameChars();
                                                                                          if (sanitizedFileName.IndexOfAny(invalidChars) >= 0)
                                                                                          {
                                                                                              GameActions.Print(World.Instance, $"Invalid script filename: {file.name}. Filename contains invalid characters.", 32);
                                                                                              Console.WriteLine($"Security: Rejected filename with invalid characters: {file.name}");
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
                                                                                              Console.WriteLine($"Security: Path traversal attempt blocked. File: {file.name}, Resolved: {fullFilePath}");
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
                                                                                          GameActions.Print(World.Instance, $"Error saving script: {file.name} - {ex.Message}");
                                                                                      }
                                                                                  });
                                                                              }
                                                                              catch (Exception ex)
                                                                              {
                                                                                  Console.WriteLine($"Error loading file: {ex.Message}");
                                                                                  _mainThreadActions.Enqueue(() =>
                                                                                  {
                                                                                      GameActions.Print(World.Instance, $"Error loading script: {file.name}");
                                                                                  });
                                                                              }
                                                                          });

        public override void Dispose()
        {
            cache?.Dispose();
            base.Dispose();
        }

        private class DirectoryNode
        {
            public string Path { get; set; }
            public List<GHFileObject> Contents { get; set; } = new();
            public bool IsLoaded { get; set; }
            public bool IsLoading { get; set; }
            public bool IsExpanded { get; set; }
        }

        public class GHFileObject
        {
            public string name { get; set; }
            public string path { get; set; }
            public string sha { get; set; }
            public int size { get; set; }
            public string url { get; set; }
            public string html_url { get; set; }
            public string git_url { get; set; }
            public string download_url { get; set; }
            public string type { get; set; }
            public _Links _links { get; set; }
        }

        public class _Links
        {
            public string self { get; set; }
            public string git { get; set; }
            public string html { get; set; }
        }
    }

    /// <summary>
    /// Caches GitHub repository content using WebClient for Mono compatibility
    /// </summary>
    internal class GitHubContentCache : IDisposable
    {
        private readonly string repository;
        private readonly string baseUrl;
        private readonly Dictionary<string, List<ScriptBrowser.GHFileObject>> directoryCache;
        private readonly Dictionary<string, string> fileContentCache;
        private readonly Dictionary<string, DateTime> cacheTimestamps;
        private readonly TimeSpan cacheExpiration = TimeSpan.FromMinutes(10);
        private DateTime lastApiCallTime = DateTime.MinValue;
        private readonly object rateLimitLock = new object();
        private const int MIN_MS_BETWEEN_REQUESTS = 1000; // 1 second between requests

        public GitHubContentCache(string repo)
        {
            repository = repo;
            baseUrl = $"https://api.github.com/repos/{repository}/contents";
            directoryCache = new Dictionary<string, List<ScriptBrowser.GHFileObject>>();
            fileContentCache = new Dictionary<string, string>();
            cacheTimestamps = new Dictionary<string, DateTime>();
        }

        /// <summary>
        /// Get directory contents, using cache if available and not expired
        /// </summary>
        public async Task<List<ScriptBrowser.GHFileObject>> GetDirectoryContentsAsync(string path = "")
        {
            string cacheKey = string.IsNullOrEmpty(path) ? "ROOT" : path;

            // Check if we have cached data that's still valid
            if (directoryCache.ContainsKey(cacheKey) &&
                cacheTimestamps.ContainsKey(cacheKey) &&
                DateTime.Now - cacheTimestamps[cacheKey] < cacheExpiration)
            {
                return directoryCache[cacheKey];
            }

            // Fetch from API
            List<ScriptBrowser.GHFileObject> contents = await FetchDirectoryFromApi(path);

            // Cache the results
            directoryCache[cacheKey] = contents;
            cacheTimestamps[cacheKey] = DateTime.Now;

            // Pre-cache subdirectories in background for faster navigation
            // Process sequentially to respect rate limiting (1 request per second)
            _ = Task.Run(async () =>
            {
                IEnumerable<ScriptBrowser.GHFileObject> directories = contents.Where(f => f.type == "dir").Take(3); // Reduced from 5 to 3 to minimize initial load time
                foreach (ScriptBrowser.GHFileObject dir in directories)
                {
                    try
                    {
                        if (!directoryCache.ContainsKey(dir.path))
                        {
                            await GetDirectoryContentsAsync(dir.path); // Rate limiting is enforced in DownloadStringAsync
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
            if (fileContentCache.ContainsKey(downloadUrl))
            {
                return fileContentCache[downloadUrl];
            }

            string content = await DownloadStringAsync(downloadUrl);
            fileContentCache[downloadUrl] = content;

            return content;
        }

        /// <summary>
        /// Fetch directory contents from GitHub API using WebClient
        /// </summary>
        private async Task<List<ScriptBrowser.GHFileObject>> FetchDirectoryFromApi(string path)
        {
            try
            {
                string url = string.IsNullOrEmpty(path) ? baseUrl : $"{baseUrl}/{path}";
                string response = await DownloadStringAsync(url);

                if (string.IsNullOrEmpty(response))
                {
                    return new List<ScriptBrowser.GHFileObject>();
                }

                List<ScriptBrowser.GHFileObject> files = JsonSerializer.Deserialize<List<ScriptBrowser.GHFileObject>>(response);
                return files ?? new List<ScriptBrowser.GHFileObject>();
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

            lock (rateLimitLock)
            {
                int timeSinceLastCall = (int)(DateTime.Now - lastApiCallTime).TotalMilliseconds;
                if (timeSinceLastCall < MIN_MS_BETWEEN_REQUESTS)
                {
                    delayNeeded = MIN_MS_BETWEEN_REQUESTS - timeSinceLastCall;
                }
                lastApiCallTime = DateTime.Now.AddMilliseconds(delayNeeded);
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
            directoryCache.Clear();
            fileContentCache.Clear();
            cacheTimestamps.Clear();
        }

        /// <summary>
        /// Clear expired cache entries
        /// </summary>
        public void ClearExpiredCache()
        {
            DateTime now = DateTime.Now;
            var expiredKeys = cacheTimestamps
                .Where(kvp => now - kvp.Value >= cacheExpiration)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (string key in expiredKeys)
            {
                directoryCache.Remove(key);
                cacheTimestamps.Remove(key);
            }
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public (int Directories, int Files, int Expired) GetCacheStats()
        {
            DateTime now = DateTime.Now;
            int expired = cacheTimestamps.Count(kvp => now - kvp.Value >= cacheExpiration);

            return (directoryCache.Count, fileContentCache.Count, expired);
        }

        public void Dispose() => ClearCache();
    }
}
