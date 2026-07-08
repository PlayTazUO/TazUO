#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Resources;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using static ClassicUO.Game.UI.Gumps.WorldMapGump;

namespace ClassicUO.Game.UI.MyraWindows;

/// <summary>
/// Myra-based replacement for the legacy <c>MarkersManagerGump</c>. Lists the markers of the
/// selected marker file with search, per-row edit/remove/go-to actions, and — on editable
/// files (.usr and .csv) — bulk selection to delete or move markers to another editable file.
///
/// Rebuilding only refreshes the lightweight widget list rather than tearing down and
/// recreating per-label textures every keystroke like the old gump did, and edits are
/// persisted straight to each file's own path in the shared CSV format.
/// </summary>
public sealed class MarkersManagerWindow : MyraControl
{
    private readonly World _world;

    private int _categoryId;
    private string _filterText = "";

    // Markers currently ticked for a bulk operation on the active (editable) tab.
    private readonly HashSet<WMapMarker> _selected = new();

    private readonly VerticalStackPanel _listPanel = new() { Spacing = 2 };
    private readonly HorizontalStackPanel _tabRow = new() { Spacing = 4 };
    private readonly HorizontalStackPanel _bulkBar = new() { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };

    private MyraLabel? _selectionCountLabel;
    private MyraButton? _deleteSelectedButton;
    private MyraButton? _moveSelectedButton;

    private static List<WMapMarkerFile> MarkerFiles => _markerFiles;

    public MarkersManagerWindow(World world) : base(ResGumps.MarkersManager)
    {
        _world = world;
        Build();
        CenterInViewPort();
    }

    /// <summary>Opens the window, focusing an existing instance instead of stacking duplicates.</summary>
    public static void Show(World world)
    {
        foreach (IGui gump in UIManager.Gumps)
        {
            if (gump is MarkersManagerWindow w && !w.IsDisposed)
            {
                w.BringOnTop();
                return;
            }
        }

        UIManager.Add(new MarkersManagerWindow(world));
    }

    private void Build()
    {
        var root = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };

        root.Widgets.Add(BuildToolbar());
        BuildTabs();
        root.Widgets.Add(_tabRow);
        root.Widgets.Add(_bulkBar);
        root.Widgets.Add(new ScrollViewer { MaxHeight = 400, Content = _listPanel });

        RebuildList();

