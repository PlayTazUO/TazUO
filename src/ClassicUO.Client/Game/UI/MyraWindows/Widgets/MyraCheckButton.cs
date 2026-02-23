#nullable enable
using System;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class MyraCheckButton : CheckButton
{
    /// <summary>
    /// This includes a label
    /// </summary>
    /// <param name="text"></param>
    /// <param name="isChecked"></param>
    public MyraCheckButton(string text, bool isChecked = false)
    {
        IsChecked = isChecked;
        Content = new MyraLabel(text, MyraLabel.Style.P);
    }
    public MyraCheckButton(bool isChecked = false)
    {
        IsChecked = isChecked;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="isChecked"></param>
    /// <param name="onChange"></param>
    /// <param name="text"></param>
    /// <param name="tooltip"></param>
    /// <returns></returns>
    public static MyraCheckButton CreateWithCallback(bool isChecked,
        Action<bool> onChange,
        string? text = null,
        string? tooltip = null)
    {
        MyraCheckButton cb = text != null ? new MyraCheckButton(text, isChecked) : new MyraCheckButton(isChecked);

        if (tooltip != null)
            cb.Tooltip = tooltip;

        cb.IsCheckedChanged += (_, _) => onChange(cb.IsChecked);
        return cb;
    }
}
