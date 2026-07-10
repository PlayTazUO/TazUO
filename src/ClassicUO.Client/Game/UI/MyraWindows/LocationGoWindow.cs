using System;
using System.Text.RegularExpressions;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

/// <summary>
/// Myra-based replacement for the legacy <c>LocationGoGump</c>. Collects a target location for the
/// world map — either raw map coordinates (<c>X, Y</c>) or sextant coordinates
/// (e.g. <c>100o25'S, 40o04'E</c>) — and, as the user types, live-decodes the input to show the
/// resulting map coordinates (or an invalid indicator) so the user gets immediate feedback.
/// A clear button empties the input field in one click.
/// </summary>
public sealed partial class LocationGoWindow : MyraControl
{
    /**
     * Valid inputs:
     * 3123, 124
     * 123 4141
     * 1331:745
     */
    [GeneratedRegex(@"^(?<X>\d+)\s*[,:\s]\s*(?<Y>\d+)$")]
    private static partial Regex PointCoordsRegex();

    private static readonly Color ValidColor = Color.LightGreen;
    private static readonly Color InvalidColor = Color.OrangeRed;

    private readonly World _world;
    private readonly Action<int, int> _goTo;
    private readonly Action _onClear;

    private readonly MyraInputBox _inputBox;
    private readonly MyraLabel _decodeLabel;
    private Point _location;

    public LocationGoWindow(
        World world,
        Action<int, int> goTo,
        Action onClear,
        Point? prefilledLocation = null
    ) : base(TazLang.Get("map_goto_title", "Enter Location"))
    {
        _world = world;
        _goTo = goTo;
        _onClear = onClear;
        _location = prefilledLocation ?? Sextant.InvalidPoint;

        var layout = new VerticalStackPanel { Spacing = 8, Padding = new Thickness(8) };

        // Input row: text field + clear button + go button
        var inputRow = new HorizontalStackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };

        _inputBox = new MyraInputBox
        {
            Width = 220,
            HintText = TazLang.Get("map_goto_hint", "e.g. 1639, 1532"),
            Text = prefilledLocation.HasValue ? $"{prefilledLocation.Value.X}, {prefilledLocation.Value.Y}" : ""
        };
        _inputBox.TextChangedByUser += (_, _) => UpdateDecode();
        _inputBox.KeyDown += (_, e) =>
        {
            if (e.Data == Microsoft.Xna.Framework.Input.Keys.Enter)
                Submit();
        };
        inputRow.Widgets.Add(_inputBox);

        inputRow.Widgets.Add(new MyraButton(TazLang.Get("map_goto_clear", "Clear"), Clear));
        inputRow.Widgets.Add(new MyraButton(TazLang.Get("map_goto_button", "Go To"), Submit));

        layout.Widgets.Add(inputRow);

        // Live decode feedback line
        _decodeLabel = new MyraLabel("", MyraLabel.TextStyle.P);
        layout.Widgets.Add(_decodeLabel);

        layout.Widgets.Add(new MyraLabel(TazLang.Get("map_goto_examples", "Examples:\n 1639, 1532\n 100o25'S,40o04'E\n 9 14'N 91 37'W"), MyraLabel.TextStyle.P));

        SetRootContent(layout);
        UpdateDecode();

        CenterInViewPort();
        UIManager.Add(this);
        BringOnTop();
        UIManager.KeyboardFocusControl = this;
        _inputBox.SetKeyboardFocus();
    }

    /// <summary>Opens the window, focusing an existing instance instead of stacking duplicates.</summary>
    public static void Show(World world, Action<int, int> goTo, Action onClear, Point? prefilledLocation = null)
    {
        foreach (IGui gump in UIManager.Gumps)
        {
            if (gump is LocationGoWindow w && !w.IsDisposed)
            {
                w.BringOnTop();
                return;
            }
        }

        _ = new LocationGoWindow(world, goTo, onClear, prefilledLocation);
    }

    private bool ParsePoint(string text, out Point point)
    {
        point = Sextant.InvalidPoint;

        Match match = PointCoordsRegex().Match(text);
        if (!match.Success)
            return false;

        point.X = int.Parse(match.Groups["X"].Value);
        point.Y = int.Parse(match.Groups["Y"].Value);
        return true;
    }

    /// <summary>
    /// Re-parses the current input and updates the feedback line so the user can tell, as they type,
    /// whether the input is valid and what map coordinates it resolves to. Handles both sextant
    /// coordinates (which are converted to map coordinates) and raw map coordinates.
    /// </summary>
    private void UpdateDecode()
    {
        string text = _inputBox.Text ?? "";

        if (string.IsNullOrWhiteSpace(text))
        {
            _location = Sextant.InvalidPoint;
            _decodeLabel.Text = "";
            return;
        }

        bool valid = Sextant.Parse(_world.Map, text, out _location) || ParsePoint(text, out _location);

        if (valid)
        {
            _decodeLabel.Text = TazLang.Get("map_goto_decoded", [_location.X.ToString(), _location.Y.ToString()]);
            _decodeLabel.TextColor = ValidColor;
        }
        else
        {
            _location = Sextant.InvalidPoint;
            _decodeLabel.Text = TazLang.Get("map_goto_invalid", "Invalid coordinates");
            _decodeLabel.TextColor = InvalidColor;
        }
    }

    private void Clear()
    {
        _inputBox.Text = "";
        UpdateDecode();
        _inputBox.SetKeyboardFocus();
    }

    private void Submit()
    {
        // Default to the 'Go To' branch if no On-Clear callback was given. This retains standard behavior.
        if (_onClear != null && string.IsNullOrWhiteSpace(_inputBox.Text))
        {
            _onClear();
            _disposeRequested = true;
            return;
        }

        if (_location == Sextant.InvalidPoint)
            return;

        _goTo(_location.X, _location.Y);
        _disposeRequested = true;
    }
}
