#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;
using SDL2;
using Control = ClassicUO.Game.UI.Controls.Control;
using Label = ClassicUO.Game.UI.Controls.Label;
using TextBox = ClassicUO.Game.UI.Controls.TextBox;

namespace ClassicUO.Game.UI.Gumps
{
    public enum ChatMode
    {
        Default,
        Whisper,
        Emote,
        Yell,
        Party,
        //PartyPrivate,
        Guild,
        Alliance,
        ClientCommand,
        UOAMChat,
        Prompt,
        UOChat,
        ServUOCommand,
        PolCommand
    }

    public class SystemChatControl : Control
    {
        private const int MAX_MESSAGE_LENGHT = 100;
        private const int CHAT_X_OFFSET = 3;
        private const int CHAT_HEIGHT = 15;
        private static readonly List<Tuple<ChatMode, string>> _messageHistory = new List<Tuple<ChatMode, string>>();
        private static int _messageHistoryIndex = -1;

        private readonly Label _currentChatModeLabel;

        private bool _isActive;
        private ChatMode _mode = ChatMode.Default;

        private readonly LinkedList<ChatLineTime> _textEntries;
        private readonly AlphaBlendControl _trans;
        private int lastAutoCompleteIndex = 0;
        private string command = string.Empty;

        public SystemChatControl(int x, int y, int w, int h)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            _textEntries = new LinkedList<ChatLineTime>();
            CanCloseWithRightClick = false;
            AcceptMouseInput = false;
            AcceptKeyboardInput = false;

            TextBoxControl = new StbTextBox
            (
                ProfileManager.CurrentProfile.ChatFont,
                -1,
                Width,
                true,
                FontStyle.BlackBorder | FontStyle.Fixed,
                33
            )
            {
                X = CHAT_X_OFFSET,
                Y = Height - CHAT_HEIGHT,
                Width = Width - CHAT_X_OFFSET,
                Height = CHAT_HEIGHT,
                LoseFocusOnEscapeKey = false
            };

            float gradientTransparency = ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.HideChatGradient ? 0.0f : 0.5f;

            Add
            (
                _trans = new AlphaBlendControl(gradientTransparency)
                {
                    X = TextBoxControl.X,
                    Y = TextBoxControl.Y,
                    Width = Width,
                    Height = CHAT_HEIGHT + 5,
                    IsVisible = !ProfileManager.CurrentProfile.ActivateChatAfterEnter,
                    AcceptMouseInput = true
                }
            );

            Add(TextBoxControl);

            Add
            (
                _currentChatModeLabel = new Label(string.Empty, true, 0, style: FontStyle.BlackBorder)
                {
                    X = TextBoxControl.X,
                    Y = TextBoxControl.Y,
                    IsVisible = false
                }
            );

            WantUpdateSize = false;

            EventSink.MessageReceived += ChatOnMessageReceived;
            Mode = ChatMode.Default;

            IsActive = !ProfileManager.CurrentProfile.ActivateChatAfterEnter;

            SetFocus();
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = TextBoxControl.IsVisible = TextBoxControl.IsEditable = value;

                if (_isActive)
                {
                    TextBoxControl.Width = _trans.Width - CHAT_X_OFFSET;
                    TextBoxControl.ClearText();
                }

