using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Input;
using FontStashSharp;
using FontStashSharp.RichText;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.Controls.ResizableComponents;

#region Auxiliary Classes

public class ResizableWindowProps : MyraCommonProps
{
    public ResizeBehavior Resize { get; set; } = new();
    public bool Minimizable { get; set; } = true;
}

#endregion

public class ResizableWindow : Window, IDisposable
{
    #region Events

    public event EventHandler<ResizeEventArgs> Resized;

    #endregion

    #region Accessors

    /// <summary>
    /// Gets the properties that define the behavior and configuration
    /// of the resizable window, including resize behavior and minimization settings.
    /// </summary>
    public ResizableWindowProps Props { get; }

    /// <summary>
    /// Gets a value indicating whether the current instance has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the window is currently minimized.
    /// When minimized, the window's content is hidden, and its size settings are temporarily overridden.
    /// </summary>
    public bool IsMinimized { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the window is currently resizable.
    /// </summary>
    /// <remarks>
    /// A window is considered resizable if resizing is enabled via the configuration
    /// properties and the window is not minimized.
    /// </remarks>
    public bool IsCurrentlyResizable => Props.Resize.Enabled && !IsMinimized;

    /// <summary>
    /// The window's title text
    /// </summary>
    [Category("Appearance")]
    public new string Title
    {
        get => base.Title;

        set
        {
            base.Title = value;
            _titlePanelFullWidth = TitlePanel.Measure(new Point(2000, 2000)).X;
            UpdateTitleLabelVisibility();
        }
    }

    private string MinMaxButtonText => IsMinimized ? "□" : "−";

    private SpriteFontBase MinMaxButtonFont => IsMinimized
        ? TrueTypeLoader.Instance.GetFont(EmbeddedFontNames.NOTO_SANS_2_SYMBOLS, 24)
        : TrueTypeLoader.Instance.GetFont(EmbeddedFontNames.NOTO_SANS_2_SYMBOLS, 32);

    #endregion

    #region Members

    private ResizeEdges? _activeResizeEdge;

    private Point _resizeStartMouse;
    private int _resizeStartLeft;
    private int _resizeStartTop;
    private int _resizeStartWidth;
    private int _resizeStartHeight;

    private int? _restoreWidth;
    private int? _restoreHeight;
    private bool _restoreAutoWidth;
    private bool _restoreAutoHeight;

    private bool _isOverridingCursorStyle;

    private int _titlePanelFullWidth;

    #region Components

    private Widget _minMaxButton;
    private MyraLabel _minMaxButtonLabel;

    private Widget _resetSizeButton;

    private Widget _content;

    #endregion

    #endregion

    #region Constructors

    /// <summary>
    ///     Initializes a new instance of the <see cref="ResizableWindow" /> class.
    /// </summary>
    /// <param name="props">Properties defining initial window state and resize behavior.</param>
    public ResizableWindow(ResizableWindowProps props = null)
    {
        Props = props ?? new ResizableWindowProps();
        Configure();
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Minimizes the window to its title bar only.
    /// </summary>
    public void Minimize()
    {
        if (IsMinimized)
            return;

        _restoreAutoWidth = !Width.HasValue;
        _restoreAutoHeight = !Height.HasValue;
        _restoreWidth = Width ?? Bounds.Width;
        _restoreHeight = Height ?? Bounds.Height;
        Width = null;
        Height = null;

        IsMinimized = true;
        UpdateMinMaxButtonLabel();
        // Using 'Visible' to hide causes some padding to remain. This is a bit of an ugly workaround.
        base.Content = null;
        InvalidateMeasure();
    }

    /// <summary>
    ///     Restores the window to its previous size after being minimized.
    ///     Does nothing when called without a previous call to minimize.
    /// </summary>
    public void Maximize()
    {
        if (!IsMinimized)
            return;

        IsMinimized = false;
        Width = _restoreAutoWidth ? null : _restoreWidth;
        Height = _restoreAutoHeight ? null : _restoreHeight;

        _restoreWidth = 0;
        _restoreHeight = 0;

        UpdateMinMaxButtonLabel();
        base.Content = _content;
        InvalidateMeasure();
    }

    /// <summary>
    ///     A handler that should be called by the UI I.S when the window loses focus, stopping any active resizing.
    ///     This is necessary due to the lack of built-in support at the Myra level.
    /// </summary>
    public void OnFocusLost()
    {
        if (_activeResizeEdge.HasValue)
            OnDragStop(null, EventArgs.Empty);

        StopOverridingCursorStyle();
    }

    /// <summary>
    ///     Disposes of the window resources and unregisters mouse event handlers.
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed)
            return;

        Mouse.Moved -= OnMouseMovedWhileInWindow;
        Mouse.LeftButtonClickStateChanged -= LeftClickChangedHandler;
        Mouse.Moved -= OnMouseMovedWhileResizing;
        TitlePanel?.TouchDoubleClick -= OnMinMaxButtonClick;
        _minMaxButton?.TouchDown -= OnMinMaxButtonClick;
        _resetSizeButton?.TouchDown -= OnResetSizeButtonClick;
        StopOverridingCursorStyle();

        IsDisposed = true;
        GC.SuppressFinalize(this);
    }

