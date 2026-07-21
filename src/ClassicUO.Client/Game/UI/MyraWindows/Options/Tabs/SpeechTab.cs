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

        string disableSysChatWhileJournal = TazLang.Get("disablesystemchat_journalopen", "Disable system chat while Resizable Journal is open");

        return OptionsUi.Vertical(
            GetDelaySection(),
            OptionsUi.Vertical(
                Option.Checkbox(
                    TazLang.Get("mog_chattab_speech_chatgradient"),
                    new Accessor<bool>(() => profile.HideChatGradient),
                    search: new SearchMetadata(TazLang.Get("mog_chattab_speech_chatgradient"))
                ),
                Option.Checkbox(
                    TazLang.Get("mog_chattab_speech_hideguildchat"),
                    new Accessor<bool>(() => profile.IgnoreGuildMessages),
                    search: new SearchMetadata(TazLang.Get("mog_chattab_speech_hideguildchat"))
                ),
                Option.Checkbox(
                    TazLang.Get("mog_chattab_speech_hidealliancechat"),
                    new Accessor<bool>(() => profile.IgnoreAllianceMessages),
                    search: new SearchMetadata(TazLang.Get("mog_chattab_speech_hidealliancechat"))
                ),
                Option.Checkbox(
                    TazLang.Get("mog_chattab_speech_disablesystemchat"),
                    new Accessor<bool>(() => profile.DisableSystemChat),
                    search: new SearchMetadata(TazLang.Get("mog_chattab_speech_disablesystemchat"))
                ),
                Option.Checkbox(
                    disableSysChatWhileJournal,
                    new Accessor<bool>(() => profile.DisableSystemChatWhileJournalOpen),
                    search: new SearchMetadata(disableSysChatWhileJournal)
                ),
                Option.Checkbox(
                    TazLang.Get("mog_chattab_speech_stripchatusernameid"),
                    new Accessor<bool>(() => profile.StripChatUsernameId),
                    search: new SearchMetadata(TazLang.Get("mog_chattab_speech_stripchatusernameid"))
                )
            ),
            GetActivationSection(),
            GetOverheadDisplaySection(),
            OptionsUi.Horizontal(
                GetColorSection(),
                GetOverheadSuppressionSection()
            )
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_chattab_speech_label"), [TazLang.Get("mog_kw_speech"), TazLang.Get("mog_kw_chat"), TazLang.Get("mog_kw_text")]));
    }

    private static OptionFragment GetDelaySection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.CheckBoxGroup(
            new PropertyBinder(new Accessor<bool>(() => profile.ScaleSpeechDelay), TazLang.Get("mog_chattab_speech_scalespeechdelay")),
            Option.Slider(
                TazLang.Get("mog_chattab_speech_speechdelay"),
                0,
                1000,
                new Accessor<int>(() => profile.SpeechDelay),
                search: new SearchMetadata(TazLang.Get("mog_chattab_speech_speechdelay"))
            )
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_chattab_speech_scalespeechdelay")));
    }

    private static OptionFragment GetActivationSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.CheckBoxGroup(
            new PropertyBinder(new Accessor<bool>(() => profile.ActivateChatAfterEnter), TazLang.Get("mog_chattab_speech_chatenteractivation")),
            Option.Checkbox(
                TazLang.Get("mog_chattab_speech_chatenterspecial"),
                new Accessor<bool>(() => profile.ActivateChatAdditionalButtons),
                search: new SearchMetadata(TazLang.Get("mog_chattab_speech_chatenterspecial"))
            ),
            Option.Checkbox(
                TazLang.Get("mog_chattab_speech_shiftenterchat"),
                new Accessor<bool>(() => profile.ActivateChatShiftEnterSupport),
                search: new SearchMetadata(TazLang.Get("mog_chattab_speech_shiftenterchat"))
            )
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_chattab_speech_chatenteractivation")));
    }

    private static OptionFragment GetColorSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = TazLang.Get("mog_chattab_speech_colorssection") },
            Option.HuePicker(TazLang.Get("mog_chattab_speech_speechcolor"), new Accessor<ushort>(() => profile.SpeechHue, h => profile.SpeechHue = h),
                new SearchMetadata(TazLang.Get("mog_chattab_speech_speechcolor"))),
            Option.HuePicker(
                TazLang.Get("mog_chattab_speech_yellcolor"),
                new Accessor<ushort>(() => profile.YellHue, h => profile.YellHue = h),
                new SearchMetadata(TazLang.Get("mog_chattab_speech_yellcolor"))
            ),
            Option.HuePicker(TazLang.Get("mog_chattab_speech_partycolor"), new Accessor<ushort>(() => profile.PartyMessageHue), new SearchMetadata(TazLang.Get("mog_chattab_speech_partycolor"))),
            Option.HuePicker(TazLang.Get("mog_chattab_speech_alliancecolor"), new Accessor<ushort>(() => profile.AllyMessageHue), new SearchMetadata(TazLang.Get("mog_chattab_speech_alliancecolor"))),
            Option.HuePicker(TazLang.Get("mog_chattab_speech_emotecolor"), new Accessor<ushort>(() => profile.EmoteHue), new SearchMetadata(TazLang.Get("mog_chattab_speech_emotecolor"))),
            Option.HuePicker(TazLang.Get("mog_chattab_speech_whispercolor"), new Accessor<ushort>(() => profile.WhisperHue), new SearchMetadata(TazLang.Get("mog_chattab_speech_whispercolor"))),
            Option.HuePicker(TazLang.Get("mog_chattab_speech_guildcolor"), new Accessor<ushort>(() => profile.GuildMessageHue), new SearchMetadata(TazLang.Get("mog_chattab_speech_guildcolor"))),
            Option.HuePicker(TazLang.Get("mog_chattab_speech_charcolor"), new Accessor<ushort>(() => profile.ChatMessageHue), new SearchMetadata(TazLang.Get("mog_chattab_speech_charcolor")))
        );
    }

    private static OptionFragment GetOverheadDisplaySection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        string hideMacroTargetMessage = TazLang.Get("hidemacrotargetmessage", "Hide macro target message");
        string hideMacroTargetMessageTooltip = TazLang.Get("hidemacrotargetmessage_tooltip", "Hide the \"Target: name\" message shown overhead when a macro sets a target");

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = TazLang.Get("mog_chattab_speech_overheadtext") },
            Option.Checkbox(
                TazLang.Get("mog_tazuo_displaypartychatoverplayerheads"),
                new Accessor<bool>(() => profile.DisplayPartyChatOverhead),
                TazLang.Get("mog_tazuo_tooltippartychat"),
                new SearchMetadata(TazLang.Get("mog_tazuo_displaypartychatoverplayerheads"), Keywords: [TazLang.Get("mog_kw_party")])
            ),
            Option.Slider(
                TazLang.Get("mog_tazuo_overheadtextwidth"),
                0,
                600,
                new Accessor<int>(() => profile.OverheadChatWidth)
            ),
            Option.Checkbox(
                TazLang.Get("mog_tazuo_disablemouseinteractionsforoverheadtext"),
                new Accessor<bool>(() => profile.DisableMouseInteractionOverheadText),
                search: new SearchMetadata(TazLang.Get("mog_tazuo_disablemouseinteractionsforoverheadtext"), Keywords: [TazLang.Get("mog_kw_mouse")])
            ),
            Option.Checkbox(
                hideMacroTargetMessage,
                new Accessor<bool>(() => profile.HideMacroTargetMessage),
                hideMacroTargetMessageTooltip,
                new SearchMetadata(hideMacroTargetMessage, Keywords: [TazLang.Get("mog_kw_target")])
            )
        ).AsSearchGroup()
         .WithSearch(new SearchMetadata(Keywords: [TazLang.Get("mog_kw_overhead")]));
    }

    private static OptionFragment GetOverheadSuppressionSection()
    {

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = TazLang.Get("mog_chattab_speech_disableoverheadmessages") },
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
        string mTypeString = Enum.GetName(mType);
        string tazLangLoc = TazLang.Get(mTypeString);

        if (!string.IsNullOrWhiteSpace(tazLangLoc))
            return tazLangLoc;

        return mTypeString switch
        {
            "Regular" => TazLang.Get("mog_chattab_speech_messagetyperegular"),
            "System" => TazLang.Get("mog_chattab_speech_messagetypesystem"),
            "Emote" => TazLang.Get("mog_chattab_speech_messagetypeemote"),
            "Limit3Spell" => TazLang.Get("mog_chattab_speech_messagetypelimit3spell"),
            "Label" => TazLang.Get("mog_chattab_speech_messagetypelabel"),
            "Focus" => TazLang.Get("mog_chattab_speech_messagetypefocus"),
            "Whisper" => TazLang.Get("mog_chattab_speech_messagetypewhisper"),
            "Yell" => TazLang.Get("mog_chattab_speech_messagetypeyell"),
            "Spell" => TazLang.Get("mog_chattab_speech_messagetypespell"),
            "Guild" => TazLang.Get("mog_chattab_speech_messagetypeguild"),
            "Alliance" => TazLang.Get("mog_chattab_speech_messagetypealliance"),
            "Command" => TazLang.Get("mog_chattab_speech_messagetypecommand"),
            "Encoded" => TazLang.Get("mog_chattab_speech_messagetypeencoded"),
            "ChatSystem" => TazLang.Get("mog_chattab_speech_messagetypechatsystem"),
            "Damage" => TazLang.Get("mog_chattab_speech_messagetypedamage"),
            "Discord" => TazLang.Get("mog_chattab_speech_messagetypediscord"),
            "Party" => TazLang.Get("mog_chattab_speech_messagetypeparty"),
            _ => mType.ToString()
        };
    }
}
