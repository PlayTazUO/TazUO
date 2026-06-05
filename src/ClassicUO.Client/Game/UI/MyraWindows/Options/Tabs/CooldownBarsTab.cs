using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Widgets;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public class CooldownBarsTab
{
    internal static OptionItem GetContent()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.CooldownsTabLang cdLang = Language.Instance.GetModernOptionsGumpLanguage.CooldownsTab;

        return new OptionItem(cdLang.CooldownBarsLabel, GetSection);
    }

    private static VisualContainer GetSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.CooldownsTabLang cdLang = Language.Instance.GetModernOptionsGumpLanguage.CooldownsTab;

        return new VisualContainer(
            new VisualContainerProps { LabelText = cdLang.CustomCooldownBars },
            OptionsFactory.PropBoundNumericInput(
                cdLang.PositionX,
                new Accessor<int>(() => profile.CoolDownX),
                0,
                8192
            ),
            OptionsFactory.PropBoundNumericInput(
                cdLang.PositionY,
                new Accessor<int>(() => profile.CoolDownY),
                0,
                8192
            ),
            OptionsFactory.CreateCheckboxOption(cdLang.UseLastMovedBarPosition, new Accessor<bool>(() => profile.UseLastMovedCooldownPosition))
        );
    }
}