    #region Overrides

    /// <summary>
    ///     Gets or sets the content of the window
    /// </summary>
    public override Widget Content
    {
        get => base.Content;
        set
        {
            if (value == null)
            {
                _content = null;
                base.Content = null;
                return;
            }

            Widget component = Props.Resize.ScrollerMode == ScrollViewerMode.None
                ? value
                : WrapWithScrollViewer(value);

            _content = component;

            // If we're currently minimized, just keep track of the new content via _content but don't replace the base.Content.
            // This way we remain minimized but maximize will display the new content.
            if (!IsMinimized)
            {
                base.Content = component;
                InvalidateMeasure();
            }
        }
    }

    /// <summary>
    ///     Handles mouse leave events, resetting the cursor style if necessary.
    /// </summary>
    public override void OnMouseLeft()
    {
        base.OnMouseLeft();
        Mouse.Moved -= OnMouseMovedWhileInWindow;

        // Stop overriding cursor style if the mouse has left the window, and we're not currently resizing
        if (_isOverridingCursorStyle && !_activeResizeEdge.HasValue)
            StopOverridingCursorStyle();
    }

    /// <summary>
    ///     Initiates a resize operation if the user clicks on a resize handle.
    /// </summary>
    public override void OnTouchDown()
    {
        // To preserve normal Myra window behavior, we have to consider whether the window is in front.
        // If not, the click is 'directed' to bringing-to-front rather than dragging
        Widget[] widgets = (Parent != null ? Parent.GetChildren() : Desktop.Widgets).ToArray();
        if (widgets[^1] != this)
        {
            // The base cals BringToFront so we just let it take control here.
            base.OnTouchDown();
            // If we ever want the visual cursor style changed right after focusing, we can add it here.
            // Just a nitpick.
            return;
        }

        if (!IsCurrentlyResizable)
            return;

        _activeResizeEdge = GetResizerUnderCursor();
        if (!_activeResizeEdge.HasValue)
            return;

        _resizeStartMouse = Mouse.Position;
        _resizeStartLeft = Left;
        _resizeStartTop = Top;
        _resizeStartWidth = Width ?? Bounds.Width;
        _resizeStartHeight = Height ?? Bounds.Height;

        Mouse.LeftButtonClickStateChanged += LeftClickChangedHandler;
        Mouse.Moved += OnMouseMovedWhileResizing;
    }

    /// <summary>
    ///     Handles touch-up events, stopping any active dragging or resizing.
    /// </summary>
    public override void OnTouchUp()
    {
        base.OnTouchUp();
        OnDragStop(null, EventArgs.Empty);
    }

    /// <summary>
    ///     Closes the window and disposes of its resources.
    /// </summary>
    public override void Close()
    {
        base.Close();
        Dispose();
    }

    /// <summary>
    ///     Handles mouse entry events, registering mouse movement handlers.
    /// </summary>
    public override void OnMouseEntered()
    {
        base.OnMouseEntered();
        Mouse.Moved += OnMouseMovedWhileInWindow;
    }

    #endregion

    #endregion

    #region Private Methods

    /// <summary>
    ///     Configures the window UI components.
    /// </summary>
    private void Configure()
    {
        // The close button is kinda ugly, so we center it manually during construction.
        CloseButton?.VerticalAlignment = VerticalAlignment.Center;
        CloseButton?.Margin = new Thickness(2, 0);

        if (Props.Minimizable)
            ConfigureMinMaxButton();

        if (Props.Resize.Enabled)
            ConfigureResizeResetButton();
    }

