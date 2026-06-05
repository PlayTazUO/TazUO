using System;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class FontsTab
{
    internal static OptionItem GetContent()
    {
        ModernOptionsGumpLanguage.FontTabLang fontsLang = Language.Instance.GetModernOptionsGumpLanguage.ChatTab.FontTab;
        return new OptionItem(fontsLang.FontsLabel, GetSection);
    }

    private static StackPanel GetSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.FontTabLang fontsLang = Language.Instance.GetModernOptionsGumpLanguage.ChatTab.FontTab;

        // We need special styling here so no point in using the factory
        var panel = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            UniformSizing = true,
            Aligned = false,
            VerticalSpacing = MyraStyle.STANDARD_SPACING,
            VerticalAlignment = VerticalAlignment.Center
        };

        panel.AddRange(
            CreateFontSelectorSection(
                fontsLang.InfoBarFont,
                new Accessor<string>(() => profile.InfoBarFont),
                new Accessor<int>(() => profile.InfoBarFontSize),
                InfoBarGump.UpdateAllOptions
            ),
            CreateFontSelectorSection(
                fontsLang.SystemChatFont,
                new Accessor<string>(() => profile.GameWindowSideChatFont),
                new Accessor<int>(() => profile.GameWindowSideChatFontSize)
            ),
            CreateFontSelectorSection(
                fontsLang.TooltipFont,
                new Accessor<string>(() => profile.SelectedToolTipFont),
                new Accessor<int>(() => profile.SelectedToolTipFontSize)
            ),
            CreateFontSelectorSection(
                fontsLang.OverheadFont,
                new Accessor<string>(() => profile.OverheadChatFont),
                new Accessor<int>(() => profile.OverheadChatFontSize)
            ),
            CreateFontSelectorSection(
                fontsLang.JournalFont,
                new Accessor<string>(() => profile.SelectedTTFJournalFont),
                new Accessor<int>(() => profile.SelectedJournalFontSize),
                ResizableJournal.UpdateJournalOptions
            ),
            CreateFontSelectorSection(
                fontsLang.NameplateFont,
                new Accessor<string>(() => profile.NamePlateFont),
                new Accessor<int>(() => profile.NamePlateFontSize)
            ),
            CreateFontSelectorSection(
                fontsLang.OptionsFont,
                new Accessor<string>(() => profile.OptionsFont),
                new Accessor<int>(() => profile.OptionsFontSize)
            )
        );

        return OptionTabCommons.StyledStackPanel(
            Orientation.Vertical,
            OptionsFactory.CreateSpacer(),
            new LinkLabel(fontsLang.FontsWikiLabel, "https://tazuo.org/wiki/tazuottf-fonts/") { HorizontalAlignment = HorizontalAlignment.Stretch },
            panel
        );
    }

    private static VisualContainer CreateFontSelectorSection(
        string label,
        Accessor<string> fontProp,
        Accessor<int> fontSizeProp,
        Action onAfterUpdate = null
    )
    {
        ModernOptionsGumpLanguage.FontTabLang fontsLang = Language.Instance.GetModernOptionsGumpLanguage.ChatTab.FontTab;

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

        OptionItem sizeSlider = OptionsFactory.PropBoundSliderOption(fontsLang.Size, fontSizePropToUse, 5, 50, true);

        return new VisualContainer(
            new VisualContainerProps { LabelText = label },
            OptionTabCommons.StyledFontSelector(fontsLang.FontLabel, fontPropToUse),
            sizeSlider
        );
    }
}
