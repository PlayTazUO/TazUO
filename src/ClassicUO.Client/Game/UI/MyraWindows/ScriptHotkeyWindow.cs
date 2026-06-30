using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Game.UI.MyraWindows.Widgets.HotkeyInput;
using ClassicUO.LegionScripting;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

/// <summary>
/// Small window for assigning (or clearing) the hotkey bound to a Legion script. Reuses the central
/// <see cref="HotkeyInput"/> widget for capture/describe and persists the binding through
/// <see cref="ScriptHotkeysManager"/>. Pressing the hotkey in game toggles the script.
/// </summary>
public class ScriptHotkeyWindow : MyraControl
{
    private readonly ScriptFile _script;

    public ScriptHotkeyWindow(ScriptFile script) : base("Set Script Hotkey")
    {
        _script = script;

        var layout = new VerticalStackPanel { Spacing = 8, Padding = new Thickness(8) };

        string displayName = script.FileName;
        int dot = displayName.LastIndexOf('.');
        if (dot != -1) displayName = displayName.Substring(0, dot);

        layout.Widgets.Add(new MyraLabel($"Hotkey for '{displayName}'", MyraLabel.TextStyle.H3));
        layout.Widgets.Add(new MyraLabel(script.RelativePath, MyraLabel.TextStyle.P) { TextColor = Color.Gray });
        layout.Widgets.Add(new MyraLabel("Pressing this hotkey toggles the script on/off.", MyraLabel.TextStyle.P));

        // Key, mouse-button and controller bindings can all toggle the script; wheel and
        // modifier-only captures are rejected by SetBinding since they can't reliably toggle.
        var input = new HotkeyInput(
            existingSelection: ScriptHotkeysManager.GetBinding(script),
            onSelectionChanged: e => ScriptHotkeysManager.SetBinding(_script, e.NewValue),
            capturesMouseEvents: true);
        layout.Widgets.Add(input);

        var btnRow = new HorizontalStackPanel { Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        btnRow.Widgets.Add(new MyraButton("Close", () => _disposeRequested = true));
        layout.Widgets.Add(btnRow);

        SetRootContent(layout);
        CenterInViewPort();
        UIManager.Add(this);
        BringOnTop();
    }
}