    /// <summary>
    ///     Updates the text and font of the minimize/maximize button based on the current state.
    /// </summary>
    private void UpdateMinMaxButtonLabel()
    {
        _minMaxButtonLabel?.Text = MinMaxButtonText;
        _minMaxButtonLabel?.Font = MinMaxButtonFont;
    }

    /// <summary>
    ///     Event handler for the minimize/maximize button click.
    ///     Minimizes or maximizes the window
    /// </summary>
    private void OnMinMaxButtonClick(object _, EventArgs _1)
    {
        if (IsMinimized)
            Maximize();
        else
            Minimize();
    }

    /// <summary>
    ///     Event handler for the reset-window-size button click.
    ///     Resets the window's width/height which causes the window to size itself to fit its current content.
    /// </summary>
    /// <param name="_"></param>
    /// <param name="_1"></param>
    private void OnResetSizeButtonClick(object _, EventArgs _1)
    {
        Width = null;
        Height = null;
    }

    /// <summary>
    ///     Initializes and adds the minimize/maximize button to the title panel.
    /// </summary>
    private void ConfigureMinMaxButton(int index = 0)
    {
        _minMaxButtonLabel = new MyraLabel(MinMaxButtonText, 24)
        {
            Font = MinMaxButtonFont,
            Wrap = false,
            SingleLine = true,
            TextAlign = TextHorizontalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Width = StyleConstantsDefaults.TOOLBAR_BUTTON_SIZE,
            Height = StyleConstantsDefaults.TOOLBAR_BUTTON_SIZE
        };

        _minMaxButton = new Myra.Graphics2D.UI.Button
        {
            Width = StyleConstantsDefaults.TOOLBAR_BUTTON_SIZE,
            Height = StyleConstantsDefaults.TOOLBAR_BUTTON_SIZE,
            Tooltip = Language.Instance.UiCommons.MinMaxWindowButtonTooltip,
            Content = _minMaxButtonLabel,
            VerticalAlignment = VerticalAlignment.Center
        };

        _minMaxButton.TouchDown += OnMinMaxButtonClick;

        TitlePanel.Widgets.Insert(index, _minMaxButton);
        TitlePanel.TouchDoubleClick += OnMinMaxButtonClick;
    }

    /// <summary>
    ///     Initializes and adds the reset-window-size button to the title panel.
    /// </summary>
    private void ConfigureResizeResetButton(int index = 1)
    {
        var label = new MyraLabel(StyleConstantsDefaults.RESET_LABEL_ICON_TEXT, 24)
        {
            Font = TrueTypeLoader.Instance.GetFont(EmbeddedFontNames.NOTO_SANS_2_SYMBOLS, 24),
            Wrap = false,
            SingleLine = true,
            TextAlign = TextHorizontalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Width = StyleConstantsDefaults.TOOLBAR_BUTTON_SIZE,
            Height = StyleConstantsDefaults.TOOLBAR_BUTTON_SIZE
        };

        _resetSizeButton = new Myra.Graphics2D.UI.Button
        {
            Width = StyleConstantsDefaults.TOOLBAR_BUTTON_SIZE,
            Height = StyleConstantsDefaults.TOOLBAR_BUTTON_SIZE,
            Tooltip = Language.Instance.UiCommons.ResetWindowSizeButtonTooltip,
            Content = label,
            VerticalAlignment = VerticalAlignment.Center
        };

        _resetSizeButton.TouchDown += OnResetSizeButtonClick;
        TitlePanel.Widgets.Insert(index, _resetSizeButton);
    }

    /// <summary>
    ///     Wraps a widget with a <see cref="ScrollViewer" />.
    /// </summary>
    /// <param name="widget">The widget to wrap.</param>
    /// <returns>A new <see cref="ScrollViewer" /> containing the widget.</returns>
    private ScrollViewer WrapWithScrollViewer(Widget widget)
    {
        var scroller = new ScrollViewer
        {
            ShowHorizontalScrollBar = (Props.Resize.ScrollerMode & ScrollViewerMode.Horizontal) != 0,
            ShowVerticalScrollBar = (Props.Resize.ScrollerMode & ScrollViewerMode.Vertical) != 0
        };

        // We need a panel here to serve as a sort-of barrier between the scroller and the widget, otherwise we can get superposition.
        var panel = new Panel
        {
            // Note that if the scroll component's height/width changes post-construction, that won't be reflected. Might improve later.
            //Padding = new Thickness(scroller.VerticalScrollbarWidth(), scroller.HorizontalScrollbarHeight())
        };
        panel.Widgets.Add(widget);

        scroller.Content = panel;

        return scroller;
    }

