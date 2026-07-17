using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

/// <summary>
/// Lets the user configure the base X,Y map coordinates (Lord British's throne, i.e. 0° 0'N 0° 0'E)
/// used to anchor sextant coordinate conversions on the world map. The values are persisted to the
/// current profile so every conversion (map display, go-to, web map) uses the same origin.
/// </summary>
public sealed class SextantBaseWindow : MyraControl
{
    private readonly LabeledIntegerInput _xInput;
    private readonly LabeledIntegerInput _yInput;

    public SextantBaseWindow() : base(TazLang.Get("map_sextant_base_title", "Sextant Base Coordinates"))
    {
        Profile profile = ProfileManager.CurrentProfile;

        var layout = new VerticalStackPanel { Spacing = 8, Padding = new Thickness(8) };

        layout.Widgets.Add(new MyraLabel(
            TazLang.Get("map_sextant_base_desc", "Base map X,Y used to convert sextant coordinates (0° 0'N 0° 0'E)."),
            MyraLabel.TextStyle.P));

        _xInput = new LabeledIntegerInput(TazLang.Get("map_sextant_base_x", "Base X:"), profile?.WorldMapSextantBaseX ?? Sextant.DefaultBaseX, _ => { })
        {
            InputBoxWidth = 100
        };
        _yInput = new LabeledIntegerInput(TazLang.Get("map_sextant_base_y", "Base Y:"), profile?.WorldMapSextantBaseY ?? Sextant.DefaultBaseY, _ => { })
        {
            InputBoxWidth = 100
        };

        layout.Widgets.Add(_xInput);
        layout.Widgets.Add(_yInput);

        var buttonRow = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        buttonRow.Widgets.Add(new MyraButton(TazLang.Get("map_sextant_base_save", "Save"), Save));
        buttonRow.Widgets.Add(new MyraButton(TazLang.Get("map_sextant_base_reset", "Reset"), ResetToDefault));
        layout.Widgets.Add(buttonRow);

        SetRootContent(layout);

        CenterInViewPort();
        UIManager.Add(this);
        BringOnTop();
        UIManager.KeyboardFocusControl = this;
    }

    /// <summary>Opens the window, focusing an existing instance instead of stacking duplicates.</summary>
    public static void Show()
    {
        foreach (IGui gump in UIManager.Gumps)
        {
            if (gump is SextantBaseWindow w && !w.IsDisposed)
            {
                w.BringOnTop();
                return;
            }
        }

        _ = new SextantBaseWindow();
    }

    private void ResetToDefault()
    {
        _xInput.Value = Sextant.DefaultBaseX;
        _yInput.Value = Sextant.DefaultBaseY;
    }

    private void Save()
    {
        Profile profile = ProfileManager.CurrentProfile;

        if (profile != null)
        {
            profile.WorldMapSextantBaseX = _xInput.Value;
            profile.WorldMapSextantBaseY = _yInput.Value;
        }

        _disposeRequested = true;
    }
}
