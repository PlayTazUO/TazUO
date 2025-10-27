using ClassicUO.Assets;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using Discord.Sdk;
using Microsoft.Xna.Framework;
using MouseEventArgs = ClassicUO.Input.MouseEventArgs;
using TextBox = ClassicUO.Game.UI.Controls.TextBox;

namespace ClassicUO.Game.UI.Gumps;

public class DiscordGump : Gump
{
    public ulong ActiveChannel => _discordChatArea == null ? 0 : _discordChatArea.ActiveChannel;

    private const int WIDTH = 800, LEFT_WIDTH = 200;
    private const int HEIGHT = 700;

    private NiceButton _connect;
    private EmbeddedGumpPic _discordLogo;
    private TextBox _statusText, _currentChatTitle;
    private DiscordFriendListControl _discordFriendList;
    private DiscordChannelListControl _discordChannelList;
    private DiscordChatAreaControl _discordChatArea;
    private MenuButton menuButton;

    public DiscordGump(World world) : base(world, 0, 0)
    {
        Width = WIDTH;
        Height = HEIGHT;
        CanMove = true;

        BuildLeftArea();
        BuildChatArea();
        GenMenuContextMenu();

        DiscordManager.Instance.OnStatusTextUpdated += OnStatusTextUpdated;
        DiscordManager.Instance.OnUserUpdated += OnUserUpdated;
        DiscordManager.Instance.OnMessageReceived += OnMessageReceived;

        CenterXInViewPort();
        CenterYInViewPort();
    }

    private void OnMessageReceived(object sender)
    {
        var msg = (MessageHandle)sender;

        if (msg == null)
            return;


        if (msg.Channel()?.Type() == ChannelType.Dm)
        {
            OnDMReceived(msg);

            return;
        }

        _discordChannelList.OnChannelActivity(msg.ChannelId());

        if (ActiveChannel != msg.ChannelId())
            return;

        _discordChatArea.AddMessageToChatBox(msg);
    }

    private void OnDMReceived(MessageHandle msg)
    {
        ulong id = msg.AuthorId();

        if (id == DiscordManager.Instance.UserId) //Message was sent by us
            id = msg.RecipientId();               //Put this into the dmg history for this user

        _discordChannelList.OnChannelActivity(id);

        if (ActiveChannel != id)
            return;

        _discordChatArea.AddMessageToChatBox(msg);
    }

    private void OnUserUpdated() => _discordFriendList.BuildFriendsList();

    private void BuildLeftArea()
    {
        AcceptMouseInput = true;

        PNGLoader.Instance.TryGetEmbeddedTexture("Discord-Symbol-Blurple-SM.png", out Microsoft.Xna.Framework.Graphics.Texture2D discordTexture);
        _discordLogo = new(LEFT_WIDTH / 2 - 66, HEIGHT / 2 - 50, discordTexture);
        Add(_discordLogo);

        AlphaBlendControl c;

        Add
        (
            c = new(0.75f)
            {
                Width = LEFT_WIDTH,
                Height = HEIGHT,
            }
        );

        c.BaseColor = new Color(21, 21, 21);

        if (!DiscordManager.Instance.Connected)
        {
            _connect = new(LEFT_WIDTH - 100, 0, 100, 30, ButtonAction.Activate, "Login");
            _connect.MouseUp += OnConnectClicked;
            Add(_connect);
        }

        int splitH = (HEIGHT - 20) / 2;

        _statusText = TextBox.GetOne(DiscordManager.Instance.StatusText, TrueTypeLoader.EMBEDDED_FONT, 16f, Color.DarkOrange, TextBox.RTLOptions.Default());
        Add(_statusText);

        Add
        (
            _discordChannelList = new DiscordChannelListControl(LEFT_WIDTH, splitH, this)
            {
                Y = 20
            }
        );

        Add(new Line(0, _discordChannelList.Height + _discordChannelList.Y, LEFT_WIDTH, 1, 0xFF383838));

        _discordChannelList.BuildChannelList();

        Add
        (
            _discordFriendList = new DiscordFriendListControl(LEFT_WIDTH, splitH - 1, this)
            {
                Y = _discordChannelList.Height + _discordChannelList.Y + 1 //1 is for the line
            }
        );

        _discordFriendList.BuildFriendsList();
    }

