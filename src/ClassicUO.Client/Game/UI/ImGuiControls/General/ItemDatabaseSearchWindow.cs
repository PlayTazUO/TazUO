using ImGuiNET;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class ItemDatabaseSearchWindow : SingletonImGuiWindow<ItemDatabaseSearchWindow>
    {

        // Search parameters
        private string _searchName = "";
        private string _searchProperties = "";
        private int _searchGraphic = 0;
        private int _searchHue = -1;
        private int _searchContainer = 0;
        private int _searchLayer = -1;
        private bool _onGroundOnly = false;
        private bool _inContainersOnly = false;
        private int _maxResults = 100;
        private bool _searchCurrentCharacterOnly = true;
        private bool _enabled;

        // Results
        private List<ItemInfo> _searchResults = new List<ItemInfo>();
        private bool _searchInProgress = false;
        private string _statusMessage = "Ready to search";

        // Cached strings for results display
        private Dictionary<uint, string> _cachedGraphicTooltips = new Dictionary<uint, string>();
        private Dictionary<uint, string> _cachedHueStrings = new Dictionary<uint, string>();
        private Dictionary<Layer, string> _cachedLayerStrings = new Dictionary<Layer, string>();
        private Dictionary<uint, string> _cachedLocationStrings = new Dictionary<uint, string>();
        private Dictionary<uint, string> _cachedContainerStrings = new Dictionary<uint, string>();
        private Dictionary<DateTime, string> _cachedTimeStrings = new Dictionary<DateTime, string>();

        // UI state
        private bool _showAdvancedSearch = false;

        // Clear old data
        private int _clearOlderThanDays = 120;
        private bool _clearInProgress = false;

        private ItemDatabaseSearchWindow() : base("Item Database Search")
        {
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;
            _enabled = ProfileManager.CurrentProfile.ItemDatabaseEnabled;
        }

        public override void DrawContent()
        {
            Profile currentProfile = ProfileManager.CurrentProfile;

            ImGui.Spacing();

            if (ImGui.Checkbox("Enable", ref _enabled))
            {
                currentProfile.ItemDatabaseEnabled = _enabled;
            }

            if (!_enabled)
            {
                ImGui.TextColored(new Vector4(1, 0.5f, 0.5f, 1), "Item Database is disabled.");
                return;
            }

            ImGui.Spacing();

            // Basic search fields
            DrawBasicSearchFields();

            ImGui.Spacing();

            // Advanced search toggle
            if (ImGui.Checkbox("Advanced Search", ref _showAdvancedSearch))
            {
                // Reset advanced fields when toggling
                if (!_showAdvancedSearch)
                {
                    _searchContainer = 0;
                    _onGroundOnly = false;
                    _inContainersOnly = false;
                }
            }

            if (_showAdvancedSearch)
            {
                DrawAdvancedSearchFields();
            }

            ImGui.Spacing();

            // Use a table to place search controls and database maintenance side by side
            if (ImGui.BeginTable("ActionsTable", 2, ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn("SearchControls", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("DatabaseMaintenance", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextRow();

                // Search controls column
                ImGui.TableSetColumnIndex(0);
                DrawSearchControls();

                // Database maintenance column
                ImGui.TableSetColumnIndex(1);
                DrawDatabaseMaintenance();

                ImGui.EndTable();
            }

            ImGui.Spacing();

            // Results
            DrawSearchResults();

            ImGui.SeparatorText("Status:");
            ImGui.Text(_statusMessage);
        }

        private void DrawBasicSearchFields()
        {
            ImGui.SeparatorText("Options:");

            // Use a table for consistent two-column layout
            if (ImGui.BeginTable("SearchFieldsTable", 2, ImGuiTableFlags.None))
            {
                ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 90);
                ImGui.TableSetupColumn("Input", ImGuiTableColumnFlags.WidthStretch);

                // Name search
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Name");
                ImGuiComponents.Tooltip("Search for items containing this text in their name");

                ImGui.TableSetColumnIndex(1);
                ImGui.SetNextItemWidth(300);
                ImGui.InputText("##SearchName", ref _searchName, 100);

                // Properties search
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Properties");
                ImGuiComponents.Tooltip("Search for items containing this text in their properties/tooltip");

                ImGui.TableSetColumnIndex(1);
                ImGui.SetNextItemWidth(300);
                ImGui.InputText("##SearchProperties", ref _searchProperties, 200);

                // Graphic ID and Hue on same row
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Graphic ID");
                ImGuiComponents.Tooltip("Search for items with this graphic ID (0 = any)");

                ImGui.TableSetColumnIndex(1);
                ImGui.SetNextItemWidth(120);
                ImGui.InputInt("##SearchGraphic", ref _searchGraphic);
                ImGui.SameLine();
                ImGui.Text("Hue:");
                ImGuiComponents.Tooltip("Search for items with this hue (-1 = any)");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(110);
                ImGui.InputInt("##SearchHue", ref _searchHue);

                // Layer
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Layer");
                ImGuiComponents.Tooltip("Search for items on this layer (-1 = any, 0 = Invalid/Ground)");

                ImGui.TableSetColumnIndex(1);
                ImGui.SetNextItemWidth(100);
                ImGui.InputInt("##SearchLayer", ref _searchLayer);

                ImGui.EndTable();
            }
        }

        private void DrawAdvancedSearchFields()
        {
            ImGui.Spacing();
            ImGui.SeparatorText("Advanced Options:");

            // Container filter
            ImGui.Text("Container Serial:");
            ImGui.SameLine();
            ImGui.InputInt("##SearchContainer", ref _searchContainer);
            ImGuiComponents.Tooltip("Search only in this container (0 = any)");

            // Location filters
            ImGui.Checkbox("On ground only", ref _onGroundOnly);
            ImGui.SameLine();
            ImGui.Checkbox("In containers only", ref _inContainersOnly);

            // Character filter
            ImGui.Checkbox("Current character only", ref _searchCurrentCharacterOnly);

            // Max results
            ImGui.Text("Max Results:");
            ImGui.SameLine();
            ImGui.SliderInt("##MaxResults", ref _maxResults, 10, 1000);
        }

        private void DrawSearchControls()
        {
            ImGui.SeparatorText("Actions:");
            ImGui.AlignTextToFramePadding();
            if (_searchInProgress)
            {
                ImGui.Text("Searching...");
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    _searchInProgress = false;
                    _statusMessage = "Search canceled";
                }
            }
            else
            {
                if (ImGui.Button("Search"))
                {
                    PerformSearch();
                }

                ImGui.SameLine();

                if (ImGui.Button("Clear"))
                {
                    ClearSearch();
                }
                ImGui.SameLine();
                if (ImGui.Button("Clear Results"))
                {
                    _searchResults.Clear();
                    _statusMessage = "Results cleared";
                }
            }
        }

        private void DrawSearchResults()
        {
            if (_searchResults.Count == 0)
            {
                ImGui.Text("No results to display");
                return;
            }

            ImGui.Text($"Results ({_searchResults.Count}):");
            ImGui.Separator();

            // Table headers
            if (ImGui.BeginTable("SearchResults", 9, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0, 300)))
            {
                ImGui.TableSetupColumn("Graphic", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 150);
                ImGui.TableSetupColumn("Hue", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn("Layer", ImGuiTableColumnFlags.WidthFixed, 70);
                ImGui.TableSetupColumn("Location", ImGuiTableColumnFlags.WidthFixed, 100);
                ImGui.TableSetupColumn("Container", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("Character", ImGuiTableColumnFlags.WidthFixed, 100);
                ImGui.TableSetupColumn("Updated", ImGuiTableColumnFlags.WidthFixed, 120);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableHeadersRow();

                for (int i = 0; i < _searchResults.Count; i++)
                {
                    ItemInfo item = _searchResults[i];
                    ImGui.TableNextRow();

                    // Graphic column with image
                    ImGui.TableSetColumnIndex(0);
                    if (item.Graphic > 0)
                    {
                        DrawArt(item.Graphic, new Vector2(32, 32));
                        if (ImGui.IsItemHovered() && _cachedGraphicTooltips.TryGetValue(item.Graphic, out string tooltip))
                            ImGui.SetTooltip(tooltip);
                    }
                    else
                    {
                        ImGui.Text($"{item.Graphic}");
                    }

                    // Name
                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text(item.Name);
                    if (ImGui.IsItemHovered() && !string.IsNullOrEmpty(item.Properties))
                        ImGui.SetTooltip(item.Properties.Replace("|", "\n"));

                    // Hue
                    ImGui.TableSetColumnIndex(2);
                    if (_cachedHueStrings.TryGetValue(item.Hue, out string hueStr))
                        ImGui.Text(hueStr);

                    // Layer
                    ImGui.TableSetColumnIndex(3);
                    if (_cachedLayerStrings.TryGetValue(item.Layer, out string layerStr))
                    {
                        ImGui.Text(layerStr);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip($"Layer: {item.Layer}");
                    }

                    // Location
                    ImGui.TableSetColumnIndex(4);
                    if (_cachedLocationStrings.TryGetValue(item.Serial, out string locationStr))
                        ImGui.Text(locationStr);

                    // Container
                    ImGui.TableSetColumnIndex(5);
                    if (_cachedContainerStrings.TryGetValue(item.Container, out string containerStr))
                        ImGui.Text(containerStr);

                    // Character
                    ImGui.TableSetColumnIndex(6);
                    ImGui.Text(item.CharacterName);

                    // Updated time
                    ImGui.TableSetColumnIndex(7);
                    if (_cachedTimeStrings.TryGetValue(item.UpdatedTime, out string timeStr))
                        ImGui.Text(timeStr);

                    // Actions column
                    ImGui.TableSetColumnIndex(8);
                    if (ImGui.SmallButton($"Details##{i}"))
                    {
                        OpenItemDetailWindow(item);
                    }
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("View detailed information about this item");
                }

                ImGui.EndTable();
            }
        }

        private void PerformSearch()
        {
            if (_searchInProgress)
                return;

            _searchInProgress = true;
            _statusMessage = "Searching...";
            _searchResults.Clear();

            // Build search parameters
            uint? serial = null;
            ushort? graphic = _searchGraphic > 0 ? (ushort)_searchGraphic : null;
            ushort? hue = _searchHue >= 0 ? (ushort)_searchHue : null;
            string name = string.IsNullOrWhiteSpace(_searchName) ? null : _searchName.Trim();
            string properties = string.IsNullOrWhiteSpace(_searchProperties) ? null : _searchProperties.Trim();
            uint? container = _searchContainer > 0 ? (uint)_searchContainer : null;
            Layer? layer = _searchLayer >= 0 ? (Layer)_searchLayer : null;
            uint? character = null;
            bool? onGround = null;

            // Apply filters
            if (_searchCurrentCharacterOnly && Client.Game.UO?.World?.Player != null)
            {
                character = Client.Game.UO.World.Player.Serial;
            }

            if (_onGroundOnly && !_inContainersOnly)
            {
                onGround = true;
            }
            else if (_inContainersOnly && !_onGroundOnly)
            {
                onGround = false;
            }

            // Perform search
            ItemDatabaseManager.Instance.SearchItems(
                results =>
                {
                    MainThreadQueue.EnqueueAction(() => {
                        _searchResults = results ?? new List<ItemInfo>();
                        _searchInProgress = false;

                        // Cache all formatted strings for the results
                        CacheResultStrings();

                        if (_searchResults.Count == 0)
                        {
                            _statusMessage = "No items found";
                        }
                        else if (_searchResults.Count >= _maxResults)
                        {
                            _statusMessage = $"Found {_searchResults.Count} items (max limit reached)";
                        }
                        else
                        {
                            _statusMessage = $"Found {_searchResults.Count} items";
                        }
                    });
                },
                serial: serial,
                graphic: graphic,
                hue: hue,
                name: name,
                properties: properties,
                container: container,
                layer: layer,
                character: character,
                onGround: onGround,
                limit: _maxResults
            );
        }

        private void CacheResultStrings()
        {
            // Clear previous caches
            _cachedGraphicTooltips.Clear();
            _cachedHueStrings.Clear();
            _cachedLayerStrings.Clear();
            _cachedLocationStrings.Clear();
            _cachedContainerStrings.Clear();
            _cachedTimeStrings.Clear();

            foreach (ItemInfo item in _searchResults)
            {
                // Cache graphic tooltip
                if (item.Graphic > 0 && !_cachedGraphicTooltips.ContainsKey(item.Graphic))
                {
                    _cachedGraphicTooltips[item.Graphic] = $"Graphic: {item.Graphic} (0x{item.Graphic:X})";
                }

                // Cache hue string
                if (!_cachedHueStrings.ContainsKey(item.Hue))
                {
                    _cachedHueStrings[item.Hue] = $"{item.Hue}";
                }

                // Cache layer string
                if (!_cachedLayerStrings.ContainsKey(item.Layer))
                {
                    _cachedLayerStrings[item.Layer] = $"{item.Layer} ({(int)item.Layer})";
                }

                // Cache location string - use serial as key since location is per-item
                if (item.OnGround)
                {
                    _cachedLocationStrings[item.Serial] = $"{item.X}, {item.Y}";
                }
                else
                {
                    _cachedLocationStrings[item.Serial] = "Container";
                }

                // Cache container string
                if (!_cachedContainerStrings.ContainsKey(item.Container))
                {
                    if (item.Container != 0 && item.Container != 0xFFFFFFFF)
                        _cachedContainerStrings[item.Container] = $"0x{item.Container:X}";
                    else
                        _cachedContainerStrings[item.Container] = "Ground";
                }

                // Cache time string - use DateTime as key
                if (!_cachedTimeStrings.ContainsKey(item.UpdatedTime))
                {
                    TimeSpan timeAgo = DateTime.Now - item.UpdatedTime;
                    string timeStr;
                    if (timeAgo.TotalDays >= 1)
                        timeStr = $"{timeAgo.Days}d ago";
                    else if (timeAgo.TotalHours >= 1)
                        timeStr = $"{timeAgo.Hours}h ago";
                    else if (timeAgo.TotalMinutes >= 1)
                        timeStr = $"{(int)timeAgo.TotalMinutes}m ago";
                    else
                        timeStr = "Just now";

                    _cachedTimeStrings[item.UpdatedTime] = timeStr;
                }
            }
        }

        private void ClearSearch()
        {
            _searchName = "";
            _searchProperties = "";
            _searchGraphic = 0;
            _searchHue = -1;
            _searchContainer = 0;
            _searchLayer = -1;
            _onGroundOnly = false;
            _inContainersOnly = false;
            _searchCurrentCharacterOnly = true;
            _maxResults = 100;
            _statusMessage = "Search cleared";
        }

        private void DrawDatabaseMaintenance()
        {
            ImGui.SeparatorText("Database Maintenance:");
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Clear entries older than:");
            ImGui.SetNextItemWidth(100);
            ImGui.InputInt("##ClearOlderThanDays", ref _clearOlderThanDays);
            ImGui.SameLine();
            ImGui.Text("days");
            ImGuiComponents.Tooltip("Delete all database entries older than this many days");

            // Ensure days is at least 1
            if (_clearOlderThanDays < 1)
                _clearOlderThanDays = 1;

            if (_clearInProgress)
            {
                ImGui.Text("Clearing...");
            }
            else
            {
                if (ImGui.Button("Clear Old Entries"))
                {
                    ClearOldEntries();
                }
            }
            ImGui.Spacing();
        }

        private async void ClearOldEntries()
        {
            if (_clearInProgress)
                return;

            _clearInProgress = true;
            _statusMessage = $"Clearing entries older than {_clearOlderThanDays} days...";

            try
            {
                await ItemDatabaseManager.Instance.ClearOldDataAsync(TimeSpan.FromDays(_clearOlderThanDays));
                _statusMessage = $"Successfully cleared entries older than {_clearOlderThanDays} days";
            }
            catch (Exception ex)
            {
                _statusMessage = $"Error clearing old entries: {ex.Message}";
            }
            finally
            {
                _clearInProgress = false;
            }
        }

        private void OpenItemDetailWindow(ItemInfo item)
        {
            if (item == null)
                return;

            if (ItemDetailWindow.OpenedWindows.Contains(item))
                return;

            var detailWindow = new ItemDetailWindow(item);
            ImGuiManager.AddWindow(detailWindow);
        }
    }
}
