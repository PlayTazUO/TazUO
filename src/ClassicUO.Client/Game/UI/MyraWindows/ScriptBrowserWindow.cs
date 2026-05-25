using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.LegionScripting;
using ClassicUO.Utility.Platforms;
using GhFileObject = ClassicUO.LegionScripting.ScriptBrowser.GhFileObject;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

public class ScriptBrowserWindow : MyraControl
{
    private readonly ConcurrentQueue<Action> _mainThreadActions = new();

    private readonly GitHubContentCache _cache;
    private readonly Dictionary<string, DirectoryNode> _directoryCache = new();
    private bool _isInitialLoading = false;
    private string _errorMessage = "";
    private bool _rebuildPending = false;

    private readonly VerticalStackPanel _treePanel = new() { Spacing = 2 };
    private MyraLabel _statusLabel;

    // Inline preview panel (replaces ImGui popup modal)
    private VerticalStackPanel _previewPanel;
    private MyraLabel _previewTitleLabel;
    private MyraLabel _previewLoadingLabel;
    private MyraInputBox _previewContentBox;

    public ScriptBrowserWindow() : base("公共脚本浏览器")
    {
        _cache = new GitHubContentCache(ScriptBrowser.REPO);
        CanBeSaved = true;
        Build();
        CenterInViewPort();
        LoadDirectoryAsync("");
    }

    public static void Show()
    {
        foreach (IGui g in UIManager.Gumps)
        {
            if (g is ScriptBrowserWindow w)
            {
                w.BringOnTop();
                return;
            }
        }
        UIManager.Add(new ScriptBrowserWindow());
    }

    private void Build()
    {
        var grid = new MyraGrid();
        grid.AddRow(new Proportion(ProportionType.Fill));   // Row 0: tree
        grid.AddRow();                                       // Row 1: preview (auto-height)
        grid.AddColumn(new Proportion(ProportionType.Fill));

        _statusLabel = new MyraLabel("正在加载仓库内容...", MyraLabel.TextStyle.P);

        var treeContainer = new VerticalStackPanel { Spacing = 4 };
        treeContainer.Widgets.Add(_statusLabel);
        treeContainer.Widgets.Add(new ScrollViewer
        {
            Width = 600,
            Height = 400,
            Content = _treePanel
        });

        grid.AddWidget(treeContainer, 0, 0);

        // Preview panel - hidden until user clicks View on a script
        _previewPanel = new VerticalStackPanel { Spacing = 4, Visible = false };

        _previewTitleLabel = new MyraLabel("", MyraLabel.TextStyle.H2);
        _previewLoadingLabel = new MyraLabel("加载中...", MyraLabel.TextStyle.P);

        _previewContentBox = new MyraInputBox
        {
            Readonly = true,
            Multiline = true,
            Width = 600,
            Height = 300,
        };

        var previewHeader = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        previewHeader.Widgets.Add(_previewTitleLabel);
        previewHeader.Widgets.Add(new MyraButton("关闭预览", () => _previewPanel.Visible = false));

        _previewPanel.Widgets.Add(previewHeader);
        _previewPanel.Widgets.Add(_previewLoadingLabel);
        _previewPanel.Widgets.Add(_previewContentBox);

        grid.AddWidget(_previewPanel, 1, 0);

        SetRootContent(grid);
    }

    public override void PreDraw()
    {
        base.PreDraw();

        int count = 0;
        while (_mainThreadActions.TryDequeue(out Action action) && count < 10)
        {
            try { action(); }
            catch (Exception ex) { Console.WriteLine($"Error processing action: {ex.Message}"); }
            count++;
        }

        if (_rebuildPending)
        {
            _rebuildPending = false;
            RebuildTree();
        }
    }

