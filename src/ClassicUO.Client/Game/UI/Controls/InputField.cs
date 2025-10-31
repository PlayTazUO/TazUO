using System;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Controls;

public class InputField : Control
{
    private readonly StbTextBox _textbox;

    public event EventHandler TextChanged { add { _textbox.TextChanged += value; } remove { _textbox.TextChanged -= value; } }

    public InputField
    (
        ushort backgroundGraphic,
        byte font,
        ushort hue,
        bool unicode,
        int width,
        int height,
        int maxWidthText = 0,
        int maxCharsCount = -1
    )
    {
        WantUpdateSize = false;

        Width = width;
        Height = height;

        var background = new ResizePic(backgroundGraphic)
        {
            Width = width,
            Height = height
        };

        _textbox = new StbTextBox
        (
            font,
            maxCharsCount,
            maxWidthText,
            unicode,
            FontStyle.BlackBorder,
            hue
        )
        {
            X = 4,
            Y = 4,
            Width = width - 8,
            Height = height - 8
        };


        Add(background);
        Add(_textbox);
    }

    public override bool Draw(UltimaBatcher2D batcher, int x, int y)
    {
        if (batcher.ClipBegin(x, y, Width, Height))
        {
            base.Draw(batcher, x, y);

            batcher.ClipEnd();
        }

        return true;
    }


    public string Text => _textbox.Text;

    public override bool AcceptKeyboardInput
    {
        get => _textbox.AcceptKeyboardInput;
        set => _textbox.AcceptKeyboardInput = value;
    }

    public bool NumbersOnly
    {
        get => _textbox.NumbersOnly;
        set => _textbox.NumbersOnly = value;
    }


    public void SetText(string text) => _textbox.SetText(text);
}