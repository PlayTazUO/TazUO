#nullable enable
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class GumpsTab
{
    public static Widget GetContent()
    {
        ModernOptionsGumpLanguage.GumpsTabLang lang = Language.Instance.GetModernOptionsGumpLanguage.GumpsTab;
        return new OptionItem(lang.GumpsLabel, GetSection);
    }

    private static WrapPanel GetSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.GumpsTabLang lang = Language.Instance.GetModernOptionsGumpLanguage.GumpsTab;

        return OptionTabCommons.StyledVerticalWrapPanel(
            OptionsFactory.CreateCheckboxOption(lang.AltForAnchorsGumps, new Accessor<bool>(() => profile.HoldDownKeyAltToCloseAnchored)),
            OptionsFactory.CreateCheckboxOption(lang.AltToMoveGumps, new Accessor<bool>(() => profile.HoldAltToMoveGumps)),
            OptionsFactory.CreateCheckboxOption(lang.CloseEntireAnchorWithRClick,
                new Accessor<bool>(() => profile.CloseAllAnchoredGumpsInGroupWithRightClick)),
            OptionsFactory.CreateSpacer(),
            OptionsFactory.CreateCheckboxOption(lang.OriginalSkillsGump, new Accessor<bool>(() => profile.StandardSkillsGump)),
            OptionsFactory.CreateCheckboxOption(lang.OldStatusGump, new Accessor<bool>(() => profile.UseOldStatusGump)),
            OptionsFactory.CreateCheckboxOption(lang.PartyInviteGump, new Accessor<bool>(() => profile.PartyInviteGump)),
            OptionsFactory.CreateSpacer(),
            new CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.UseImprovedBuffBar), lang.EnableImprovedBuffGump),
                OptionsFactory.PropBoundHuePicker(lang.BuffGumpHue, new Accessor<ushort>(() => profile.ImprovedBuffBarHue))
            ),
            OptionsFactory.CreateSpacer(),
            OptionsFactory.CreateCheckboxOption(lang.EnableAdvancedShopGump, new Accessor<bool>(() => profile.UseModernShopGump)),
            OptionsFactory.CreateCheckboxOption(lang.EnableGumpOpacityAdjustViaAltScroll, new Accessor<bool>(() => profile.EnableAlphaScrollingOnGumps))
        );
    }
}
