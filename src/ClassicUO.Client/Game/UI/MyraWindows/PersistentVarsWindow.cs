#nullable enable
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.LegionScripting;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

public class PersistentVarsWindow : MyraControl
{
    private LegionAPI.PersistentVar _selectedScope = LegionAPI.PersistentVar.Char;
    private string _filterText = "";
    private string? _editingKey;
    private string _editingValue = "";

    private readonly VerticalStackPanel _varsPanel = new() { Spacing = 2 };
    private readonly HorizontalStackPanel _scopeButtonRow = new() { Spacing = 4 };
    private readonly HorizontalStackPanel _scopeDescPanel = new() { Spacing = 4 };

    public PersistentVarsWindow() : base("Persistent Variables Manager")
    {
        CanBeSaved = true;
        Build();
        CenterInViewPort();
    }

    public static void Show()
    {
        foreach (IGui gump in UIManager.Gumps)
        {
            if (gump is PersistentVarsWindow w)
            {
                w.BringOnTop();
                return;
            }
        }
        UIManager.Add(new PersistentVarsWindow());
    }

    private void Build()
    {
        var root = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };

        // Scope selector
        var scopeRow = new HorizontalStackPanel { Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        scopeRow.Widgets.Add(new MyraLabel("Scope:", MyraLabel.TextStyle.P));
        BuildScopeButtons();
        scopeRow.Widgets.Add(_scopeButtonRow);
        BuildScopeDesc();
        scopeRow.Widgets.Add(_scopeDescPanel);
        root.Widgets.Add(scopeRow);

        // Toolbar
        root.Widgets.Add(BuildToolbar());

        // Variables list
        BuildVarsGrid();
        root.Widgets.Add(new ScrollViewer { MaxHeight = 400, Content = _varsPanel });

        SetRootContent(root);
    }

    private void BuildScopeButtons()
    {
        _scopeButtonRow.Widgets.Clear();

        (LegionAPI.PersistentVar scope, string label)[] scopes =
        [
            (LegionAPI.PersistentVar.Char,    "Character"),
            (LegionAPI.PersistentVar.Account, "Account"),
            (LegionAPI.PersistentVar.Server,  "Server"),
            (LegionAPI.PersistentVar.Global,  "Global"),
        ];

        foreach ((LegionAPI.PersistentVar scope, string label) in scopes)
        {
            LegionAPI.PersistentVar capturedScope = scope;
            var btn = new MyraButton(label, () =>
            {
                _selectedScope = capturedScope;
                _editingKey    = null;
                _editingValue  = "";
                BuildScopeButtons();
                BuildScopeDesc();
                BuildVarsGrid();
            });

            if (_selectedScope == scope)
                btn.Background = new SolidBrush(new Color(170, 105, 13, 220));

            _scopeButtonRow.Widgets.Add(btn);
        }
    }

    private void BuildScopeDesc()
    {
        _scopeDescPanel.Widgets.Clear();
        _scopeDescPanel.Widgets.Add(new MyraLabel($"({GetScopeDescription()})", MyraLabel.TextStyle.P));
    }

    private Widget BuildToolbar()
    {
        var toolbar = new HorizontalStackPanel { Spacing = 4 };

        var filterBox = new MyraInputBox { Text = _filterText, HintText = "Filter variables...", Width = 200 };
        filterBox.TextChangedByUser += (_, _) =>
        {
            _filterText = filterBox.Text ?? "";
            BuildVarsGrid();
        };
        toolbar.Widgets.Add(filterBox);

        toolbar.Widgets.Add(new MyraButton("Add New Variable", ShowAddDialog));
        toolbar.Widgets.Add(new MyraButton("Refresh", () =>
        {
            PersistentVars.Load();
            BuildVarsGrid();
        }));

        return toolbar;
    }

