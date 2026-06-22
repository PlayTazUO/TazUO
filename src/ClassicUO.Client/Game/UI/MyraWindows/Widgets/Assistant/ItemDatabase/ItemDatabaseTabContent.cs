#nullable enable
using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.ItemDatabase;

public static class ItemDatabaseTabContent
{
    public static Widget Build()
    {
        Profile? profile = ProfileManager.CurrentProfile;
        if (profile == null)
            return new MyraLabel(TazLang.Get("item_database_tabs_profile_not_loaded", "Profile not loaded"), MyraLabel.TextStyle.P);

        var root = new VerticalStackPanel { Spacing = 6 };

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.ItemDatabaseEnabled,
            b => profile.ItemDatabaseEnabled = b,
            TazLang.Get("item_database_tabs_checkbox_enable", "Enable Item Database")));

        // ── Search state ────────────────────────────────────────────────────
        List<ItemInfo> searchResults = new();
        bool searchInProgress = false;

        string searchName = "";
        string searchProps = "";
        uint searchGraphic = 0;
        int searchHue = -1;
        int searchLayer = -1;
        int searchContainer = 0;
        bool onGroundOnly = false;
        bool inContainersOnly = false;
        bool currentCharOnly = false;
        int maxResults = 100;

        // Keep widget references for ClearSearch resets
        TextBox nameBox = null!;
        TextBox propsBox = null!;
        TextBox graphicBox = null!;
        TextBox hueBox = null!;
        TextBox layerBox = null!;
        TextBox containerBox = null!;
        MyraHSlider? maxResultsSlider = null;

        var resultsPanel = new VerticalStackPanel { Spacing = 2 };
        var statusLabel = new MyraLabel(TazLang.Get("item_database_tabs_status_ready", "Ready to search"), MyraLabel.TextStyle.P);

        // ── Results grid ────────────────────────────────────────────────────
        void BuildResultsGrid()
        {
            resultsPanel.Widgets.Clear();
            if (searchResults.Count == 0)
            {
                resultsPanel.Widgets.Add(new MyraLabel(TazLang.Get("item_database_tabs_empty_results", "No results to display"), MyraLabel.TextStyle.P));
                return;
            }

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Auto(TazLang.Get("item_database_tabs_col_art", "Art")),
                GridColumnInfo.Fill(TazLang.Get("item_database_tabs_col_name", "Name")),
                GridColumnInfo.Auto(TazLang.Get("item_database_tabs_col_hue", "Hue")),
                GridColumnInfo.Auto(TazLang.Get("item_database_tabs_col_layer", "Layer")),
                GridColumnInfo.Auto(TazLang.Get("item_database_tabs_col_location", "Location")),
                GridColumnInfo.Auto(TazLang.Get("item_database_tabs_col_container", "Container")),
                GridColumnInfo.Auto(TazLang.Get("item_database_tabs_col_character", "Character")),
                GridColumnInfo.Auto(TazLang.Get("item_database_tabs_col_updated", "Updated")),
                GridColumnInfo.Auto(TazLang.Get("item_database_tabs_col_actions", "Actions"))
            );

            int dataRow = 1;
            foreach (ItemInfo item in searchResults)
            {
                if (item.Graphic > 0)
                    grid.AddWidget(
                        new MyraArtTexture(item.Graphic)
                            { Tooltip = TazLang.Get("item_database_tabs_tooltip_graphic_fmt", new[] { item.Graphic.ToString(), item.Graphic.ToString("X") }) },
                        dataRow, 0);

                var nameLabel = new MyraLabel(item.Name, MyraLabel.TextStyle.P);
                if (!string.IsNullOrEmpty(item.Properties))
                    nameLabel.Tooltip = item.Properties.Replace("|", "\n");
                grid.AddWidget(nameLabel, dataRow, 1);

                grid.AddWidget(new MyraLabel($"{item.Hue}", MyraLabel.TextStyle.P, MyraLabel.AlignMode.Right), dataRow, 2);

                grid.AddWidget(
                    new MyraLabel($"{item.Layer}", MyraLabel.TextStyle.P, MyraLabel.AlignMode.Right)
                        { Tooltip = TazLang.Get("item_database_tabs_tooltip_layer_fmt", new[] { ((int)item.Layer).ToString() }) },
                    dataRow, 3);

                string locationStr = item.OnGround
                    ? $"{item.X}, {item.Y}"
                    : TazLang.Get("item_database_tabs_location_container", "Container");
                grid.AddWidget(new MyraLabel(locationStr, MyraLabel.TextStyle.P), dataRow, 4);

                string containerStr = (item.Container != 0 && item.Container != 0xFFFFFFFF)
                    ? $"0x{item.Container:X}"
                    : TazLang.Get("item_database_tabs_container_ground", "Ground");
                grid.AddWidget(new MyraLabel(containerStr, MyraLabel.TextStyle.P), dataRow, 5);

                grid.AddWidget(new MyraLabel(item.CharacterName, MyraLabel.TextStyle.P), dataRow, 6);

                TimeSpan timeAgo = DateTime.Now - item.UpdatedTime;
                string timeStr = timeAgo.TotalDays >= 1
                    ? TazLang.Get("item_database_tabs_time_days_fmt", new[] { timeAgo.Days.ToString() })
                    : timeAgo.TotalHours >= 1
                        ? TazLang.Get("item_database_tabs_time_hours_fmt", new[] { timeAgo.Hours.ToString() })
                        : timeAgo.TotalMinutes >= 1
                            ? TazLang.Get("item_database_tabs_time_minutes_fmt", new[] { ((int)timeAgo.TotalMinutes).ToString() })
                            : TazLang.Get("item_database_tabs_time_just_now", "Just now");
                grid.AddWidget(new MyraLabel(timeStr, MyraLabel.TextStyle.P), dataRow, 7);

                ItemInfo captured = item;
                grid.AddWidget(
                    new MyraButton(TazLang.Get("item_database_tabs_btn_details", "Details"), () => new ItemDetailMyraWindow(captured))
                        { Tooltip = TazLang.Get("item_database_tabs_tooltip_details", "View detailed information about this item") },
                    dataRow, 8);

                dataRow++;
            }

            resultsPanel.Widgets.Add(grid);
        }

        // ── Search execution ────────────────────────────────────────────────
        void PerformSearch()
        {
            if (searchInProgress) return;
            if (!profile.ItemDatabaseEnabled)
            {
                statusLabel.Text = TazLang.Get("item_database_tabs_status_disabled", "Item Database is disabled.");
                return;
            }

            searchInProgress = true;
            statusLabel.Text = TazLang.Get("item_database_tabs_status_searching", "Searching...");
            searchResults.Clear();
            resultsPanel.Widgets.Clear();

            ushort? graphic   = searchGraphic > 0   ? (ushort)searchGraphic   : null;
            ushort? hue       = searchHue >= 0      ? (ushort)searchHue       : null;
            Layer?  layer     = searchLayer >= 0    ? (Layer)searchLayer      : null;
            uint?   container = searchContainer > 0 ? (uint)searchContainer   : null;
            string? name      = string.IsNullOrWhiteSpace(searchName)  ? null : searchName.Trim();
            string? props     = string.IsNullOrWhiteSpace(searchProps) ? null : searchProps.Trim();
            uint?   character = null;
            bool?   ground    = null;

            if (currentCharOnly && Client.Game.UO?.World?.Player != null)
                character = Client.Game.UO.World.Player.Serial;

            if (onGroundOnly && !inContainersOnly)       ground = true;
            else if (inContainersOnly && !onGroundOnly)  ground = false;

            ItemDatabaseManager.Instance.SearchItems(
                results =>
                {
                    MainThreadQueue.EnqueueAction(() =>
                    {
                        searchResults   = results ?? new List<ItemInfo>();
                        searchInProgress = false;
                        BuildResultsGrid();
                        statusLabel.Text = searchResults.Count == 0
                            ? TazLang.Get("item_database_tabs_status_no_results", "No items found")
                            : searchResults.Count >= maxResults
                                ? TazLang.Get("item_database_tabs_status_max_results_fmt", new[] { searchResults.Count.ToString() })
                                : TazLang.Get("item_database_tabs_status_results_fmt", new[] { searchResults.Count.ToString() });
                    });
                },
                graphic:    graphic,
                hue:        hue,
                name:       name,
                properties: props,
                container:  container,
                layer:      layer,
                character:  character,
                onGround:   ground,
                limit:      maxResults
            );
        }

        void ClearSearch()
        {
            searchName    = "";  nameBox.Text    = "";
            searchProps   = "";  propsBox.Text   = "";
            searchGraphic = 0;   graphicBox.Text = "0";
            searchHue     = -1;  hueBox.Text     = "-1";
            searchLayer   = -1;  layerBox.Text   = "-1";
            searchContainer = 0; containerBox.Text = "0";
            onGroundOnly       = false;
            inContainersOnly   = false;
            currentCharOnly    = false;
            maxResults         = 100;
            if (maxResultsSlider != null) maxResultsSlider.Value = 100;
            statusLabel.Text = TazLang.Get("item_database_tabs_status_cleared", "Search cleared");
        }

        // ── Basic search fields ─────────────────────────────────────────────
        root.Widgets.Add(new MyraLabel(TazLang.Get("item_database_tabs_label_search_options", "Search Options:"), MyraLabel.TextStyle.H3));

        nameBox = new MyraInputBox { HintText = TazLang.Get("item_database_tabs_hint_name", "Item name (partial match)"), Width = 280 };
        nameBox.TextChangedByUser += (_, _) => searchName = nameBox.Text ?? "";

        propsBox = new MyraInputBox { HintText = TazLang.Get("item_database_tabs_hint_properties", "Property text (partial match)"), Width = 280 };
        propsBox.TextChangedByUser += (_, _) => searchProps = propsBox.Text ?? "";

        graphicBox = new MyraInputBox { Text = "0", Width = 100, Tooltip = TazLang.Get("item_database_tabs_tooltip_graphic_field", "Graphic ID to search for (0 = any)") };
        graphicBox.TextChangedByUser += (_, _) =>
        {
            if (StringHelper.TryParseUint(graphicBox.Text ?? "", out uint g)) searchGraphic = g;
        };

        hueBox = MyraInputBox.Hue(ushort.MaxValue, 80, TazLang.Get("item_database_tabs_hint_hue", "Hue to search for (-1 = any)"));
        hueBox.TextChangedByUser += (_, _) =>
        {
            if (MyraInputBox.TryParseHue(hueBox.Text, out ushort h))
                searchHue = h;
            else if (hueBox.Text == "-1")
                searchHue = -1;
        };

        layerBox = new MyraInputBox { Text = "-1", Width = 80, Tooltip = TazLang.Get("item_database_tabs_tooltip_layer_field", "Layer to search for (-1 = any, 0 = on ground)") };
        layerBox.TextChangedByUser += (_, _) =>
        {
            if (int.TryParse(layerBox.Text, out int l)) searchLayer = l;
        };

        var nameRow = new HorizontalStackPanel { Spacing = 4 };
        nameRow.Widgets.Add(new MyraLabel(TazLang.Get("item_database_tabs_label_name", "Name:"), MyraLabel.TextStyle.P));
        nameRow.Widgets.Add(nameBox);
        root.Widgets.Add(nameRow);

        var propsRow = new HorizontalStackPanel { Spacing = 4 };
        propsRow.Widgets.Add(new MyraLabel(TazLang.Get("item_database_tabs_label_properties", "Properties:"), MyraLabel.TextStyle.P));
        propsRow.Widgets.Add(propsBox);
        root.Widgets.Add(propsRow);

        var graphicHueRow = new HorizontalStackPanel { Spacing = 8 };
        graphicHueRow.Widgets.Add(new MyraLabel(TazLang.Get("item_database_tabs_label_graphic_id", "Graphic ID:"), MyraLabel.TextStyle.P));
        graphicHueRow.Widgets.Add(graphicBox);
        graphicHueRow.Widgets.Add(new MyraLabel(TazLang.Get("item_database_tabs_label_hue", "Hue:"), MyraLabel.TextStyle.P));
        graphicHueRow.Widgets.Add(hueBox);
        root.Widgets.Add(graphicHueRow);

        var layerRow = new HorizontalStackPanel { Spacing = 4 };
        layerRow.Widgets.Add(new MyraLabel(TazLang.Get("item_database_tabs_label_layer", "Layer:"), MyraLabel.TextStyle.P));
        layerRow.Widgets.Add(layerBox);
        root.Widgets.Add(layerRow);

        // ── Advanced search ─────────────────────────────────────────────────
        var advancedPanel = new VerticalStackPanel { Visible = false, Spacing = 4 };

        containerBox = new MyraInputBox { Text = "0", Width = 120, Tooltip = TazLang.Get("item_database_tabs_tooltip_container_field", "Search only in this container serial (0 = any)") };
        containerBox.TextChangedByUser += (_, _) =>
        {
            if (StringHelper.TryParseInt(containerBox.Text ?? "", out int c)) searchContainer = c;
        };

        var contRow = new HorizontalStackPanel { Spacing = 4 };
        contRow.Widgets.Add(new MyraLabel(TazLang.Get("item_database_tabs_label_container_serial", "Container Serial:"), MyraLabel.TextStyle.P));
        contRow.Widgets.Add(containerBox);
        advancedPanel.Widgets.Add(contRow);

        var locationCheckRow = new HorizontalStackPanel { Spacing = 12 };
        locationCheckRow.Widgets.Add(
            MyraCheckButton.CreateWithCallback(false, b => onGroundOnly = b, TazLang.Get("item_database_tabs_checkbox_on_ground", "On ground only")));
        locationCheckRow.Widgets.Add(
            MyraCheckButton.CreateWithCallback(false, b => inContainersOnly = b, TazLang.Get("item_database_tabs_checkbox_in_containers", "In containers only")));
        locationCheckRow.Widgets.Add(
            MyraCheckButton.CreateWithCallback(false, b => currentCharOnly = b, TazLang.Get("item_database_tabs_checkbox_current_char", "Current character only")));
        advancedPanel.Widgets.Add(locationCheckRow);

        HorizontalStackPanel sliderWidget = MyraHSlider.SliderWithLabel(
            TazLang.Get("item_database_tabs_label_max_results", "Max results"),
            out MyraHSlider ms,
            v => maxResults = (int)v,
            10, 1000, 100);
        maxResultsSlider = ms;
        advancedPanel.Widgets.Add(sliderWidget);

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(false, b =>
        {
            advancedPanel.Visible = b;
            if (!b)
            {
                searchContainer  = 0; containerBox.Text = "0";
                onGroundOnly     = false;
                inContainersOnly = false;
            }
        }, TazLang.Get("item_database_tabs_checkbox_advanced", "Advanced Search")));
        root.Widgets.Add(advancedPanel);

        // ── Action row ──────────────────────────────────────────────────────
        var actionRow = new HorizontalStackPanel { Spacing = 4 };
        actionRow.Widgets.Add(new MyraButton(TazLang.Get("item_database_tabs_btn_search", "Search"),        () => PerformSearch()));
        actionRow.Widgets.Add(new MyraButton(TazLang.Get("item_database_tabs_btn_clear_fields", "Clear Fields"),  () => ClearSearch()));
        actionRow.Widgets.Add(new MyraButton(TazLang.Get("item_database_tabs_btn_clear_results", "Clear Results"), () =>
        {
            searchResults.Clear();
            BuildResultsGrid();
            statusLabel.Text = TazLang.Get("item_database_tabs_status_results_cleared", "Results cleared");
        }));
        root.Widgets.Add(actionRow);

        // ── Database maintenance ────────────────────────────────────────────
        root.Widgets.Add(new MyraLabel(TazLang.Get("item_database_tabs_label_maintenance", "Database Maintenance:"), MyraLabel.TextStyle.H3));

        int[] clearDays = { 120 };
        bool[] clearInProgress = { false };
        var clearDaysBox = new MyraInputBox { Text = "120", Width = 60, Tooltip = TazLang.Get("item_database_tabs_tooltip_clear_days", "Delete all database entries older than this many days") };
        clearDaysBox.TextChangedByUser += (_, _) =>
        {
            if (int.TryParse(clearDaysBox.Text, out int d) && d >= 1) clearDays[0] = d;
        };

        var clearStatusLabel = new MyraLabel("", MyraLabel.TextStyle.P) { Visible = false };

        async void DoClear()
        {
            if (clearInProgress[0]) return;
            clearInProgress[0] = true;
            clearStatusLabel.Text    = TazLang.Get("item_database_tabs_status_clearing_fmt", new[] { clearDays[0].ToString() });
            clearStatusLabel.Visible = true;
            try
            {
                await ItemDatabaseManager.Instance.ClearOldDataAsync(TimeSpan.FromDays(clearDays[0]));
                clearStatusLabel.Text = TazLang.Get("item_database_tabs_status_cleared_entries_fmt", new[] { clearDays[0].ToString() });
            }
            catch (Exception ex)
            {
                clearStatusLabel.Text = TazLang.Get("item_database_tabs_error_clear_fmt", new[] { ex.Message });
            }
            finally
            {
                clearInProgress[0] = false;
            }
        }

        var maintenanceRow = new HorizontalStackPanel { Spacing = 4 };
        maintenanceRow.Widgets.Add(new MyraLabel(TazLang.Get("item_database_tabs_label_clear_older_than", "Clear entries older than:"), MyraLabel.TextStyle.P));
        maintenanceRow.Widgets.Add(clearDaysBox);
        maintenanceRow.Widgets.Add(new MyraLabel(TazLang.Get("item_database_tabs_label_days", "days"), MyraLabel.TextStyle.P));
        maintenanceRow.Widgets.Add(new MyraButton(TazLang.Get("item_database_tabs_btn_clear_old", "Clear Old Entries"), DoClear));
        root.Widgets.Add(maintenanceRow);
        root.Widgets.Add(clearStatusLabel);

        // ── Status + results ────────────────────────────────────────────────
        root.Widgets.Add(new MyraLabel(TazLang.Get("item_database_tabs_label_status", "Status:"), MyraLabel.TextStyle.H3));
        root.Widgets.Add(statusLabel);
        root.Widgets.Add(new MyraLabel(TazLang.Get("item_database_tabs_label_results", "Results:"), MyraLabel.TextStyle.H3));
        BuildResultsGrid();
        root.Widgets.Add(new ScrollViewer { MaxHeight = 300, Content = resultsPanel });

        return root;
    }
}
