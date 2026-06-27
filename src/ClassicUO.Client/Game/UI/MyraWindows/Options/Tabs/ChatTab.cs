using System;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for chat and text settings (speech, journal, guild, and party chat)</summary>
public static class ChatTab
{
    /// <summary>Returns the tab group containing speech, journal, guild, and party chat sub-tabs</summary>
    internal static IOptionSource GetContent() => GetChatMenuTabs();

    private static OptionTabGroup GetChatMenuTabs()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.ChatTabLang chatLang = lang.ChatTab;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return new OptionTabGroup()
            .AddTab(
                chatLang.Speech.Label,
                SpeechTab.GetContent,
                new SearchMetadata(chatLang.Speech.Label, Keywords: [kw.Speech, kw.Talk])
            )
            .AddTab(
                chatLang.Journal.Label,
                GetJournalSubTabContentSource,
                new SearchMetadata(chatLang.Journal.Label, Keywords: [kw.Journal, kw.Log, kw.History])
            )
            .AddTab(
                chatLang.FontTab.FontsLabel,
                FontsTab.GetContent,
                new SearchMetadata(chatLang.FontTab.FontsLabel, Keywords: [kw.Font, kw.Text, kw.Style])
            );
    }

    #region Journal

    private static IOptionSource GetJournalSubTabContentSource()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.ChatTabLang.JournalSection journalLang = lang.ChatTab.Journal;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return OptionsUi.Vertical(
            GetJournalSubTabContent()
        ).WithSearch(new SearchMetadata(journalLang.Label, [kw.Journal, kw.Log]));
    }

    private static OptionFragment GetJournalSubTabContent()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.ChatTabLang.JournalSection journalLang = lang.ChatTab.Journal;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = journalLang.Label, LabelLink = "https://tazuo.org/wiki/tazuojournal/" },
            Option.Slider(
                journalLang.MaxJournalEntries,
                100,
                2000,
                new Accessor<int>(() => profile.MaxJournalEntries),
                search: new SearchMetadata(journalLang.MaxJournalEntries)
            ),
            Option.Slider(
                journalLang.JournalOpacity,
                0,
                100,
                new Accessor<float>(() => profile.JournalOpacity, newValue =>
                {
                    profile.JournalOpacity = (byte)newValue;
                    ResizableJournal.UpdateJournalOptions();
                }),
                search: new SearchMetadata(journalLang.JournalOpacity)
            ),
            Option.ComboBox(
                journalLang.JournalStyle,
                profile.JournalStyle,
                Enum.GetNames<ResizableJournal.BorderStyle>(),
                newValue => profile.JournalStyle = newValue,
                search: new SearchMetadata(journalLang.JournalStyle)
            ),
            Option.HuePicker(
                journalLang.JournalBackgroundColor,
                new Accessor<ushort>(() => profile.AltJournalBackgroundHue, h =>
                {
                    profile.AltJournalBackgroundHue = h;
                    ResizableJournal.UpdateJournalOptions();
                }),
                new SearchMetadata(journalLang.JournalBackgroundColor)
            ),
            Option.Checkbox(
                journalLang.JournalHideBorders,
                new Accessor<bool>(() => profile.HideJournalBorder),
                search: new SearchMetadata(journalLang.JournalHideBorders)
            ),
            Option.Checkbox(
                journalLang.HideTimestamp,
                new Accessor<bool>(() => profile.HideJournalTimestamp),
                search: new SearchMetadata(journalLang.HideTimestamp)
            ),
            Option.Checkbox(
                journalLang.JournalHideSystemPrefix,
                new Accessor<bool>(() => profile.HideJournalSystemPrefix),
                search: new SearchMetadata(journalLang.JournalHideSystemPrefix)
            ),
            Option.Checkbox(
                journalLang.MakeAnchorable,
                new Accessor<bool>(() => profile.JournalAnchorEnabled),
                search: new SearchMetadata(journalLang.MakeAnchorable)
            ),
            Option.Checkbox(
                journalLang.SaveJournalToFile,
                new Accessor<bool>(() => profile.SaveJournalToFile),
                search: new SearchMetadata(journalLang.SaveJournalToFile)
            )
        );
    }

    #endregion
}
