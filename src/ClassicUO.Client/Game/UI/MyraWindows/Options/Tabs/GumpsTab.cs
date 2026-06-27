#nullable enable

using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.MyraWindows.Widgets;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for gump interaction settings (anchoring, dragging, closing behavior)</summary>
internal static class GumpsTab
{
    /// <summary>Returns the option fragment for gump interaction settings</summary>
    internal static IOptionSource GetContent() => GetOptionsContent();

    private static OptionFragment GetOptionsContent()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.GumpsTabLang lang = Language.Instance.GetModernOptionsGumpLanguage.GumpsTab;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

        return OptionsUi.Vertical(
            Option.Checkbox(
                lang.AltForAnchorsGumps,
                new Accessor<bool>(() => profile.HoldDownKeyAltToCloseAnchored),
                null,
                new SearchMetadata(lang.AltForAnchorsGumps, Keywords: [kw.Anchor, kw.Alt, kw.Close])
            ),
            Option.Checkbox(
                lang.AltToMoveGumps,
                new Accessor<bool>(() => profile.HoldAltToMoveGumps),
                null,
                new SearchMetadata(lang.AltToMoveGumps, Keywords: [kw.Move, kw.Drag, kw.Alt])
            ),
            Option.Checkbox(
                lang.CloseEntireAnchorWithRClick,
                new Accessor<bool>(() => profile.CloseAllAnchoredGumpsInGroupWithRightClick),
                null,
                new SearchMetadata(lang.CloseEntireAnchorWithRClick, Keywords: [kw.Anchor, kw.Right, kw.RightClick, kw.Group])
            ),
            Option.Spacer(),
            Option.Checkbox(
                lang.OriginalSkillsGump,
                new Accessor<bool>(() => profile.StandardSkillsGump),
                null,
                new SearchMetadata(lang.OriginalSkillsGump, Keywords: [kw.Skill, kw.Old, kw.Original])
            ),
            Option.Checkbox(
                lang.OldStatusGump,
                new Accessor<bool>(() => profile.UseOldStatusGump),
                null,
                new SearchMetadata(lang.OldStatusGump, Keywords: [kw.Old, kw.Status])
            ),
            Option.Checkbox(
                lang.PartyInviteGump,
                new Accessor<bool>(() => profile.PartyInviteGump),
                null,
                new SearchMetadata(lang.PartyInviteGump, Keywords: [kw.Party, kw.Invite])
            ),
            Option.Spacer(),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.UseImprovedBuffBar), lang.EnableImprovedBuffGump),
                Option.HuePicker(
                    lang.BuffGumpHue,
                    new Accessor<ushort>(() => profile.ImprovedBuffBarHue),
                    new SearchMetadata(lang.BuffGumpHue, Keywords: [kw.Buff, kw.BuffBar, kw.Hue, kw.Color, kw.Colour])
                )
            ).WithSearch(new SearchMetadata(Tags: [kw.Gump], Keywords: [kw.Buff, kw.BuffBar])),
            Option.Spacer(),
            Option.Checkbox(
                lang.EnableAdvancedShopGump,
                new Accessor<bool>(() => profile.UseModernShopGump),
                null,
                new SearchMetadata(lang.EnableAdvancedShopGump, Keywords: [kw.Modern, kw.Advanced, kw.Shop, kw.Vendor])
            ),
            Option.Checkbox(
                lang.EnableGumpOpacityAdjustViaAltScroll,
                new Accessor<bool>(() => profile.EnableAlphaScrollingOnGumps),
                null,
                new SearchMetadata(
                    lang.EnableGumpOpacityAdjustViaAltScroll,
                    Keywords: [kw.Alpha, kw.Opacity, kw.Transparency, kw.Scroll, kw.AltScroll]
                )
            )
        ).WithSearch(new SearchMetadata(Tags: [kw.Gump, kw.Interface, kw.Window]));
    }
}
