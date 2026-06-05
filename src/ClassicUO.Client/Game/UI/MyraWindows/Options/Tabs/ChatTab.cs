using System;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class ChatTab
{
    internal static OptionItem GetContent()
    {
        ModernOptionsGumpLanguage gumpLang = Language.Instance.GetModernOptionsGumpLanguage;
        return new OptionItem(gumpLang.LabelChatAndText, GetChatMenuTabs);
    }

    private static MyraTabControl GetChatMenuTabs()
    {
        ModernOptionsGumpLanguage gumpLang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.TazUO tuoLang = Language.Instance.GetModernOptionsGumpLanguage.GetTazUO;
        ModernOptionsGumpLanguage.FontTabLang fontsLang = Language.Instance.GetModernOptionsGumpLanguage.ChatTab.FontTab;

        var tabs = new MyraTabControl();
        tabs.AddTab(gumpLang.LabelSpeech, GetSpeechSubTabContent);
        tabs.AddTab(tuoLang.Journal, GetJournalSubTabContent);
        tabs.AddTab(fontsLang.FontsLabel, FontsTab.GetContent);
        return tabs;
    }

    #region Speech

    private static OptionItem GetSpeechSubTabContent()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.TazUO tuoLang = Language.Instance.GetModernOptionsGumpLanguage.GetTazUO;

        return new OptionItem(
            lang.LabelSpeech,
            () => OptionTabCommons.StyledVerticalWrapPanel(
                GetDelaySection(),
                OptionsFactory.CreateSpacer(),
                GetActivationSection(),
                OptionsFactory.CreateSpacer(),
                OptionsFactory.CreateCheckboxOption(lang.GetSpeech.ChatGradient, new Accessor<bool>(() => profile.HideChatGradient)),
                OptionsFactory.CreateCheckboxOption(lang.GetSpeech.HideGuildChat, new Accessor<bool>(() => profile.IgnoreGuildMessages)),
                OptionsFactory.CreateCheckboxOption(lang.GetSpeech.HideAllianceChat, new Accessor<bool>(() => profile.IgnoreAllianceMessages)),
                OptionsFactory.CreateCheckboxOption(tuoLang.DisableSystemChat, new Accessor<bool>(() => profile.DisableSystemChat)),
                OptionsFactory.CreateSpacer(),
                GetColorSection()
            )
        );
    }

    private static CheckBoxGroup GetDelaySection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        return new CheckBoxGroup(
            new PropertyBinder(new Accessor<bool>(() => profile.ScaleSpeechDelay), lang.GetSpeech.ScaleSpeechDelay),
            OptionsFactory.CreateSliderOption(lang.GetSpeech.SpeechDelay, 0, 1000, profile.SpeechDelay, f => profile.SpeechDelay = (int)f)
        );
    }

    private static CheckBoxGroup GetActivationSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        return new CheckBoxGroup(
            new PropertyBinder(new Accessor<bool>(() => profile.ActivateChatAfterEnter), lang.GetSpeech.ChatEnterActivation),
            OptionsFactory.CreateCheckboxOption(lang.GetSpeech.ChatEnterSpecial, new Accessor<bool>(() => profile.ActivateChatAdditionalButtons)),
            OptionsFactory.CreateCheckboxOption(lang.GetSpeech.ShiftEnterChat, new Accessor<bool>(() => profile.ActivateChatShiftEnterSupport))
        );
    }

    private static VisualContainer GetColorSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        return new VisualContainer(
            new VisualContainerProps { LabelText = lang.LabelSpeech },
            OptionsFactory.CreateHuePicker(lang.GetSpeech.SpeechColor, profile.SpeechHue, b => profile.SpeechHue = b),
            OptionsFactory.CreateHuePicker(lang.GetSpeech.YellColor, profile.YellHue, b => profile.YellHue = b),
            OptionsFactory.CreateHuePicker(lang.GetSpeech.PartyColor, profile.PartyMessageHue, b => profile.PartyMessageHue = b),
            OptionsFactory.CreateHuePicker(lang.GetSpeech.AllianceColor, profile.AllyMessageHue, b => profile.AllyMessageHue = b),
            OptionsFactory.CreateHuePicker(lang.GetSpeech.EmoteColor, profile.EmoteHue, b => profile.EmoteHue = b),
            OptionsFactory.CreateHuePicker(lang.GetSpeech.WhisperColor, profile.WhisperHue, b => profile.WhisperHue = b),
            OptionsFactory.CreateHuePicker(lang.GetSpeech.GuildColor, profile.GuildMessageHue, b => profile.GuildMessageHue = b),
            OptionsFactory.CreateHuePicker(lang.GetSpeech.CharColor, profile.ChatMessageHue, b => profile.ChatMessageHue = b)
        );
    }

    #endregion

    #region Jouranl

    private static VisualContainer GetJournalSubTabContent()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.TazUO tuoLang = Language.Instance.GetModernOptionsGumpLanguage.GetTazUO;
        ModernOptionsGumpLanguage.Speech speechLang = Language.Instance.GetModernOptionsGumpLanguage.GetSpeech;

        return new VisualContainer(
            new VisualContainerProps { LabelText = tuoLang.Journal, LabelLink = "https://tazuo.org/wiki/tazuojournal/" },
            OptionsFactory.CreateSliderOption(
                tuoLang.MaxJournalEntries,
                100,
                2000,
                profile.MaxJournalEntries,
                newValue => profile.MaxJournalEntries = (int)newValue
            ),
            OptionsFactory.CreateSliderOption(
                tuoLang.JournalOpacity,
                0,
                100,
                profile.JournalOpacity,
                newValue =>
                {
                    profile.JournalOpacity = (byte)newValue;
                    ResizableJournal.UpdateJournalOptions();
                }
            ),
            OptionsFactory.CreateComboBox(
                tuoLang.JournalStyle,
                profile.JournalStyle,
                Enum.GetNames<ResizableJournal.BorderStyle>(),
                newValue => profile.JournalStyle = newValue
            ),
            OptionsFactory.CreateHuePicker(
                tuoLang.JournalBackgroundColor,
                profile.AltJournalBackgroundHue,
                h =>
                {
                    profile.AltJournalBackgroundHue = h;
                    ResizableJournal.UpdateJournalOptions();
                }
            ),
            OptionsFactory.CreateCheckboxOption(tuoLang.JournalHideBorders, new Accessor<bool>(() => profile.HideJournalBorder)),
            OptionsFactory.CreateCheckboxOption(tuoLang.HideTimestamp, new Accessor<bool>(() => profile.HideJournalTimestamp)),
            OptionsFactory.CreateCheckboxOption(tuoLang.JournalHideSystemPrefix, new Accessor<bool>(() => profile.HideJournalSystemPrefix)),
            OptionsFactory.CreateCheckboxOption(tuoLang.MakeAnchorable, new Accessor<bool>(() => profile.JournalAnchorEnabled)),
            OptionsFactory.CreateCheckboxOption(speechLang.SaveJournalE, new Accessor<bool>(() => profile.SaveJournalToFile))
        );
    }

    #endregion
}
