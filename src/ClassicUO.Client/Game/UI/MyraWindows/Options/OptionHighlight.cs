#nullable enable

using System;
using ClassicUO.Game.UI.MyraWindows.Theme;
using ClassicUO.Utility.Logging;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options;

/// <summary>
///     Points out one setting on a freshly opened page: scrolls it into view, tints it, and fades the
///     tint away.
///     <para>
///         For arriving from a search result, where the page alone doesn't say which of its settings
///         was asked for. The cue clears itself: a highlight that waits to be dismissed is one more
///         thing the user has to deal with.
///     </para>
///     <para>
///         Owned by the window that drives it. <see cref="Update" /> must run every frame, and
///         <see cref="Cancel" /> before the page being pointed into is rebuilt - the target's own
///         background is borrowed for the duration and has to be given back.
///     </para>
/// </summary>
internal sealed class OptionHighlight
{
    #region Members

    #region Consts/Readonly

    /// <summary>Peak tint alpha: enough to catch the eye, not enough to obscure the setting.</summary>
    private const float PEAK_ALPHA = 0.35f;

    private const uint HOLD_MS = 400;
    private const uint FADE_MS = 900;

    /// <summary>Room left above the target so it doesn't land flush against the viewport edge.</summary>
    private const int SCROLL_MARGIN = 12;

    /// <summary>
    ///     Frames to keep retrying while the newly built page has not been laid out. Bounds read as
    ///     empty until it has, and Myra raises nothing to say layout finished.
    /// </summary>
    private const int LAYOUT_WAIT_FRAMES = 10;

    /// <summary>One brush for the life of the highlighter: the tint changes every frame it runs.</summary>
    private readonly SolidBrush _tint = new(Color.Transparent);

    #endregion

    #region Mutables

    private Widget? _target;

    /// <summary>The target's own background, put back by <see cref="Cancel" />.</summary>
    private IBrush? _borrowedFrom;

    private uint _startTick;
    private int _waitFrames;
    private bool _placed;

    #endregion

    #endregion

    #region Public Methods

    /// <summary>
    ///     Starts pointing at the setting captioned <paramref name="label" /> somewhere under
    ///     <paramref name="pageRoot" />. Does nothing when no such caption is on the page - a missed
    ///     cue is better than a tint on the wrong setting.
    /// </summary>
    /// <param name="pageRoot">The page just navigated to</param>
    /// <param name="label">The setting's visible caption, as the search result displayed it</param>
    public void Start(Widget pageRoot, string label)
    {
        Cancel();

        if (string.IsNullOrEmpty(label) || FindTarget(pageRoot, label) is not { } target)
        {
            // TEMP DIAGNOSTICS
            Log.Trace($"[opthl] no target for '{label}' under {pageRoot.GetType().Name}");

            return;
        }

        // TEMP DIAGNOSTICS
        Log.Trace($"[opthl] target '{label}' -> {target.GetType().Name}, bounds {target.Bounds}");

        _target = target;
        _borrowedFrom = target.Background;
        _waitFrames = LAYOUT_WAIT_FRAMES;
        _placed = false;
    }

    /// <summary>Advances the cue, scrolling to the target once it has bounds and then fading it out</summary>
    public void Update()
    {
        if (_target == null)
            return;

        if (!_placed)
        {
            if (!TryPlace(_target))
            {
                // Still unlaid-out. Give up rather than hold the target indefinitely: whatever is
                // wrong with the page won't fix itself, and a stale target keeps a widget alive.
                if (--_waitFrames <= 0)
                    Cancel();

                return;
            }

            _placed = true;
            _startTick = Time.Ticks;

            // TEMP DIAGNOSTICS
            Log.Trace(
                $"[opthl] placed after {LAYOUT_WAIT_FRAMES - _waitFrames} frames, bounds {_target.Bounds}, "
                + $"scroller {(NearestScroller(_target)?.GetType().Name ?? "none")}"
            );
        }

        uint elapsed = Time.Ticks - _startTick;

        if (elapsed >= HOLD_MS + FADE_MS)
        {
            // TEMP DIAGNOSTICS
            Log.Trace("[opthl] done");

            Cancel();
            return;
        }

        _tint.Color = MyraTheme.Current.Notice * AlphaAt(elapsed);
        _target.Background = _tint;
    }

