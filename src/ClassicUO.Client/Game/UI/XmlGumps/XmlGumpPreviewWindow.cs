#nullable enable
using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using Myra.Events;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI;

/// <summary>
/// The floating preview window of the XML gump editor: hosts the canvas on its own, separate from the
/// editor's element list and property panel so the two can be moved and resized independently.
/// </summary>
/// <remarks>
/// Lifetime is tied to the editor: closing this closes the editor and vice versa. The canvas belongs to
/// the editor, so the callback only ever fires back the one way.
/// </remarks>
public sealed class XmlGumpPreviewWindow : MyraControl
{
    private readonly XmlGumpEditorCanvas _canvas;
    private readonly Action? _closed;
    private bool _closing;
    private uint _nextRefresh;

    public XmlGumpPreviewWindow(XmlGumpEditorCanvas canvas, Action? closed)
        : base(TazLang.Get("xmlgumpeditor_preview_title", "XML Gump Preview"))
    {
        _canvas = canvas;
        _closed = closed;

        SetRootContent(canvas);

        // The window body is stripped back so the canvas sits directly over the game. The title bar
        // keeps its backing, since it is what the window is moved and closed by.
        _rootWindow.Background = null;
        _rootWindow.Border = null;
        _rootWindow.BorderThickness = new Thickness(0);

        // The base handler drops the client's keyboard focus for anything that is not a text field,
        // which would let the arrow keys walk the character while nudging. Subscribing after it and
        // re-claiming focus keeps the keys on the canvas for as long as it is focused.
        _desktop.WidgetGotKeyboardFocus += OnWidgetGotKeyboardFocus;
    }

    private void OnWidgetGotKeyboardFocus(object? sender, GenericEventArgs<Widget> e)
    {
        if (ReferenceEquals(e.Data, _canvas))
            UIManager.KeyboardFocusControl = this;
    }

    /// <inheritdoc />
    /// <remarks>Re-resolves the live text and progress values at the runtime's own update cadence.</remarks>
    public override void Update()
    {
        base.Update();

        if (Time.Ticks >= _nextRefresh)
        {
            _nextRefresh = Time.Ticks + XmlGump.UpdateFrequency;
            _canvas.RefreshLive();
        }
    }

    public override void Dispose()
    {
        if (_closing)
        {
            base.Dispose();

            return;
        }

        _closing = true;
        base.Dispose();
        _closed?.Invoke();
    }
}
