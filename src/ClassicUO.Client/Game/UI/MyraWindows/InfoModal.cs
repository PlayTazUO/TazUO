#nullable enable

using System;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

/// <summary>
///     A dialog that presents read-only content and is dismissed by right-clicking it. The window has
///     no close button and cannot be minimized, but it is drag-resizable within
///     <see cref="MinSize" />/<see cref="MaxSize" />, and its content scrolls once the body outgrows
///     the frame.
///     <para>
///         Takes ownership of the content widget: it is attached to this window's desktop and detached
///         when the modal is disposed, so the same widget cannot back two live modals at once. A
///         dismissed modal is disposed and cannot be shown again - callers that re-open on demand must
///         construct a new instance, checking <see cref="MyraControl.IsDisposed" /> on any cached one.
///     </para>
/// </summary>
public class InfoModal : MyraControl
{
    #region Public accessors

    /// <summary>
    ///     Smallest size the dialog may be dragged down to. Also its layout minimum, so a short body
    ///     still produces a dialog wide enough to read a title in.
    /// </summary>
    public Point MinSize
    {
        get;
        set
        {
            field = value;
            ApplySizeLimits();
        }
    } = new(DEFAULT_MIN_WIDTH, DEFAULT_MIN_HEIGHT);

    /// <summary>
    ///     Largest size the dialog may take. Load-bearing rather than cosmetic: the window sizes
    ///     itself to its content, so without a ceiling a long help text yields a dialog taller than
    ///     the screen, with the parts that would close it off-screen.
    /// </summary>
    public Point MaxSize
    {
        get;
        set
        {
            field = value;
            ApplySizeLimits();
        }
    } = new(DEFAULT_MAX_WIDTH, DEFAULT_MAX_HEIGHT);

    #endregion

    #region Private members

    /// <summary>Wide enough for a title and a line of prose; taller than a single line so a short
    /// body does not collapse into a strip.</summary>
    private const int DEFAULT_MIN_WIDTH = 300;
    private const int DEFAULT_MIN_HEIGHT = 120;

    /// <summary>Fits within the smallest resolution the client is expected to run at, so the dialog
    /// cannot grow past the viewport on any supported display.</summary>
    private const int DEFAULT_MAX_WIDTH = 700;
    private const int DEFAULT_MAX_HEIGHT = 600;

    #endregion

    #region Ctor

    /// <param name="title">Window title bar text, or null for a bare title bar.</param>
    /// <param name="modalContent">Widget rendered as the dialog body.</param>
    public InfoModal(string? title, Widget modalContent) : base(title)
    {
        ConfigureRootWindow();
        SetRootContent(GetContent(modalContent));
    }

    #endregion

    #region Public methods

    /// <summary>
    ///     Registers the modal with <see cref="UIManager" /> and places it.
    /// </summary>
    /// <param name="placement">Top-left corner, in screen coordinates. Null centers the dialog.</param>
    /// <remarks>
    ///     Visibility of a <see cref="MyraControl" /> is owned by <see cref="UIManager" />, which drives
    ///     its update and draw passes - Myra's own <c>Window.Show</c>/<c>ShowModal</c> must not be used
    ///     here, as the root window is already this control's desktop root and showing it again appends a
    ///     duplicate desktop entry. Call once per instance: a second call would duplicate the
    ///     <see cref="UIManager" /> registration instead.
    /// </remarks>
    public void Show(Point? placement)
    {
        if (placement != null)
            SetPosition(placement.Value.X, placement.Value.Y);
        else
            CenterInScreen();

        UIManager.Add(this);
    }

    #endregion

    #region Private methods

    /// <summary>
    ///     Applies modal-specific window settings: centered title, hidden close button, no minimize,
    ///     and the size limits the dialog resizes within.
    /// </summary>
    private void ConfigureRootWindow()
    {
        _rootWindow.TitlePanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        _rootWindow.TitlePanel.VerticalAlignment = VerticalAlignment.Center;
        _rootWindow.TitleLabelAlignment = HorizontalAlignment.Center;

        _rootWindow.CloseButton.Visible = false;
        _rootWindow.Props.Minimizable = false;

        ApplySizeLimits();
    }

    /// <summary>
    ///     Pushes <see cref="MinSize" />/<see cref="MaxSize" /> onto both the drag-resize clamp and
    ///     the window's own layout limits.
    /// </summary>
    /// <remarks>
    ///     Both halves are needed, and for different reasons: the resize clamp bounds what a drag can
    ///     do, while the layout limits bound what the window sizes itself to when it measures its
    ///     content. Setting only the former leaves the initial, un-dragged dialog unbounded.
    ///     <para>
    ///         The two are written as an ordered pair, treating the minimum as the hard floor and
    ///         lifting the ceiling to meet it where a caller has set them crossed. That is not
    ///         cosmetic tidying: the drag path clamps through <see cref="Math.Clamp(int,int,int)" />,
    ///         which throws when its bounds cross, and would do so on every mouse-move of a resize.
    ///     </para>
    /// </remarks>
    private void ApplySizeLimits()
    {
        int minWidth = Math.Max(0, MinSize.X);
        int minHeight = Math.Max(0, MinSize.Y);
        int maxWidth = Math.Max(minWidth, MaxSize.X);
        int maxHeight = Math.Max(minHeight, MaxSize.Y);

        _rootWindow.Props.Resize.MinWidth = minWidth;
        _rootWindow.Props.Resize.MinHeight = minHeight;
        _rootWindow.Props.Resize.MaxWidth = maxWidth;
        _rootWindow.Props.Resize.MaxHeight = maxHeight;

        _rootWindow.MinWidth = minWidth;
        _rootWindow.MinHeight = minHeight;
        _rootWindow.MaxWidth = maxWidth;
        _rootWindow.MaxHeight = maxHeight;
    }

    /// <summary>
    ///     Wraps the caller-supplied body widget in a stretching panel, so it fills the dialog's
    ///     width rather than sitting at its natural size.
    /// </summary>
    /// <param name="modalContent">The body widget.</param>
    /// <returns>The panel to use as the window's content.</returns>
    private static VerticalStackPanel GetContent(Widget modalContent)
    {
        var content = new VerticalStackPanel { HorizontalAlignment = HorizontalAlignment.Stretch };
        content.Widgets.Add(modalContent);
        return content;
    }

    #endregion
}
