using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class MovementTab
{
    internal static OptionItem GetContent()
    {
        ModernOptionsGumpLanguage.MovementTabLang tabLang = Language.Instance.GetModernOptionsGumpLanguage.MovementTab;
        return new OptionItem(tabLang.Movement, GetSection);
    }

    private static WrapPanel GetSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.General genLang = Language.Instance.GetModernOptionsGumpLanguage.GetGeneral;
        ModernOptionsGumpLanguage.MovementTabLang moveLang = Language.Instance.GetModernOptionsGumpLanguage.MovementTab;

        return OptionTabCommons.StyledVerticalWrapPanel(
            new CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.EnablePathfind), genLang.Pathfinding),
                OptionsFactory.CreateCheckboxOption(genLang.ShiftPathfinding, new Accessor<bool>(() => profile.UseShiftToPathfind)),
                OptionsFactory.CreateCheckboxOption(genLang.SingleClickPathfind, new Accessor<bool>(() => profile.PathfindSingleClick))
            ),
            new CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.AlwaysRun), genLang.AlwaysRun),
                OptionsFactory.CreateCheckboxOption(genLang.RunUnlessHidden, new Accessor<bool>(() => profile.AlwaysRunUnlessHidden))
            ),
            OptionsFactory.CreateSpacer(),
            new CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.AutoOpenDoors), genLang.AutoOpenDoors),
                OptionsFactory.CreateCheckboxOption(genLang.AutoOpenPathfinding, new Accessor<bool>(() => profile.SmoothDoors))
            ),
            OptionsFactory.CreateSpacer(),
            OptionsFactory.CreateCheckboxOption(moveLang.AutoAvoidObstacles, new Accessor<bool>(() => profile.AutoAvoidObstacules)),
            OptionsFactory.CreateCheckboxOption(moveLang.UseWasdMovement, new Accessor<bool>(() => profile.UseWASDInsteadArrowKeys))
        );
    }
}