    /// <summary>
    ///     Identifies which resize handle, if any, is currently under the mouse cursor.
    /// </summary>
    /// <returns>The <see cref="ResizeEdges" /> under the cursor, or null if none.</returns>
    private ResizeEdges? GetResizerUnderCursor()
    {
        if (!LocalMousePosition.HasValue)
            return null;

        Point mousePos = LocalMousePosition.Value;
        int radius = (int)Props.Resize.CornerTriggerRadiusPx;
        int radiusSq = radius * radius;

        foreach (ResizeEdges edges in GetEnabledResizeEdges())
            if (IsCursorOverResizer(mousePos, edges, radiusSq))
                return edges;

        return null;
    }

    /// <summary>
    ///     Determines if the cursor is over a specific set of resize edges.
    /// </summary>
    /// <param name="mousePos">Current mouse position.</param>
    /// <param name="edges">Edges to check against.</param>
    /// <param name="cornerTriggerRadiusSquared">Squared radius for corner trigger areas.</param>
    /// <returns>True if the cursor is over the specified resizer area.</returns>
    private bool IsCursorOverResizer(Point mousePos, ResizeEdges edges, int cornerTriggerRadiusSquared)
    {
        int width = Width ?? Bounds.Width;
        int height = Height ?? Bounds.Height;

        int left = Bounds.X;
        int top = Bounds.Y;
        int right = left + width;
        int bottom = top + height;

        bool hasLeft = (edges & ResizeEdges.Left) != 0;
        bool hasRight = (edges & ResizeEdges.Right) != 0;
        bool hasTop = (edges & ResizeEdges.Top) != 0;
        bool hasBottom = (edges & ResizeEdges.Bottom) != 0;

        // Corner handles: circular hit areas around the corner point.
        if (hasLeft && hasTop)
            return IsWithinRadius(mousePos, left, top, cornerTriggerRadiusSquared);

        if (hasRight && hasTop)
            return IsWithinRadius(mousePos, right, top, cornerTriggerRadiusSquared);

        if (hasLeft && hasBottom)
            return IsWithinRadius(mousePos, left, bottom, cornerTriggerRadiusSquared);

        if (hasRight && hasBottom)
            return IsWithinRadius(mousePos, right, bottom, cornerTriggerRadiusSquared);

        // Edge handles: strip along the edge, with thickness based on radius.
        if (hasLeft)
            return IsWithinVerticalEdge(mousePos, left, top, bottom, Props.Resize.EdgeTriggerBandWidthPx);

        if (hasRight)
            return IsWithinVerticalEdge(mousePos, right, top, bottom, Props.Resize.EdgeTriggerBandWidthPx);

        if (hasTop)
            return IsWithinHorizontalEdge(mousePos, top, left, right, Props.Resize.EdgeTriggerBandWidthPx);

        if (hasBottom)
            return IsWithinHorizontalEdge(mousePos, bottom, left, right, Props.Resize.EdgeTriggerBandWidthPx);

        return false;
    }

    /// <summary>
    ///     Checks if a point is within a given radius of another point.
    /// </summary>
    /// <param name="mousePos">Point to check.</param>
    /// <param name="x">Center X coordinate.</param>
    /// <param name="y">Center Y coordinate.</param>
    /// <param name="radiusSq">Squared radius.</param>
    /// <returns>True if the point is within the radius.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWithinRadius(Point mousePos, int x, int y, int radiusSq)
    {
        int dx = mousePos.X - x;
        int dy = mousePos.Y - y;
        return dx * dx + dy * dy <= radiusSq;
    }

    /// <summary>
    ///     Checks if a point is within a vertical edge hit area.
    /// </summary>
    /// <param name="mousePos">Point to check.</param>
    /// <param name="edgeX">X coordinate of the edge.</param>
    /// <param name="top">Top Y coordinate of the edge.</param>
    /// <param name="bottom">Bottom Y coordinate of the edge.</param>
    /// <param name="edgeStripWidth">Thickness of the hit area.</param>
    /// <returns>True if the point is within the edge area.</returns>
    private static bool IsWithinVerticalEdge(Point mousePos, int edgeX, int top, int bottom, uint edgeStripWidth) =>
        mousePos.X >= edgeX - edgeStripWidth
        && mousePos.X <= edgeX + edgeStripWidth
        && mousePos.Y >= top - edgeStripWidth
        && mousePos.Y <= bottom + edgeStripWidth;

