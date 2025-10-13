using System;

namespace ClassicUO.Game.Data;

[Flags]
public enum HideHudFlags : ulong //Up to 63 gump types for ulong
{
    None = 0,
    Paperdoll = 1 << 0,
    WorldMap = 1 << 1,
    GridContainers = 1 << 2,
    Containers = 1 << 3,
    Healthbars = 1 << 4,
    StatusBar = 1 << 5,
    SpellBar = 1 << 6,
    Journal = 1 << 7,
    XMLGumps = 1 << 8,
    NearbyCorpseLoot = 1 << 9,
    MacroButtons = 1 << 10,
    SkillButtons = 1 << 11,
    SkillsMenus = 1 << 12,
    TopMenuBar = 1 << 13,
    DurabilityTracker = 1 << 14,
    BuffBar = 1 << 15,
    CounterBar = 1 << 16,
    InfoBar = 1 << 17,
    SpellIcons = 1 << 18,
    NameOverheadGump = 1 << 19,
    ScriptManagerGump = 1 << 20,
    PlayerChar = 1 << 21,
    Mouse = 1 << 22,

    All = (1UL << 23) - 1 //Update 23 if more are added
}
