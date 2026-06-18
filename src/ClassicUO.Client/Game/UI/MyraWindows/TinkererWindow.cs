using ClassicUO.Game.Managers;
using ClassicUO.Game.UI;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Tinkerer;

namespace ClassicUO.Game.UI.MyraWindows;

public class TinkererWindow : MyraControl
{
    public static void Show()
    {
        foreach (IGui g in UIManager.Gumps)
        {
            if (g is TinkererWindow w)
            {
                w.CenterInViewPort();
                w.BringOnTop();
                return;
            }
        }
        UIManager.Add(new TinkererWindow());
    }

    public TinkererWindow() : base("Tinkerer")
    {
        CanBeSaved = true;
        SetRootContent(TinkererTab.Build());
        CenterInViewPort();
    }
}
