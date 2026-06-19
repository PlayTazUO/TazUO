using System;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class ChatTab
{
    internal static IOptionSource GetContent() => GetChatMenuTabs();

    private static OptionTabGroup GetChatMenuTabs()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.ChatTabLang chatLang = lang.ChatTab;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return new OptionTabGroup()
            .AddTab(
                chatLang.Speech.Label,
                GetSpeechSubTabContent,
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

    #region Speech

    private static IOptionSource GetSpeechSubTabContent()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.ChatTabLang.SpeechSection speechLang = lang.ChatTab.Speech;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return OptionsUi.Vertical(
            GetDelaySection(),
            OptionsUi.Vertical(
                Option.Checkbox(
                    speechLang.ChatGradient,
                    new Accessor<bool>(() => profile.HideChatGradient),
                    search: new SearchMetadata(speechLang.ChatGradient)
                ),
                Option.Checkbox(speechLang.HideGuildChat, new Accessor<bool>(() => profile.IgnoreGuildMessages),
                    search: new SearchMetadata(speechLang.HideGuildChat)),
                Option.Checkbox(speechLang.HideAllianceChat, new Accessor<bool>(() => profile.IgnoreAllianceMessages),
                    search: new SearchMetadata(speechLang.HideAllianceChat)),
                Option.Checkbox(speechLang.DisableSystemChat, new Accessor<bool>(() => profile.DisableSystemChat),
                    search: new SearchMetadata(speechLang.DisableSystemChat))
            ),
            GetActivationSection(),
            GetColorSection()
        ).WithSearch(new SearchMetadata(speechLang.Label, [kw.Speech, kw.Chat, kw.Text]));
    }

    private static OptionFragment GetDelaySection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.ChatTabLang.SpeechSection speechLang = lang.ChatTab.Speech;

        return OptionsUi.Vertical(
            Option.Checkbox(speechLang.ScaleSpeechDelay, new Accessor<bool>(() => profile.ScaleSpeechDelay),
                search: new SearchMetadata(speechLang.ScaleSpeechDelay)),
            Option.Slider(speechLang.SpeechDelay, 0, 1000, new Accessor<float>(() => profile.SpeechDelay, f => profile.SpeechDelay = (int)f),
                search: new SearchMetadata(speechLang.SpeechDelay))
        );
    }

    private static OptionFragment GetActivationSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.ChatTabLang.SpeechSection speechLang = lang.ChatTab.Speech;

        return OptionsUi.Vertical(
            Option.Checkbox(speechLang.ChatEnterActivation, new Accessor<bool>(() => profile.ActivateChatAfterEnter),
                search: new SearchMetadata(speechLang.ChatEnterActivation)),
            Option.Checkbox(speechLang.ChatEnterSpecial, new Accessor<bool>(() => profile.ActivateChatAdditionalButtons),
                search: new SearchMetadata(speechLang.ChatEnterSpecial)),
            Option.Checkbox(speechLang.ShiftEnterChat, new Accessor<bool>(() => profile.ActivateChatShiftEnterSupport),
                search: new SearchMetadata(speechLang.ShiftEnterChat))
        );
    }

    private static OptionFragment GetColorSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.ChatTabLang.SpeechSection speechLang = lang.ChatTab.Speech;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = speechLang.ColorsSection },
            Option.HuePicker(speechLang.SpeechColor, new Accessor<ushort>(() => profile.SpeechHue, h => profile.SpeechHue = h),
                new SearchMetadata(speechLang.SpeechColor)),
            Option.HuePicker(
                speechLang.YellColor,
                new Accessor<ushort>(() => profile.YellHue, h => profile.YellHue = h),
                new SearchMetadata(speechLang.YellColor)
            ),
            Option.HuePicker(speechLang.PartyColor, new Accessor<ushort>(() => profile.PartyMessageHue, h => profile.PartyMessageHue = h),
                new SearchMetadata(speechLang.PartyColor)),
            Option.HuePicker(speechLang.AllianceColor, new Accessor<ushort>(() => profile.AllyMessageHue, h => profile.AllyMessageHue = h),
                new SearchMetadata(speechLang.AllianceColor)),
            Option.HuePicker(speechLang.EmoteColor, new Accessor<ushort>(() => profile.EmoteHue, h => profile.EmoteHue = h),
                new SearchMetadata(speechLang.EmoteColor)),
            Option.HuePicker(speechLang.WhisperColor, new Accessor<ushort>(() => profile.WhisperHue, h => profile.WhisperHue = h),
                new SearchMetadata(speechLang.WhisperColor)),
            Option.HuePicker(speechLang.GuildColor, new Accessor<ushort>(() => profile.GuildMessageHue, h => profile.GuildMessageHue = h),
                new SearchMetadata(speechLang.GuildColor)),
            Option.HuePicker(speechLang.CharColor, new Accessor<ushort>(() => profile.ChatMessageHue, h => profile.ChatMessageHue = h),
                new SearchMetadata(speechLang.CharColor))
        );
    }

    #endregion

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
                new Accessor<float>(() => profile.MaxJournalEntries, newValue => profile.MaxJournalEntries = (int)newValue),
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
            Option.Checkbox(journalLang.JournalHideBorders, new Accessor<bool>(() => profile.HideJournalBorder),
                search: new SearchMetadata(journalLang.JournalHideBorders)),
            Option.Checkbox(journalLang.HideTimestamp, new Accessor<bool>(() => profile.HideJournalTimestamp),
                search: new SearchMetadata(journalLang.HideTimestamp)),
            Option.Checkbox(journalLang.JournalHideSystemPrefix, new Accessor<bool>(() => profile.HideJournalSystemPrefix),
                search: new SearchMetadata(journalLang.JournalHideSystemPrefix)),
            Option.Checkbox(journalLang.MakeAnchorable, new Accessor<bool>(() => profile.JournalAnchorEnabled),
                search: new SearchMetadata(journalLang.MakeAnchorable)),
            Option.Checkbox(journalLang.SaveJournalToFile, new Accessor<bool>(() => profile.SaveJournalToFile),
                search: new SearchMetadata(journalLang.SaveJournalToFile))
        );
    }

    #endregion
}
