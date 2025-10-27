using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClassicUO.Assets;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    public class FileSelector : Gump
    {
        private const int GUMP_WIDTH = 400;
        private const int GUMP_HEIGHT = 500;
        private const int LIST_HEIGHT = 340;

        private ScrollArea _scrollArea;
        private VBoxContainer _scrollVBox;
        private TextBox _pathTextBox;
        private TextBox _fileNameTextBox;
        private TextBox _filterTextBox;
        private string _currentPath;
        private static string _lastPath;
        private string _selectedFile;
        private string[] _fileExtensions;
        private Action<string> _onFileSelected;
        private Label _statusLabel;
        private string _title;
        private FileSelectorType _type;

        public FileSelector(World world, FileSelectorType type, string initialPath = null, string[] fileExtensions = null, Action<string> onFileSelected = null, string title = "File Browser")
            : base(world, 0, 0)
        {
            _type = type;
            _title = title;

            if (!string.IsNullOrEmpty(initialPath))
                _currentPath = initialPath;
            else if (!string.IsNullOrEmpty(_lastPath))
                _currentPath = _lastPath;
            else
                _currentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            _currentPath = new DirectoryInfo(_currentPath).FullName;
            _currentPath = _currentPath.TrimEnd('/', '\\');
            _fileExtensions = fileExtensions;
            _onFileSelected = onFileSelected;
            _selectedFile = string.Empty;

            Width = GUMP_WIDTH;
            Height = GUMP_HEIGHT;

            CanMove = true;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;

            BuildGump();
            RefreshFileList();

            CenterXInViewPort();
            CenterYInViewPort();
        }

        private void BuildGump()
        {
            // Background
            Add(new AlphaBlendControl(0.8f)
            {
                Width = GUMP_WIDTH,
                Height = GUMP_HEIGHT,
                Hue = 0x0386
            });

            // Title bar
            Add(new Label(_title, true, 32)
            {
                X = 20,
                Y = 8
            });

            // Close button
            Add(new NiceButton(GUMP_WIDTH - 80, 5, 75, 20, ButtonAction.Activate, "Close")
            {
                ButtonParameter = 0
            });

            // Current path display
            Add(new Label("Current Path:", true, 0x0386, font: 1)
            {
                X = 20,
                Y = 35
            });

            _pathTextBox = TextBox.GetOne(_currentPath, TrueTypeLoader.EMBEDDED_FONT, 16, Color.LightGray, TextBox.RTLOptions.Default(GUMP_WIDTH - 120));
            _pathTextBox.X = 20;
            _pathTextBox.Y = 55;
            Add(_pathTextBox);

            // File extension filter
            Add(new Label("Filter:", true, 0x0386, font: 1)
            {
                X = 20,
                Y = 85
            });

            _filterTextBox = TextBox.GetOne(_fileExtensions != null ? string.Join(", ", _fileExtensions) : string.Empty, TrueTypeLoader.EMBEDDED_FONT, 16, Color.LightGray, TextBox.RTLOptions.Default(150));
            _filterTextBox.X = 80;
            _filterTextBox.Y = 85;

            Add(_filterTextBox);

            // File list scroll area
            _scrollArea = new ScrollArea(20, 115, GUMP_WIDTH - 40, LIST_HEIGHT, true);
            Add(_scrollArea);
            _scrollVBox = new VBoxContainer(_scrollArea.Width - _scrollArea.ScrollBarWidth());
            _scrollArea.Add(_scrollVBox);

            // Selected file name
            Control c;
            Add(c = new Label("File Name:", true, 0x0386, font: 1)
            {
                X = 20,
                Y = GUMP_HEIGHT - 40
            });

            _fileNameTextBox = TextBox.GetOne(string.Empty, TrueTypeLoader.EMBEDDED_FONT, 16, Color.OrangeRed, TextBox.RTLOptions.Default(Width - c.Width - c.X - 5));
            _fileNameTextBox.X = c.Width + c.X + 5;
            _fileNameTextBox.Y = GUMP_HEIGHT - 40;
            Add(_fileNameTextBox);

            // OK button
            Add(new NiceButton(GUMP_WIDTH - 180, 477, 75, 20, ButtonAction.Activate, "OK")
            {
                ButtonParameter = 3
            });

            // Cancel button
            Add(new NiceButton(GUMP_WIDTH - 90, 477, 75, 20, ButtonAction.Activate, "Cancel")
            {
                ButtonParameter = 4
            });

            // Status label
            _statusLabel = new Label("Ready", true, 0x0386, font: 1)
            {
                X = 20,
                Y = GUMP_HEIGHT - 20
            };
            Add(_statusLabel);
        }

        private void RefreshFileList()
        {
            const int BUTTON_WIDTH = GUMP_WIDTH - 60;
            _scrollVBox.Clear();

            try
            {
                string dirc = _currentPath;
                var dirButtonUp = new NiceButton(0, 0, BUTTON_WIDTH, 20, ButtonAction.Default, $"(Current Dir)", align: TEXT_ALIGN_TYPE.TS_LEFT, hue:693);
                if (_type == FileSelectorType.Directory)
                    dirButtonUp.MouseUp += (sender, e) => SelectFile(dirc);
                _scrollVBox.Add(dirButtonUp);

                DirectoryInfo parent = Directory.GetParent(_currentPath);
                if(parent != null)
                {
                    string dirp = parent.FullName;
                    dirButtonUp = new NiceButton(0, 0, BUTTON_WIDTH, 20, ButtonAction.Default, $"(Parent Dir)",
                        align: TEXT_ALIGN_TYPE.TS_LEFT, hue: 693);
                    if (_type == FileSelectorType.Directory)
                        dirButtonUp.MouseUp += (sender, e) => SelectFile(dirp);
                    dirButtonUp.MouseDoubleClick += (sender, e) => NavigateToDirectory(dirp);
                    _scrollVBox.Add(dirButtonUp);
                }
                else
                {
                    // We're at root, show available drives
                    try
                    {
                        DriveInfo[] drives = DriveInfo.GetDrives();
                        foreach (DriveInfo drive in drives.Where(d => d.IsReady))
                        {
                            string driveName = $"{drive.Name} ({drive.DriveType})";
                            var driveButton = new NiceButton(0, 0, BUTTON_WIDTH, 20, ButtonAction.Default, driveName,
                                align: TEXT_ALIGN_TYPE.TS_LEFT, hue: 692);
                            
                            if (_type == FileSelectorType.Directory)
                                driveButton.MouseUp += (sender, e) => SelectFile(drive.RootDirectory.FullName);
                            driveButton.MouseDoubleClick += (sender, e) => NavigateToDirectory(drive.RootDirectory.FullName);
                            _scrollVBox.Add(driveButton);
                        }
                    }
                    catch (Exception ex)
                    {
                        _statusLabel.Text = $"Error loading drives: {ex.Message}";
                    }
                }

                if (!Directory.Exists(_currentPath))
                {
                    _statusLabel.Text = "Invalid directory path";
                    return;
                }

                _pathTextBox.Text = _currentPath;

                int itemHeight = 20;

                // Add directories first
                string[] directories = Directory.EnumerateDirectories(_currentPath)
                                           .Where(dir => !Path.GetFileName(dir).StartsWith("."))
                                           .ToArray();
                foreach (string dir in directories.OrderBy(d => Path.GetFileName(d)))
                {
                    string dirName = Path.GetFileName(dir);
                    var dirButton = new NiceButton(0, 0, BUTTON_WIDTH, 20, ButtonAction.Default, $"/{dirName}/", align: TEXT_ALIGN_TYPE.TS_LEFT, hue:691);

                    if (_type == FileSelectorType.Directory)
                        dirButton.MouseUp += (sender, e) => SelectFile(dir);

                    dirButton.MouseDoubleClick += (sender, e) => NavigateToDirectory(dir);
                    _scrollVBox.Add(dirButton);
                }

                // Add files
                string[] files = GetFilteredFiles(_currentPath);
                foreach (string file in files.OrderBy(f => Path.GetFileName(f)))
                {
                    string fileName = "/" + Path.GetFileName(file);
                    var fileButton = new NiceButton(0, 0, BUTTON_WIDTH, itemHeight, ButtonAction.Default, fileName, align: TEXT_ALIGN_TYPE.TS_LEFT, hue: 68);
                    if(_type == FileSelectorType.File)
                        fileButton.MouseUp += (sender, e) =>SelectFile(file);

                    _scrollVBox.Add(fileButton);
                }

                _statusLabel.Text = $"Found {directories.Length} directories and {files.Length} files";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Error: {ex.Message}";
            }
        }

        private string[] GetFilteredFiles(string path)
        {
            if (_fileExtensions == null || _fileExtensions.Length == 0)
                return [];

            var files = new List<string>();
            foreach (string extension in _fileExtensions)
            {
                string pattern = extension.StartsWith("*.") ? extension : $"*.{extension.TrimStart('.')}";
                files.AddRange(Directory.GetFiles(path, pattern));
            }

            return files.Distinct().ToArray();
        }

        private void NavigateToDirectory(string path)
        {
            _currentPath = _lastPath = path;
            RefreshFileList();
        }

        private void SelectFile(string filePath)
        {
            _selectedFile = filePath;
            _fileNameTextBox.Text = Path.GetFileName(filePath);
        }

        private void ConfirmSelection()
        {
            if (string.IsNullOrEmpty(_selectedFile))
            {
                return;
            }

            _onFileSelected?.Invoke(_selectedFile);
            Dispose();
        }

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 0: // Close
                    Dispose();
                    break;
                case 1: // Up directory
                    DirectoryInfo parent = Directory.GetParent(_currentPath);
                    if (parent != null)
                    {
                        _currentPath = parent.FullName;
                        RefreshFileList();
                    }
                    break;
                case 2: // Refresh
                    UpdateFileExtensionsFromFilter();
                    RefreshFileList();
                    break;
                case 3: // OK
                    ConfirmSelection();
                    break;
                case 4: // Cancel
                    Dispose();
                    break;
            }
        }

        private void UpdateFileExtensionsFromFilter()
        {
            if (!string.IsNullOrEmpty(_filterTextBox.Text))
            {
                string filterText = _filterTextBox.Text.Replace(" ", "");
                if (string.IsNullOrEmpty(filterText) || filterText == "*.*" || filterText == "*")
                {
                    _fileExtensions = null;
                }
                else
                {
                    _fileExtensions = filterText.Split(',')
                        .Where(f => !string.IsNullOrEmpty(f))
                        .ToArray();
                }
            }
        }

        private Vector3 borderVec = new Vector3(1, 0, 1);
        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (!base.Draw(batcher, x, y))
                return false;

            batcher.DrawRectangle(
                SolidColorTextureCache.GetTexture(Color.OrangeRed),
                x - 1, y - 1,
                Width + 2, Height + 2,
                borderVec
            );

            return true;
        }

        // Static helper method to create and show the file browser
        public static void ShowFileBrowser(World world, FileSelectorType type, string initialPath = null, string[] fileExtensions = null, Action<string> onFileSelected = null, string title = "File Browser")
        {
            var gump = new FileSelector(world, type, initialPath, fileExtensions, onFileSelected, title);
            UIManager.Add(gump);
        }
    }

    public enum FileSelectorType
    {
        File,
        Directory
    }
}
