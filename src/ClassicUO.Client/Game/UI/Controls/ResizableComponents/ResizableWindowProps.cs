using System.ComponentModel;
using ClassicUO.Common;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls.ResizableComponents;

public class ResizableWindowProps : MyraCommonProps
{
    public ResizeBehavior Resize
    {
        get;
        set
        {
            ResizeBehavior oldValue = field;
            if (SetField(ref field, value))
            {
                oldValue?.PropertyChanged -= OnResizePropertyChanged;
                field?.PropertyChanged += OnResizePropertyChanged;
            }
        }
    } = new();
    public bool Minimizable { get; set => SetField(ref field, value); } = true;

    public Accessor<Point?> InitialSizeStore { get; set => SetField(ref field, value); }

    public ResizableWindowProps()
    {
        Resize?.PropertyChanged += OnResizePropertyChanged;
    }

    private void OnResizePropertyChanged(object sender, PropertyChangedEventArgs e) => OnPropertyChanged(nameof(Resize));
}
