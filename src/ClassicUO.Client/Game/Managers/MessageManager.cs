// SPDX-License-Identifier: BSD-2-Clause


using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility;
using ClassicUO.Game.Scenes;

namespace ClassicUO.Game.Managers
{
    //enum MessageFont : byte
    //{
    //    INVALID = 0xFF,
    //    Bold = 0,
    //    Shadow = 1,
    //    BoldShadow = 2,
    //    Normal = 3,
    //    Gothic = 4,
    //    Italic = 5,
    //    SmallDark = 6,
    //    Colorful = 7,
    //    Rune = 8,
    //    SmallLight = 9
    //}

    public enum AffixType : byte
    {
        Append = 0x00,
        Prepend = 0x01,
        System = 0x02,
        None = 0xFF
    }


    public sealed class MessageManager
    {
        private readonly World _world;

        public MessageManager(World world)
        {
            _world = world;
        }

        public PromptData PromptData { get; set; }

        public event EventHandler<MessageEventArgs> MessageReceived;

        public event EventHandler<MessageEventArgs> LocalizedMessageReceived;


        public void HandleMessage
        (
            Entity parent,
            string text,
            string name,
            ushort hue,
            MessageType type,
            byte font,
            TextType textType,
            bool unicode = false,
            string lang = null
        )
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            Profile currentProfile = ProfileManager.CurrentProfile;

            if (textType == TextType.OBJECT)
            {
                if (currentProfile.ForceTooltipsOnOldClients && ForcedTooltipManager.IsObjectTextRequested(_world, parent, text, hue))
                    return;
            }

            EventSink.InvokeRawMessageReceived(parent, new MessageEventArgs
                (
                    parent,
                    text,
                    name,
                    hue,
                    type,
                    font,
                    textType,
                    unicode,
                    lang
                ));

            if (currentProfile != null && currentProfile.OverrideAllFonts)
            {
                font = currentProfile.ChatFont;
                unicode = currentProfile.OverrideAllFontsIsUnicode;
            }

            switch (type)
            {
                case MessageType.ChatSystem:
                    break;
                case MessageType.Command:
                case MessageType.Encoded:
                case MessageType.System:
                    break;
                case MessageType.Party:
                    if (!currentProfile.DisplayPartyChatOverhead)
                        break;

                    if (parent == null) //No parent entity, need to check party members by name
                    {
                        foreach (PartyMember member in _world.Party.Members)
                            if (member != null)
                                if (member.Name == name) //Name matches message from server
                                {
                                    Mobile m = _world.Mobiles.Get(member.Serial);
                                    if (m != null) //Mobile exists
                                    {
                                        parent = m;
                                        break;
                                    }
                                }
                    }

                    if (type != MessageType.Spell && parent != null && _world.IgnoreManager.IgnoredCharsList.Contains(parent.Name))
                        break;

                    //Add null check in case parent was not found above.
                    parent?.AddMessage(CreateMessage
                    (
                        text,
                        hue,
                        font,
                        unicode,
                        type,
                        textType
                    ));
                    break;

                case MessageType.Guild:
                    if (currentProfile.IgnoreGuildMessages) return;
                    break;

                case MessageType.Alliance:
                    if (currentProfile.IgnoreAllianceMessages) return;
                    break;

                case MessageType.Spell:
                    {
                        //server hue color per default
                        if (!string.IsNullOrEmpty(text) && SpellDefinition.WordToTargettype.TryGetValue(text, out SpellDefinition spell))
                        {
                            if (currentProfile != null && currentProfile.EnabledSpellFormat && !string.IsNullOrWhiteSpace(currentProfile.SpellDisplayFormat))
                            {
                                var sb = new ValueStringBuilder(currentProfile.SpellDisplayFormat.AsSpan());
                                {
                                    sb.Replace("{power}".AsSpan(), spell.PowerWords.AsSpan());
                                    sb.Replace("{spell}".AsSpan(), spell.Name.AsSpan());

                                    text = sb.ToString().Trim();
                                }
                                sb.Dispose();
                            }

                            //server hue color per default if not enabled
                            if (currentProfile != null && currentProfile.EnabledSpellHue)
                            {
                                if (spell.TargetType == TargetType.Beneficial)
                                {
                                    hue = currentProfile.BeneficHue;
                                }
                                else if (spell.TargetType == TargetType.Harmful)
                                {
                                    hue = currentProfile.HarmfulHue;
                                }
                                else
                                {
                                    hue = currentProfile.NeutralHue;
                                }
                            }
                        }

                        goto case MessageType.Label;
                    }

                default:
                case MessageType.Focus:
                case MessageType.Whisper:
                case MessageType.Yell:
                case MessageType.Regular:
                case MessageType.Label:
                    if (textType == TextType.OBJECT)
                    {
                        for (LinkedListNode<Gump> gump = UIManager.Gumps.Last; gump != null; gump = gump.Previous)
                        {
                            if (gump.Value is GridContainer && !gump.Value.IsDisposed)
                            {
                                ((GridContainer)gump.Value).HandleObjectMessage(parent, text, hue);
                            }
                            if(gump.Value is ModernPaperdoll && !gump.Value.IsDisposed)
                            {
                                ((ModernPaperdoll)gump.Value).HandleObjectMessage(parent, text, hue);
                            }
                        }
                    }
                    goto case MessageType.Limit3Spell;
                case MessageType.Limit3Spell:
                    {
                        if (parent == null)
                        {
                            break;
                        }

                        // If person who send that message is in ignores list - but filter out Spell Text
                        if (_world.IgnoreManager.IgnoredCharsList.Contains(parent.Name) && type != MessageType.Spell)
                            break;

                        TextObject msg = CreateMessage
                        (
                            text,
                            hue,
                            font,
                            unicode,
                            type,
                            textType
                        );

                        msg.Owner = parent;

                        if (parent is Item it && !it.OnGround)
                        {
                            msg.X = _world.DelayedObjectClickManager.X;
                            msg.Y = _world.DelayedObjectClickManager.Y;
                            msg.IsTextGump = true;
                            bool found = false;

                            for (LinkedListNode<Gump> gump = UIManager.Gumps.Last; gump != null; gump = gump.Previous)
                            {
                                Control g = gump.Value;

                                if (!g.IsDisposed)
                                {
                                    switch (g)
                                    {
                                        case PaperDollGump paperDoll when g.LocalSerial == it.Container:
                                            paperDoll.AddText(msg);
                                            found = true;

                                            break;

                                        case ContainerGump container when g.LocalSerial == it.Container:
                                            container.AddText(msg);
                                            found = true;

                                            break;

                                        case TradingGump trade when trade.ID1 == it.Container || trade.ID2 == it.Container:
                                            trade.AddText(msg);
                                            found = true;

                                            break;
                                    }
                                }

                                if (found)
                                {
                                    break;
                                }
                            }
                        }

                        parent.AddMessage(msg);

                        break;
                    }
            }

