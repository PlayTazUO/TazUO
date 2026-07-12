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

        return OptionsUi.Vertical(
            Option.Checkbox(
                TazLang.Get("mog_gumpstab_altforanchorsgumps"),
                new Accessor<bool>(() => profile.HoldDownKeyAltToCloseAnchored),
                null,
                new SearchMetadata(TazLang.Get("mog_gumpstab_altforanchorsgumps"), Keywords: [TazLang.Get("mog_kw_anchor"), TazLang.Get("mog_kw_alt"), TazLang.Get("mog_kw_close")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_gumpstab_alttomovegumps"),
                new Accessor<bool>(() => profile.HoldAltToMoveGumps),
                null,
                new SearchMetadata(TazLang.Get("mog_gumpstab_alttomovegumps"), Keywords: [TazLang.Get("mog_kw_move"), TazLang.Get("mog_kw_drag"), TazLang.Get("mog_kw_alt")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_gumpstab_closeentireanchorwithrclick"),
                new Accessor<bool>(() => profile.CloseAllAnchoredGumpsInGroupWithRightClick),
                null,
                new SearchMetadata(TazLang.Get("mog_gumpstab_closeentireanchorwithrclick"), Keywords: [TazLang.Get("mog_kw_anchor"), TazLang.Get("mog_kw_right"), TazLang.Get("mog_kw_rightclick"), TazLang.Get("mog_kw_group")])
            ),
            Option.Spacer(),
            Option.Checkbox(
                TazLang.Get("mog_gumpstab_originalskillsgump"),
                new Accessor<bool>(() => profile.StandardSkillsGump),
                null,
                new SearchMetadata(TazLang.Get("mog_gumpstab_originalskillsgump"), Keywords: [TazLang.Get("mog_kw_skill"), TazLang.Get("mog_kw_old"), TazLang.Get("mog_kw_original")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_gumpstab_oldstatusgump"),
                new Accessor<bool>(() => profile.UseOldStatusGump),
                null,
                new SearchMetadata(TazLang.Get("mog_gumpstab_oldstatusgump"), Keywords: [TazLang.Get("mog_kw_old"), TazLang.Get("mog_kw_status")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_gumpstab_partyinvitegump"),
                new Accessor<bool>(() => profile.PartyInviteGump),
                null,
                new SearchMetadata(TazLang.Get("mog_gumpstab_partyinvitegump"), Keywords: [TazLang.Get("mog_kw_party"), TazLang.Get("mog_kw_invite")])
            ),
            Option.Spacer(),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.UseImprovedBuffBar), TazLang.Get("mog_gumpstab_enableimprovedbuffgump")),
                Option.HuePicker(
                    TazLang.Get("mog_gumpstab_buffgumphue"),
                    new Accessor<ushort>(() => profile.ImprovedBuffBarHue),
                    new SearchMetadata(TazLang.Get("mog_gumpstab_buffgumphue"), Keywords: [TazLang.Get("mog_kw_buff"), TazLang.Get("mog_kw_buffbar"), TazLang.Get("mog_kw_hue"), TazLang.Get("mog_kw_color"), TazLang.Get("mog_kw_colour")])
                )
            ).WithSearch(new SearchMetadata(Tags: [TazLang.Get("mog_kw_gump")], Keywords: [TazLang.Get("mog_kw_buff"), TazLang.Get("mog_kw_buffbar")])),
            Option.Spacer(),
            Option.Checkbox(
                TazLang.Get("mog_gumpstab_enableadvancedshopgump"),
                new Accessor<bool>(() => profile.UseModernShopGump),
                null,
                new SearchMetadata(TazLang.Get("mog_gumpstab_enableadvancedshopgump"), Keywords: [TazLang.Get("mog_kw_modern"), TazLang.Get("mog_kw_advanced"), TazLang.Get("mog_kw_shop"), TazLang.Get("mog_kw_vendor")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_gumpstab_enablegumpopacityadjustviaaltscroll"),
                new Accessor<bool>(() => profile.EnableAlphaScrollingOnGumps),
                null,
                new SearchMetadata(
                    TazLang.Get("mog_gumpstab_enablegumpopacityadjustviaaltscroll"),
                    Keywords: [TazLang.Get("mog_kw_alpha"), TazLang.Get("mog_kw_opacity"), TazLang.Get("mog_kw_transparency"), TazLang.Get("mog_kw_scroll"), TazLang.Get("mog_kw_altscroll")]
                )
            )
        ).WithSearch(new SearchMetadata(Tags: [TazLang.Get("mog_kw_gump"), TazLang.Get("mog_kw_interface"), TazLang.Get("mog_kw_window")]));
    }
}
