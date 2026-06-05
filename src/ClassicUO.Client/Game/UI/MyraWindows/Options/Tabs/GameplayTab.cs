using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class GameplayTab
{
    internal static OptionItem GetContent()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        return new OptionItem(lang.ButtonGameplay, GetGameplayMenuTabs);
    }

    private static MyraTabControl GetGameplayMenuTabs()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.MovementTabLang movementTabLang = Language.Instance.GetModernOptionsGumpLanguage.MovementTabTab;

        var tabs = new MyraTabControl();
        tabs.AddTab(lang.ButtonCombatSpells, CombatTab.GetContent);
        tabs.AddTab(lang.ButtonMobiles, MobilesTab.GetContent);
        tabs.AddTab(movementTabLang.Movement, MovementTab.GetContent);
        tabs.AddTab(lang.ButtonTerrainStatics, GetTerrainAndStaticsSubTabContent);
        return tabs;
    }

    private static WrapPanel GetTerrainAndStaticsSubTabContent()
    {
        ModernOptionsGumpLanguage.General generalLang = Language.Instance.GetModernOptionsGumpLanguage.GetGeneral;
        Profile profile = ProfileManager.CurrentProfile;

        return OptionTabCommons.StyledVerticalWrapPanel(
            OptionsFactory.CreateCheckboxOption(generalLang.HideRoof, !profile.DrawRoofs, b => profile.DrawRoofs = !b),
            OptionsFactory.CreateCheckboxOption(generalLang.TreesToStump, new Accessor<bool>(() => profile.TreeToStumps)),
            OptionsFactory.CreateCheckboxOption(generalLang.HideVegetation, new Accessor<bool>(() => profile.HideVegetation)),
            OptionsFactory.CreateComboBox(
                generalLang.MagicFieldType,
                profile.FieldsType,
                [
                    generalLang.MagicFieldOpt_Normal,
                    generalLang.MagicFieldOpt_Static,
                    generalLang.MagicFieldOpt_Tile
                ],
                i => profile.FieldsType = i
            )
        );
    }
}
