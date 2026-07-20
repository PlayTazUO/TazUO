#nullable enable

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;

namespace ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;

/// <summary>A brush plus per-side thickness, used for the table's outer border.</summary>
public record struct BorderStyle(IBrush Brush, Thickness Thickness);

/// <summary>A brush plus a single uniform thickness, used for column/row separator lines.</summary>
public record struct UniBorderStyle(IBrush Brush, int Thickness);

/// <summary>
/// Visual styling options for a <see cref="RulebaseTableView{TRule}"/>. Raises
/// <see cref="PropertyChanged"/> on every change so the table can refresh itself.
/// </summary>
public sealed class RulebaseStyleOptions : INotifyPropertyChanged
{
    /// <summary>Whether the header row is shown</summary>
    public bool ShowHeader
    {
        get;
        set => SetField(ref field, value);
    } = true;

    /// <summary>Whether rows alternate between <see cref="EvenRowBackground"/> and <see cref="OddRowBackground"/></summary>
    public bool UseStripedRows
    {
        get;
        set => SetField(ref field, value);
    } = true;

    /// <summary>Brush used for the vertical separators between header cells</summary>
    public IBrush HeaderVerticalBorder
    {
        get;
        set => SetField(ref field, value);
    } = new SolidBrush(MyraStyle.GridBorderColor);

    /// <summary>Border drawn around the entire table</summary>
    public BorderStyle OuterBorder
    {
        get;
        set => SetField(ref field, value);
    } = new(new SolidBrush(MyraStyle.GridBorderColor), new Thickness(1));

    /// <summary>Border drawn between columns, or null to disable</summary>
    public UniBorderStyle? ColumnBorders
    {
        get;
        set => SetField(ref field, value);
    } = new UniBorderStyle(new SolidBrush(MyraStyle.GridBorderColor), 1);

    /// <summary>Border drawn between rows, or null to disable</summary>
    public UniBorderStyle? RowBorders
    {
        get;
        set => SetField(ref field, value);
    } = new UniBorderStyle(new SolidBrush(MyraStyle.GridBorderColor), 1);

    /// <summary>Whether the currently selected row is drawn with <see cref="SelectedRowBackground"/></summary>
    public bool HighlightSelectedRow
    {
        get;
        set => SetField(ref field, value);
    } = true;

    /// <summary>Background color of the header row</summary>
    public Color HeaderBackground
    {
        get;
        set => SetField(ref field, value);
    } = new(0, 0, 0, 55);

    /// <summary>Background color of odd-indexed rows when <see cref="UseStripedRows"/> is true</summary>
    public Color OddRowBackground
    {
        get;
        set => SetField(ref field, value);
    } = new(20, 20, 45, 70);

    /// <summary>Background color of even-indexed rows when <see cref="UseStripedRows"/> is true</summary>
    public Color EvenRowBackground
    {
        get;
        set => SetField(ref field, value);
    } = new(0, 0, 0, 20);

    /// <summary>Background color of the selected row when <see cref="HighlightSelectedRow"/> is true</summary>
    public Color SelectedRowBackground
    {
        get;
        set => SetField(ref field, value);
    } = new(80, 120, 180, 75);

    /// <summary>Raised whenever any style property changes</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Raises <see cref="PropertyChanged"/> for the given property</summary>
    /// <param name="propertyName">The property that changed; defaults to the calling member's name</param>
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>Updates a backing field and raises <see cref="PropertyChanged"/> if the value changed</summary>
    /// <param name="field">Reference to the backing field</param>
    /// <param name="value">The new value</param>
    /// <param name="propertyName">The property being set; defaults to the calling member's name</param>
    /// <returns>True if the value changed; false if it was equal to the current value</returns>
    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
