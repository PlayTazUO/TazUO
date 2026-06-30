using System;
using System.ComponentModel;
using ClassicUO.Common;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class LabeledUIntInput : Widget
{
    private readonly UIntInputBox _inputBox;
    private string _label;
    private MyraLabel.TextStyle _labelStyle;

    public string Label
    {
        get => _label;
        set
        {
            _label = value;
            BuildJustifyContentNone();
        }
    }

    public MyraLabel.TextStyle LabelStyle
    {
        get => _labelStyle;
        set
        {
            _labelStyle = value;
            BuildJustifyContentNone();
        }
    }

    public uint Value { get => _inputBox.Value; set => _inputBox.Value = value; }
    public int? InputBoxMinWidth { get => _inputBox.MinWidth; set => _inputBox.MinWidth = value; }
    public int? InputBoxWidth { get => _inputBox.Width; set => _inputBox.Width = value; }
    public int? InputBoxMaxWidth { get => _inputBox.MaxWidth; set => _inputBox.MaxWidth = value; }
    public uint? MinValue { get => _inputBox.MinValue; set => _inputBox.MinValue = value; }
    public uint? MaxValue { get => _inputBox.MaxValue; set => _inputBox.MaxValue = value; }

    public LabeledUIntInput(string label, Accessor<uint> backingProperty) : this(label, backingProperty.Get(), backingProperty.Set) { }

    public LabeledUIntInput(
        string label,
        uint value,
        Action<uint> onChanged,
        MyraLabel.TextStyle labelStyle = MyraLabel.TextStyle.P
    )
    {
        EnabledChanged += OnEnabledChanged;
        _inputBox = new UIntInputBox(onChanged) { Value = value };
        _label = label;
        _labelStyle = labelStyle;
        BuildJustifyContentNone();
    }

    private void BuildJustifyContentNone()
    {
        Children.Clear();
        if (!string.IsNullOrWhiteSpace(Label))
            Children.Add(
                new MyraLabel(Label, _labelStyle)
                {
                    Margin = new Thickness(0, 0,
                        MyraStyle.STANDARD_SPACING,
                        0
                    )
                }
            );


        Children.Add(_inputBox);
        ChildrenLayout = new StackPanelLayout(Orientation.Horizontal);
    }

    protected void OnEnabledChanged(object sender, EventArgs e) => _inputBox.Enabled = Enabled;
}