    private void RebuildTree()
    {
        _treePanel.Widgets.Clear();

        if (_isInitialLoading)
        {
            _statusLabel.Text = "正在加载仓库内容...";
            _statusLabel.TextColor = Color.White;
            _statusLabel.Visible = true;
            return;
        }

        if (!string.IsNullOrEmpty(_errorMessage))
        {
            _statusLabel.Text = _errorMessage;
            _statusLabel.TextColor = Color.OrangeRed;
            _statusLabel.Visible = true;
            _treePanel.Widgets.Add(new MyraButton("重试", () =>
            {
                _errorMessage = "";
                _directoryCache.Clear();
                LoadDirectoryAsync("");
            }));
            return;
        }

        _statusLabel.Visible = false;
        BuildTreeRows("", 0);
    }

    private void BuildTreeRows(string path, int depth)
    {
        if (!_directoryCache.TryGetValue(path, out DirectoryNode node))
        {
            node = new DirectoryNode { Path = path };
            _directoryCache[path] = node;
        }

        if (!node.IsLoaded && !node.IsLoading)
        {
            LoadDirectoryAsync(path);
            _treePanel.Widgets.Add(new MyraLabel(Indent(depth) + "加载中...", MyraLabel.TextStyle.P));
            return;
        }

        if (node.IsLoading)
        {
            _treePanel.Widgets.Add(new MyraLabel(Indent(depth) + "加载中...", MyraLabel.TextStyle.P));
            return;
        }

        // Directories
        foreach (GhFileObject dir in node.Contents.Where(f => f.Type == "dir").OrderBy(f => f.Name))
        {
            GhFileObject d = dir;
            bool isExpanded = _directoryCache.TryGetValue(d.Path, out DirectoryNode child) && child.IsExpanded;

            var row = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
            if (depth > 0)
                row.Widgets.Add(new MyraLabel(Indent(depth), MyraLabel.TextStyle.P));

            row.Widgets.Add(new MyraButton(isExpanded ? "[-]" : "[+]", () =>
            {
                if (!_directoryCache.ContainsKey(d.Path))
                    _directoryCache[d.Path] = new DirectoryNode { Path = d.Path };
                _directoryCache[d.Path].IsExpanded = !_directoryCache[d.Path].IsExpanded;
                _rebuildPending = true;
            }));
            row.Widgets.Add(new MyraLabel(d.Name, MyraLabel.TextStyle.P));
            _treePanel.Widgets.Add(row);

            if (isExpanded)
                BuildTreeRows(d.Path, depth + 1);
        }

        // Script files
        foreach (GhFileObject file in node.Contents
            .Where(f => f.Type == "file" && (f.Name.EndsWith(".py") || f.Name.EndsWith(".cs")))
            .OrderBy(f => f.Name))
        {
            GhFileObject f = file;
            var row = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
            if (depth > 0)
                row.Widgets.Add(new MyraLabel(Indent(depth + 1), MyraLabel.TextStyle.P));

            row.Widgets.Add(new MyraLabel(f.Name, MyraLabel.TextStyle.P));
            row.Widgets.Add(new MyraButton("查看",      () => ViewScript(f)));
            row.Widgets.Add(new MyraButton("下载",  () => DownloadAndOpenScript(f)));
            row.Widgets.Add(new MyraButton("打开链接", () => PlatformHelper.LaunchBrowser(f.HtmlUrl)));
            _treePanel.Widgets.Add(row);
        }
    }

