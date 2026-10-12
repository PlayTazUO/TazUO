#nullable enable

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility.Platforms;
using Myra.Events;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.Controls;

/// <summary>
///     A help button that, when pressed, either shows a widget in an <see cref="InfoModal" /> or
///     opens a URL in the player's browser, per the constructor used.
///     <para>
///         Seated in a <see cref="ResizableComponents.ResizableWindow" />'s title bar through
///         <see cref="ResizableComponents.ResizableWindow.Help" />, which owns it. The owner must call
///         <see cref="CloseModal" /> when done: the modal is registered with <c>UIManager</c> in its
///         own right, so nothing else dismisses it.
///     </para>
/// </summary>
public class HelpButton : Widget
{
    #region Public accessors

    /// <summary>
    ///     Configuration handed to the help modal: its chrome, placement and size limits. Edit it to
    ///     shape the dialog; ignored by help built in URL mode, which opens no dialog.
    /// </summary>
    public InfoModalProps ModalProps { get; } = new();

    #endregion

    #region Private members

    private readonly string? _helpUrl;
    private readonly Widget? _content;
    private readonly string? _contentModalTitle;

    private InfoModal? _helpModal;

    #endregion

    #region Ctor

    /// <summary>
    ///     Builds help that shows a widget, behind a caller-supplied button.
    /// </summary>
    /// <param name="helpButton">The widget to show as the button. Sized and styled by the caller.</param>
    /// <param name="content">The widget to show as the modal's body.</param>
    /// <param name="contentModalTitle">The modal's title, or null for a bare title bar.</param>
    /// <exception cref="ArgumentNullException">Either widget is null.</exception>
    public HelpButton(Widget helpButton, Widget content, string? contentModalTitle = null) : this(helpButton, content, contentModalTitle, null)
    {
        ArgumentNullException.ThrowIfNull(content);
    }

    /// <summary>
    ///     Builds help that shows a widget, behind the standard question-mark button.
    /// </summary>
    /// <param name="content">The widget to show as the modal's body.</param>
    /// <param name="contentModalTitle">The modal's title, or null for a bare title bar.</param>
    /// <exception cref="ArgumentNullException"><paramref name="content" /> is null.</exception>
    public HelpButton(Widget content, string? contentModalTitle = null) : this(GetDefaultButtonWidget(), content, contentModalTitle, null)
    {
        ArgumentNullException.ThrowIfNull(content);
    }

    /// <summary>
    ///     Builds help that opens a URL in the player's browser, behind the standard question-mark
    ///     button. No modal is involved.
    /// </summary>
    /// <param name="url">The address to open. A scheme is worth including: without one,
    /// <see cref="PlatformHelper.LaunchBrowser" /> logs the address as malformed before retrying it
    /// under https.</param>
    /// <exception cref="ArgumentNullException"><paramref name="url" /> is null.</exception>
    public HelpButton(string url) : this(GetDefaultButtonWidget(), null, null, url)
    {
        ArgumentNullException.ThrowIfNull(url);
    }

    /// <summary>
    ///     The single initialization path: seats the button as this widget's only child, so every
    ///     overload gets the same layout regardless of which of the content/URL modes it selects.
    /// </summary>
    /// <param name="helpButton">The widget to show as the button.</param>
    /// <param name="content">The modal body, or null for URL mode.</param>
    /// <param name="contentModalTitle">The modal's title. Meaningless in URL mode.</param>
    /// <param name="helpUrl">The address to open, or null for modal mode.</param>
    private HelpButton(Widget helpButton, Widget? content, string? contentModalTitle, string? helpUrl)
    {
        ArgumentNullException.ThrowIfNull(helpButton);

        _content = content;
        _contentModalTitle = contentModalTitle;
        _helpUrl = helpUrl;

        ChildrenLayout = new SingleItemLayout<Widget>(this) { Child = helpButton };
    }

    #endregion

    #region Public methods

    /// <summary>
    ///     Dismisses the help modal, if one is open. Required on teardown - nothing else disposes it,
    ///     so it would outlive the control it documents.
    /// </summary>
    public void CloseModal()
    {
        if (_helpModal is { IsDisposed: false, IsClosing: false })
            _helpModal.Dispose();

        _helpModal = null;
    }

    /// <summary>
    ///     Opens the help on a left press.
    /// </summary>
    /// <param name="args">Event data describing the press.</param>
    public override void OnTouchDown(TouchEventArgs args)
    {
        // Myra reports a touch-down for every button, and the client closes gumps on right-click -
        // opening help on one would fire alongside that close.
        if (args.Button == TouchButton.Left)
            ShowHelp();

        base.OnTouchDown(args);
    }

    #endregion

    #region Private methods

    /// <summary>
    ///     The standard help button: a question mark, sized with the other title-bar glyphs.
    /// </summary>
    /// <returns>The button. Its click handler is empty - the press is read on the parent, which
    /// covers the same area.</returns>
    private static IconButton GetDefaultButtonWidget() => new("❓", () => { }, TazLang.Get("uicommons_help_button"));

    /// <summary>
    ///     Shows the help: the browser in URL mode, otherwise the modal - raising the one already
    ///     open rather than stacking a second.
    /// </summary>
    private void ShowHelp()
    {
        if (_content == null)
        {
            if (_helpUrl != null)
                PlatformHelper.LaunchBrowser(_helpUrl);
            return;
        }

        switch (_helpModal)
        {
            // A modal mid-dismissal still holds the content widget until its deferred teardown runs.
            // Handing that widget to a replacement would leave it in both widget trees, and the dying
            // one nulls its desktop on the way out - killing the replacement with it. So the press is
            // dropped; the next one finds a clean slate.
            case { IsClosing: true }:
                return;
            // A dismissed modal cannot be shown again, so only a live one is worth raising.
            case { IsDisposed: false }:
            {
                if (ModalProps.Placement?.Invoke() is { } placement)
                    _helpModal.SetPosition(placement.X, placement.Y);

                _helpModal.BringOnTop();
                return;
            }
            default:
                _helpModal = new InfoModal(_contentModalTitle, _content, ModalProps);
                _helpModal.Show();
                break;
        }
    }

    #endregion
}
