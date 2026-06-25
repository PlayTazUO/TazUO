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
        ModernOptionsGumpLanguage.FontTabLang fontsLang = Language.Instance.GetModernOptionsGumpLanguage.ChatTab.FontTab;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

        return OptionsUi.Vertical(
            Option.Spacer(),
            Option.Custom(
                () => new LinkLabel(fontsLang.FontsWikiLabel, "https://tazuo.org/wiki/tazuottf-fonts/")
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch
                },
                new SearchMetadata(fontsLang.FontsWikiLabel, Keywords: [kw.Wiki, kw.Help])
            ),
            UniformHorizontal(
                CreateFontSelectorFragment(
                    fontsLang.InfoBarFont,
                    new Accessor<string>(() => profile.InfoBarFont),
                    new Accessor<int>(() => profile.InfoBarFontSize),
                    InfoBarGump.UpdateAllOptions
                ),
                CreateFontSelectorFragment(
                    fontsLang.SystemChatFont,
                    new Accessor<string>(() => profile.GameWindowSideChatFont),
                    new Accessor<int>(() => profile.GameWindowSideChatFontSize)
                ),
                CreateFontSelectorFragment(
                    fontsLang.TooltipFont,
                    new Accessor<string>(() => profile.SelectedToolTipFont),
                    new Accessor<int>(() => profile.SelectedToolTipFontSize)
                ),
                CreateFontSelectorFragment(
                    fontsLang.OverheadFont,
                    new Accessor<string>(() => profile.OverheadChatFont),
                    new Accessor<int>(() => profile.OverheadChatFontSize)
                ),
                CreateFontSelectorFragment(
                    fontsLang.JournalFont,
                    new Accessor<string>(() => profile.SelectedTTFJournalFont),
                    new Accessor<int>(() => profile.SelectedJournalFontSize),
                    ResizableJournal.UpdateJournalOptions
                ),
                CreateFontSelectorFragment(
                    fontsLang.NameplateFont,
                    new Accessor<string>(() => profile.NamePlateFont),
                    new Accessor<int>(() => profile.NamePlateFontSize)
                ),
                CreateFontSelectorFragment(
                    fontsLang.OptionsFont,
                    new Accessor<string>(() => profile.OptionsFont),
                    new Accessor<int>(() => profile.OptionsFontSize)
                )
            )
        ).WithSearch(new SearchMetadata(fontsLang.FontsLabel, Keywords: [kw.Font, kw.Text, kw.Style], Tags: [kw.Font, kw.Style]));
    }

    private static OptionFragment UniformHorizontal(params OptionContent[] children) =>
        new(
            () =>
            {
                Widget[] widgets = children.Select(c => c.Render()).ToArray();
                WrapPanel panel = OptionTabCommons.StyledHorizontalWrapPanel(widgets);
                panel.UniformSizing = true;
                panel.VerticalAlignment = VerticalAlignment.Center;
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
        ModernOptionsGumpLanguage.FontTabLang fontsLang = Language.Instance.GetModernOptionsGumpLanguage.ChatTab.FontTab;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

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
            Option.FontSelector(fontsLang.FontLabel, fontPropToUse, search: new SearchMetadata(label, Keywords: [kw.Font])),
            Option.Slider(
                fontsLang.Size,
                5,
                50,
                new Accessor<float>(() => fontSizePropToUse.Get(), f => fontSizePropToUse.Set((int)f)),
                search: new SearchMetadata(label, Keywords: [kw.Size])
            )
        );
    }
}