    private static string Indent(int depth) => new string(' ', depth * 3);

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
                        node.IsExpanded = true;
                    }
                    _rebuildPending = true;
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
                        _errorMessage = $"加载脚本失败: {ex.Message}";
                    }
                    _rebuildPending = true;
                });
            }
        });
    }

    private void ViewScript(GhFileObject file) => Task.Run(async () =>
    {
        _mainThreadActions.Enqueue(() =>
        {
            _previewTitleLabel.Text = file.Name;
            _previewContentBox.Text = "";
            _previewLoadingLabel.Visible = true;
            _previewContentBox.Visible = false;
            _previewPanel.Visible = true;
        });

        try
        {
            string content = await _cache.GetFileContentAsync(file.DownloadUrl);
            _mainThreadActions.Enqueue(() =>
            {
                _previewContentBox.Text = content;
                _previewLoadingLabel.Visible = false;
                _previewContentBox.Visible = true;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading file for preview: {ex.Message}");
            _mainThreadActions.Enqueue(() =>
            {
                _previewContentBox.Text = $"加载文件出错: {ex.Message}";
                _previewLoadingLabel.Visible = false;
                _previewContentBox.Visible = true;
            });
        }
    });

    private void DownloadAndOpenScript(GhFileObject file) => Task.Run(async () =>
    {
        try
        {
            string content = await _cache.GetFileContentAsync(file.DownloadUrl);
            _mainThreadActions.Enqueue(() =>
            {
                try
                {
                    string sanitizedFileName = Path.GetFileName(file.Name);

                    if (string.IsNullOrWhiteSpace(sanitizedFileName) ||
                        sanitizedFileName != file.Name ||
                        sanitizedFileName.Contains("\\") ||
                        sanitizedFileName.Contains("/") ||
                        sanitizedFileName.Contains("..") ||
                        sanitizedFileName == "." ||
                        sanitizedFileName == "..")
                    {
                        GameActions.Print(World.Instance, $"无效的脚本文件名: {file.Name}。文件名包含无效字符或路径分隔符。", 32);
                        Console.WriteLine($"Security: Rejected invalid filename: {file.Name}");
                        return;
                    }

                    char[] invalidChars = Path.GetInvalidFileNameChars();
                    if (sanitizedFileName.IndexOfAny(invalidChars) >= 0)
                    {
                        GameActions.Print(World.Instance, $"无效的脚本文件名: {file.Name}。文件名包含无效字符。", 32);
                        Console.WriteLine($"Security: Rejected filename with invalid characters: {file.Name}");
                        return;
                    }

                    if (!Directory.Exists(LegionScripting.LegionScripting.ScriptPath))
                        Directory.CreateDirectory(LegionScripting.LegionScripting.ScriptPath);

                    string filePath = Path.Combine(LegionScripting.LegionScripting.ScriptPath, sanitizedFileName);
                    string fullFilePath = Path.GetFullPath(filePath);
                    string fullScriptPath = Path.GetFullPath(LegionScripting.LegionScripting.ScriptPath);

                    if (!fullFilePath.StartsWith(fullScriptPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                        !fullFilePath.Equals(fullScriptPath, StringComparison.OrdinalIgnoreCase))
                    {
                        GameActions.Print(World.Instance, $"安全错误: 脚本路径必须在脚本目录内。", 32);
                        Console.WriteLine($"Security: Path traversal attempt blocked. File: {file.Name}, Resolved: {fullFilePath}");
                        return;
                    }

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
                            finalFilePath = Path.Combine(LegionScripting.LegionScripting.ScriptPath, finalFileName);

                            string fullFinalPath = Path.GetFullPath(finalFilePath);
                            if (!fullFinalPath.StartsWith(fullScriptPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                                !fullFinalPath.Equals(fullScriptPath, StringComparison.OrdinalIgnoreCase))
                            {
                                GameActions.Print(World.Instance, $"安全错误: 生成的路径无效。", 32);
                                return;
                            }

                            finalFilePath = fullFinalPath;
                            counter++;
                        } while (File.Exists(finalFilePath) && counter < 1000);

                        if (counter >= 1000)
                        {
                            GameActions.Print(World.Instance, $"重复文件过多，请清理您的脚本目录。", 32);
                            return;
                        }
                    }

                    File.WriteAllText(finalFilePath, content, Encoding.UTF8);

                    var f = new ScriptFile(World.Instance, LegionScripting.LegionScripting.ScriptPath, finalFileName);
                    new ScriptEditorWindow(f);

                    GameActions.Print(World.Instance, $"已下载脚本: {finalFileName}");

                    ScriptManagerWindow.Instance?.Refresh();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating script file: {ex.Message}");
                    GameActions.Print(World.Instance, $"保存脚本出错: {file.Name} - {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading file: {ex.Message}");
            _mainThreadActions.Enqueue(() =>
            {
                GameActions.Print(World.Instance, $"加载脚本出错: {file.Name}");
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
}
