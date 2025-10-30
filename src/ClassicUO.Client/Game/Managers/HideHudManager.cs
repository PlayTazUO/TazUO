using System;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.Gumps.SpellBar;
using ClassicUO.Utility;
using SDL3;

namespace ClassicUO.Game.Managers;

public static class HideHudManager
{
    private static bool isVisible = true;
    public static string GetFlagName(HideHudFlags flag)
    {
        string name = Enum.GetName(typeof(HideHudFlags), flag);
        return StringHelper.AddSpaceBeforeCapital(name);
    }

    public static void ToggleHidden(ulong flags)
    {
        isVisible = !isVisible;

        // Toggle player character visibility
        if (ByteFlagHelper.HasFlag(flags, (ulong)HideHudFlags.PlayerChar))
        {
            if (UIManager.World?.Player != null)
            {
                UIManager.World.Player.IsVisible = isVisible;
            }
        }

        // Toggle mouse cursor visibility
        if (ByteFlagHelper.HasFlag(flags, (ulong)HideHudFlags.Mouse))
        {
            if (Client.Game.UO?.GameCursor != null)
            {
                Client.Game.UO.GameCursor.IsVisible = isVisible;

                if (!isVisible)
                    SDL.SDL_HideCursor();
                else
                    SDL.SDL_ShowCursor();
            }
        }

        foreach (Gump gump in UIManager.Gumps)
        {
            if (gump == null)
                continue;

            if (ByteFlagHelper.HasFlag(flags, (ulong)HideHudFlags.Paperdoll) && (gump is PaperDollGump || gump is ModernPaperdoll))
                gump.IsVisible = isVisible;
            else if (ByteFlagHelper.HasFlag(flags, (ulong)HideHudFlags.WorldMap) && gump is WorldMapGump)
                gump.IsVisible = isVisible;
            else if (ByteFlagHelper.HasFlag(flags, (ulong)HideHudFlags.GridContainers) && gump is GridContainer)
                gump.IsVisible = isVisible;
            else if (ByteFlagHelper.HasFlag(flags, (ulong)HideHudFlags.Containers) && gump is ContainerGump)
                gump.IsVisible = isVisible;
            else if (ByteFlagHelper.HasFlag(flags, (ulong)HideHudFlags.Healthbars) && gump is BaseHealthBarGump)
                gump.IsVisible = isVisible;
            else if (ByteFlagHelper.HasFlag(flags, (ulong)HideHudFlags.StatusBar) && gump is StatusGumpBase)
                gump.IsVisible = isVisible;
            else if (ByteFlagHelper.HasFlag(flags, (ulong)HideHudFlags.SpellBar) && gump is SpellBar)
                gump.IsVisible = isVisible;
            else if (ByteFlagHelper.HasFlag(flags, (ulong)HideHudFlags.Journal) && gump is ResizableJournal)
                gump.IsVisible = isVisible;
            else if (ByteFlagHelper.HasFlag(flags, (ulong)HideHudFlags.XMLGumps) && gump is XmlGump)
                gump.IsVisible = isVisible;
            else if (ByteFlagHelper.HasFlag(flags, (ulong)HideHudFlags.NearbyCorpseLoot) && gump is NearbyLootGump)
                gump.IsVisible = isVisible;
            else if (ByteFlagHelper.HasFlag(flags, (ulong)HideHudFlags.MacroButtons) && gump is MacroButtonGump)
                gump.IsVisible = isVisible;
            else if (ByteFlagHelper.HasFlag(flags, (ulong)HideHudFlags.SkillButtons) && gump is SkillButtonGump)
                gump.IsVisible = isVisible;
            else if (ByteFlagHelper.HasFlag(flags, (ulong)HideHudFlags.SkillsMenus) && (gump is StandardSkillsGump || gump is SkillGumpAdvanced))
                gump.IsVisible = isVisible;
            else if (ByteFlagHelper.HasFlag(flags, (ulong)HideHudFlags.TopMenuBar) && gump is TopBarGump)
                gump.IsVisible = isVisible;
            else if (ByteFlagHelper.HasFlag(flags, (ulong)HideHudFlags.DurabilityTracker) && (gump is DurabilitysGump || gump is DurabilityGumpMinimized))
                gump.IsVisible = isVisible;
            else if (ByteFlagHelper.HasFlag(flags, (ulong)HideHudFlags.BuffBar) && (gump is BuffGump || gump is ImprovedBuffGump))
                gump.IsVisible = isVisible;
            else if (ByteFlagHelper.HasFlag(flags, (ulong)HideHudFlags.CounterBar) && gump is CounterBarGump)
                gump.IsVisible = isVisible;
            else if (ByteFlagHelper.HasFlag(flags, (ulong)HideHudFlags.InfoBar) && gump is InfoBarGump)
                gump.IsVisible = isVisible;
            else if (ByteFlagHelper.HasFlag(flags, (ulong)HideHudFlags.SpellIcons) && (gump is UseSpellButtonGump))
                gump.IsVisible = isVisible;
            else if (ByteFlagHelper.HasFlag(flags, (ulong)HideHudFlags.NameOverheadGump) && gump is NameOverHeadHandlerGump)
                gump.IsVisible = isVisible;
            // ScriptManagerGump removed - use ScriptManagerWindow instead
        }
    }
}
