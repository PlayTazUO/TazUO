#nullable enable

using System.ComponentModel;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

/// <summary>
///     A dialog presenting read-only content, dismissed by right-clicking it or through its close
///     button. Drag-resizable within the limits on <see cref="Props" />, which also decide whether
///     the close and minimize buttons are offered; the body scrolls once it outgrows the frame.
///     <para>
///         Takes ownership of the content widget: it is attached to this window's desktop and detached
///         when the modal is disposed, so the same widget cannot back two live modals at once. A
///         dismissed modal is disposed and cannot be shown again - callers that re-open on demand must
///         construct a new instance, checking <see cref="MyraControl.IsDisposed" /> and
///         <see cref="MyraControl.IsClosing" /> on any cached one.
///     </para>
/// </summary>
public class InfoModal : MyraControl
{
    #region Public accessors

    /// <summary>
    ///     Chrome, placement and size limits of this dialog. Edits apply immediately.
    /// </summary>
    public InfoModalProps Props { get; }

    #endregion

    #region Ctor

    /// <param name="title">Window title bar text, or null for a bare title bar.</param>
    /// <param name="modalContent">Widget rendered as the dialog body.</param>
    /// <param name="props">Configuration to adopt, or null for the defaults. Held by reference, so
    /// the caller may keep editing it.</param>
    public InfoModal(string? title, Widget modalContent, InfoModalProps? props = null) : base(title)
    {
        Props = props ?? new InfoModalProps();
        Props.PropertyChanged += OnPropsChanged;

        ConfigureRootWindow();
        SetRootContent(GetContent(modalContent));
    }

    #endregion

    #region Public methods

    /// <summary>
    ///     Registers the modal with <see cref="UIManager" /> and places it per
    ///     <see cref="InfoModalProps.Placement" />.
    /// </summary>
    /// <remarks>
    ///     <see cref="UIManager" /> owns a <see cref="MyraControl" />'s visibility and drives its
    ///     update and draw passes. Myra's own <c>Window.Show</c>/<c>ShowModal</c> must not be used:
    ///     the root window is already this control's desktop root, and showing it again appends a
    ///     duplicate desktop entry. Call once per instance - a second call duplicates the
    ///     <see cref="UIManager" /> registration.
    /// </remarks>
    public void Show()
    {
        Point? placement = Props.Placement?.Invoke();

        if (placement != null)
            SetPosition(placement.Value.X, placement.Value.Y);
        else
            CenterInScreen();

        UIManager.Add(this);
    }

    /// <summary>
    ///     Requests teardown, dropping this dialog's subscription to <see cref="Props" /> - which the
    ///     caller may well outlive it and keep using.
    /// </summary>
    public override void Dispose()
    {
        Props.PropertyChanged -= OnPropsChanged;
        base.Dispose();
    }

    #endregion

    #region Private methods

    /// <summary>
    ///     Applies the settings that do not change with <see cref="Props" />: a centered title that
    ///     stretches the bar's full width.
    /// </summary>
    private void ConfigureRootWindow()
    {
        _rootWindow.TitlePanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        _rootWindow.TitlePanel.VerticalAlignment = VerticalAlignment.Center;
        _rootWindow.TitleLabelAlignment = HorizontalAlignment.Center;

        Configure();
    }

    /// <summary>
    ///     Brings the window in line with <see cref="Props" />.
    /// </summary>
    private void Configure()
    {
        _rootWindow.CloseButton.Visible = Props.Closable;
        _rootWindow.Props.Minimizable = Props.Minimizable;

        ApplySizeLimits();
    }

    /// <summary>
    ///     Pushes the size limits onto both the drag-resize clamp and the window's own layout limits.
    /// </summary>
    /// <remarks>
    ///     Both halves, for different reasons: the clamp bounds a drag, the layout limits bound what
    ///     the window measures itself to. Only the former leaves the un-dragged dialog unbounded.
    /// </remarks>
    private void ApplySizeLimits()
    {
        (Point min, Point max) = Props.GetOrderedSizeLimits();

        _rootWindow.Props.Resize.MinWidth = min.X;
        _rootWindow.Props.Resize.MinHeight = min.Y;
        _rootWindow.Props.Resize.MaxWidth = max.X;
        _rootWindow.Props.Resize.MaxHeight = max.Y;

        _rootWindow.MinWidth = min.X;
        _rootWindow.MinHeight = min.Y;
        _rootWindow.MaxWidth = max.X;
        _rootWindow.MaxHeight = max.Y;
    }

    /// <summary>
    ///     Wraps the body widget in a stretching panel, so it fills the dialog's width rather than
    ///     sitting at its natural size.
    /// </summary>
    /// <param name="modalContent">The body widget.</param>
    /// <returns>The panel to use as the window's content.</returns>
    private static VerticalStackPanel GetContent(Widget modalContent)
    {
        var content = new VerticalStackPanel { HorizontalAlignment = HorizontalAlignment.Stretch };
        content.Widgets.Add(modalContent);
        return content;
    }

    /// <summary>Reconfigures the window when <see cref="Props" /> changes.</summary>
    /// <param name="sender">Unused event sender.</param>
    /// <param name="e">Unused event data - every property feeds the same reconfigure.</param>
    private void OnPropsChanged(object? sender, PropertyChangedEventArgs e) => Configure();

    #endregion
}
