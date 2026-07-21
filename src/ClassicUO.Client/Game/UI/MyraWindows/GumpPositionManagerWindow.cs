#nullable enable
using System.Collections.Generic;
using System.Linq;
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

    public GumpPositionManagerWindow() : base("Gump Position Manager")
    {
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

        root.Widgets.Add(toolbar);
        root.Widgets.Add(_openHeader);
        root.Widgets.Add(new ScrollViewer { MaxHeight = 230, Content = _openTable });
        root.Widgets.Add(_savedHeader);
        root.Widgets.Add(new ScrollViewer { MaxHeight = 230, Content = _savedTable });

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
            CellFactory = row => MyraCheckButton.CreateWithCallback(
                UIManager.IsPositionPersistent(row.Serial),
                isChecked => TogglePersistent(row, isChecked),
                tooltip: "Permanently save this gump's position")
        });
        _openColumns.Add(new RulebaseColumn<OpenGumpRow>
        {
            Header = "Center",
            HeaderTooltip = "Center this gump in the game viewport",
            Proportion = new Proportion(ProportionType.Auto),
            CellContentAlignment = HorizontalAlignment.Center,
            CellFactory = row => new MyraButton("Center", () => CenterGump(row))
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

    private void TogglePersistent(OpenGumpRow row, bool isChecked)
    {
        if (isChecked)
            UIManager.SetPositionPersistent(row.Serial, row.Name, new Point(row.Gump.X, row.Gump.Y));
        else
            UIManager.RemovePersistentPosition(row.Serial);

        RefreshSavedList();
    }

    private void CenterGump(OpenGumpRow row)
    {
        row.Gump.CenterInViewPort();
        // Persist the new location (only actually written to the DB when the gump is pinned).
        UIManager.SavePosition(row.Serial, new Point(row.Gump.X, row.Gump.Y));
        RefreshOpenList();
    }

    private void DeleteSaved(SavedGumpRow row)
    {
        UIManager.RemovePersistentPosition(row.Serial);
        RefreshSavedList();
        // A checkbox in the open list may reflect this entry, so refresh it too.
        RefreshOpenList();
    }

    #endregion

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

            string name = gump.GumpType != Gumps.GumpType.None ? gump.GumpType.ToString() : gump.GetType().Name;
            rows.Add(new OpenGumpRow(gump, gump.ServerSerial, name));
        }

        _openTable.SetRules(rows);
        _openHeader.Text = $"Open Server Gumps ({rows.Count})";
    }

    private void RefreshSavedList()
    {
        List<SavedGumpRow> rows = GumpPositionSQLManager.Instance.GetAll()
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
