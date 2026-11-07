#nullable enable

using System;
using ClassicUO.Configuration;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Search;

/// <summary>
///     One result row: either the entry's live editor, hosted as-is, or its name beside a link to the
///     page that owns it.
///     <para>
///         An inlined editor already draws its own caption, so the row adds none - doing otherwise
///         prints the setting's name twice.
///     </para>
///     <para>
///         The row keeps one persistent host and swaps only its child when the presentation changes,
///         so re-deciding on a window resize does not destroy a control the user is typing into - see
///         <see cref="HoldsKeyboardFocus" />.
///     </para>
/// </summary>
internal sealed class SearchResultRow : Panel
{
    #region Public Accessors

    /// <summary>The hit this row displays</summary>
    public SearchHit Hit { get; }

    /// <summary>Whether the row currently hosts the entry's live editor rather than a link</summary>
    public bool IsInline => _isInline == true;

    /// <summary>
    ///     Size offset applied to the row's text, from the user's configured UI font size. Applies to
    ///     the hosted editor too, so an inlined control matches the rows around it.
    /// </summary>
    public int FontOffset { get; init; }

    /// <summary>
    ///     Width this row loses to indentation and container padding before any of it reaches the
    ///     editor, subtracted from the budget the inline guard works against.
    /// </summary>
    public int IndentReserve { get; init; }

    #endregion

    #region Members

    #region Consts/Readonly

    private readonly Panel _host = new();
    private readonly SearchEntry _entry;
    private readonly Action<SearchEntry>? _onActivate;
    private readonly string? _attribution;
    private readonly InlineSizeCache _inlineSizes;

    #endregion

    #region Mutables

    private bool? _isInline;

    /// <summary>
    ///     Width the attribution takes beside an inlined editor, measured on first use. Negative until
    ///     then; the text and font are fixed for the row's lifetime, so one measurement holds.
    /// </summary>
    private int _attributionReserve = -1;

    #endregion

    #endregion

    #region Ctor

