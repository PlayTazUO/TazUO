using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for paperdoll display settings, including modern paperdoll appearance</summary>
public class PaperdollTab
{
    /// <summary>Returns the option fragment for modern-paperdoll enable/disable and styling</summary>
    internal static IOptionSource GetContent()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;
        return OptionsUi.Vertical(
            GetModernPaperdollSection()
        ).WithSearch(new SearchMetadata(lang.ButtonPaperdoll, [kw.Paperdoll, kw.Character, kw.Equipment]));
    }

    private static OptionFragment GetModernPaperdollSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.TazUO tuoLang = lang.GetTazUO;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = tuoLang.ModernPaperdoll, LabelLink = "https://tazuo.org/wiki/alternate-paperdoll/" },
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.UseModernPaperdoll), tuoLang.EnableModernPaperdoll),
                Option.HuePicker(
                    tuoLang.PaperdollHue,
                    new Accessor<ushort>(() => profile.ModernPaperDollHue, newHue =>
                    {
                        profile.ModernPaperDollHue = newHue;
                        ModernPaperdoll.UpdateAllOptions();
                    }),
                    new SearchMetadata(tuoLang.PaperdollHue, Keywords: [kw.Hue, kw.Color])
                ),
                Option.HuePicker(
                    tuoLang.DurabilityBarHue,
                    new Accessor<ushort>(() => profile.ModernPaperDollDurabilityHue, newHue =>
                    {
                        profile.ModernPaperDollDurabilityHue = newHue;
                        ModernPaperdoll.UpdateAllOptions();
                    }),
                    new SearchMetadata(tuoLang.DurabilityBarHue, Keywords: [kw.Durability, kw.Bar, kw.Hue, kw.Color])
                ),
                Option.Slider(
                    tuoLang.ShowDurabilityBarBelow,
                    1,
                    100,
                    new Accessor<float>(() => profile.ModernPaperDoll_DurabilityPercent, f => profile.ModernPaperDoll_DurabilityPercent = (int)f),
                    search: new SearchMetadata(tuoLang.ShowDurabilityBarBelow, Keywords: [kw.Durability, kw.Bar, kw.Below])
                ),
                Option.Checkbox(
                    tuoLang.PaperdollAnchor,
                    new Accessor<bool>(() => profile.ModernPaperdollAnchorEnabled, newValue =>
                    {
                        profile.ModernPaperdollAnchorEnabled = newValue;
                        ModernPaperdoll.UpdateAllOptions();
                    }),
                    search: new SearchMetadata(tuoLang.PaperdollAnchor, Keywords: [kw.Anchor])
                )
            ).WithSearch(new SearchMetadata(Tags: [kw.Paperdoll], Keywords: [kw.Enable]))
        );
    }
}
