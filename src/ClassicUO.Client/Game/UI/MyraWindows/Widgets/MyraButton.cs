#nullable enable

using System;
using System.Collections.Generic;
using FontStashSharp;
using Myra.Events;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class MyraButton : Button
{
    public Action? OnClick { get; set; }

    private string _text = "";

    public string Text
    {
        get => _text;
        set => SetField(ref _text, value);
    }

    private SpriteFontBase? _labelFont;

    public SpriteFontBase? LabelFont
    {
        get => _labelFont;
        set => SetField(ref _labelFont, value);
    }

    private MyraLabel.TextStyle _labelStyle;

    public MyraLabel.TextStyle LabelStyle
    {
        get => _labelStyle;
        set => SetField(ref _labelStyle, value);
    }

    public MyraButton(
        string text,
        Action? onClick = null,
        MyraLabel.TextStyle style = MyraLabel.TextStyle.P,
        SpriteFontBase? labelFont = null
    )
    {
        if (!string.IsNullOrEmpty(text))
            _text = text;

        _labelStyle = style;
        _labelFont = labelFont;

        OnClick = onClick;
        Margin = new Thickness(2);
        DisabledBackground = Background;
        VerticalAlignment = VerticalAlignment.Center;

        Build();
    }

    public override void OnTouchDown(TouchEventArgs args)
    {
        base.OnTouchDown(args);

        if (Enabled)
            OnClick?.Invoke();
    }

    private void Build()
    {
        var content = new MyraLabel(Text, LabelStyle);

        if (LabelFont != null)
            content.Font = LabelFont;

        Content = content;
    }

    protected void SetField<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        Build();
    }
}