                SetFocus();
            }
        }


        public ChatMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;

                if (IsActive)
                {
                    switch (value)
                    {
                        case ChatMode.Default:
                            DisposeChatModePrefix();
                            TextBoxControl.Hue = ProfileManager.CurrentProfile.SpeechHue;

                            break;

                        case ChatMode.Whisper:
                            AppendChatModePrefix(ResGumps.Whisper, ProfileManager.CurrentProfile.WhisperHue, TextBoxControl.Text);

                            break;

                        case ChatMode.Emote:
                            AppendChatModePrefix(ResGumps.Emote, ProfileManager.CurrentProfile.EmoteHue, TextBoxControl.Text);

                            break;

                        case ChatMode.Yell:
                            AppendChatModePrefix(ResGumps.Yell, ProfileManager.CurrentProfile.YellHue, TextBoxControl.Text);

                            break;

                        case ChatMode.Party:
                            AppendChatModePrefix(ResGumps.Party, ProfileManager.CurrentProfile.PartyMessageHue, TextBoxControl.Text);

                            break;

                        case ChatMode.Guild:
                            AppendChatModePrefix(ResGumps.Guild, ProfileManager.CurrentProfile.GuildMessageHue, TextBoxControl.Text);

                            break;

                        case ChatMode.Alliance:
                            AppendChatModePrefix(ResGumps.Alliance, ProfileManager.CurrentProfile.AllyMessageHue, TextBoxControl.Text);

                            break;

                        case ChatMode.ClientCommand:
                            AppendChatModePrefix(ResGumps.Command, 1161, TextBoxControl.Text);

                            break;

                        case ChatMode.UOAMChat:
                            DisposeChatModePrefix();
                            AppendChatModePrefix(ResGumps.UOAM, 83, TextBoxControl.Text);

                            break;

                        case ChatMode.UOChat:
                            DisposeChatModePrefix();

                            AppendChatModePrefix(ResGumps.Chat, ProfileManager.CurrentProfile.ChatMessageHue, TextBoxControl.Text);

                            break;

                        case ChatMode.ServUOCommand:
                            DisposeChatModePrefix();
                            AppendChatModePrefix($"[{command}", 32, null);
                            break;

                        case ChatMode.PolCommand:
                            DisposeChatModePrefix();
                            AppendChatModePrefix($".{command}", 32, null);
                            break;
                    }
                }
            }
        }

        public readonly StbTextBox TextBoxControl;

        public void SetFocus()
        {
            TextBoxControl.IsEditable = true;
            TextBoxControl.SetKeyboardFocus();
            TextBoxControl.IsEditable = _isActive;
            _trans.IsVisible = _isActive;
        }

        public void ToggleChatVisibility()
        {
            IsActive = !IsActive;
        }

        private void ChatOnMessageReceived(object sender, MessageEventArgs e)
        {
            if (e.TextType == TextType.CLIENT)
            {
                return;
            }

            if (ProfileManager.CurrentProfile.DisableSystemChat)
                return;

            switch (e.Type)
            {
                case MessageType.Regular when e.Parent == null || !SerialHelper.IsValid(e.Parent.Serial):
                case MessageType.System:
                    if (!string.IsNullOrEmpty(e.Name) && !e.Name.Equals("system", StringComparison.InvariantCultureIgnoreCase))
                    {
                        AddLine($"{e.Name}: {e.Text}", e.Font, e.Hue, e.IsUnicode);
                    }
                    else
                    {
                        AddLine(e.Text, e.Font, e.Hue, e.IsUnicode);
                    }

                    break;

                case MessageType.Label when e.Parent == null || !SerialHelper.IsValid(e.Parent.Serial):
                    AddLine(e.Text, e.Font, e.Hue, e.IsUnicode);

                    break;

                case MessageType.Party:
                    AddLine(string.Format(ResGumps.PartyName0Text1, e.Name, e.Text), e.Font, ProfileManager.CurrentProfile.PartyMessageHue, e.IsUnicode);

                    break;

                case MessageType.Guild:
                    AddLine(string.Format(ResGumps.GuildName0Text1, e.Name, e.Text), e.Font, ProfileManager.CurrentProfile.GuildMessageHue, e.IsUnicode);

                    break;

                case MessageType.Alliance:
                    AddLine(string.Format(ResGumps.AllianceName0Text1, e.Name, e.Text), e.Font, ProfileManager.CurrentProfile.AllyMessageHue, e.IsUnicode);

                    break;
                case MessageType.ChatSystem:
                    AddLine($"{e.Name}: {e.Text}", e.Font, ProfileManager.CurrentProfile.ChatMessageHue, e.IsUnicode);
                    break;
                default:

                    if (e.Parent == null || !SerialHelper.IsValid(e.Parent.Serial))
                    {
                        if (string.IsNullOrEmpty(e.Name) || e.Name.Equals("system", StringComparison.InvariantCultureIgnoreCase))
                        {
                            AddLine(e.Text, e.Font, e.Hue, e.IsUnicode);
                        }
                    }

                    break;
            }
        }

        public override void Dispose()
        {
            EventSink.MessageReceived -= ChatOnMessageReceived;
            base.Dispose();
        }

        private void AppendChatModePrefix(string labelText, ushort hue, string text)
        {
            if (!_currentChatModeLabel.IsVisible)
            {
                _currentChatModeLabel.Hue = hue;
                _currentChatModeLabel.Text = labelText;
                _currentChatModeLabel.IsVisible = true;
                _currentChatModeLabel.Location = TextBoxControl.Location;
                TextBoxControl.X = _currentChatModeLabel.Width;
                TextBoxControl.Hue = hue;

                int idx = string.IsNullOrEmpty(text) ? -1 : TextBoxControl.Text.IndexOf(text);
                string str = string.Empty;

                if (idx > 0)
                {
                    str = TextBoxControl.Text.Substring(idx, TextBoxControl.Text.Length - labelText.Length - 1);
                }

                TextBoxControl.SetText(str);
            }
        }

        private void DisposeChatModePrefix()
        {
            if (_currentChatModeLabel.IsVisible)
            {
                TextBoxControl.Hue = 33;
                TextBoxControl.X -= _currentChatModeLabel.Width;
                _currentChatModeLabel.IsVisible = false;
            }
        }

        private string ExtractCommand(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length < 2)
                return string.Empty;

            int pos = 1;
            while (pos < text.Length && text[pos] != ' ')
            {
                pos++;
            }

            return pos > 1 ? text.Substring(1, pos - 1) : string.Empty;
        }

        public void AddLine(string text, byte font, ushort hue, bool isunicode)
        {
            if (_textEntries.Count >= 30)
            {
                LinkedListNode<ChatLineTime> lineToRemove = _textEntries.First;
                lineToRemove.Value.Destroy();
                _textEntries.Remove(lineToRemove);
            }


            var last = _textEntries.Last?.Value;

            if (last != null && last.Duplicate(text))
            {
                last.IncreaseCount();
                return;
            }

            _textEntries.AddLast(new ChatLineTime(text, font, isunicode, hue));
        }

        internal void Resize()
        {
            if (TextBoxControl != null)
            {
                TextBoxControl.X = CHAT_X_OFFSET;
                TextBoxControl.Y = Height - CHAT_HEIGHT - CHAT_X_OFFSET;
                TextBoxControl.Width = Width - CHAT_X_OFFSET;
                TextBoxControl.Height = CHAT_HEIGHT + CHAT_X_OFFSET;
                _trans.X = TextBoxControl.X - CHAT_X_OFFSET;
                _trans.Y = TextBoxControl.Y;
                _trans.Width = Width;
                _trans.Height = CHAT_HEIGHT + 5;
            }
        }

        public override void Update()
        {
            LinkedListNode<ChatLineTime> first = _textEntries.First;

            while (first != null)
            {
                LinkedListNode<ChatLineTime> next = first.Next;

                first.Value.Update();

                if (first.Value.IsDisposed)
                {
                    _textEntries.Remove(first);
                }

                first = next;
            }

            if (Mode == ChatMode.Default && IsActive)
            {
                if (TextBoxControl.Text.Length > 0)
                {
                    string text = TextBoxControl.Text;
                    switch (TextBoxControl.Text[0])
                    {
                        case '/':

                            int pos = 1;

                            while (pos < TextBoxControl.Text.Length && TextBoxControl.Text[pos] != ' ')
                            {
                                pos++;
                            }

                            if (pos < TextBoxControl.Text.Length && int.TryParse(TextBoxControl.Text.Substring(1, pos), out int index) && index > 0 && index < 11)
                            {
                                if (World.Party.Members[index - 1] != null && World.Party.Members[index - 1].Serial != 0)
                                {
                                    AppendChatModePrefix(string.Format(ResGumps.Tell0, World.Party.Members[index - 1].Name), ProfileManager.CurrentProfile.PartyMessageHue, string.Empty);
                                }
                                else
                                {
                                    AppendChatModePrefix(ResGumps.TellEmpty, ProfileManager.CurrentProfile.PartyMessageHue, string.Empty);
                                }

                                Mode = ChatMode.Party;
                                TextBoxControl.SetText($" ");
                            }
                            else
                            {
                                Mode = ChatMode.Party;
                            }

                            break;

                        case '\\':
                            Mode = ChatMode.Guild;

                            break;

                        case '|':
                            Mode = ChatMode.Alliance;

                            break;

                        case '-':
                            Mode = ChatMode.ClientCommand;

                            break;

                        case ',' when ChatManager.ChatIsEnabled == ChatStatus.Enabled:
                            Mode = ChatMode.UOChat;

                            break;


                        case ':' when TextBoxControl.Text.Length > 1 && TextBoxControl.Text[1] == ' ':
                            Mode = ChatMode.Emote;

                            break;

                        case ';' when TextBoxControl.Text.Length > 1 && TextBoxControl.Text[1] == ' ':
                            Mode = ChatMode.Whisper;

                            break;

                        case '!' when TextBoxControl.Text.Length > 1 && TextBoxControl.Text[1] == ' ':
                            Mode = ChatMode.Yell;

                            break;

                        case '[' when text.Length > 1 && text.Contains(" "):
                            int bracketSpaceIndex = text.IndexOf(' ');
                            command = text.Substring(1, bracketSpaceIndex - 1);
                            Mode = ChatMode.ServUOCommand;
                            // Remove the command from the textbox, keep parameters
                            TextBoxControl.SetText(text.Substring(bracketSpaceIndex));
                            break;

                        case '.' when text.Length > 1 && text.Contains(" "):
                            int dotSpaceIndex = text.IndexOf(' ');
                            command = text.Substring(1, dotSpaceIndex - 1);
                            Mode = ChatMode.PolCommand;
                            // Remove the command from the textbox, keep parameters
                            TextBoxControl.SetText(text.Substring(dotSpaceIndex));
                            break;
                    }
                }
            }
            else if (Mode == ChatMode.ClientCommand && TextBoxControl.Text.Length == 1 && TextBoxControl.Text[0] == '-')
            {
                Mode = ChatMode.UOAMChat;
            }

            if (ProfileManager.CurrentProfile.SpeechHue != TextBoxControl.Hue)
            {
                TextBoxControl.Hue = ProfileManager.CurrentProfile.SpeechHue;
            }

            _trans.Alpha = ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.HideChatGradient ? 0.0f : 0.5f;

            base.Update();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            int yy = TextBoxControl.Y + y - 20;

            LinkedListNode<ChatLineTime> last = _textEntries.Last;

            while (last != null)
            {
                LinkedListNode<ChatLineTime> prev = last.Previous;

                if (last.Value.IsDisposed)
                {
                    _textEntries.Remove(last);
                }
                else
                {
                    yy -= last.Value.TextHeight;

                    if (yy >= y)
                    {
                        last.Value.Draw(batcher, x + 2, yy);
                    }
                }

                last = prev;
            }

            return base.Draw(batcher, x, y);
        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            switch (key)
            {
                case SDL.SDL_Keycode.SDLK_q when Keyboard.Ctrl && _messageHistoryIndex > -1 && !ProfileManager.CurrentProfile.DisableCtrlQWBtn:

                    GameScene scene = Client.Game.GetScene<GameScene>();

                    if (scene == null)
                    {
                        return;
                    }

                    if (scene.Macros.FindMacro(key, false, true, false) != null)
                    {
                        return;
                    }

                    if (!IsActive)
                    {
                        return;
                    }

                    if (_messageHistoryIndex > 0)
                    {
                        _messageHistoryIndex--;
                    }

                    Mode = _messageHistory[_messageHistoryIndex].Item1;

                    TextBoxControl.SetText(_messageHistory[_messageHistoryIndex].Item2);

                    break;

                case SDL.SDL_Keycode.SDLK_w when Keyboard.Ctrl && !ProfileManager.CurrentProfile.DisableCtrlQWBtn:

                    scene = Client.Game.GetScene<GameScene>();

                    if (scene == null)
                    {
                        return;
                    }

                    if (scene.Macros.FindMacro(key, false, true, false) != null)
                    {
                        return;
                    }

                    if (!IsActive)
                    {
                        return;
                    }

                    if (_messageHistoryIndex < _messageHistory.Count - 1)
                    {
                        _messageHistoryIndex++;

                        Mode = _messageHistory[_messageHistoryIndex].Item1;

                        TextBoxControl.SetText(_messageHistory[_messageHistoryIndex].Item2);
                    }
                    else
                    {
                        TextBoxControl.ClearText();
                    }

                    break;

                case SDL.SDL_Keycode.SDLK_BACKSPACE when !Keyboard.Ctrl && !Keyboard.Alt && !Keyboard.Shift && string.IsNullOrEmpty(TextBoxControl.Text):
                    Mode = ChatMode.Default;

                    break;

                case SDL.SDL_Keycode.SDLK_ESCAPE when MessageManager.PromptData.Prompt != ConsolePrompt.None:

                    if (MessageManager.PromptData.Prompt == ConsolePrompt.ASCII)
                    {
                        NetClient.Socket.Send_ASCIIPromptResponse(string.Empty, true);
                    }
                    else if (MessageManager.PromptData.Prompt == ConsolePrompt.Unicode)
                    {
                        NetClient.Socket.Send_UnicodePromptResponse(string.Empty, Settings.GlobalSettings.Language, true);
                    }

                    MessageManager.PromptData = default;

                    break;

                case SDL.SDL_Keycode.SDLK_TAB when _mode == ChatMode.Default:
                    List<string> autoComplete = TextHistoryManager.GetAutocompleteSuggestions(TextBoxControl.Text, 5);
                    if (autoComplete != null && autoComplete.Count > 0)
                    {
                        autoComplete.Insert(0, TextBoxControl.Text);
                        void selected(string s)
                        {
                            TextBoxControl?.SetText(s);
                        }

                        SelectableItemListGump listGump = new SelectableItemListGump(autoComplete, selected, selected);
                        var viewport = Client.Game.GetScene<GameScene>().Camera.GetViewport();
                        listGump.X = viewport.X + 10;
                        listGump.Y = viewport.Height - listGump.Height + viewport.Y - 20;
                        UIManager.Add(listGump);
                        listGump.SetKeyboardFocus();
                    }
                    break;
            }
        }

        public override void OnKeyboardReturn(int textID, string text)
        {
            if (!IsActive && ProfileManager.CurrentProfile.ActivateChatAfterEnter || Mode != ChatMode.Default && string.IsNullOrEmpty(text))
            {
                TextBoxControl.ClearText();
                text = string.Empty;
                Mode = ChatMode.Default;
            }

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            ChatMode sentMode = Mode;
            Mode = ChatMode.Default;
            TextBoxControl.ClearText();

            bool sendAgain = false;

            if (text.Length > MAX_MESSAGE_LENGHT)
            {
                GameActions.Print("Message too long, sending the first 100 characters.");
                Mode = sentMode;
                TextBoxControl.SetText(text.Substring(MAX_MESSAGE_LENGHT));
                text = text.Substring(0, MAX_MESSAGE_LENGHT);
                sendAgain = true;
            }

            _messageHistory.Add(new Tuple<ChatMode, string>(sentMode, text));
            _messageHistoryIndex = _messageHistory.Count;


            if (MessageManager.PromptData.Prompt != ConsolePrompt.None)
            {
                if (MessageManager.PromptData.Prompt == ConsolePrompt.ASCII)
                {
                    NetClient.Socket.Send_ASCIIPromptResponse(text, text.Length < 1);
                }
                else if (MessageManager.PromptData.Prompt == ConsolePrompt.Unicode)
                {
                    NetClient.Socket.Send_UnicodePromptResponse(text, Settings.GlobalSettings.Language, text.Length < 1);
                }

                MessageManager.PromptData = default;
            }
            else
            {
                switch (sentMode)
                {
                    case ChatMode.Default:
                        GameActions.Say(text, ProfileManager.CurrentProfile.SpeechHue);

                        TextHistoryManager.AddToHistoryIfCommand(text);

                        break;

                    case ChatMode.Whisper:
                        GameActions.Say(text, ProfileManager.CurrentProfile.WhisperHue, MessageType.Whisper);

                        break;

                    case ChatMode.Emote:
                        text = ResGeneral.EmoteChar + text + ResGeneral.EmoteChar;
                        GameActions.Say(text, ProfileManager.CurrentProfile.EmoteHue, MessageType.Emote);

                        break;

                    case ChatMode.Yell:
                        GameActions.Say(text, ProfileManager.CurrentProfile.YellHue, MessageType.Yell);

                        break;

                    case ChatMode.Party:

                        switch (text.ToLower())
                        {
                            case "add":
                                if (World.Party.Leader == 0 || World.Party.Leader == World.Player)
                                {
                                    GameActions.RequestPartyInviteByTarget();
                                }
                                else
                                {
                                    MessageManager.HandleMessage
                                    (
                                        null,
                                        ResGumps.YouAreNotPartyLeader,
                                        "System",
                                        0xFFFF,
                                        MessageType.Regular,
                                        3,
                                        TextType.SYSTEM
                                    );
                                }

                                break;

                            case "loot":

                                if (World.Party.Leader != 0)
                                {
                                    World.Party.CanLoot = !World.Party.CanLoot;
                                }
                                else
                                {
                                    MessageManager.HandleMessage
                                    (
                                        null,
                                        ResGumps.YouAreNotInAParty,
                                        "System",
                                        0xFFFF,
                                        MessageType.Regular,
                                        3,
                                        TextType.SYSTEM
                                    );
                                }


                                break;

                            case "quit":

                                if (World.Party.Leader == 0)
                                {
                                    MessageManager.HandleMessage
                                    (
                                        null,
                                        ResGumps.YouAreNotInAParty,
                                        "System",
                                        0xFFFF,
                                        MessageType.Regular,
                                        3,
                                        TextType.SYSTEM
                                    );
                                }
                                else
                                {
                                    GameActions.RequestPartyQuit();

                                    //for (int i = 0; i < World.Party.Members.Length; i++)
                                    //{
                                    //    if (World.Party.Members[i] != null && World.Party.Members[i].Serial != 0)
                                    //        GameActions.RequestPartyRemoveMember(World.Party.Members[i].Serial);
                                    //}
                                }

                                break;

                            case "accept":

                                if (World.Party.Leader == 0 && World.Party.Inviter != 0)
                                {
                                    GameActions.RequestPartyAccept(World.Party.Inviter);
                                    World.Party.Leader = World.Party.Inviter;
                                    World.Party.Inviter = 0;
                                }
                                else
                                {
                                    MessageManager.HandleMessage
                                    (
                                        null,
                                        ResGumps.NoOneHasInvitedYouToBeInAParty,
                                        "System",
                                        0xFFFF,
                                        MessageType.Regular,
                                        3,
                                        TextType.SYSTEM
                                    );
                                }

                                break;

                            case "decline":

                                if (World.Party.Leader == 0 && World.Party.Inviter != 0)
                                {
                                    NetClient.Socket.Send_PartyDecline(World.Party.Inviter);
                                    World.Party.Leader = 0;
                                    World.Party.Inviter = 0;
                                }
                                else
                                {
                                    MessageManager.HandleMessage
                                    (
                                        null,
                                        ResGumps.NoOneHasInvitedYouToBeInAParty,
                                        "System",
                                        0xFFFF,
                                        MessageType.Regular,
                                        3,
                                        TextType.SYSTEM
                                    );
                                }


                                break;

                            case "rem":

                                if (World.Party.Leader != 0 && World.Party.Leader == World.Player)
                                {
                                    GameActions.RequestPartyRemoveMemberByTarget();
                                }
                                else
                                {
                                    MessageManager.HandleMessage
                                    (
                                        null,
                                        ResGumps.YouAreNotPartyLeader,
                                        "System",
                                        0xFFFF,
                                        MessageType.Regular,
                                        3,
                                        TextType.SYSTEM
                                    );
                                }


                                break;

                            default:

                                if (World.Party.Leader != 0)
                                {
                                    uint serial = 0;

                                    int pos = 0;

                                    while (pos < text.Length && text[pos] != ' ')
                                    {
                                        pos++;
                                    }

                                    if (pos < text.Length)
                                    {
                                        if (int.TryParse(text.Substring(0, pos), out int index) && index > 0 && index < 11 && World.Party.Members[index - 1] != null && World.Party.Members[index - 1].Serial != 0)
                                        {
                                            serial = World.Party.Members[index - 1].Serial;
                                        }
                                    }

                                    GameActions.SayParty(text, serial);
                                }
                                else
                                {
                                    GameActions.Print
                                    (
                                        string.Format(ResGumps.NoteToSelf0, text),
                                        0,
                                        MessageType.System,
                                        3,
                                        false
                                    );
                                }

                                break;
                        }

                        break;

                    case ChatMode.Guild:
                        GameActions.Say(text, ProfileManager.CurrentProfile.GuildMessageHue, MessageType.Guild);

                        break;

                    case ChatMode.Alliance:
                        GameActions.Say(text, ProfileManager.CurrentProfile.AllyMessageHue, MessageType.Alliance);

                        break;

                    case ChatMode.ClientCommand:
                        string[] tt = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        if (tt.Length != 0)
                        {
                            CommandManager.Execute(tt[0], tt);
                        }

                        break;

                    case ChatMode.UOAMChat:
                        UoAssist.SignalMessage(text);

                        break;

                    case ChatMode.UOChat:
                        NetClient.Socket.Send_ChatMessageCommand(text);

                        break;

                    case ChatMode.ServUOCommand:
                        if(!text.StartsWith(" "))
                            text = " " + text;
                        GameActions.Say($"[{command}{text}", ProfileManager.CurrentProfile.SpeechHue);
                        TextHistoryManager.AddToHistoryIfCommand($"[{command} {text}");

                        break;

                    case ChatMode.PolCommand:
                        if(!text.StartsWith(" "))
                            text = " " + text;
                        GameActions.Say($".{command}{text}", ProfileManager.CurrentProfile.SpeechHue);
                        TextHistoryManager.AddToHistoryIfCommand($".{command} {text}");

                        break;
                }
            }

            if(!sendAgain)
            {
                DisposeChatModePrefix();
                command = string.Empty;
            }
        }

        private class ChatLineTime
        {
            private uint _createdTime;
            private TextBox textBox;
            private static TextBox.RTLOptions TextBoxOptions = new() { Width = 320, StrokeEffect = true };
            private string text;
            private int count = 1;
            public ChatLineTime(string text, byte font, bool isunicode, ushort hue)
            {
                this.text = text;
                textBox = TextBox.GetOne(text, ProfileManager.CurrentProfile.GameWindowSideChatFont, ProfileManager.CurrentProfile.GameWindowSideChatFontSize, hue, TextBoxOptions);
                _createdTime = Time.Ticks + Constants.TIME_DISPLAY_SYSTEM_MESSAGE_TEXT;
            }

            private string Text => textBox?.Text ?? string.Empty;

            public bool IsDisposed => textBox == null || textBox.IsDisposed;

            public int TextHeight => textBox?.Height ?? 0;

            public bool Duplicate(string text) => this.text == text;

            public void IncreaseCount()
            {
                count++;
                textBox.SetText($"{text} [x{count}]");
            }

            public void Update()
            {
                if (Time.Ticks > _createdTime)
                {
                    Destroy();
                }
            }


            public bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                return !IsDisposed && textBox.Draw(batcher, x, y);
            }

            public override string ToString()
            {
                return Text;
            }

            public void Destroy()
            {
                if (!IsDisposed)
                {
                    textBox?.Dispose();
                    textBox = null;
                }
            }
        }
    }
}
