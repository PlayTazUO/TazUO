using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI.MyraWindows.Widgets;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Top-level gameplay options tab that groups combat, mobiles, movement, and layer-hiding sub-tabs</summary>
public static class GameplayTab
{
    /// <summary>Returns the tab group containing combat, mobiles, movement, layer-hiding, and paperdoll sub-tabs</summary>
    internal static IOptionSource GetContent() => GetGameplayMenuTabs();

    private static OptionTabGroup GetGameplayMenuTabs() => new OptionTabGroup()
            .AddTab(
                TazLang.Get("mog_combattab_combat_label"),
                CombatTab.GetContent,
                new SearchMetadata(TazLang.Get("mog_combattab_combat_label"), Keywords: [TazLang.Get("mog_kw_combat"), TazLang.Get("mog_kw_attack"), TazLang.Get("mog_kw_battle")])
            )
            .AddTab(
                TazLang.Get("mog_mobilestab_label"),
                MobilesTab.GetContent,
                new SearchMetadata(TazLang.Get("mog_mobilestab_label"), Keywords: [TazLang.Get("mog_kw_mobile"), TazLang.Get("mog_kw_humanoid"), TazLang.Get("mog_kw_monster"), TazLang.Get("mog_kw_hp"), TazLang.Get("mog_kw_health")])
            )
            .AddTab(
                TazLang.Get("mog_movementtab_label"),
                MovementTab.GetContent,
                new SearchMetadata(TazLang.Get("mog_movementtab_label"), Keywords: [TazLang.Get("mog_kw_movement"), TazLang.Get("mog_kw_pathfinding"), TazLang.Get("mog_kw_wasd"), TazLang.Get("mog_kw_move")])
            )
            .AddTab(
                TazLang.Get("mog_gameplaytab_terrain_label"),
                GetTerrainAndStaticsSubTabContent,
                new SearchMetadata(TazLang.Get("mog_gameplaytab_terrain_label"), Keywords: [TazLang.Get("mog_kw_terrain"), TazLang.Get("mog_kw_static"), TazLang.Get("mog_kw_tree"), TazLang.Get("mog_kw_roof"), TazLang.Get("mog_kw_vegetation")])
            )
            .AddTab(
                TazLang.Get("mog_layerhidingtab_label"),
                LayerHidingTab.GetContent,
                new SearchMetadata(TazLang.Get("mog_layerhidingtab_label"), Keywords: [TazLang.Get("mog_kw_layer"), TazLang.Get("mog_kw_hide"), TazLang.Get("mog_kw_equipment"), TazLang.Get("mog_kw_clothing")])
            )
            .AddTab(
                TazLang.Get("mog_gameplaytab_misc_label"),
                GetMiscSubTabContent,
                new SearchMetadata(TazLang.Get("mog_gameplaytab_misc_label"), Keywords: [TazLang.Get("mog_kw_misc"), TazLang.Get("mog_kw_miscellaneous")])
            );

    private static IOptionSource GetMiscSubTabContent()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.Vertical(
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.EnableHealthIndicator), TazLang.Get("mog_tazuo_healthbarindicator"), TazLang.Get("mog_tazuo_healthbarindicatortooltip")),
                Option.Slider(
                    TazLang.Get("mog_tazuo_onlyshowbelowhp"),
                    0,
                    100,
                    new Accessor<float>(() => profile.ShowHealthIndicatorBelow),
                    search: new SearchMetadata(TazLang.Get("mog_tazuo_onlyshowbelowhp"), Keywords: [TazLang.Get("mog_kw_hp")])
                ),
                Option.Slider(
                    TazLang.Get("mog_tazuo_size"),
                    1,
                    25,
                    new Accessor<float>(() => profile.HealthIndicatorWidth, f => profile.HealthIndicatorWidth = (int)f),
                    search: new SearchMetadata(TazLang.Get("mog_tazuo_size"), Keywords: [TazLang.Get("mog_kw_size")])
                )
            ).WithSearch(new SearchMetadata(TazLang.Get("mog_tazuo_healthbarindicator"), Keywords: [TazLang.Get("mog_kw_indicator"), TazLang.Get("mog_kw_border")]))
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_gameplaytab_misc_label"), Tags: [TazLang.Get("mog_kw_misc")]));
    }

    private static IOptionSource GetTerrainAndStaticsSubTabContent()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.Vertical(
            Option.Checkbox(
                TazLang.Get("mog_gameplaytab_terrain_hideroof"),
                !profile.DrawRoofs,
                b => profile.DrawRoofs = !b,
                search: new SearchMetadata(TazLang.Get("mog_gameplaytab_terrain_hideroof"), Keywords: [TazLang.Get("mog_kw_roof")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_gameplaytab_terrain_treestostump"),
                new Accessor<bool>(() => profile.TreeToStumps),
                search: new SearchMetadata(TazLang.Get("mog_gameplaytab_terrain_treestostump"), Keywords: [TazLang.Get("mog_kw_tree"), TazLang.Get("mog_kw_stump")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_gameplaytab_terrain_treestostumpradius"),
                new Accessor<bool>(() => profile.TreeToStumpsWithinRadius),
                search: new SearchMetadata(TazLang.Get("mog_gameplaytab_terrain_treestostumpradius"), Keywords: [TazLang.Get("mog_kw_tree"), TazLang.Get("mog_kw_stump")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_gameplaytab_terrain_hidevegetation"),
                new Accessor<bool>(() => profile.HideVegetation),
                search: new SearchMetadata(TazLang.Get("mog_gameplaytab_terrain_hidevegetation"), Keywords: [TazLang.Get("mog_kw_vegetation")])
            ),
            Option.ComboBox(
                TazLang.Get("mog_gameplaytab_terrain_magicfieldtype"),
                profile.FieldsType,
                [TazLang.Get("mog_general_magicfieldopt_normal"), TazLang.Get("mog_general_magicfieldopt_static"), TazLang.Get("mog_general_magicfieldopt_tile")],
                i => profile.FieldsType = i,
                search: new SearchMetadata(TazLang.Get("mog_gameplaytab_terrain_magicfieldtype"), Keywords: [TazLang.Get("mog_kw_magic"), TazLang.Get("mog_kw_field")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_gameplaytab_terrain_applybordercavetiles"),
                new Accessor<bool>(() => profile.EnableCaveBorder, newValue =>
                {
                    profile.EnableCaveBorder = newValue;
                    if (newValue)
                        StaticFilters.ApplyCaveTileBorder();
                }),
                search: new SearchMetadata(TazLang.Get("mog_gameplaytab_terrain_applybordercavetiles"), Keywords: [TazLang.Get("mog_kw_cave"), TazLang.Get("mog_kw_border")])
            )
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_gameplaytab_terrain_label"), Tags: [TazLang.Get("mog_kw_terrain"), TazLang.Get("mog_kw_static")]));
    }
}
