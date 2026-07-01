using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for speech text, chat-gradient, and overhead text display settings</summary>
internal static class SpeechTab
{
    /// <summary>Returns the option fragment for speech delay, color, and overhead text options</summary>
    internal static IOptionSource GetContent()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.ChatTabLang.SpeechSection speechLang = lang.ChatTab.Speech;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        string disableSysChatWhileJournal = TazLang.Get("disablesystemchat_journalopen", "Disable system chat while Resizable Journal is open");

        return OptionsUi.Vertical(
            GetDelaySection(),
            OptionsUi.Vertical(
                Option.Checkbox(
                    speechLang.ChatGradient,
                    new Accessor<bool>(() => profile.HideChatGradient),
                    search: new SearchMetadata(speechLang.ChatGradient)
                ),
                Option.Checkbox(
                    speechLang.HideGuildChat,
                    new Accessor<bool>(() => profile.IgnoreGuildMessages),
                    search: new SearchMetadata(speechLang.HideGuildChat)
                ),
                Option.Checkbox(
                    speechLang.HideAllianceChat,
                    new Accessor<bool>(() => profile.IgnoreAllianceMessages),
                    search: new SearchMetadata(speechLang.HideAllianceChat)
                ),
                Option.Checkbox(
                    speechLang.DisableSystemChat,
                    new Accessor<bool>(() => profile.DisableSystemChat),
                    search: new SearchMetadata(speechLang.DisableSystemChat)
                ),
                Option.Checkbox(
                    disableSysChatWhileJournal,
                    new Accessor<bool>(() => profile.DisableSystemChatWhileJournalOpen),
                    search: new SearchMetadata(disableSysChatWhileJournal)
                )
            ),
            GetActivationSection(),
            GetOverheadDisplaySection(),
            OptionsUi.Horizontal(
                GetColorSection(),
                GetOverheadSuppressionSection()
            )
        ).WithSearch(new SearchMetadata(speechLang.Label, [kw.Speech, kw.Chat, kw.Text]));
    }

    private static OptionFragment GetDelaySection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.ChatTabLang.SpeechSection speechLang = lang.ChatTab.Speech;

        return OptionsUi.CheckBoxGroup(
            new PropertyBinder(new Accessor<bool>(() => profile.ScaleSpeechDelay), speechLang.ScaleSpeechDelay),
            Option.Slider(
                speechLang.SpeechDelay,
                0,
                1000,
                new Accessor<int>(() => profile.SpeechDelay),
                search: new SearchMetadata(speechLang.SpeechDelay)
            )
        ).WithSearch(new SearchMetadata(speechLang.ScaleSpeechDelay));
    }

    private static OptionFragment GetActivationSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.ChatTabLang.SpeechSection speechLang = lang.ChatTab.Speech;

        return OptionsUi.CheckBoxGroup(
            new PropertyBinder(new Accessor<bool>(() => profile.ActivateChatAfterEnter), speechLang.ChatEnterActivation),
            Option.Checkbox(
                speechLang.ChatEnterSpecial,
                new Accessor<bool>(() => profile.ActivateChatAdditionalButtons),
                search: new SearchMetadata(speechLang.ChatEnterSpecial)
            ),
            Option.Checkbox(
                speechLang.ShiftEnterChat,
                new Accessor<bool>(() => profile.ActivateChatShiftEnterSupport),
                search: new SearchMetadata(speechLang.ShiftEnterChat)
            )
        ).WithSearch(new SearchMetadata(speechLang.ChatEnterActivation));
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
            Option.HuePicker(speechLang.PartyColor, new Accessor<ushort>(() => profile.PartyMessageHue), new SearchMetadata(speechLang.PartyColor)),
            Option.HuePicker(speechLang.AllianceColor, new Accessor<ushort>(() => profile.AllyMessageHue), new SearchMetadata(speechLang.AllianceColor)),
            Option.HuePicker(speechLang.EmoteColor, new Accessor<ushort>(() => profile.EmoteHue), new SearchMetadata(speechLang.EmoteColor)),
            Option.HuePicker(speechLang.WhisperColor, new Accessor<ushort>(() => profile.WhisperHue), new SearchMetadata(speechLang.WhisperColor)),
            Option.HuePicker(speechLang.GuildColor, new Accessor<ushort>(() => profile.GuildMessageHue), new SearchMetadata(speechLang.GuildColor)),
            Option.HuePicker(speechLang.CharColor, new Accessor<ushort>(() => profile.ChatMessageHue), new SearchMetadata(speechLang.CharColor))
        );
    }

    private static OptionFragment GetOverheadDisplaySection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.ChatTabLang.SpeechSection speechLang = lang.ChatTab.Speech;
        ModernOptionsGumpLanguage.TazUO tuoLang = lang.GetTazUO;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = speechLang.OverheadText },
            Option.Checkbox(
                tuoLang.DisplayPartyChatOverPlayerHeads,
                new Accessor<bool>(() => profile.DisplayPartyChatOverhead),
                tuoLang.TooltipPartyChat,
                new SearchMetadata(tuoLang.DisplayPartyChatOverPlayerHeads, Keywords: [kw.Party])
            ),
            Option.Slider(
                tuoLang.OverheadTextWidth,
                0,
                600,
                new Accessor<int>(() => profile.OverheadChatWidth)
            ),
            Option.Checkbox(
                tuoLang.DisableMouseInteractionsForOverheadText,
                new Accessor<bool>(() => profile.DisableMouseInteractionOverheadText),
                search: new SearchMetadata(tuoLang.DisableMouseInteractionsForOverheadText, Keywords: [kw.Mouse])
            )
        ).AsSearchGroup()
         .WithSearch(new SearchMetadata(Keywords: [kw.Overhead]));
    }

    private static OptionFragment GetOverheadSuppressionSection()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.ChatTabLang.SpeechSection speechLang = lang.ChatTab.Speech;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = speechLang.DisableOverheadMessages },
            OptionsUi.Horizontal(CreateMessageTypeCheckboxes())
        );
    }

    private static OptionFragment CreateMessageTypeCheckboxes()
    {
        Profile profile = ProfileManager.CurrentProfile;

        MessageType[] messageTypes = Enum.GetValues<MessageType>();

        var options = new List<OptionContent>(messageTypes.Length);

        foreach (MessageType mType in messageTypes)
            options.Add(
                Option.Checkbox(
                    LocalizeMessageType(mType),
                    new Accessor<bool>(
                        () => MessageTypeFilter.IsEnabled(profile.DisabledOverheadMessageTypes, mType),
                        enabled => MessageTypeFilter.SetEnabled(profile.DisabledOverheadMessageTypes, mType, enabled)
                    ),
                    mType.ToString()
                )
            );

        return new OptionFragment(
            () =>
            {
                var panel = new WrapPanel
                {
                    Orientation = Orientation.Vertical,
                    Aligned = true,
                    UniformSizing = true,
                    VerticalSpacing = MyraStyle.STANDARD_SPACING,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(MyraStyle.STANDARD_SPACING, 10, MyraStyle.STANDARD_SPACING, 10),
                    MaxHeight = 300
                };

                panel.AddRange(options.Select(c => c.Render()).ToArray());

                return panel;
            },
            options
        );
    }

    private static string LocalizeMessageType(MessageType mType)
    {
        ModernOptionsGumpLanguage.ChatTabLang.SpeechSection speechLang = Language.Instance.GetModernOptionsGumpLanguage.ChatTab.Speech;
        string mTypeString = Enum.GetName(mType);
        string tazLangLoc = TazLang.Get(mTypeString);

        if (!string.IsNullOrWhiteSpace(tazLangLoc))
            return tazLangLoc;

        return mTypeString switch
        {
            "Regular" => speechLang.MessageTypeRegular,
            "System" => speechLang.MessageTypeSystem,
            "Emote" => speechLang.MessageTypeEmote,
            "Limit3Spell" => speechLang.MessageTypeLimit3Spell,
            "Label" => speechLang.MessageTypeLabel,
            "Focus" => speechLang.MessageTypeFocus,
            "Whisper" => speechLang.MessageTypeWhisper,
            "Yell" => speechLang.MessageTypeYell,
            "Spell" => speechLang.MessageTypeSpell,
            "Guild" => speechLang.MessageTypeGuild,
            "Alliance" => speechLang.MessageTypeAlliance,
            "Command" => speechLang.MessageTypeCommand,
            "Encoded" => speechLang.MessageTypeEncoded,
            "ChatSystem" => speechLang.MessageTypeChatSystem,
            "Damage" => speechLang.MessageTypeDamage,
            "Discord" => speechLang.MessageTypeDiscord,
            "Party" => speechLang.MessageTypeParty,
            _ => mType.ToString()
        };
    }
}
