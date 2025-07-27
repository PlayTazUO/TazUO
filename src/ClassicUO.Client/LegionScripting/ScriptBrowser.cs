using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;

namespace ClassicUO.LegionScripting
{
    internal class ScriptBrowser : Gump
    {
        private static ConcurrentQueue<Action> _mainThreadActions = new();
        private const int WIDTH = 400;
        private const int HEIGHT = 600;
        private const string REPO = "PlayTazUO/PublicLegionScripts";

        private ScrollArea scrollArea;
        private readonly GitHubContentCache cache;
        private string currentPath = "";
        private readonly Stack<string> navigationHistory = new Stack<string>();
        private bool isLoading = false;
        private TextBox loadingText;

        public ScriptBrowser() : base(0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;

            Width = WIDTH;
            Height = HEIGHT;

            cache = new GitHubContentCache(REPO);

            Add(new AlphaBlendControl() { Width = Width, Height = Height });
            Add(scrollArea = new ScrollArea(0, 0, Width, Height, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways });

            // Add loading indicator
            loadingText = GenTextBox("Loading...", 16);
            loadingText.X = (Width - loadingText.MeasuredSize.X) / 2;
            loadingText.Y = (Height - loadingText.MeasuredSize.Y) / 2;
            scrollArea.Add(loadingText);

            CenterXInViewPort();
            CenterYInViewPort();

            // Start loading after UI is set up
            Task.Run(async () =>
            {
                try
                {
                    await LoadCurrentDirectoryAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading initial directory: {ex.Message}");
                    _mainThreadActions.Enqueue(() => {
                        ShowError($"Failed to load scripts: {ex.Message}");
                        isLoading = false;
                    });
                }
            });
        }