    /// <summary>
    ///     Checks if a point is within a horizontal edge hit area.
    /// </summary>
    /// <param name="mousePos">Point to check.</param>
    /// <param name="edgeY">Y coordinate of the edge.</param>
    /// <param name="left">Left X coordinate of the edge.</param>
    /// <param name="right">Right X coordinate of the edge.</param>
    /// <param name="edgeStripWidth">Thickness of the hit area.</param>
    /// <returns>True if the point is within the edge area.</returns>
    private static bool IsWithinHorizontalEdge(Point mousePos, int edgeY, int left, int right, uint edgeStripWidth) =>
        mousePos.Y >= edgeY - edgeStripWidth
        && mousePos.Y <= edgeY + edgeStripWidth
        && mousePos.X >= left - edgeStripWidth
        && mousePos.X <= right + edgeStripWidth;

    /// <summary>
    ///     Returns an enumerable of all enabled resize edge combinations.
    /// </summary>
    /// <returns>An enumeration of <see cref="ResizeEdges" />.</returns>
    private IEnumerable<ResizeEdges> GetEnabledResizeEdges()
    {
        ResizeEdges placements = Props.Resize.Placements;

        if ((placements & ResizeEdges.Left) != 0 && (placements & ResizeEdges.Top) != 0)
            yield return ResizeEdges.Left | ResizeEdges.Top;

        if ((placements & ResizeEdges.Right) != 0 && (placements & ResizeEdges.Top) != 0)
            yield return ResizeEdges.Right | ResizeEdges.Top;

        if ((placements & ResizeEdges.Left) != 0 && (placements & ResizeEdges.Bottom) != 0)
            yield return ResizeEdges.Left | ResizeEdges.Bottom;

        if ((placements & ResizeEdges.Right) != 0 && (placements & ResizeEdges.Bottom) != 0)
            yield return ResizeEdges.Right | ResizeEdges.Bottom;

        if ((placements & ResizeEdges.Left) != 0)
            yield return ResizeEdges.Left;

        if ((placements & ResizeEdges.Right) != 0)
            yield return ResizeEdges.Right;

        if ((placements & ResizeEdges.Top) != 0)
            yield return ResizeEdges.Top;

        if ((placements & ResizeEdges.Bottom) != 0)
            yield return ResizeEdges.Bottom;
    }

    /// <summary>
    ///     Called when the mouse hovers over a 'resize handle'.
    ///     Sets the cursor to the dragging (grabby hand) style
    /// </summary>
    private void OnHoverEnterOverDrag()
    {
        Client.Game.UO.GameCursor.ForceSetCursorVisualStyle(GameCursorVisualType.Dragging);
        _isOverridingCursorStyle = true;
    }

    /// <summary>
    ///     Updates the cursor style as it moves within the window based on whether it is over a resize handle.
    /// </summary>
    private void OnMouseMovedWhileInWindow(object _, MouseMovedEventArgs e)
    {
        // Resize is disabled when the window is minimized, so no need to even check the cursor position.
        if (!IsCurrentlyResizable)
            return;

        bool isOnHandle = GetResizerUnderCursor() != null;

        switch (isOnHandle)
        {
            case true when !_isOverridingCursorStyle:
                OnHoverEnterOverDrag();
                break;
            case false when _isOverridingCursorStyle:
                StopOverridingCursorStyle();
                break;
        }
    }

    /// <summary>
    ///     Processes window resizing as the mouse moves.
    /// </summary>
    private void OnMouseMovedWhileResizing(object _, MouseMovedEventArgs e)
    {
        if (!_activeResizeEdge.HasValue || !Mouse.LButtonPressed)
        {
            Mouse.Moved -= OnMouseMovedWhileResizing;
            return;
        }

        ResizeEdges edges = _activeResizeEdge.Value;
        Rectangle newBounds = CalculateNewBounds(edges);

        if (newBounds.X == Left &&
            newBounds.Y == Top &&
            newBounds.Width == (Width ?? Bounds.Width) &&
            newBounds.Height == (Height ?? Bounds.Height))
            return;

        Left = newBounds.X;
        Top = newBounds.Y;
        Width = newBounds.Width;
        Height = newBounds.Height;

        UpdateTitleLabelVisibility();

        Resized?.Invoke(this, new ResizeEventArgs { NewWidth = newBounds.Width, NewHeight = newBounds.Height });
    }

