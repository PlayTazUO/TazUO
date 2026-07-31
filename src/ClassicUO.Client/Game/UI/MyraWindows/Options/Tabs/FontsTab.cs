using System;
using System.Linq;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for font configuration, including per-usage font selectors</summary>
public static class FontsTab
{
    /// <summary>Returns the option fragment for font selectors and wiki link</summary>
    internal static IOptionSource GetContent()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.Vertical(
            Option.Spacer(),
            Option.Custom(
                () => new LinkLabel(TazLang.Get("mog_chattab_fonttab_fontswikilabel"), "https://tazuo.org/wiki/tazuottf-fonts/")
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch
                },
                new SearchMetadata(TazLang.Get("mog_chattab_fonttab_fontswikilabel"), Keywords: [TazLang.Get("mog_kw_wiki"), TazLang.Get("mog_kw_help")])
            ),
            UniformHorizontal(
                CreateFontSelectorFragment(
                    TazLang.Get("mog_chattab_fonttab_infobarfont"),
                    new Accessor<string>(() => profile.InfoBarFont),
                    new Accessor<int>(() => profile.InfoBarFontSize),
                    InfoBarGump.UpdateAllOptions
                ),
                CreateFontSelectorFragment(
                    TazLang.Get("mog_chattab_fonttab_systemchatfont"),
                    new Accessor<string>(() => profile.GameWindowSideChatFont),
                    new Accessor<int>(() => profile.GameWindowSideChatFontSize)
                ),
                CreateFontSelectorFragment(
                    TazLang.Get("mog_chattab_fonttab_tooltipfont"),
                    new Accessor<string>(() => profile.SelectedToolTipFont),
                    new Accessor<int>(() => profile.SelectedToolTipFontSize)
                ),
                CreateFontSelectorFragment(
                    TazLang.Get("mog_chattab_fonttab_overheadfont"),
                    new Accessor<string>(() => profile.OverheadChatFont),
                    new Accessor<int>(() => profile.OverheadChatFontSize)
                ),
                CreateFontSelectorFragment(
                    TazLang.Get("mog_chattab_fonttab_journalfont"),
                    new Accessor<string>(() => profile.SelectedTTFJournalFont),
                    new Accessor<int>(() => profile.SelectedJournalFontSize),
                    ResizableJournal.UpdateJournalOptions
                ),
                CreateFontSelectorFragment(
                    TazLang.Get("mog_chattab_fonttab_nameplatefont"),
                    new Accessor<string>(() => profile.NamePlateFont),
                    new Accessor<int>(() => profile.NamePlateFontSize)
                ),
                CreateFontSelectorFragment(
                    TazLang.Get("mog_chattab_fonttab_optionsfont"),
                    new Accessor<string>(() => profile.OptionsFont),
                    new Accessor<int>(() => profile.OptionsFontSize)
                )
            )
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_chattab_fonttab_fontslabel"), Keywords: [TazLang.Get("mog_kw_font"), TazLang.Get("mog_kw_text"), TazLang.Get("mog_kw_style")], Tags: [TazLang.Get("mog_kw_font"), TazLang.Get("mog_kw_style")]));
    }

    private static OptionFragment UniformHorizontal(params OptionContent[] children) =>
        new(
            () =>
            {
                Widget[] widgets = children.Select(c => c.Render()).ToArray();
                WrapPanel panel = OptionTabCommons.StyledHorizontalWrapPanel(widgets);
                panel.UniformSizing = true;
                panel.VerticalAlignment = VerticalAlignment.Center;
                // Cap to 1000; Wrapping is not perfect, so this sorta forces
                // a 2-column display by default which generally fits better.
                panel.MaxWidth = 1000;
                return panel;
            },
            children
        );

    private static OptionFragment CreateFontSelectorFragment(
        string label,
        Accessor<string> fontProp,
        Accessor<int> fontSizeProp,
        Action onAfterUpdate = null
    )
    {
        Accessor<string> fontPropToUse;
        Accessor<int> fontSizePropToUse;
        if (onAfterUpdate != null)
        {
            fontPropToUse = new Accessor<string>(
                fontProp.Get,
                value =>
                {
                    fontProp.Set(value);
                    onAfterUpdate();
                }
            );

            fontSizePropToUse = new Accessor<int>(
                fontSizeProp.Get,
                value =>
                {
                    fontSizeProp.Set(value);
                    onAfterUpdate();
                }
            );
        }
        else
        {
            fontPropToUse = fontProp;
            fontSizePropToUse = fontSizeProp;
        }

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = label },
            Option.FontSelector(TazLang.Get("mog_chattab_fonttab_fontlabel"), fontPropToUse, search: new SearchMetadata(label, Keywords: [TazLang.Get("mog_kw_font")])),
            Option.Slider(
                TazLang.Get("mog_chattab_fonttab_size"),
                5,
                50,
                new Accessor<float>(() => fontSizePropToUse.Get(), f => fontSizePropToUse.Set((int)f)),
                search: new SearchMetadata(label, Keywords: [TazLang.Get("mog_kw_size")]),
                labelOnLeft: true
            )
        );
    }
}
