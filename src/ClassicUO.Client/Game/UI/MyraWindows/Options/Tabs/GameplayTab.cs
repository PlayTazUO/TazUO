using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Top-level gameplay options tab that groups combat, mobiles, movement, and layer-hiding sub-tabs</summary>
public static class GameplayTab
{
    /// <summary>Returns the tab group containing combat, mobiles, movement, layer-hiding, and paperdoll sub-tabs</summary>
    internal static IOptionSource GetContent() => GetGameplayMenuTabs();

    private static OptionTabGroup GetGameplayMenuTabs()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.GameplayTabLang gameplayLang = lang.GameplayTab;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return new OptionTabGroup()
            .AddTab(
                lang.CombatTab.Combat.Label,
                CombatTab.GetContent,
                new SearchMetadata(lang.CombatTab.Combat.Label, Keywords: [kw.Combat, kw.Attack, kw.Battle])
            )
            .AddTab(
                lang.MobilesTab.Label,
                MobilesTab.GetContent,
                new SearchMetadata(lang.MobilesTab.Label, Keywords: [kw.Mobile, kw.Humanoid, kw.Monster, kw.HP, kw.Health])
            )
            .AddTab(
                lang.MovementTab.Label,
                MovementTab.GetContent,
                new SearchMetadata(lang.MovementTab.Label, Keywords: [kw.Movement, kw.Pathfinding, kw.WASD, kw.Move])
            )
            .AddTab(
                gameplayLang.Terrain.Label,
                GetTerrainAndStaticsSubTabContent,
                new SearchMetadata(gameplayLang.Terrain.Label, Keywords: [kw.Terrain, kw.Static, kw.Tree, kw.Roof, kw.Vegetation])
            )
            .AddTab(
                lang.LayerHidingTab.Label,
                LayerHidingTab.GetContent,
                new SearchMetadata(lang.LayerHidingTab.Label, Keywords: [kw.Layer, kw.Hide, kw.Equipment, kw.Clothing])
            );
    }

    private static IOptionSource GetTerrainAndStaticsSubTabContent()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.General genLang = lang.GetGeneral;
        ModernOptionsGumpLanguage.GameplayTabLang gameplayLang = lang.GameplayTab;
        ModernOptionsGumpLanguage.GameplayTabLang.TerrainSection terrainLang = gameplayLang.Terrain;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return OptionsUi.Vertical(
            Option.Checkbox(
                terrainLang.HideRoof,
                !profile.DrawRoofs,
                b => profile.DrawRoofs = !b,
                search: new SearchMetadata(terrainLang.HideRoof, Keywords: [kw.Roof])
            ),
            Option.Checkbox(
                terrainLang.TreesToStump,
                new Accessor<bool>(() => profile.TreeToStumps),
                search: new SearchMetadata(terrainLang.TreesToStump, Keywords: [kw.Tree, kw.Stump])
            ),
            Option.Checkbox(
                terrainLang.HideVegetation,
                new Accessor<bool>(() => profile.HideVegetation),
                search: new SearchMetadata(terrainLang.HideVegetation, Keywords: [kw.Vegetation])
            ),
            Option.ComboBox(
                terrainLang.MagicFieldType,
                profile.FieldsType,
                [genLang.MagicFieldOpt_Normal, genLang.MagicFieldOpt_Static, genLang.MagicFieldOpt_Tile],
                i => profile.FieldsType = i,
                search: new SearchMetadata(terrainLang.MagicFieldType, Keywords: [kw.Magic, kw.Field])
            ),
            Option.Checkbox(
                terrainLang.ApplyBorderCaveTiles,
                new Accessor<bool>(() => profile.EnableCaveBorder, newValue =>
                {
                    profile.EnableCaveBorder = newValue;
                    if (newValue)
                        StaticFilters.ApplyCaveTileBorder();
                }),
                search: new SearchMetadata(terrainLang.ApplyBorderCaveTiles, Keywords: [kw.Cave, kw.Border])
            )
        ).WithSearch(new SearchMetadata(terrainLang.Label, Tags: [kw.Terrain, kw.Static]));
    }
}