    /// <summary>
    ///     Ends the cue and gives the target its own background back. Safe to call when nothing is
    ///     highlighted, and required before the page is rebuilt.
    /// </summary>
    public void Cancel()
    {
        if (_target == null)
            return;

        _target.Background = _borrowedFrom;

        _target = null;
        _borrowedFrom = null;
        _placed = false;
    }

    #endregion

    #region Private Methods

    /// <summary>
    ///     Scrolls the target into view if it isn't already.
    ///     <para>
    ///         Browser anchor behaviour: something already fully visible is left where it is rather
    ///         than re-centred, and anything else is brought to the top of the viewport.
    ///     </para>
    /// </summary>
    /// <param name="target">The widget to bring into view</param>
    /// <returns>Whether the target was laid out enough to place; false asks to retry next frame</returns>
    private static bool TryPlace(Widget target)
    {
        if (target.Bounds.Height <= 0)
            return false;

        if (NearestScroller(target) is not { } scroller || scroller.Bounds.Height <= 0)
            return true;

        // ToGlobal, not ActualBounds: Myra's Bounds are local - (0, 0, width, height) - so screen
        // position only comes out of the widget's transform.
        int offset = target.ToGlobal(Point.Zero).Y - scroller.ToGlobal(Point.Zero).Y;

        if (offset >= 0 && offset + target.Bounds.Height <= scroller.Bounds.Height)
            return true;

        Point position = scroller.ScrollPosition;

        scroller.ScrollPosition = new Point(
            position.X,
            Math.Clamp(position.Y + offset - SCROLL_MARGIN, 0, Math.Max(scroller.ScrollMaximum.Y, 0))
        );

        return true;
    }

    /// <summary>
    ///     The scroller the target actually sits in, found by walking up rather than being handed in:
    ///     a page's content may be scrolled by its own viewer or by the window's.
    /// </summary>
    private static ScrollViewer? NearestScroller(Widget target)
    {
        for (Widget? widget = target.Parent; widget != null; widget = widget.Parent)
        {
            if (widget is ScrollViewer scroller)
                return scroller;
        }

        return null;
    }

    /// <summary>
    ///     The setting named <paramref name="label" />.
    ///     <para>
    ///         Matched on the option's own name rather than on any caption inside it:
    ///         <see cref="OptionItem.SearchText" /> and <see cref="CheckBoxGroup.PrimaryLabel" /> are
    ///         the very strings the index was built from, so the comparison is exact. A rendered
    ///         caption is not - an editor may draw its name itself, decorate it, or not draw one.
    ///     </para>
    ///     <para>
    ///         Matched by name rather than by identity because the indexing pass and the render pass
    ///         walk different object graphs: a tab's content factory is invoked separately for each,
    ///         so there is no shared handle to carry from one to the other. Duplicate names on one
    ///         page resolve to the first, which is still a setting by that name.
    ///     </para>
    /// </summary>
    private static Widget? FindTarget(Widget pageRoot, string label)
    {
        Widget? captionMatch = null;

        foreach (Widget descendant in pageRoot.Descendants())
        {
            switch (descendant)
            {
                case OptionItem item when string.Equals(item.SearchText, label, StringComparison.Ordinal):
                    return item;

                case CheckBoxGroup group when string.Equals(group.PrimaryLabel, label, StringComparison.Ordinal):
                    return group;

                // Kept as a fallback for a widget built outside the option factories, but only used
                // if nothing names itself - an owner is always the better target than its text.
                case Label caption when captionMatch == null
                                        && string.Equals(caption.Text, label, StringComparison.Ordinal):
                    captionMatch = caption;
                    break;
            }
        }

        return captionMatch == null ? null : EnclosingOption(captionMatch);
    }

    /// <summary>
    ///     The row a caption belongs to, so the tint covers the setting and its editor rather than
    ///     just its text. Falls back to the caption for anything not built by the option factories.
    /// </summary>
    private static Widget EnclosingOption(Widget caption)
    {
        for (Widget? widget = caption; widget != null; widget = widget.Parent)
        {
            if (widget is OptionItem or CheckBoxGroup)
                return widget;
        }

        return caption;
    }

    /// <summary>Full tint while the eye arrives, then off quickly enough not to linger as decoration</summary>
    private static float AlphaAt(uint elapsedMs)
    {
        if (elapsedMs <= HOLD_MS)
            return PEAK_ALPHA;

        float remaining = 1f - (elapsedMs - HOLD_MS) / (float)FADE_MS;

        return PEAK_ALPHA * remaining * remaining;
    }

    #endregion
}
