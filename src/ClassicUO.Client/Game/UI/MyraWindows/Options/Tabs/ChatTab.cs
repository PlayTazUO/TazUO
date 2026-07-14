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

        return new OptionTabGroup()
            .AddTab(
                TazLang.Get("mog_chattab_speech_label"),
                SpeechTab.GetContent,
                new SearchMetadata(TazLang.Get("mog_chattab_speech_label"), Keywords: [TazLang.Get("mog_kw_speech"), TazLang.Get("mog_kw_talk")])
            )
            .AddTab(
                TazLang.Get("mog_chattab_journal_label"),
                GetJournalSubTabContentSource,
                new SearchMetadata(TazLang.Get("mog_chattab_journal_label"), Keywords: [TazLang.Get("mog_kw_journal"), TazLang.Get("mog_kw_log"), TazLang.Get("mog_kw_history")])
            )
            .AddTab(
                TazLang.Get("mog_chattab_fonttab_fontslabel"),
                FontsTab.GetContent,
                new SearchMetadata(TazLang.Get("mog_chattab_fonttab_fontslabel"), Keywords: [TazLang.Get("mog_kw_font"), TazLang.Get("mog_kw_text"), TazLang.Get("mog_kw_style")])
            );
    }

    #region Journal

    private static IOptionSource GetJournalSubTabContentSource()
    {

        return OptionsUi.Vertical(
            GetJournalSubTabContent()
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_chattab_journal_label"), [TazLang.Get("mog_kw_journal"), TazLang.Get("mog_kw_log")]));
    }

    private static OptionFragment GetJournalSubTabContent()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = TazLang.Get("mog_chattab_journal_label"), LabelLink = "https://tazuo.org/wiki/tazuojournal/" },
            Option.Slider(
                TazLang.Get("mog_chattab_journal_maxjournalentries"),
                100,
                2000,
                new Accessor<int>(() => profile.MaxJournalEntries),
                search: new SearchMetadata(TazLang.Get("mog_chattab_journal_maxjournalentries"))
            ),
            Option.Slider(
                TazLang.Get("mog_chattab_journal_journalopacity"),
                0,
                100,
                new Accessor<float>(() => profile.JournalOpacity, newValue =>
                {
                    profile.JournalOpacity = (byte)newValue;
                    ResizableJournal.UpdateJournalOptions();
                }),
                search: new SearchMetadata(TazLang.Get("mog_chattab_journal_journalopacity"))
            ),
            Option.ComboBox(
                TazLang.Get("mog_chattab_journal_journalstyle"),
                profile.JournalStyle,
                Enum.GetNames<ResizableJournal.BorderStyle>(),
                newValue => profile.JournalStyle = newValue,
                search: new SearchMetadata(TazLang.Get("mog_chattab_journal_journalstyle"))
            ),
            Option.HuePicker(
                TazLang.Get("mog_chattab_journal_journalbackgroundcolor"),
                new Accessor<ushort>(() => profile.AltJournalBackgroundHue, h =>
                {
                    profile.AltJournalBackgroundHue = h;
                    ResizableJournal.UpdateJournalOptions();
                }),
                new SearchMetadata(TazLang.Get("mog_chattab_journal_journalbackgroundcolor"))
            ),
            Option.Checkbox(
                TazLang.Get("mog_chattab_journal_journalhideborders"),
                new Accessor<bool>(() => profile.HideJournalBorder),
                search: new SearchMetadata(TazLang.Get("mog_chattab_journal_journalhideborders"))
            ),
            Option.Checkbox(
                TazLang.Get("journal_transparencywheninactive", "Journal transparency when not active"),
                new Accessor<bool>(() => profile.JournalTransparencyWhenInactive),
                search: new SearchMetadata(TazLang.Get("journal_transparencywheninactive", "Journal transparency when not active"))
            ),
            Option.Checkbox(
                TazLang.Get("mog_chattab_journal_hidetimestamp"),
                new Accessor<bool>(() => profile.HideJournalTimestamp),
                search: new SearchMetadata(TazLang.Get("mog_chattab_journal_hidetimestamp"))
            ),
            Option.Checkbox(
                TazLang.Get("mog_chattab_journal_journalhidesystemprefix"),
                new Accessor<bool>(() => profile.HideJournalSystemPrefix),
                search: new SearchMetadata(TazLang.Get("mog_chattab_journal_journalhidesystemprefix"))
            ),
            Option.Checkbox(
                TazLang.Get("mog_chattab_journal_makeanchorable"),
                new Accessor<bool>(() => profile.JournalAnchorEnabled),
                search: new SearchMetadata(TazLang.Get("mog_chattab_journal_makeanchorable"))
            ),
            Option.Checkbox(
                TazLang.Get("mog_chattab_journal_savejournaltofile"),
                new Accessor<bool>(() => profile.SaveJournalToFile),
                search: new SearchMetadata(TazLang.Get("mog_chattab_journal_savejournaltofile"))
            )
        );
    }

    #endregion
}
