using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ClassicUO.Game.UI.MyraWindows;

namespace ClassicUO.Game.UI.Controls;

public class MyraCommonProps : INotifyPropertyChanged
{
    public int MinWidth { get; set => SetField(ref field, value); } = StyleConstantsDefaults.WINDOW_MIN_WIDTH;
    public int MinHeight { get; set => SetField(ref field, value); } = StyleConstantsDefaults.WINDOW_MIN_HEIGHT;

    public int MaxWidth { get; set => SetField(ref field, value); } = StyleConstantsDefaults.WINDOW_MAX_WIDTH;
    public int MaxHeight { get; set => SetField(ref field, value); } = StyleConstantsDefaults.WINDOW_MAX_HEIGHT;
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