            EventSink.InvokeMessageReceived(parent, new MessageEventArgs
                (
                    parent,
                    text,
                    name,
                    hue,
                    type,
                    font,
                    textType,
                    unicode,
                    lang
                )
            );
        }

        public void OnLocalizedMessage(Entity entity, MessageEventArgs args) => LocalizedMessageReceived.Raise(args, entity);

        public TextObject CreateMessage
        (
            string msg,
            ushort hue,
            byte font,
            bool isunicode,
            MessageType type,
            TextType textType
        )
        {

            ushort fixedColor = (ushort)(hue & 0x3FFF);

            if (fixedColor != 0)
            {
                if (fixedColor >= 0x0BB8)
                {
                    fixedColor = 1;
                }

                fixedColor |= (ushort)(hue & 0xC000);
            }
            else
            {
                fixedColor = (ushort)(hue & 0x8000);
            }


            var textObject = TextObject.Create(_world);
            textObject.Alpha = 0xFF;
            textObject.Type = type;
            //textObject.Hue = fixedColor;

            if (!isunicode && textType == TextType.OBJECT)
            {
                fixedColor = 0x7FFF;
            }

            //Ignored the fixedColor in the textbox creation because it seems to interfere with correct colors, but if issues arrise I left the fixColor code here
            TextBox.RTLOptions options = TextBox.RTLOptions.DefaultCenterStroked(ProfileManager.CurrentProfile.OverheadChatWidth).MouseInput(!ProfileManager.CurrentProfile.DisableMouseInteractionOverheadText);

            textObject.TextBox = TextBox.GetOne(msg, ProfileManager.CurrentProfile.OverheadChatFont, ProfileManager.CurrentProfile.OverheadChatFontSize, hue, options);
            textObject.Time = CalculateTimeToLive(textObject.TextBox);

            return textObject;
        }

        private static long CalculateTimeToLive(TextBox rtext)
        {
            Profile currentProfile = ProfileManager.CurrentProfile;

            if (currentProfile == null)
            {
                return 0;
            }

            long timeToLive;

            if (currentProfile.ScaleSpeechDelay)
            {
                int delay = currentProfile.SpeechDelay;

                if (delay < 10)
                {
                    delay = 10;
                }

                int fakeLines = 0;
                if (rtext.Text.Length > 99)
                    fakeLines = 3;
                else if (rtext.Text.Length > 66)
                    fakeLines = 2;
                else
                    fakeLines = 1;


                timeToLive = (long)(4000 * fakeLines * delay / 100.0f);
            }
            else
            {
                long delay = (5497558140000 * currentProfile.SpeechDelay) >> 32 >> 5;

                timeToLive = (delay >> 31) + delay;
            }

            timeToLive += Time.Ticks;

            return timeToLive;
        }
    }
}
