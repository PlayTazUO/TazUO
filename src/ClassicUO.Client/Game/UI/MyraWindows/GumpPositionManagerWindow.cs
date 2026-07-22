#nullable enable
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

/// <summary>
/// Myra window for managing gump positions. The top section lists the gumps currently open, each with
/// a checkbox that permanently saves its position to the database and a button to re-center it in the
/// game viewport. The bottom section lists every permanently saved position with a delete action.
/// Both lists are rendered with the Rulebase table-view UI system for a consistent look.
/// </summary>
public sealed class GumpPositionManagerWindow : MyraControl
{
    private readonly RulebaseStyleOptions _openStyle = new();
    private readonly RulebaseStyleOptions _savedStyle = new();

    private readonly List<RulebaseColumn<OpenGumpRow>> _openColumns = new();
    private readonly List<RulebaseColumn<SavedGumpRow>> _savedColumns = new();

    private RulebaseTableView<OpenGumpRow> _openTable = null!;
    private RulebaseTableView<SavedGumpRow> _savedTable = null!;

    private MyraLabel _openHeader = null!;
    private MyraLabel _savedHeader = null!;

    // Identify blink state: toggles _identifyGump.IsVisible on/off IDENTIFY_TOGGLES times, one toggle
    // every IDENTIFY_STEP_MS, driven off Time.Ticks from this window's Update().
    private const int IDENTIFY_TOGGLES = 10; // 5 off/on cycles
    private const int IDENTIFY_STEP_MS = 150; // 10 * 150ms = 1.5 seconds
    private Gump? _identifyGump;
    private int _identifyTogglesLeft;
    private uint _identifyNextToggle;

    public GumpPositionManagerWindow() : base("Gump Position Manager")
    {
        // The title-bar close button disposes via base.Update()/PreDraw() without routing through this
        // window's Dispose() override, so restore any blinking gump the moment a close is requested.
        _rootWindow.Closed += (_, _) => RestoreIdentifyGump();

        BuildColumns();
        Build();
        RefreshOpenList();
        RefreshSavedList();
        CenterInViewPort();
    }

    /// <summary>Opens the window, focusing an existing instance instead of stacking duplicates.</summary>
    public static void Show()
    {
        foreach (IGui gump in UIManager.Gumps)
        {
            if (gump is GumpPositionManagerWindow w && !w.IsDisposed)
            {
                w.BringOnTop();
                return;
            }
        }

        UIManager.Add(new GumpPositionManagerWindow());
    }

    #region Layout

    private void Build()
    {
        var root = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING, Width = 460 };

        _openHeader = new MyraLabel("Open Server Gumps", MyraLabel.TextStyle.H4);
        _savedHeader = new MyraLabel("Saved Gump Positions", MyraLabel.TextStyle.H4);

        _openTable = new RulebaseTableView<OpenGumpRow>(_openColumns, _openStyle);
        _savedTable = new RulebaseTableView<SavedGumpRow>(_savedColumns, _savedStyle);

        var toolbar = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        toolbar.Widgets.Add(new MyraButton("Refresh", () =>
        {
            RefreshOpenList();
            RefreshSavedList();
        }));
        toolbar.Widgets.Add(new MyraLabel("Tick a gump to keep its position permanently.", MyraLabel.TextStyle.P));

        bool autoSave = ProfileManager.CurrentProfile?.AutoSaveGumpPositions == true;
        var autoSaveCheck = MyraCheckButton.CreateWithCallback(
            autoSave,
            OnToggleAutoSaveAll,
            "Save all gumps automatically");

        var warning = new MyraLabel(
            "Some servers may send a significant amount of unique gumps, use with caution",
            MyraLabel.TextStyle.P)
        {
            TextColor = new Color(230, 170, 60)
        };

        root.Widgets.Add(toolbar);
        root.Widgets.Add(autoSaveCheck);
        root.Widgets.Add(warning);
        root.Widgets.Add(_openHeader);
        root.Widgets.Add(new ScrollViewer
        {
            MaxHeight = 230,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Content = _openTable
        });
        root.Widgets.Add(_savedHeader);
        root.Widgets.Add(new ScrollViewer
        {
            MaxHeight = 230,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Content = _savedTable
        });

