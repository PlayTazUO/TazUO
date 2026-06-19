#nullable enable

using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Widgets;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

internal static class GumpsTab
{
    internal static IOptionSource GetContent() => GetOptionsContent();

    private static OptionFragment GetOptionsContent()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.GumpsTabLang lang = Language.Instance.GetModernOptionsGumpLanguage.GumpsTab;

        return OptionsUi.Vertical(
            Option.Checkbox(
                lang.AltForAnchorsGumps,
                new Accessor<bool>(() => profile.HoldDownKeyAltToCloseAnchored),
                null,
                new SearchMetadata(lang.AltForAnchorsGumps, Keywords: ["Anchor", "Anchored", "Alt", "Close"])
            ),
            Option.Checkbox(
                lang.AltToMoveGumps,
                new Accessor<bool>(() => profile.HoldAltToMoveGumps),
                null,
                new SearchMetadata(lang.AltToMoveGumps, Keywords: ["Move", "Moving", "Drag", "Alt"])
            ),
            Option.Checkbox(
                lang.CloseEntireAnchorWithRClick,
                new Accessor<bool>(() => profile.CloseAllAnchoredGumpsInGroupWithRightClick),
                null,
                new SearchMetadata(lang.CloseEntireAnchorWithRClick, Keywords: ["Anchor", "Anchored", "Right", "Right Click", "Group"])
            ),
            Option.Spacer(),
            Option.Checkbox(
                lang.OriginalSkillsGump,
                new Accessor<bool>(() => profile.StandardSkillsGump),
                null,
                new SearchMetadata(lang.OriginalSkillsGump, Keywords: ["Move", "Alt"])
            ),
            Option.Checkbox(
                lang.OldStatusGump,
                new Accessor<bool>(() => profile.UseOldStatusGump),
                null,
                new SearchMetadata(lang.OldStatusGump, Keywords: ["Old", "Status"])
            ),
            Option.Checkbox(
                lang.PartyInviteGump,
                new Accessor<bool>(() => profile.PartyInviteGump),
                null,
                new SearchMetadata(lang.PartyInviteGump, Keywords: ["Party", "Invite"])
            ),
            Option.Spacer(),
            OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = lang.EnableImprovedBuffGump },
                Option.Checkbox(
                    lang.EnableImprovedBuffGump,
                    new Accessor<bool>(() => profile.UseImprovedBuffBar),
                    null,
                    new SearchMetadata(lang.EnableImprovedBuffGump, Keywords: ["Improved", "Buff", "Buff Bar", "Buff Gump"])
                ),
                Option.HuePicker(
                    lang.BuffGumpHue,
                    new Accessor<ushort>(() => profile.ImprovedBuffBarHue),
                    new SearchMetadata(lang.BuffGumpHue, Keywords: ["Buff", "Buff Bar", "Hue", "Color", "Colour"])
                )
            ),
            Option.Spacer(),
            Option.Checkbox(
                lang.EnableAdvancedShopGump,
                new Accessor<bool>(() => profile.UseModernShopGump),
                null,
                new SearchMetadata(lang.EnableAdvancedShopGump, Keywords: ["Modern", "Advanced", "Shop", "Vendor"])
            ),
            Option.Checkbox(
                lang.EnableGumpOpacityAdjustViaAltScroll,
                new Accessor<bool>(() => profile.EnableAlphaScrollingOnGumps),
                null,
                new SearchMetadata(lang.EnableGumpOpacityAdjustViaAltScroll, Keywords: ["Alpha", "Opacity", "Transparency", "Scroll", "Alt Scroll"])
            )
        ).WithSearch(new SearchMetadata(Tags: ["Gump", "Gumps", "Interface", "Window", "Windows"]));
    }
}