    private void BuildChatArea()
    {
        int w = WIDTH - LEFT_WIDTH - 5;

        AlphaBlendControl c;

        Add
        (
            c = new(0.75f)
            {
                Width = w,
                Height = 20,
                X = LEFT_WIDTH + 5
            }
        );

        c.BaseColor = new Color(21, 21, 21);

        _currentChatTitle = TextBox.GetOne(string.Empty, TrueTypeLoader.EMBEDDED_FONT, 20f, Color.LightSteelBlue, TextBox.RTLOptions.Default());
        _currentChatTitle.X = c.X + 5;
        _currentChatTitle.Y = 2;
        Add(_currentChatTitle);

        _discordChatArea = new DiscordChatAreaControl(w, HEIGHT - 20, LEFT_WIDTH + 5, 20);
        Add(_discordChatArea);
    }

    public void SetActiveChatChannel(ulong channelId, bool isDm)
    {
        _discordChatArea.SetActiveChatChannel(channelId, isDm);

        _discordFriendList.UpdateSelectedFriend();
        _discordChannelList.UpdateSelectedChannel();

        string chanName = string.Empty;

        if (isDm)
        {
            UserHandle user = DiscordManager.Instance.GetUser(channelId);
            chanName = user.DisplayName();
        }
        else if (DiscordManager.Instance.GetLobby(channelId) is LobbyHandle lobby)
        {
            chanName = DiscordManager.Instance.GetLobbyName(lobby);
        }
        else if (DiscordManager.Instance.GetChannel(channelId) is ChannelHandle chan)
        {
            chanName = chan.Name();
        }

        _currentChatTitle.Text = chanName;
    }

    private void OnStatusTextUpdated() => _statusText.Text = DiscordManager.Instance.StatusText;

    private void OnConnectClicked(object sender, MouseEventArgs e)
    {
        DiscordManager.Instance.OnConnected += DiscordOnConnected;
        _connect.Dispose();
        DiscordManager.Instance.StartOAuthFlow();
    }

    private void DiscordOnConnected()
    {
        _discordFriendList.BuildFriendsList();
        DiscordManager.Instance.OnConnected -= DiscordOnConnected;
    }

    private void GenMenuContextMenu(bool regen = false)
    {
        if(!regen)
        {
            menuButton = new MenuButton(20, 997, 0.8f, "Options", Color.LightSteelBlue.PackedValue);
            menuButton.X = Width - 22;
            menuButton.Y = 2;
            menuButton.MouseUp += (_,_) => { menuButton.ContextMenu.Show(); };
            Add(menuButton);
        }
        
        menuButton.ContextMenu = new ContextMenuControl(this);
        menuButton.ContextMenu.Add(new ContextMenuItemEntry("Show DM messages in system chat and journal?", () => { DiscordManager.DiscordSettings.ShowDMInGame = !DiscordManager.DiscordSettings.ShowDMInGame; GenMenuContextMenu(true); }, true, DiscordManager.DiscordSettings.ShowDMInGame));
        menuButton.ContextMenu.Add(new ContextMenuItemEntry("Show Channel messages in system chat and journal?", () => { DiscordManager.DiscordSettings.ShowChatInGame = !DiscordManager.DiscordSettings.ShowChatInGame; GenMenuContextMenu(true); }, true, DiscordManager.DiscordSettings.ShowChatInGame));
    }
    
    public override void Dispose()
    {
        base.Dispose();
        DiscordManager.Instance.OnStatusTextUpdated -= OnStatusTextUpdated;
        DiscordManager.Instance.OnUserUpdated -= OnUserUpdated;
        DiscordManager.Instance.OnMessageReceived -= OnMessageReceived;
    }
}