        SetRootContent(root);
    }

    private void BuildColumns()
    {
        _openColumns.Add(new RulebaseColumn<OpenGumpRow>
        {
            Header = "Gump",
            Proportion = new Proportion(ProportionType.Fill, 1),
            CellFactory = row => new MyraLabel(row.Name, MyraLabel.TextStyle.P) { Tooltip = row.Name }
        });
        _openColumns.Add(new RulebaseColumn<OpenGumpRow>
        {
            Header = "Serial",
            Proportion = new Proportion(ProportionType.Auto),
            CellFactory = row => new MyraLabel($"0x{row.Serial:X}", MyraLabel.TextStyle.P)
        });
        _openColumns.Add(new RulebaseColumn<OpenGumpRow>
        {
            Header = "Position",
            Proportion = new Proportion(ProportionType.Auto),
            CellFactory = row => new MyraLabel($"{row.Gump.X}, {row.Gump.Y}", MyraLabel.TextStyle.P)
        });
        _openColumns.Add(new RulebaseColumn<OpenGumpRow>
        {
            Header = "Saved",
            HeaderTooltip = "Permanently save this gump's position",
            Proportion = new Proportion(ProportionType.Auto),
            CellContentAlignment = HorizontalAlignment.Center,
            CellFactory = BuildSavedCheckbox
        });
        _openColumns.Add(new RulebaseColumn<OpenGumpRow>
        {
            Header = "Center",
            HeaderTooltip = "Center this gump in the game viewport",
            Proportion = new Proportion(ProportionType.Auto),
            CellContentAlignment = HorizontalAlignment.Center,
            CellFactory = row => new MyraButton("Center", () => CenterGump(row))
        });
        _openColumns.Add(new RulebaseColumn<OpenGumpRow>
        {
            Header = "Identify",
            HeaderTooltip = "Flash this gump on and off so you can spot it",
            Proportion = new Proportion(ProportionType.Auto),
            CellContentAlignment = HorizontalAlignment.Center,
            CellFactory = row => new MyraButton("Identify", () => StartIdentify(row.Gump))
        });

        _savedColumns.Add(new RulebaseColumn<SavedGumpRow>
        {
            Header = "Gump",
            Proportion = new Proportion(ProportionType.Fill, 1),
            CellFactory = row => new MyraLabel(row.Name, MyraLabel.TextStyle.P) { Tooltip = row.Name }
        });
        _savedColumns.Add(new RulebaseColumn<SavedGumpRow>
        {
            Header = "Serial",
            Proportion = new Proportion(ProportionType.Auto),
            CellFactory = row => new MyraLabel($"0x{row.Serial:X}", MyraLabel.TextStyle.P)
        });
        _savedColumns.Add(new RulebaseColumn<SavedGumpRow>
        {
            Header = "Position",
            Proportion = new Proportion(ProportionType.Auto),
            CellFactory = row => new MyraLabel($"{row.X}, {row.Y}", MyraLabel.TextStyle.P)
        });
        _savedColumns.Add(new RulebaseColumn<SavedGumpRow>
        {
            Header = "",
            Proportion = new Proportion(ProportionType.Auto),
            CellContentAlignment = HorizontalAlignment.Center,
            CellFactory = row => (MyraButton)MyraStyle.ApplyButtonDangerStyle(
                new MyraButton("Delete", () => DeleteSaved(row)))
        });
    }

    #endregion

    #region Actions

    private Widget BuildSavedCheckbox(OpenGumpRow row)
    {
        // While "save all gumps automatically" is on it re-pins every server gump, so per-gump control
        // would be overridden anyway - show it locked on rather than letting the user toggle it futilely.
        if (ProfileManager.CurrentProfile?.AutoSaveGumpPositions == true)
        {
            return new MyraCheckButton(true)
            {
                Enabled = false,
                Tooltip = "Managed by \"Save all gumps automatically\""
            };
        }

        return MyraCheckButton.CreateWithCallback(
            UIManager.IsPositionPersistent(row.Serial),
            isChecked => TogglePersistent(row, isChecked),
            tooltip: "Permanently save this gump's position");
    }

    private void TogglePersistent(OpenGumpRow row, bool isChecked)
    {
        if (isChecked)
            UIManager.SetPositionPersistent(row.Serial, row.Name, new Point(row.Gump.X, row.Gump.Y));
        else
            UIManager.RemovePersistentPosition(row.Serial);

        RefreshSavedList();
    }

    private void OnToggleAutoSaveAll(bool enabled)
    {
        if (ProfileManager.CurrentProfile != null)
            ProfileManager.CurrentProfile.AutoSaveGumpPositions = enabled;

        // Enabling it immediately pins every server gump that is already open.
        if (enabled)
        {
            foreach (IGui gui in UIManager.Gumps)
            {
                if (gui is Gump { IsDisposed: false } gump)
                    UIManager.AutoSaveGumpPositionIfEnabled(gump);
            }
        }

        RefreshSavedList();
        RefreshOpenList();
    }

    private void CenterGump(OpenGumpRow row)
    {
        row.Gump.CenterInViewPort();
        // Persist the new location (only actually written to the DB when the gump is pinned).
        UIManager.SavePosition(row.Serial, new Point(row.Gump.X, row.Gump.Y));
        RefreshOpenList();
        // The saved-list position for a pinned gump just changed, so refresh it too.
        RefreshSavedList();
    }

    private void DeleteSaved(SavedGumpRow row)
    {
        UIManager.RemovePersistentPosition(row.Serial);
        RefreshSavedList();
        // A checkbox in the open list may reflect this entry, so refresh it too.
        RefreshOpenList();
    }

    /// <summary>Begins the identify blink for a gump (finishing any blink already in progress first).</summary>
    private void StartIdentify(Gump gump)
    {
        // Restore any previously blinking gump before switching targets.
        if (_identifyGump != null && !_identifyGump.IsDisposed)
            _identifyGump.IsVisible = true;

        _identifyGump = gump;
        _identifyTogglesLeft = IDENTIFY_TOGGLES;
        _identifyNextToggle = Time.Ticks;
    }

    /// <summary>Leaves the currently blinking gump visible and clears the blink state.</summary>
    private void RestoreIdentifyGump()
    {
        if (_identifyGump != null && !_identifyGump.IsDisposed)
            _identifyGump.IsVisible = true;

        _identifyGump = null;
    }

    #endregion

    /// <summary>Drives the identify blink off the game clock so it needs no background thread.</summary>
    public override void Update()
    {
        base.Update();

        if (IsDisposed)
            return;

        if (_identifyGump == null)
            return;

        if (_identifyGump.IsDisposed)
        {
            _identifyGump = null;
            return;
        }

        if (Time.Ticks < _identifyNextToggle)
            return;

        if (_identifyTogglesLeft <= 0)
        {
            // Always leave the gump visible when the blink finishes.
            _identifyGump.IsVisible = true;
            _identifyGump = null;
            return;
        }

        _identifyGump.IsVisible = !_identifyGump.IsVisible;
        _identifyTogglesLeft--;
        _identifyNextToggle = Time.Ticks + IDENTIFY_STEP_MS;
    }

    public override void Dispose()
    {
        // Don't leave a gump hidden if the window is closed mid-blink.
        RestoreIdentifyGump();
        base.Dispose();
    }

    #region Refresh

    private void RefreshOpenList()
    {
        var rows = new List<OpenGumpRow>();

        foreach (IGui gui in UIManager.Gumps)
        {
            if (gui is not Gump gump || gump.IsDisposed)
                continue;

            // Only server gumps participate in the server-serial position cache this feature manages.
            if (gump.ServerSerial == 0)
                continue;

            rows.Add(new OpenGumpRow(gump, gump.ServerSerial, UIManager.GetGumpDisplayName(gump)));
        }

        _openTable.SetRules(rows);
        _openHeader.Text = $"Open Server Gumps ({rows.Count})";
    }

    private void RefreshSavedList()
    {
        // Read from UIManager's in-memory snapshot rather than the database: it is always consistent
        // with the writes just made (which persist asynchronously) and needs no blocking DB read.
        List<SavedGumpRow> rows = UIManager.GetPersistentPositions()
            .OrderBy(s => s.Name)
            .Select(s => new SavedGumpRow(s.Serial, s.Name, s.X, s.Y))
            .ToList();

        _savedTable.SetRules(rows);
        _savedHeader.Text = $"Saved Gump Positions ({rows.Count})";
    }

    #endregion

    #region Row models

    /// <summary>A live open gump displayed in the top table.</summary>
    private sealed class OpenGumpRow : IRule
    {
        public OpenGumpRow(Gump gump, uint serial, string name)
        {
            Gump = gump;
            Serial = serial;
            Name = name;
        }

        public Gump Gump { get; }
        public uint Serial { get; }
        public string Name { get; }

        public uint Order { get; set; }
        public bool Enabled { get; set; } = true;
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    /// <summary>A permanently saved position displayed in the bottom table.</summary>
    private sealed class SavedGumpRow : IRule
    {
        public SavedGumpRow(uint serial, string name, int x, int y)
        {
            Serial = serial;
            Name = name;
            X = x;
            Y = y;
        }

        public uint Serial { get; }
        public string Name { get; }
        public int X { get; }
        public int Y { get; }

        public uint Order { get; set; }
        public bool Enabled { get; set; } = true;
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; } = true;
    }

    #endregion
}
