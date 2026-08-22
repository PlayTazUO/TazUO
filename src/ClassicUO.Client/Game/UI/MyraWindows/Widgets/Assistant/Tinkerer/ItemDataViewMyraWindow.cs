#nullable enable

using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Tinkerer;

/// <summary>
///     Pop-out window showing the full ItemData (TileData) for a single item graphic. Opened from the
///     Tinkerer Art tab's detail section or the ItemData tab.
/// </summary>
public class ItemDataViewMyraWindow : MyraControl
{
    /// <summary>Builds and shows the window for <paramref name="graphic" />.</summary>
    /// <param name="graphic">Item graphic ID, without the 0x4000 offset.</param>
    public ItemDataViewMyraWindow(uint graphic)
        : base(TazLang.Get("tinkerer_itemdata_windowtitle", [graphic.ToString(), $"0x{graphic:X4}"]))
    {
        SetRootContent(new ScrollViewer { MaxHeight = 600, Content = ItemDataInfo.Build(graphic) });
        CenterInViewPort();
        UIManager.Add(this);
        BringOnTop();
    }
}
