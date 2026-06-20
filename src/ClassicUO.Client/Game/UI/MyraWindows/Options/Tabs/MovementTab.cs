using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Widgets;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class MovementTab
{
    internal static IOptionSource GetContent() => GetSection();

    private static OptionFragment GetSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.General genLang = Language.Instance.GetModernOptionsGumpLanguage.GetGeneral;
        ModernOptionsGumpLanguage.MovementTabLang moveLang = Language.Instance.GetModernOptionsGumpLanguage.MovementTab;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

        return OptionsUi.Vertical(
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.EnablePathfind), moveLang.Pathfinding.EnablePathfinding),
                Option.Checkbox(
                    moveLang.Pathfinding.ShiftPathfinding,
                    new Accessor<bool>(() => profile.UseShiftToPathfind),
                    search: new SearchMetadata(moveLang.Pathfinding.ShiftPathfinding, Keywords: [kw.Pathfinding, kw.Shift])
                ),
                Option.Checkbox(
                    moveLang.Pathfinding.SingleClickPathfind,
                    new Accessor<bool>(() => profile.PathfindSingleClick),
                    search: new SearchMetadata(moveLang.Pathfinding.SingleClickPathfind, Keywords: [kw.Pathfinding, kw.Click])
                )
            ).WithSearch(new SearchMetadata(moveLang.Label, Tags: [kw.Movement], Keywords: [kw.Pathfinding])),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.AlwaysRun), moveLang.Running.AlwaysRun),
                Option.Checkbox(
                    moveLang.Running.RunUnlessHidden,
                    new Accessor<bool>(() => profile.AlwaysRunUnlessHidden),
                    search: new SearchMetadata(moveLang.Running.RunUnlessHidden, Keywords: [kw.Run, kw.Hidden])
                )
            ).WithSearch(new SearchMetadata(moveLang.Label, Tags: [kw.Movement], Keywords: [kw.Run])),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.AutoOpenDoors), moveLang.Doors.AutoOpenDoors),
                Option.Checkbox(
                    moveLang.Doors.AutoOpenPathfinding,
                    new Accessor<bool>(() => profile.SmoothDoors),
                    search: new SearchMetadata(moveLang.Doors.AutoOpenPathfinding, Keywords: [kw.Door, kw.Pathfinding])
                ),
                Option.Checkbox(
                    moveLang.Doors.AutoOpenHidden,
                    new Accessor<bool>(() => profile.AutoOpenDoorsIfHidden),
                    search: new SearchMetadata(moveLang.Doors.AutoOpenHidden, Keywords: [kw.Door, kw.Hidden])
                )
            ).WithSearch(new SearchMetadata(moveLang.Label, Tags: [kw.Movement], Keywords: [kw.Door])),
            Option.Checkbox(
                moveLang.AutoAvoidObstacles,
                new Accessor<bool>(() => profile.AutoAvoidObstacules),
                search: new SearchMetadata(moveLang.AutoAvoidObstacles, Keywords: [kw.Avoid, kw.Obstacle])
            ),
            Option.Checkbox(
                moveLang.UseWasdMovement,
                new Accessor<bool>(() => profile.UseWASDInsteadArrowKeys),
                search: new SearchMetadata(moveLang.UseWasdMovement, Keywords: [kw.WASD, kw.Keyboard])
            ),
            OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = moveLang.Controller.Label, LabelLink = "https://tazuo.org/wiki/tazuocontroller-support" },
                OptionsUi.CheckBoxGroup(
                    new PropertyBinder(new Accessor<bool>(() => profile.ControllerEnabled), moveLang.Controller.EnableController),
                    Option.Slider(
                        moveLang.Controller.MouseSensitivity,
                        1,
                        20,
                        new Accessor<float>(() => profile.ControllerMouseSensativity, f => profile.ControllerMouseSensativity = (int)f),
                        search: new SearchMetadata(moveLang.Controller.MouseSensitivity, Keywords: [kw.Controller, kw.Sensitivity])
                    )
                ).WithSearch(new SearchMetadata(moveLang.Controller.Label, Tags: [kw.Movement], Keywords: [kw.Controller]))
            )
        ).WithSearch(new SearchMetadata(moveLang.Label, Keywords: [kw.Movement, kw.Pathfinding, kw.WASD, kw.Move], Tags: [kw.Movement]));
    }
}