    private void UpdateTitleLabelVisibility()
    {
        if (!Width.HasValue && Bounds.Width <= 0)
        {
            // If we've no size, fallback to displaying the title
            _titleLabel.Visible = true;
            return;
        }

        // Otherwise, we can allow the label text to span under the button, but beyond that, it starts looking broken,
        // so we hide it.
        int closeButtonWidth = CloseButton?.Visible == true
            ? Math.Max(0, CloseButton.Measure(new Point(2000, 2000)).X - (CloseButton.Margin.Right + CloseButton.Padding.Right))
            : 0;

        // Since the window may be resized to any size, we need to ensure the title label does not overflow;
        // Myra does not handle this gracefully, not even with ellipsis.
        _titleLabel.Visible = _titlePanelFullWidth - closeButtonWidth <= (Width ?? Bounds.Width);
    }

    /// <summary>
    ///     Calculates the new window bounds based on mouse delta and active resize edges.
    /// </summary>
    /// <param name="resizeEdges">The edges currently being dragged.</param>
    /// <returns>The calculated <see cref="Rectangle" /> bounds.</returns>
    private Rectangle CalculateNewBounds(ResizeEdges resizeEdges)
    {
        Point delta = Mouse.Position - _resizeStartMouse;

        int newX = _resizeStartLeft;
        int newY = _resizeStartTop;
        int newWidth = _resizeStartWidth;
        int newHeight = _resizeStartHeight;

        if ((resizeEdges & ResizeEdges.Left) != 0)
        {
            newX = _resizeStartLeft + delta.X;
            newWidth = _resizeStartWidth - delta.X;
        }
        else if ((resizeEdges & ResizeEdges.Right) != 0)
            newWidth = _resizeStartWidth + delta.X;

        if ((resizeEdges & ResizeEdges.Top) != 0)
        {
            newY = _resizeStartTop + delta.Y;
            newHeight = _resizeStartHeight - delta.Y;
        }
        else if ((resizeEdges & ResizeEdges.Bottom) != 0)
            newHeight = _resizeStartHeight + delta.Y;

        newWidth = Math.Clamp(newWidth, Props.Resize.MinWidth, Props.Resize.MaxWidth);
        newHeight = Math.Clamp(newHeight, Props.Resize.MinHeight, Props.Resize.MaxHeight);

        if ((resizeEdges & ResizeEdges.Left) != 0)
            newX = _resizeStartLeft + (_resizeStartWidth - newWidth);

        if ((resizeEdges & ResizeEdges.Top) != 0)
            newY = _resizeStartTop + (_resizeStartHeight - newHeight);

        return new Rectangle(newX, newY, newWidth, newHeight);
    }

    /// <summary>
    ///     Handles left mouse button click state changes during a drag/resize operation.
    /// </summary>
    private void LeftClickChangedHandler(object _, MouseLeftButtonClickStateChangedEventArgs e)
    {
        if (!e.Current && _activeResizeEdge.HasValue)
            OnDragStop(null, EventArgs.Empty);
    }

    /// <summary>
    ///     Stops the current drag/resize operation.
    /// </summary>
    private void OnDragStop(object _, EventArgs _1)
    {
        _activeResizeEdge = null;
        Mouse.LeftButtonClickStateChanged -= LeftClickChangedHandler;
        Mouse.Moved -= OnMouseMovedWhileResizing;

        // For the uncommon case where the dragging stops outside the window boundaries
        if (_isOverridingCursorStyle && !_activeResizeEdge.HasValue)
            StopOverridingCursorStyle();
    }

    /// <summary>
    ///     Resets the cursor style to default and clears the override flag.
    /// </summary>
    private void StopOverridingCursorStyle()
    {
        if (!_isOverridingCursorStyle)
            return;

        Client.Game.UO.GameCursor.ForceSetCursorVisualStyle(null);
        _isOverridingCursorStyle = false;
    }

    #endregion
}
