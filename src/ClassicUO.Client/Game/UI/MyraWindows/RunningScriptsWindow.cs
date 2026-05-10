using System.Collections.Generic;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.LegionScripting;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows;

public class RunningScriptsWindow : MyraControl
{
    private readonly WrapPanel _scriptList = new()
    {
        HorizontalSpacing = MyraStyle.STANDARD_SPACING,
        VerticalSpacing = MyraStyle.STANDARD_SPACING,
        Orientation = Orientation.Vertical,
    };

    public RunningScriptsWindow() : base("Running Scripts")
    {
        const int minWidth = 120;
        const int minHeight = 110;
        _rootWindow.Props.Resize.MinWidth = minWidth;
        _rootWindow.Props.Resize.MinHeight = minHeight;

        CanBeSaved = true;
        Build();
        CenterInViewPort();

        LegionScripting.LegionScripting.ScriptStarted += OnScriptChanged;
        LegionScripting.LegionScripting.ScriptStopped += OnScriptChanged;
    }

    public static void Show()
    {
        foreach (IGui gump in UIManager.Gumps)
        {
            if (gump is RunningScriptsWindow w)
            {
                w.BringOnTop();
                return;
            }
        }
        UIManager.Add(new RunningScriptsWindow());
    }

    public override void Dispose()
    {
        LegionScripting.LegionScripting.ScriptStarted -= OnScriptChanged;
        LegionScripting.LegionScripting.ScriptStopped -= OnScriptChanged;
        base.Dispose();
    }

    private void OnScriptChanged(object sender, ScriptFile script) => RebuildList();

    private void Build()
    {
        var root = new WrapPanel
        {
            HorizontalSpacing = MyraStyle.STANDARD_SPACING,
            VerticalSpacing = MyraStyle.STANDARD_SPACING,
            Padding = new Thickness(4)
        };
        root.Widgets.Add(_scriptList);
        RebuildList();
        SetRootContent(root);
    }

    private void RebuildList()
    {
        _scriptList.Widgets.Clear();

        List<ScriptFile> scripts = LegionScripting.LegionScripting.RunningScripts;

        if (scripts.Count == 0)
        {
            _scriptList.Widgets.Add(
                new MyraLabel("No scripts currently running", MyraLabel.TextStyle.P) { Margin = new Thickness(4) }
            );
            return;
        }

        foreach (ScriptFile script in scripts.ToArray())
        {
            if (script == null) continue;

            var row = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };

            row.Widgets.Add(new MyraButton("Stop", () => LegionScripting.LegionScripting.StopScript(script)));

            row.Widgets.Add(new MyraLabel(script.FileName ?? "Unknown", MyraLabel.TextStyle.P)
            {
                Tooltip = $"Path: {script.FullPath ?? "N/A"}"
            });

            _scriptList.Widgets.Add(row);
        }
    }
}
