using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
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
        ModernOptionsGumpLanguage.MovementTabLang movementTabLang = Language.Instance.GetModernOptionsGumpLanguage.MovementTab;
        ModernOptionsGumpLanguage.LayerHidingTabLang layerHidingLang = Language.Instance.GetModernOptionsGumpLanguage.LayerHidingTab;

        var tabs = new MyraTabControl();
        tabs.AddTab(lang.ButtonCombatSpells, CombatTab.GetContent);
        tabs.AddTab(lang.ButtonMobiles, MobilesTab.GetContent);
        tabs.AddTab(movementTabLang.Movement, MovementTab.GetContent);
        tabs.AddTab(lang.ButtonTerrainStatics, GetTerrainAndStaticsSubTabContent);
        tabs.AddTab(layerHidingLang.LayerHiding, LayerHidingTab.GetContent);
        return tabs;
    }

    private static WrapPanel GetTerrainAndStaticsSubTabContent()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.General generalLang = Language.Instance.GetModernOptionsGumpLanguage.GetGeneral;
        ModernOptionsGumpLanguage.TazUO tuoLang = Language.Instance.GetModernOptionsGumpLanguage.GetTazUO;

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
            ),
            OptionsFactory.CreateCheckboxOption(
                tuoLang.ApplyBorderCaveTiles,
                profile.EnableCaveBorder,
                newValue =>
                {
                    profile.EnableCaveBorder = newValue;
                    // This looks buggy in the source (i.e., the old windows option).
                    // What happens when this is reset to false? Needs a game restart?
                    if (newValue)
                        StaticFilters.ApplyCaveTileBorder();
                }
            )
        );
    }
}
