#nullable enable

using System;
using System.Collections.ObjectModel;
using ClassicUO.Game.UI.MyraWindows.Theme;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
///     A header with a twisty that shows or hides a stack of child widgets. Children may themselves
///     be expanders, which is how a tree of arbitrary depth is built without any widget needing to
///     know how deep it sits.
///     <para>
///         Collapsing hides the content rather than discarding it, so child state - a half-typed
///         field, a selection - survives being folded away and the tree never has to be rebuilt to
///         toggle one node.
///     </para>
///     <para>
///         The twisty uses the same symbol and size as the property grid's, so every disclosure
///         control in the UI reads alike.
///     </para>
/// </summary>
public class Expander : VerticalStackPanel
{
    #region Events

    /// <summary>Raised after <see cref="IsExpanded" /> changes, however it was changed</summary>
    public event EventHandler? ExpandedChanged;

    #endregion

    #region Public Accessors

    /// <summary>Whether the content is shown. Setting this raises <see cref="ExpandedChanged" />.</summary>
    public bool IsExpanded
    {
        get => field;
        set
        {
            if (field == value)
                return;

            field = value;
            _glyph.Text = value ? GLYPH_EXPANDED : GLYPH_COLLAPSED;
            _content.Visible = value;

            ExpandedChanged?.Invoke(this, EventArgs.Empty);
        }
    } = true;

    /// <summary>
    ///     Whether clicking anywhere on the header toggles, rather than only the twisty. Turn this off
    ///     for a header carrying its own interactive content - a click would otherwise both follow the
    ///     link and toggle the node.
    /// </summary>
    public bool ToggleOnHeaderClick
    {
        get => field;
        set
        {
            if (field == value)
                return;

            field = value;
            // Exactly one of the two must listen, or a click on the twisty toggles twice and nothing
            // appears to happen.
            _headerPanel.TouchDown -= OnToggleTouched;
            _glyph.TouchDown -= OnToggleTouched;

            if (value)
                _headerPanel.TouchDown += OnToggleTouched;
            else
                _glyph.TouchDown += OnToggleTouched;
        }
    } = true;

    /// <summary>Widget shown beside the twisty. Replacing it swaps the header in place.</summary>
    public Widget? HeaderContent
    {
        get => _headerContent;
        set
        {
            if (ReferenceEquals(_headerContent, value))
                return;

            if (_headerContent != null)
                _headerRow.Widgets.Remove(_headerContent);

            _headerContent = value;

            if (value != null)
                _headerRow.Widgets.Add(value);
        }
    }

    /// <summary>The header's own panel, exposed so a caller can restyle its background or padding</summary>
    public Panel Header => _headerPanel;

    /// <summary>The expander's children. Add nested expanders here to build a tree.</summary>
    public ObservableCollection<Widget> Items => _content.Widgets;

    /// <summary>How far the content is inset from the header</summary>
    public int IndentWidth
    {
        get => field;
        set
        {
            if (field == value)
                return;

            field = value;
            _content.Margin = new Thickness(value, 0, 0, 0);
        }
    } = DEFAULT_INDENT;

    #endregion

    #region Members

    #region Consts/Readonly

    private const string GLYPH_EXPANDED = "⮟";
    private const string GLYPH_COLLAPSED = "⮞";

    /// <summary>Matches the property grid's expander mark.</summary>
    private const int GLYPH_SIZE = StyleConstantsDefaults.RESET_ICON_FONT_SIZE;

    /// <summary>
    ///     Horizontal inset applied to the content per nesting level. Public so a caller laying out
    ///     against the remaining width can account for what the indent costs it.
    /// </summary>
    public const int DEFAULT_INDENT = 14;

    private readonly MyraLabel _glyph = MyraLabel.Symbol(GLYPH_EXPANDED, GLYPH_SIZE);
    private readonly HorizontalStackPanel _headerRow;
    private readonly Panel _headerPanel;
    private readonly VerticalStackPanel _content = new();

    #endregion

    #region Mutables

    private Widget? _headerContent;

    #endregion

    #endregion

    #region Ctor

    /// <param name="headerContent">Widget shown beside the twisty</param>
    /// <param name="items">Initial children</param>
    public Expander(Widget? headerContent = null, params Widget[] items)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;

        _headerRow = new HorizontalStackPanel
        {
            Spacing = MyraStyle.STANDARD_SPACING,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        _headerRow.Widgets.Add(_glyph);

        _headerPanel = new Panel
        {
            // A container with no background lets input fall through it, so with
            // ToggleOnHeaderClick set the header would not be clickable at all - see
            // Container.InputFallsThrough. Clear it only when the twisty is the sole toggle.
            Background = new SolidBrush(MyraTheme.Current.PanelFill),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(MyraStyle.STANDARD_SPACING, 1, MyraStyle.STANDARD_SPACING, 1)
        };

        _headerPanel.Widgets.Add(_headerRow);
        _headerPanel.TouchDown += OnToggleTouched;

        _content.Margin = new Thickness(IndentWidth, 0, 0, 0);
        _content.HorizontalAlignment = HorizontalAlignment.Stretch;

        Widgets.Add(_headerPanel);
        Widgets.Add(_content);

        HeaderContent = headerContent;

        foreach (Widget item in items)
            Items.Add(item);
    }

    #endregion

    #region Public Methods

    /// <summary>Flips <see cref="IsExpanded" /></summary>
    public void Toggle() => IsExpanded = !IsExpanded;

    /// <summary>
    ///     Sets the expanded state without raising <see cref="ExpandedChanged" />, for restoring a
    ///     persisted state that the listener itself is the source of.
    /// </summary>
    /// <param name="expanded">The state to apply</param>
    public void SetExpandedSilently(bool expanded)
    {
        EventHandler? subscribers = ExpandedChanged;
        ExpandedChanged = null;

        IsExpanded = expanded;

        ExpandedChanged = subscribers;
    }

    #endregion

    #region Private Methods

    private void OnToggleTouched(object? sender, EventArgs e) => Toggle();

    #endregion
}
