#nullable enable

using System;
using ClassicUO.Configuration;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Search;

/// <summary>
///     Display options for a <see cref="SearchBar" />.
/// </summary>
public record struct SearchBarProps()
{
    /// <summary>Placeholder shown while the box is empty</summary>
    public string? HintText { get; init; } = null;

    /// <summary>Whether a clear chip appears inside the box once there is something to clear</summary>
    public bool ShowClearButton { get; init; } = true;

    /// <summary>Whether a result count is shown beside the box</summary>
    public bool ShowResultCount { get; init; } = true;

    /// <summary>
    ///     Size offset for the result count, from the user's configured UI font size. Defaults to the
    ///     shared hint size, which is what makes it read as an annotation rather than as a field.
    /// </summary>
    public int ResultCountFontOffset { get; init; } = int.MinValue;

    /// <summary>Result count colour; the theme's dimmed text when omitted</summary>
    public Color? ResultCountColor { get; init; } = null;
}

/// <summary>
///     A query box with an optional clear chip inside it and an optional result count beside it.
///     <para>
///         Raises <see cref="QueryChanged" /> on every keystroke without debouncing - how long to wait
///         before acting is the consumer's decision, not the widget's.
///     </para>
/// </summary>
public sealed class SearchBar : HorizontalStackPanel
{
    #region Members

    #region Consts/Readonly

    /// <summary>Room reserved on the right of the field so text cannot run under the clear chip.</summary>
    private const int CLEAR_CHIP_RESERVE = 22;

    private const int CLEAR_CHIP_GLYPH_SIZE = 16;
    private const int CLEAR_CHIP_SIZE = 18;

    private readonly SearchBarProps _props;
    private readonly MyraInputBox _input = new();
    private readonly MyraLabel? _resultCount;
    private readonly IconButton? _clearButton;

    #endregion

    #endregion

    #region Events

    /// <summary>
    ///     Raised on every edit the user makes, carrying the box's text verbatim. Neither debounced
    ///     nor trimmed - when to act and what counts as the query are the consumer's decisions.
    /// </summary>
    public event EventHandler<string>? QueryChanged;

    /// <summary>Raised when the clear chip is used, after the box has been emptied</summary>
    public event EventHandler? Cleared;

    #endregion

    #region Public Accessors

    /// <summary>
    ///     The current query. Assigning does not raise <see cref="QueryChanged" />, so a consumer can
    ///     push state back into the bar without re-entering its own handler.
    ///     <para>
    ///         Comparison ignores surrounding whitespace, so writing back a trimmed copy of what the
    ///         user typed is a no-op. Assigning at all would reset the caret to the start - Myra's
    ///         text box treats any non-user write as a fresh value - which mangles the next keystroke
    ///         while someone is still typing.
    ///     </para>
    /// </summary>
    public string Query
    {
        get => _input.Text ?? string.Empty;
        set
        {
            if (string.Equals((_input.Text ?? string.Empty).Trim(), (value ?? string.Empty).Trim(), StringComparison.Ordinal))
                return;

            _input.Text = value;

            // Assigning Text re-runs setters that redraw the placeholder; without this it is lost
            // even though the property itself has not changed.
            _input.HintText = _props.HintText;

            UpdateClearButton();
        }
    }

    /// <summary>The underlying text box, for a consumer that needs to focus it</summary>
    public MyraInputBox Input => _input;

    #endregion

    #region Ctor

    /// <param name="props">Display options</param>
    public SearchBar(SearchBarProps props = default)
    {
        _props = props;

        Spacing = MyraStyle.STANDARD_SPACING;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Center;

        _input.HintText = props.HintText;
        _input.TextVerticalAlignment = VerticalAlignment.Center;
        _input.HorizontalAlignment = HorizontalAlignment.Stretch;
        _input.Padding = new Thickness(
            MyraStyle.STANDARD_SPACING,
            5,
            props.ShowClearButton ? CLEAR_CHIP_RESERVE : MyraStyle.STANDARD_SPACING,
            5
        );

        _input.TextChangedByUser += (_, args) =>
        {
            UpdateClearButton();
            QueryChanged?.Invoke(this, args.NewValue ?? string.Empty);
        };

        // The field and the chip share a cell so the chip sits inside the box rather than beside it.
        var field = new Panel { HorizontalAlignment = HorizontalAlignment.Stretch };
        field.Widgets.Add(_input);

        if (props.ShowClearButton)
        {
            // Flat, because it sits inside the field: a bordered button there reads as a second
            // control nested in the box rather than as an affordance on it.
            _clearButton = IconButton.Flat(
                "✕",
                Clear,
                TazLang.Get("optsearch_clear"),
                CLEAR_CHIP_SIZE,
                CLEAR_CHIP_GLYPH_SIZE
            );

            _clearButton.HorizontalAlignment = HorizontalAlignment.Right;
            _clearButton.VerticalAlignment = VerticalAlignment.Center;
            _clearButton.Margin = new Thickness(0, 0, MyraStyle.STANDARD_SPACING, 0);

            field.Widgets.Add(_clearButton);
        }

        Widgets.Add(field);
        SetProportionType(field, ProportionType.Fill);

        if (props.ShowResultCount)
        {
            _resultCount = MyraLabel.Hint(string.Empty, props.ResultCountColor);

            if (props.ResultCountFontOffset != int.MinValue)
                _resultCount.Font = MyraStyle.GetUiFont(props.ResultCountFontOffset);

            _resultCount.Margin = new Thickness(0, 0, MyraStyle.STANDARD_SPACING, 0);
            Widgets.Add(_resultCount);
        }

        UpdateClearButton();
    }

    #endregion

    #region Public Methods

    /// <summary>Sets the text shown beside the box, or hides it when <paramref name="text" /> is empty</summary>
    /// <param name="text">The annotation to show</param>
    public void SetResultText(string? text)
    {
        if (_resultCount == null)
            return;

        _resultCount.Text = text ?? string.Empty;
        _resultCount.Visible = !string.IsNullOrEmpty(text);
    }

    /// <summary>Empties the box and raises <see cref="Cleared" /></summary>
    public void Clear()
    {
        _input.Text = null;
        _input.HintText = _props.HintText;

        UpdateClearButton();
        Cleared?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Private Methods

    /// <summary>
    ///     The chip only means anything with something to clear, so it is hidden rather than
    ///     disabled - a permanently greyed control inside the field is just noise.
    /// </summary>
    private void UpdateClearButton() => _clearButton?.Visible = !string.IsNullOrEmpty(_input.Text);

    #endregion
}