    /// <param name="hit">The hit to display</param>
    /// <param name="showAttribution">
    ///     Whether to explain a match that did not come from the label. Off by default, since it
    ///     competes for the same horizontal space as the editor.
    /// </param>
    /// <param name="onActivate">Invoked when the user follows this row to its own page</param>
    /// <param name="inlineSizes">
    ///     Shared editor measurements. Owned by the list rather than the row so it survives the row
    ///     being discarded on the next query.
    /// </param>
    public SearchResultRow(
        SearchHit hit,
        bool showAttribution,
        Action<SearchEntry>? onActivate,
        InlineSizeCache inlineSizes
    )
    {
        Hit = hit;
        _entry = hit.Entry;
        _onActivate = onActivate;
        _inlineSizes = inlineSizes;
        _attribution = showAttribution && TryBuildAttribution(hit, out string text) ? text : null;

        Padding = new Thickness(MyraStyle.STANDARD_SPACING, 2, MyraStyle.STANDARD_SPACING, 2);
        HorizontalAlignment = HorizontalAlignment.Stretch;

        _host.HorizontalAlignment = HorizontalAlignment.Stretch;
        Widgets.Add(_host);
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Decides how this row should present itself given the space available, and rebuilds only if
    ///     that changed.
    /// </summary>
    /// <param name="availableWidth">Width the row has to work with</param>
    /// <param name="threshold">
    ///     Fraction of <paramref name="availableWidth" /> an editor may claim and still be inlined.
    ///     Zero disables inlining outright.
    /// </param>
    /// <param name="hysteresis">
    ///     Extra slack applied to an already-inlined row before it is demoted, so rows do not
    ///     oscillate while the window edge is being dragged.
    /// </param>
    /// <param name="keyboardFocus">The widget holding keyboard focus, so a row being typed into is left alone</param>
    /// <returns>Whether the row was rebuilt</returns>
    public bool ApplyPresentation(int availableWidth, double threshold, double hysteresis, Widget? keyboardFocus)
    {
        bool wanted = ShouldInline(availableWidth, threshold, hysteresis);

        if (_isInline == wanted)
            return false;

        if (_isInline != null && HoldsKeyboardFocus(keyboardFocus))
            return false;

        _isInline = wanted;
        _host.Widgets.Clear();
        _host.Widgets.Add(wanted ? BuildInline() : BuildNavigate());

        return true;
    }

    /// <summary>
    ///     Whether <paramref name="focused" /> sits inside this row. Rebuilding disposes the hosted
    ///     widget, which would silently drop a caret mid-edit.
    /// </summary>
    /// <param name="focused">The currently focused widget, if any</param>
    /// <returns>Whether focus is inside this row</returns>
    public bool HoldsKeyboardFocus(Widget? focused)
    {
        for (Widget? widget = focused; widget != null; widget = widget.Parent)
            if (ReferenceEquals(widget, _host))
                return true;

        return false;
    }

    #endregion

    #region Private Methods

    /// <summary>
    ///     The editor as it appears on its own page, caption and all. Attribution is appended rather
    ///     than replacing anything, since the editor owns the rest of the row.
    /// </summary>
    private Widget BuildInline()
    {
        Widget editor = _entry.InlineFactory!().ApplyUiFontOffset(FontOffset);

        if (_attribution == null)
            return editor;

        return new SpaceBetweenRow(editor, BuildAttributionLabel(), MyraStyle.STANDARD_SPACING * 2);
    }

    private Widget BuildNavigate()
    {
        var labels = new VerticalStackPanel { VerticalAlignment = VerticalAlignment.Center };
        var name = new MyraLabel(_entry.Label, MyraLabel.TextStyle.P) { Wrap = false, SingleLine = true };

        name.Font = MyraStyle.GetUiFont(FontOffset);
        labels.Widgets.Add(name);

        if (_attribution != null)
            labels.Widgets.Add(BuildAttributionLabel());

        var link = new LinkLabel(
            TazLang.Get("optsearch_open_in_page"),
            () => _onActivate?.Invoke(_entry)
        ) { Wrap = false, SingleLine = true, VerticalAlignment = VerticalAlignment.Center };

        link.Font = MyraStyle.GetUiFont(FontOffset);

        return new SpaceBetweenRow(labels, link, MyraStyle.STANDARD_SPACING * 2);
    }

    private MyraLabel BuildAttributionLabel() => MyraLabel.Hint(_attribution!);

    /// <summary>
    ///     Width left for the editor itself, after the row's indentation and anything that shares the
    ///     row with it.
    /// </summary>
    private int EditorBudget(int availableWidth) => availableWidth - IndentReserve - AttributionReserve();

    /// <summary>
    ///     Measured width of the attribution plus the gap before it, or zero when the row shows none.
    /// </summary>
    private int AttributionReserve()
    {
        if (_attributionReserve >= 0)
            return _attributionReserve;

        if (_attribution == null)
            return _attributionReserve = 0;

        int width = BuildAttributionLabel().Measure(new Point(int.MaxValue / 2, int.MaxValue / 2)).X;

        return _attributionReserve = width + MyraStyle.STANDARD_SPACING * 2;
    }

    private bool ShouldInline(int availableWidth, double threshold, double hysteresis)
    {
        // The attribution sits beside the editor in the same row, so an editor measured against the
        // undiminished width would fit on its own and overflow once the attribution is drawn too.
        availableWidth = EditorBudget(availableWidth);

        if (!_entry.CanInline || threshold <= 0d || availableWidth <= 0)
            return false;

        // The editor's size doesn't change with the window, so only the budget moves.
        Point size = _inlineSizes.GetSize(_entry, FontOffset);

        // Capped at the row: Aggressive already allows the full width, so the hysteresis slack on an
        // already-inlined row would otherwise keep an editor that no longer fits.
        double allowance = Math.Min(availableWidth * (IsInline ? threshold + hysteresis : threshold), availableWidth);

        return size.X <= allowance;
    }

    /// <summary>
    ///     Explains a hit whose visible label has nothing to do with the query - a keyword match, or
    ///     a hit that only landed because an ancestor's name or a sibling's label matched.
    /// </summary>
    private static bool TryBuildAttribution(SearchHit hit, out string attribution)
    {
        attribution = hit.Field switch
        {
            SearchField.Keyword or SearchField.Path when !string.IsNullOrEmpty(hit.MatchedTerm) =>
                TazLang.Get("optsearch_matched_via", [hit.MatchedTerm!]),
            SearchField.MatchText => TazLang.Get("optsearch_matched_related"),
            _ => string.Empty
        };

        return attribution.Length > 0;
    }

    #endregion
}
