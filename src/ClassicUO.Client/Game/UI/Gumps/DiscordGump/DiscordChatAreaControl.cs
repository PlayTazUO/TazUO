using System;
using System.Linq;
using ClassicUO.Assets;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using Discord.Sdk;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls;

public class DiscordChatAreaControl : Control
{
    public ulong ActiveChannel => _selectedChannel;
    private ScrollArea _chatScroll;
    private DataBox _chatDataBox;
    private TTFTextInputField _chatInput;
    private ulong _selectedChannel;
    private bool isDM;

    public DiscordChatAreaControl(int width, int height, int x, int y)
    {
        CanMove = true;
        CanCloseWithRightClick = true;
        Width = width;
        Height = height;
        X = x;
        Y = y;

        Build();
    }

    public override bool AcceptMouseInput => true;

    private void Build()
    {
        AlphaBlendControl c;

        Add
        (
            c = new AlphaBlendControl(0.75f)
            {
                Width = Width,
                Height = Height,
            }
        );

        c.BaseColor = new(31, 31, 31);

        _chatScroll = new(c.X, 0, Width, Height - 20, true)
        {
            ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways
        };

        Add(_chatScroll);

        _chatScroll.Add(_chatDataBox = new DataBox(0, 0, _chatScroll.Width, 0));

        _chatInput = new(c.Width - 20, 20) //-20 for scroll bar
        {
            X = c.X,
            Y = c.Height - 20
        };

        _chatInput.EnterPressed += ChatInputOnEnterPressed;
        Add(_chatInput);

        var button = new NiceButton(Width - 150, 0, 125, 25, ButtonAction.Default, "Share Item");
        button.IsSelected = true;
        button.MouseUp += (sender, args) =>
        {
            if (_selectedChannel == 0) return;

            World.Instance.TargetManager.SetTargeting((e) =>
            {
                if (e == null || !(e is Entity entity)) return;

                if (SerialHelper.IsItem(entity.Serial))
                {
                    Item item = World.Instance.Items.Get(entity.Serial);
                    DiscordManager.Instance.SendChannelItem(_selectedChannel, item, isDM);
                }
            });
        };
        Add(button);
    }

    private void ChatInputOnEnterPressed(object sender, EventArgs e)
    {
        if (_selectedChannel == 0)
            return;

        string txt = _chatInput.Text;

        if (string.IsNullOrEmpty(txt))
            return;

        _chatInput.SetText(string.Empty);

        if (txt.StartsWith("/"))
        {
            txt = txt.Substring(1);
            string command = txt.Split(' ')[0];

            if (HandleCommand(command, txt))
                return;
        }

        if (isDM)
            DiscordManager.Instance.SendDm(_selectedChannel, txt);
        else
            DiscordManager.Instance.SendChannelMsg(_selectedChannel, txt);
    }

    private bool HandleCommand(string command, string fullText)
    {
        switch (command.ToLower())
        {
            case "players":
            case "list":
            case "online":
                if (isDM)
                    return false;

                LobbyHandle lobby = DiscordManager.Instance.GetLobby(_selectedChannel);

                if (lobby == null)
                    return false;

                LobbyMemberHandle[] members = lobby?.LobbyMembers();

                if (members == null)
                    return false;

                string onlineMembers = string.Join(", ", members.Where(m => m?.User() != null).Select(m => $"[{m.User().DisplayName()}]"));

                AddMessageToChatBox(onlineMembers, Color.Goldenrod);

                return true;
        }

        return false;
    }


    public void AddMessageToChatBox(MessageHandle msg, bool rearrange = true)
    {
        _chatDataBox.Add(new DiscordMessageControl(msg, _chatScroll.Width));

        if (rearrange)
            _chatDataBox.ReArrangeChildren();
    }

    public void AddMessageToChatBox(string text, Color hue, bool rearrange = true)
    {
        _chatDataBox.Add(TextBox.GetOne(text, TrueTypeLoader.EMBEDDED_FONT, 20f, hue, TextBox.RTLOptions.Default(_chatScroll.Width - 20)));

        if (rearrange)
            _chatDataBox.ReArrangeChildren();
    }

    public void SetActiveChatChannel(ulong channelId, bool isDM)
    {
        _selectedChannel = channelId;
        this.isDM = isDM;

        _chatDataBox.Clear();

        if (DiscordManager.Instance.MessageHistory.ContainsKey(channelId))
            foreach (MessageHandle m in DiscordManager.Instance.MessageHistory[channelId])
                AddMessageToChatBox(m, false);
        else
            AddMessageToChatBox("No messages yet..", Color.Gray, false);

        _chatDataBox.ReArrangeChildren();
    }

    public override void Dispose()
    {
        base.Dispose();
        _chatInput.EnterPressed -= ChatInputOnEnterPressed;
    }
}
