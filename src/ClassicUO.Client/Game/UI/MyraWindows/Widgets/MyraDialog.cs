#nullable enable
using System;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
/// A modal-style dialog built on <see cref="MyraControl"/>, registered directly with the
/// <see cref="UIManager"/> so it owns its own Desktop and never interferes with other open windows.
/// </summary>
public class MyraDialog : MyraControl
{
    private readonly Action<bool>? _onClose;

    /// <param name="title">Window title bar text.</param>
    /// <param name="content">Widget to display above the OK/Cancel buttons.</param>
    /// <param name="onClose">Called with <c>true</c> on OK, <c>false</c> on Cancel/close.</param>
    public MyraDialog(string title, Widget content, Action<bool>? onClose = null) : base(title)
    {
        _onClose = onClose;

        var layout = new VerticalStackPanel
        {
            Spacing = 8,
            Padding = new Thickness(8)
        };
        layout.Widgets.Add(content);

        var btnRow = new HorizontalStackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        btnRow.Widgets.Add(new MyraButton("OK", OnOk));
        btnRow.Widgets.Add(new MyraButton("Cancel", OnCancel));
        layout.Widgets.Add(btnRow);

        SetRootContent(layout);
        CenterInViewPort();

        UIManager.Add(this);
        BringOnTop();
    }

    private void OnOk()
    {
        _disposeRequested = true;
        _onClose?.Invoke(true);
    }

    private void OnCancel()
    {
        _disposeRequested = true;
        _onClose?.Invoke(false);
    }
}
