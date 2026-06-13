#nullable enable

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;

namespace ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;

public record struct BorderStyle(IBrush Brush, Thickness Thickness);

public sealed class RulebaseStyleOptions : INotifyPropertyChanged
{
    public bool ShowHeader
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public bool UseStripedRows
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public IBrush HeaderVerticalBorder
    {
        get;
        set => SetField(ref field, value);
    } = new SolidBrush(MyraStyle.GridBorderColor);

    public BorderStyle OuterBorder
    {
        get;
        set => SetField(ref field, value);
    } = new(new SolidBrush(MyraStyle.GridBorderColor), new Thickness(1));

    public BorderStyle ColumnBorders
    {
        get;
        set => SetField(ref field, value);
    } = new(new SolidBrush(MyraStyle.GridBorderColor), new Thickness(0, 0, 1, 0));

    public BorderStyle RowBorders
    {
        get;
        set => SetField(ref field, value);
    } = new(new SolidBrush(MyraStyle.GridBorderColor), new Thickness(0, 0, 0, 1));

    public bool HighlightSelectedRow
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public Color HeaderBackground
    {
        get;
        set => SetField(ref field, value);
    } = new(0, 0, 0, 55);

    public Color OddRowBackground
    {
        get;
        set => SetField(ref field, value);
    } = new(20, 20, 45, 70);

    public Color EvenRowBackground
    {
        get;
        set => SetField(ref field, value);
    } = new(0, 0, 0, 20);

    public Color SelectedRowBackground
    {
        get;
        set => SetField(ref field, value);
    } = new(80, 120, 180, 75);

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