        SetRootContent(root);
    }

    private Widget BuildToolbar()
    {
        var toolbar = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };

        var searchBox = new MyraInputBox { Text = _filterText, HintText = ResGumps.MarkerSearch, Width = 220 };
        searchBox.TextChangedByUser += (_, _) =>
        {
            _filterText = searchBox.Text ?? "";
            RebuildList();
        };

        toolbar.Widgets.Add(searchBox);
        return toolbar;
    }

    private void BuildTabs()
    {
        _tabRow.Widgets.Clear();

        for (int i = 0; i < MarkerFiles.Count; i++)
        {
            int index = i;
            WMapMarkerFile file = MarkerFiles[i];

            var btn = new MyraButton(file.Name, () =>
            {
                if (_categoryId == index)
                    return;

                _categoryId = index;
                _selected.Clear();
                BuildTabs();
                RebuildList();
            })
            {
                Tooltip = file.Name
            };

            if (i == _categoryId)
                btn.Background = new SolidBrush(new Color(170, 105, 13, 220));

            _tabRow.Widgets.Add(btn);
        }
    }

    private WMapMarkerFile? CurrentFile =>
        _categoryId >= 0 && _categoryId < MarkerFiles.Count ? MarkerFiles[_categoryId] : null;

    private void RebuildList()
    {
        _listPanel.Widgets.Clear();

        WMapMarkerFile? file = CurrentFile;
        if (file == null)
        {
            _bulkBar.Visible = false;
            _listPanel.Widgets.Add(new MyraLabel("No marker files found.", MyraLabel.TextStyle.P));
            return;
        }

        bool editable = file.IsEditable;

        BuildBulkBar(editable);

        var grid = new MyraGrid();
        grid.SetupWithHeaders(BuildColumns(editable));

        int dataRow = 1;
        for (int realIdx = 0; realIdx < file.Markers.Count; realIdx++)
        {
            WMapMarker marker = file.Markers[realIdx];

            if (!string.IsNullOrEmpty(_filterText) &&
                marker.Name?.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            AddMarkerRow(grid, file, marker, realIdx, dataRow, editable);
            dataRow++;
        }

        if (dataRow == 1)
            _listPanel.Widgets.Add(new MyraLabel("No markers found.", MyraLabel.TextStyle.P));
        else
            _listPanel.Widgets.Add(grid);
    }

    private static GridColumnInfo[] BuildColumns(bool editable)
    {
        var columns = new List<GridColumnInfo>();

        if (editable)
            columns.Add(GridColumnInfo.Auto(""));

        columns.Add(GridColumnInfo.Auto(ResGumps.MarkerIcon));
        columns.Add(GridColumnInfo.Auto(ResGumps.MarkerName));
        columns.Add(GridColumnInfo.Auto(ResGumps.MarkerX));
        columns.Add(GridColumnInfo.Auto(ResGumps.MarkerY));
        columns.Add(GridColumnInfo.Auto(ResGumps.MarkerColor));
        columns.Add(GridColumnInfo.Auto(""));

        return columns.ToArray();
    }

    private void AddMarkerRow(MyraGrid grid, WMapMarkerFile file, WMapMarker marker, int realIdx, int row, bool editable)
    {
        int col = 0;

        if (editable)
        {
            MyraCheckButton check = MyraCheckButton.CreateWithCallback(_selected.Contains(marker), isChecked =>
            {
                if (isChecked)
                    _selected.Add(marker);
                else
                    _selected.Remove(marker);

                UpdateBulkControls();
            });
            grid.AddWidget(check, row, col++);
        }

        Widget icon = marker.MarkerIcon != null
            ? new Image { Renderable = new TextureRegion(marker.MarkerIcon), MaxWidth = 18, MaxHeight = 18 }
            : new MyraLabel("", MyraLabel.TextStyle.P);
        grid.AddWidget(icon, row, col++);

        grid.AddWidget(new MyraLabel(marker.Name ?? "", MyraLabel.TextStyle.P) { Tooltip = marker.Name }, row, col++);
        grid.AddWidget(new MyraLabel(marker.X.ToString(), MyraLabel.TextStyle.P), row, col++);
        grid.AddWidget(new MyraLabel(marker.Y.ToString(), MyraLabel.TextStyle.P), row, col++);
        grid.AddWidget(new MyraLabel(marker.ColorName ?? "", MyraLabel.TextStyle.P), row, col++);

        var actions = new HorizontalStackPanel { Spacing = 2 };

        if (editable)
        {
            actions.Widgets.Add(new MyraButton(ResGumps.Edit, () => EditMarker(file, marker, realIdx)));
            actions.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton(ResGumps.Remove, () => RemoveMarker(file, marker))));
        }

        actions.Widgets.Add(new MyraButton(ResGumps.MarkerGoTo, () => GoToMarker(marker)));

        grid.AddWidget(actions, row, col);
    }

    #region Bulk operations

    private void BuildBulkBar(bool editable)
    {
        _bulkBar.Widgets.Clear();
        _bulkBar.Visible = editable;

        _selectionCountLabel = null;
        _deleteSelectedButton = null;
        _moveSelectedButton = null;

        if (!editable)
            return;

        _bulkBar.Widgets.Add(new MyraButton("Select All", SelectAllVisible));
        _bulkBar.Widgets.Add(new MyraButton("Clear", ClearSelection));

        _deleteSelectedButton = new MyraButton("Delete Selected", ConfirmDeleteSelected);
        MyraStyle.ApplyButtonDangerStyle(_deleteSelectedButton);
        _bulkBar.Widgets.Add(_deleteSelectedButton);

        _moveSelectedButton = new MyraButton("Move Selected To…", ShowMoveMenu);
        _bulkBar.Widgets.Add(_moveSelectedButton);

        _selectionCountLabel = new MyraLabel("", MyraLabel.TextStyle.P);
        _bulkBar.Widgets.Add(_selectionCountLabel);

        UpdateBulkControls();
    }

    /// <summary>Refreshes the bulk action bar's label/enabled state without rebuilding the marker list.</summary>
    private void UpdateBulkControls()
    {
        bool any = _selected.Count > 0;

        if (_selectionCountLabel != null)
            _selectionCountLabel.Text = any ? $"{_selected.Count} selected" : "";

        if (_deleteSelectedButton != null)
            _deleteSelectedButton.Enabled = any;

        if (_moveSelectedButton != null)
            _moveSelectedButton.Enabled = any && OtherEditableFiles().Any();
    }

    private void SelectAllVisible()
    {
        WMapMarkerFile? file = CurrentFile;
        if (file == null)
            return;

        foreach (WMapMarker marker in file.Markers)
        {
            if (string.IsNullOrEmpty(_filterText) ||
                marker.Name?.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _selected.Add(marker);
            }
        }

        RebuildList();
    }

    private void ClearSelection()
    {
        _selected.Clear();
        RebuildList();
    }

    private void ConfirmDeleteSelected()
    {
        if (_selected.Count == 0)
            return;

        WMapMarkerFile? file = CurrentFile;
        if (file == null)
            return;

        int count = _selected.Count;

        new MyraDialog("Confirm Delete",
            new MyraLabel($"Delete {count} selected marker{(count == 1 ? "" : "s")}?", MyraLabel.TextStyle.P),
            ok =>
            {
                if (!ok)
                    return;

                file.Markers.RemoveAll(_selected.Contains);
                _selected.Clear();
                SaveFile(file);
                RebuildList();
            });
    }

    private void ShowMoveMenu()
    {
        if (_selected.Count == 0)
            return;

        (string Label, Action Action)[] targets = OtherEditableFiles()
            .Select(f => (f.Name, (Action)(() => Defer(() => MoveSelectedTo(f)))))
            .ToArray();

        if (targets.Length == 0)
            return;

        ShowContextMenu(targets);
    }

    private void MoveSelectedTo(WMapMarkerFile target)
    {
        WMapMarkerFile? source = CurrentFile;
        if (source == null || ReferenceEquals(source, target) || _selected.Count == 0)
            return;

        List<WMapMarker> moving = source.Markers.Where(_selected.Contains).ToList();
        if (moving.Count == 0)
            return;

        source.Markers.RemoveAll(_selected.Contains);
        target.Markers.AddRange(moving);
        _selected.Clear();

        SaveFile(source);
        SaveFile(target);

        GameActions.Print(_world, $"Moved {moving.Count} marker{(moving.Count == 1 ? "" : "s")} to {target.Name}", 0x2B);

        RebuildList();
    }

    private IEnumerable<WMapMarkerFile> OtherEditableFiles() =>
        MarkerFiles.Where(f => f.IsEditable && !ReferenceEquals(f, CurrentFile));

    #endregion

    #region Per-row operations

    private void EditMarker(WMapMarkerFile file, WMapMarker marker, int realIdx)
    {
        UIManager.GetGump<UserMarkersGump>()?.Dispose();

        var editGump = new UserMarkersGump(_world, marker.X, marker.Y, file.Markers, marker.ColorName, marker.MarkerIconName, true, realIdx);
        editGump.EditEnd += (_, _) =>
        {
            // Editing swaps in a new marker instance at realIdx, so drop the stale reference
            // from the selection set to keep the bulk selection count accurate.
            _selected.Remove(marker);
            SaveFile(file);
            RebuildList();
        };
    }

    private void RemoveMarker(WMapMarkerFile file, WMapMarker marker)
    {
        file.Markers.Remove(marker);
        _selected.Remove(marker);
        SaveFile(file);
        RebuildList();
    }

    private void GoToMarker(WMapMarker marker)
    {
        WorldMapGump? wmGump = UIManager.GetGump<WorldMapGump>();
        wmGump?.GoToMarker(marker.X, marker.Y, false);
    }

    #endregion

    /// <summary>
    /// Persists a marker file back to its own path in the shared CSV format. The .usr and .csv
    /// files use the identical layout, so a single serializer round-trips both. Because the
    /// in-memory <see cref="WMapMarkerFile.Markers"/> list is the same reference the world map
    /// draws from, the map reflects the change immediately without a reload.
    /// </summary>
    private void SaveFile(WMapMarkerFile file)
    {
        if (!file.IsEditable || string.IsNullOrEmpty(file.FullPath))
            return;

        try
        {
            using var writer = new StreamWriter(file.FullPath, false);
            foreach (WMapMarker m in file.Markers)
                writer.WriteLine(MarkerToCsvLine(m));
        }
        catch (Exception e)
        {
            Log.Error($"Error saving markers to {file.FullPath}: {e}");
            GameActions.Print(_world, "Failed to save markers", 32);
        }
    }
}