        private async Task LoadCurrentDirectoryAsync()
        {
            if (isLoading) return;
            isLoading = true;

            try
            {
                var files = await cache.GetDirectoryContentsAsync(currentPath);
                _mainThreadActions.Enqueue(() => {
                    SetFiles(files);
                    isLoading = false;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading directory: {ex.Message}");
                _mainThreadActions.Enqueue(() => {
                    ShowError($"Failed to load directory: {ex.Message}");
                    isLoading = false;
                });
            }
        }

        private void ShowError(string message)
        {
            ClearScrollArea();
            var errorText = GenTextBox(message, 14);
            errorText.X = (Width - errorText.MeasuredSize.X) / 2;
            errorText.Y = (Height - errorText.MeasuredSize.Y) / 2;
            scrollArea.Add(errorText);
        }

        public override void Update()
        {
            base.Update();

            // Process main thread actions
            int processedCount = 0;
            while(_mainThreadActions.TryDequeue(out var action) && processedCount < 10) // Limit to prevent frame drops
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

        private void ClearScrollArea()
        {
            // Remove all children except the first one (which should be the loading text initially)
            var childrenToRemove = scrollArea.Children.Skip(1).ToList();
            foreach (var child in childrenToRemove)
            {
                child.Dispose();
            }
        }

        public void SetFiles(List<GHFileObject> files)
        {
            ClearScrollArea();

            // Add back button if not at root
            if (!string.IsNullOrEmpty(currentPath))
            {
                var parentPath = Path.GetDirectoryName(currentPath)?.Replace('\\', '/') ?? "";
                var backItem = new GHFileObject()
                {
                    type = "dir",
                    name = $"<- Back{(string.IsNullOrEmpty(parentPath) ? "" : $" ({parentPath})")}",
                    path = parentPath
                };
                scrollArea.Add(new ItemControl(backItem, this, true));
            }

            // Add directories first, then files
            var directories = files.Where(f => f.type == "dir").OrderBy(f => f.name);
            var scriptFiles = files.Where(f => f.type == "file" && f.name.EndsWith(".lscript")).OrderBy(f => f.name);

            foreach (var dir in directories)
            {
                scrollArea.Add(new ItemControl(dir, this));
            }

            foreach (var file in scriptFiles)
            {
                scrollArea.Add(new ItemControl(file, this));
            }

            // Layout items
            int y = 0;
            foreach (Control c in scrollArea.Children)
            {
                if (c is not ItemControl)
                    continue;

                c.Y = y;
                y += c.Height + 3;
            }
        }

        public void NavigateToDirectory(string path, bool isBackNavigation = false)
        {
            if (isLoading) return;

            // Add current path to history if not going back and not already the same
            if (!isBackNavigation && currentPath != path)
            {
                navigationHistory.Push(currentPath);
            }

            currentPath = path ?? ""; // Ensure path is never null

            // Show loading state
            ClearScrollArea();
            loadingText = GenTextBox("Loading...", 16);
            loadingText.X = (Width - loadingText.MeasuredSize.X) / 2;
            loadingText.Y = (Height - loadingText.MeasuredSize.Y) / 2;
            scrollArea.Add(loadingText);

            // Load directory asynchronously
            Task.Run(async () =>
            {
                try
                {
                    await LoadCurrentDirectoryAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error navigating to directory: {ex.Message}");
                    _mainThreadActions.Enqueue(() => {
                        ShowError($"Failed to load directory: {ex.Message}");
                        isLoading = false;
                    });
                }
            });
        }

        public void GoBack()
        {
            if (navigationHistory.Count > 0)
            {
                var previousPath = navigationHistory.Pop();
                NavigateToDirectory(previousPath, true); // Pass true to indicate this is back navigation
            }
        }

        private TextBox GenTextBox(string text, int fontsize, int x = 0, int y = 0)
        {
            TextBox tb = TextBox.GetOne(text, TrueTypeLoader.EMBEDDED_FONT, fontsize, Microsoft.Xna.Framework.Color.White, TextBox.RTLOptions.Default());
            tb.X = x;
            tb.Y = y;
            tb.AcceptMouseInput = false;
            return tb;
        }

        public override void Dispose()
        {
            cache?.Dispose();
            base.Dispose();
        }

        internal class ItemControl : Control
        {
            private readonly bool isBackButton;

            public ItemControl(GHFileObject gHFileObject, ScriptBrowser scriptBrowser, bool isBackButton = false)
            {
                Width = WIDTH - 18;
                Height = 50;
                this.isBackButton = isBackButton;

                Add(new AlphaBlendControl() { Width = Width, Height = Height });

                GHFileObject = gHFileObject;
                ScriptBrowser = scriptBrowser;

                if (gHFileObject.type == "dir")
                {
                    var typeText = isBackButton ? "Back" : "Directory";
                    Add(GenTextBox(typeText, 14, 5, 5));
                    MouseDown += DirectoryMouseDown;
                }
                else if (gHFileObject.type == "file" && gHFileObject.name.EndsWith(".lscript"))
                {
                    Add(GenTextBox("Script", 14, 5, 5));
                    MouseDown += FileMouseDown;
                }

                var tb = GenTextBox(gHFileObject.name, 20);
                tb.X = Width - tb.MeasuredSize.X - 5;
                tb.Y = (Height - tb.MeasuredSize.Y) / 2;
                Add(tb);
            }

            private void FileMouseDown(object sender, MouseEventArgs e)
            {
                // Run file loading asynchronously to prevent UI freezing
                Task.Run(async () =>
                {
                    try
                    {
                        var content = await ScriptBrowser.cache.GetFileContentAsync(GHFileObject.download_url);
                        _mainThreadActions.Enqueue(() =>
                        {
                            try
                            {
                                // Ensure the script directory exists
                                if (!Directory.Exists(LegionScripting.ScriptPath))
                                {
                                    Directory.CreateDirectory(LegionScripting.ScriptPath);
                                }

                                // Create the full file path
                                var filePath = Path.Combine(LegionScripting.ScriptPath, GHFileObject.name);

                                // Write the content to disk
                                File.WriteAllText(filePath, content, Encoding.UTF8);

                                // Create ScriptFile object pointing to the saved file
                                ScriptFile f = new ScriptFile(LegionScripting.ScriptPath, GHFileObject.name);
                                UIManager.Add(new ScriptEditor(f));

                                GameActions.Print($"Downloaded script: {GHFileObject.name}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error creating script file: {ex.Message}");
                                GameActions.Print($"Error saving script: {GHFileObject.name} - {ex.Message}");
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading file: {ex.Message}");
                        _mainThreadActions.Enqueue(() =>
                        {
                            GameActions.Print($"Error loading script: {GHFileObject.name}");
                        });
                    }
                });
            }

            private void DirectoryMouseDown(object sender, MouseEventArgs e)
            {
                if (isBackButton)
                {
                    ScriptBrowser.GoBack();
                }
                else
                {
                    ScriptBrowser.NavigateToDirectory(GHFileObject.path);
                }
            }

            private TextBox GenTextBox(string text, int fontsize, int x = 0, int y = 0)
            {
                TextBox tb = TextBox.GetOne(text, TrueTypeLoader.EMBEDDED_FONT, fontsize, Microsoft.Xna.Framework.Color.White, TextBox.RTLOptions.Default());
                tb.X = x;
                tb.Y = y;
                tb.AcceptMouseInput = false;
                return tb;
            }

            public GHFileObject GHFileObject { get; }
            public ScriptBrowser ScriptBrowser { get; }
        }

        internal class GHFileObject
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

        internal class _Links
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
            var cacheKey = string.IsNullOrEmpty(path) ? "ROOT" : path;

            // Check if we have cached data that's still valid
            if (directoryCache.ContainsKey(cacheKey) &&
                cacheTimestamps.ContainsKey(cacheKey) &&
                DateTime.Now - cacheTimestamps[cacheKey] < cacheExpiration)
            {
                return directoryCache[cacheKey];
            }

            // Fetch from API
            var contents = await FetchDirectoryFromApi(path);

            // Cache the results
            directoryCache[cacheKey] = contents;
            cacheTimestamps[cacheKey] = DateTime.Now;

            // Pre-cache subdirectories in background for faster navigation
            _ = Task.Run(async () =>
            {
                var directories = contents.Where(f => f.type == "dir").Take(5);
                foreach (var dir in directories)
                {
                    try
                    {
                        if (!directoryCache.ContainsKey(dir.path))
                        {
                            await GetDirectoryContentsAsync(dir.path);
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

            var content = await DownloadStringAsync(downloadUrl);
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
                var url = string.IsNullOrEmpty(path) ? baseUrl : $"{baseUrl}/{path}";
                var response = await DownloadStringAsync(url);

                if (string.IsNullOrEmpty(response))
                {
                    return new List<ScriptBrowser.GHFileObject>();
                }

                var files = JsonSerializer.Deserialize<List<ScriptBrowser.GHFileObject>>(response);
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
        /// Download string content using WebClient with proper async handling and timeout
        /// </summary>
        private Task<string> DownloadStringAsync(string url)
        {
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

            return tcs.Task;
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
            var now = DateTime.Now;
            var expiredKeys = cacheTimestamps
                .Where(kvp => now - kvp.Value >= cacheExpiration)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
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
            var now = DateTime.Now;
            var expired = cacheTimestamps.Count(kvp => now - kvp.Value >= cacheExpiration);

            return (directoryCache.Count, fileContentCache.Count, expired);
        }

        public void Dispose()
        {
            ClearCache();
        }
    }
}
