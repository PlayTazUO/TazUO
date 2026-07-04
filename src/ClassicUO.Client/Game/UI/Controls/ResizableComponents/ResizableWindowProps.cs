using System.ComponentModel;
using ClassicUO.Common;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls.ResizableComponents;

/// <summary>
///     Defines the configurable properties of a <see cref="ResizableWindow" />, such as its
///     resize behavior, whether it can be minimized, and where its size is persisted.
/// </summary>
public class ResizableWindowProps : MyraCommonProps
{
    /// <summary>
    ///     Gets or sets the resize behavior (enabled edges, size limits, and scrollbar mode) of the window.
    /// </summary>
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

    /// <summary>
    ///     Gets or sets a value indicating whether the window can be minimized to its title bar.
    /// </summary>
    public bool Minimizable { get; set => SetField(ref field, value); } = true;

    /// <summary>
    ///     Gets or sets the accessor used to persist and restore the window's size across sessions.
    /// </summary>
    public Accessor<Point?> InitialSizeStore { get; set => SetField(ref field, value); }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ResizableWindowProps" /> class.
    /// </summary>
    public ResizableWindowProps()
    {
        Resize?.PropertyChanged += OnResizePropertyChanged;
    }

    /// <summary>
    ///     Re-raises property change notifications from <see cref="Resize" /> as a change of the <see cref="Resize" /> property itself.
    /// </summary>
    /// <param name="sender">The <see cref="ResizeBehavior" /> instance that changed.</param>
    /// <param name="e">Event data describing which property of <see cref="Resize" /> changed.</param>
    private void OnResizePropertyChanged(object sender, PropertyChangedEventArgs e) => OnPropertyChanged(nameof(Resize));
}
