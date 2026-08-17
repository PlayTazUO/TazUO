using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Widgets;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for movement and pathfinding settings</summary>
public static class MovementTab
{
    /// <summary>Returns the option fragment for pathfinding and run-mode configuration</summary>
    internal static IOptionSource GetContent() => GetSection();

    private static OptionFragment GetSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.Vertical(
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.EnablePathfind), TazLang.Get("mog_movementtab_pathfinding_enablepathfinding")),
                Option.Checkbox(
                    TazLang.Get("mog_movementtab_pathfinding_shiftpathfinding"),
                    new Accessor<bool>(() => profile.UseShiftToPathfind),
                    search: new SearchMetadata(TazLang.Get("mog_movementtab_pathfinding_shiftpathfinding"), Keywords: [TazLang.Get("mog_kw_pathfinding"), TazLang.Get("mog_kw_shift")])
                ),
                Option.Checkbox(
                    TazLang.Get("mog_movementtab_pathfinding_singleclickpathfind"),
                    new Accessor<bool>(() => profile.PathfindSingleClick),
                    search: new SearchMetadata(TazLang.Get("mog_movementtab_pathfinding_singleclickpathfind"), Keywords: [TazLang.Get("mog_kw_pathfinding"), TazLang.Get("mog_kw_click")])
                )
            ).WithSearch(new SearchMetadata(TazLang.Get("mog_movementtab_label"), Tags: [TazLang.Get("mog_kw_movement")], Keywords: [TazLang.Get("mog_kw_pathfinding")])),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.AlwaysRun), TazLang.Get("mog_movementtab_running_alwaysrun")),
                Option.Checkbox(
                    TazLang.Get("mog_movementtab_running_rununlesshidden"),
                    new Accessor<bool>(() => profile.AlwaysRunUnlessHidden),
                    search: new SearchMetadata(TazLang.Get("mog_movementtab_running_rununlesshidden"), Keywords: [TazLang.Get("mog_kw_run"), TazLang.Get("mog_kw_hidden")])
                )
            ).WithSearch(new SearchMetadata(TazLang.Get("mog_movementtab_label"), Tags: [TazLang.Get("mog_kw_movement")], Keywords: [TazLang.Get("mog_kw_run")])),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.AutoOpenDoors), TazLang.Get("mog_movementtab_doors_autoopendoors")),
                Option.Checkbox(
                    TazLang.Get("mog_movementtab_doors_autoopenpathfinding"),
                    new Accessor<bool>(() => profile.SmoothDoors),
                    search: new SearchMetadata(TazLang.Get("mog_movementtab_doors_autoopenpathfinding"), Keywords: [TazLang.Get("mog_kw_door"), TazLang.Get("mog_kw_pathfinding")])
                ),
                Option.Checkbox(
                    TazLang.Get("mog_movementtab_doors_autoopenhidden"),
                    new Accessor<bool>(() => profile.AutoOpenDoorsIfHidden),
                    search: new SearchMetadata(TazLang.Get("mog_movementtab_doors_autoopenhidden"), Keywords: [TazLang.Get("mog_kw_door"), TazLang.Get("mog_kw_hidden")])
                ),
                Option.Checkbox(
                    TazLang.Get("mog_movementtab_doors_autoclosedoors"),
                    new Accessor<bool>(() => ProfileManager.GlobalSettings.AutoCloseDoors),
                    search: new SearchMetadata(TazLang.Get("mog_movementtab_doors_autoclosedoors"), Keywords: [TazLang.Get("mog_kw_door")])
                )
            ).WithSearch(new SearchMetadata(TazLang.Get("mog_movementtab_label"), Tags: [TazLang.Get("mog_kw_movement")], Keywords: [TazLang.Get("mog_kw_door")])),
            Option.Checkbox(
                TazLang.Get("mog_movementtab_doors_blockdoormovement"),
                new Accessor<bool>(() => profile.BlockDoorMovement),
                search: new SearchMetadata(TazLang.Get("mog_movementtab_doors_blockdoormovement"), Keywords: [TazLang.Get("mog_kw_door"), TazLang.Get("mog_kw_block")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_movementtab_autoavoidobstacles"),
                new Accessor<bool>(() => profile.AutoAvoidObstacules),
                search: new SearchMetadata(TazLang.Get("mog_movementtab_autoavoidobstacles"), Keywords: [TazLang.Get("mog_kw_avoid"), TazLang.Get("mog_kw_obstacle")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_movementtab_usewasdmovement"),
                new Accessor<bool>(() => ProfileManager.GlobalSettings.UseWASDInsteadArrowKeys),
                search: new SearchMetadata(TazLang.Get("mog_movementtab_usewasdmovement"), Keywords: [TazLang.Get("mog_kw_wasd"), TazLang.Get("mog_kw_keyboard")])
            ),
            OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = TazLang.Get("mog_movementtab_autofollow") },
                Option.Slider(
                    TazLang.Get("mog_tazuo_autofollowdistance"),
                    1,
                    10,
                    new Accessor<int>(() => profile.AutoFollowDistance),
                    search: new SearchMetadata(TazLang.Get("mog_tazuo_autofollowdistance"), Keywords: [TazLang.Get("mog_kw_distance")])
                ),
                Option.Checkbox(
                    TazLang.Get("mog_tazuo_disableautofollow"),
                    new Accessor<bool>(() => profile.DisableAutoFollowAlt),
                    search: new SearchMetadata(TazLang.Get("mog_tazuo_disableautofollow"), Keywords: [TazLang.Get("mog_kw_disable"), TazLang.Get("mog_kw_alt")])
                )
            ).AsSearchGroup()
             .WithSearch(new SearchMetadata(Keywords: [TazLang.Get("mog_kw_auto"), TazLang.Get("mog_kw_follow")])),
            Option.Slider(
                TazLang.Get("mog_tazuo_turndelay"),
                45,
                120,
                new Accessor<ushort>(() => ProfileManager.ServerSettings.TurnDelay),
                search: new SearchMetadata(TazLang.Get("mog_tazuo_turndelay"), Keywords: [TazLang.Get("mog_kw_turn"), TazLang.Get("mog_kw_delay")])
            ),
            OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = TazLang.Get("mog_movementtab_controller_label"), LabelLink = "https://tazuo.org/wiki/tazuocontroller-support" },
                OptionsUi.CheckBoxGroup(
                    new PropertyBinder(new Accessor<bool>(() => profile.ControllerEnabled), TazLang.Get("mog_movementtab_controller_enablecontroller")),
                    Option.Slider(
                        TazLang.Get("mog_movementtab_controller_mousesensitivity"),
                        1,
                        20,
                        new Accessor<float>(() => profile.ControllerMouseSensativity, f => profile.ControllerMouseSensativity = (int)f),
                        search: new SearchMetadata(TazLang.Get("mog_movementtab_controller_mousesensitivity"), Keywords: [TazLang.Get("mog_kw_controller"), TazLang.Get("mog_kw_sensitivity")])
                    )
                ).WithSearch(new SearchMetadata(TazLang.Get("mog_movementtab_controller_label"), Tags: [TazLang.Get("mog_kw_movement")], Keywords: [TazLang.Get("mog_kw_controller")]))
            )
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_movementtab_label"), Tags: [TazLang.Get("mog_kw_movement")]));
    }
}