    private void BuildVarsGrid()
    {
        _varsPanel.Widgets.Clear();

        Dictionary<string, string> variables = PersistentVars.GetAllVars(_selectedScope);

        if (!string.IsNullOrWhiteSpace(_filterText))
        {
            variables = variables
                .Where(kv =>
                    kv.Key.Contains(_filterText, System.StringComparison.OrdinalIgnoreCase) ||
                    kv.Value.Contains(_filterText, System.StringComparison.OrdinalIgnoreCase))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        if (variables.Count == 0)
        {
            _varsPanel.Widgets.Add(new MyraLabel("No variables found.", MyraLabel.TextStyle.P));
            return;
        }

        var grid = new MyraGrid();
        grid.SetupWithHeaders(
            GridColumnInfo.Auto("Key"),
            GridColumnInfo.Fill("Value"),
            GridColumnInfo.Auto("Actions")
        );

        int dataRow = 1;
        foreach (KeyValuePair<string, string> kvp in variables)
        {
            string key   = kvp.Key;
            string value = kvp.Value;

            grid.AddWidget(new MyraLabel(key, MyraLabel.TextStyle.P), dataRow, 0);

            if (_editingKey == key)
            {
                var editBox = new MyraInputBox { Text = _editingValue, MinWidth = 180 };
                editBox.TextChangedByUser += (_, _) => _editingValue = editBox.Text ?? "";
                grid.AddWidget(editBox, dataRow, 1);

                var actionRow = new HorizontalStackPanel { Spacing = 2 };
                actionRow.Widgets.Add(new MyraButton("Save", () =>
                {
                    string savedKey = key;
                    string savedValue = _editingValue;
                    _editingKey   = null;
                    _editingValue = "";
                    PersistentVars.SaveVar(_selectedScope, savedKey, savedValue, () =>
                        MainThreadQueue.InvokeOnMainThread(BuildVarsGrid));
                }));
                actionRow.Widgets.Add(new MyraButton("Cancel", () =>
                {
                    _editingKey   = null;
                    _editingValue = "";
                    BuildVarsGrid();
                }));
                grid.AddWidget(actionRow, dataRow, 2);
            }
            else
            {
                grid.AddWidget(new MyraLabel(value, MyraLabel.TextStyle.P) { Tooltip = value }, dataRow, 1);

                var actionRow = new HorizontalStackPanel { Spacing = 2 };
                actionRow.Widgets.Add(new MyraButton("Edit", () =>
                {
                    _editingKey   = key;
                    _editingValue = value;
                    BuildVarsGrid();
                }));
                actionRow.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton("Delete", () =>
                    ShowDeleteDialog(key))));
                grid.AddWidget(actionRow, dataRow, 2);
            }

            dataRow++;
        }

        _varsPanel.Widgets.Add(grid);
    }

    private void ShowAddDialog()
    {
        var keyBox   = new MyraInputBox { HintText = "Key name...", Width = 300 };
        var valueBox = new MyraInputBox { HintText = "Value...",    Width = 300 };

        var form = new VerticalStackPanel { Spacing = 4 };
        form.Widgets.Add(new MyraLabel($"Add new variable to {_selectedScope} scope:", MyraLabel.TextStyle.P));
        form.Widgets.Add(new MyraLabel("Key:",   MyraLabel.TextStyle.P));
        form.Widgets.Add(keyBox);
        form.Widgets.Add(new MyraLabel("Value:", MyraLabel.TextStyle.P));
        form.Widgets.Add(valueBox);

        new MyraDialog("Add Variable", form, ok =>
        {
            if (!ok || string.IsNullOrWhiteSpace(keyBox.Text)) return;
            PersistentVars.SaveVar(_selectedScope, keyBox.Text.Trim(), valueBox.Text ?? "", () =>
                MainThreadQueue.InvokeOnMainThread(BuildVarsGrid));
        });
    }

    private void ShowDeleteDialog(string key) =>
        new MyraDialog("Confirm Delete",
            new MyraLabel($"Delete variable '{key}'?", MyraLabel.TextStyle.P),
            ok =>
            {
                if (!ok) return;
                if (_editingKey == key) { _editingKey = null; _editingValue = ""; }
                PersistentVars.DeleteVar(_selectedScope, key, () =>
                    MainThreadQueue.InvokeOnMainThread(BuildVarsGrid));
            });

    private string GetScopeDescription() => _selectedScope switch
    {
        LegionAPI.PersistentVar.Char    => $"{ProfileManager.CurrentProfile.ServerName} - {ProfileManager.CurrentProfile.CharacterName}",
        LegionAPI.PersistentVar.Account => $"{ProfileManager.CurrentProfile.ServerName} - {ProfileManager.CurrentProfile.Username}",
        LegionAPI.PersistentVar.Server  => ProfileManager.CurrentProfile.ServerName,
        LegionAPI.PersistentVar.Global  => "All servers and characters",
        _                               => ""
    };
}
