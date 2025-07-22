using System;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.Gumps.SpellBar;
using ClassicUO.Utility;

namespace ClassicUO.Game.Managers;

public static class HideHudManager
{
    private static bool isVisible = true;
    public static string GetFlagName(HideHudFlags flag)
    {
        string name = Enum.GetName(typeof(HideHudFlags), flag);
        return StringHelper.AddSpaceBeforeCapital(name);
    }

    public static void ToggleHidden(byte flags)
    {
        isVisible = !isVisible;

        foreach (Gump gump in UIManager.Gumps)
        {
            if (gump == null)
                continue;

            if (ByteFlagHelper.HasFlag(flags, (byte)HideHudFlags.Paperdoll) && gump is PaperDollGump)
                gump.IsVisible = isVisible;
            else if (ByteFlagHelper.HasFlag(flags, (byte)HideHudFlags.WorldMap) && gump is WorldMapGump)
                gump.IsVisible = isVisible;
            else if (ByteFlagHelper.HasFlag(flags, (byte)HideHudFlags.GridContainers) && gump is GridContainer)
                gump.IsVisible = isVisible;
            else if (ByteFlagHelper.HasFlag(flags, (byte)HideHudFlags.Containers) && gump is ContainerGump)
                gump.IsVisible = isVisible;
            else if (ByteFlagHelper.HasFlag(flags, (byte)HideHudFlags.Healthbars) && gump is BaseHealthBarGump)
                gump.IsVisible = isVisible;
            else if (ByteFlagHelper.HasFlag(flags, (byte)HideHudFlags.StatusBar) && gump is StatusGumpBase)
                gump.IsVisible = isVisible;
            else if(ByteFlagHelper.HasFlag(flags, (byte)HideHudFlags.SpellBar) && gump is SpellBar)
                gump.IsVisible = isVisible;
        }
    }
}
