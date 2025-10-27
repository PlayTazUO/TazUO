namespace ClassicUO.Game.UI.Controls;

public class SettingsSection : Control
{
    private const byte FONT = 0xFF;
    private const ushort HUE_FONT = 0xFFFF;

    private readonly DataBox _databox;
    private int _indent;

    public SettingsSection(string title, int width)
    {
        CanMove = true;
        AcceptMouseInput = true;
        WantUpdateSize = true;


        var label = new Label(title, true, HUE_FONT, font: FONT);
        label.X = 5;
        base.Add(label);

        base.Add
        (
            new Line
            (
                0,
                label.Height,
                width - 30,
                1,
                0xFFbabdc2
            )
        );

        Width = width;
        Height = label.Height + 1;

        _databox = new DataBox(label.X + 10, label.Height + 4, 0, 0);

        base.Add(_databox);
    }

    public void PushIndent() => _indent += 40;

    public void PopIndent() => _indent -= 40;


    public void AddRight(Control c, int offset = 15)
    {
        int i = _databox.Children.Count - 1;

        for (; i >= 0; --i)
        {
            if (_databox.Children[i].IsVisible)
            {
                break;
            }
        }

        c.X = i >= 0 ? _databox.Children[i].Bounds.Right + offset : _indent;

        c.Y = i >= 0 ? _databox.Children[i].Bounds.Top : 0;

        _databox.Add(c);
        _databox.WantUpdateSize = true;
    }

    public void BaseAdd(Control c, int page = 0) => base.Add(c, page);

    public override T Add<T>(T c, int page = 0)
    {
        int i = _databox.Children.Count - 1;
        int bottom = 0;

        for (; i >= 0; --i)
        {
            if (_databox.Children[i].IsVisible)
            {
                if (bottom == 0 || bottom < _databox.Children[i].Bounds.Bottom + 2)
                {
                    bottom = _databox.Children[i].Bounds.Bottom + 2;
                }
                else
                {
                    break;
                }
            }
        }

        c.X = _indent;
        c.Y = bottom;

        _databox.Add(c, page);
        _databox.WantUpdateSize = true;

        Height += c.Height + 2;
        return c;
    }
}
