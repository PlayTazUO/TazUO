#nullable enable
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

public class TazUOChatWindow : MyraControl
{
    private const int CHANNEL_WIDTH = 130;
    private const int USER_WIDTH    = 120;
    private const int MSG_WIDTH     = 400;
    private const int PANEL_HEIGHT  = 350;

    private readonly TazUOChatManager _manager = TazUOChatManager.Instance;

    private string _selectedChannel = "";
    private int    _lastChannelCount = -1;
    private int    _lastMessageCount = -1;
    private bool   _wasConnected;
    private bool   _pendingScroll;

    private VerticalStackPanel? _channelsPanel;
    private VerticalStackPanel? _messagesPanel;
    private VerticalStackPanel? _usersPanel;
    private ScrollViewer?       _messageScroll;
    private MyraInputBox?       _chatInput;
    private MyraLabel?          _titleNickName;
    private string _nickName
    {
        get;
        set
        {
            field = value;
            _titleNickName?.Text = "/c[green]" + value;
        }
    }

    public TazUOChatWindow() : base("TazUO Chat")
    {
        CanBeSaved = true;
        _wasConnected = _manager.IsConnected;
        RebuildContent();
        CenterInViewPort();

        _titleNickName = new MyraLabel("", MyraLabel.TextStyle.H3);
        _rootWindow.TitlePanel.Widgets.Insert(1, _titleNickName);

        _nickName = _manager.CurrentNick;
    }

    public static void Show()
    {
        foreach (IGui gump in UIManager.Gumps)
        {
            if (gump is TazUOChatWindow w) { w.BringOnTop(); return; }
        }
        UIManager.Add(new TazUOChatWindow());
    }

    public override void PreDraw()
    {
        base.PreDraw();

        bool connected = _manager.IsConnected;
        if (connected != _wasConnected)
        {
            _wasConnected = connected;
            RebuildContent();
            return;
        }

        if (!connected) return;

        if (_manager.TotalChannelCount != _lastChannelCount)
            RebuildChannels();

        if (_manager.TotalMessageCount != _lastMessageCount)
        {
            RebuildMessages();
            _pendingScroll = true;
        }

        if(_manager.CurrentNick != _nickName)
            _nickName =  _manager.CurrentNick;

        if (_pendingScroll && _messageScroll != null)
        {
            _messageScroll.ScrollPosition = new Point(0, _messageScroll.ScrollMaximum.Y);
            _pendingScroll = false;
        }
    }

    private void RebuildContent()
    {
        _channelsPanel  = null;
        _messagesPanel  = null;
        _usersPanel     = null;
        _messageScroll  = null;
        _chatInput      = null;
        _lastChannelCount = -1;
        _lastMessageCount = -1;
        _pendingScroll  = false;

        var root = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };

        if (!_manager.IsConnected)
        {
            var row = new HorizontalStackPanel { Spacing = 6 };
            row.Widgets.Add(new MyraLabel("Not connected..", MyraLabel.TextStyle.P));
            row.Widgets.Add(new MyraButton("Try to connect", () =>
            {
                _manager.Dispose();
                _manager.Init();
            }));
            root.Widgets.Add(row);
        }
        else
        {
            root.Widgets.Add(BuildMainArea());
            root.Widgets.Add(BuildInputRow());
            RebuildChannels();
            RebuildMessages();
            RebuildUsers();
        }

        SetRootContent(root);
    }

    private Widget BuildMainArea()
    {
        _channelsPanel = new VerticalStackPanel { Spacing = 2 };
        _messagesPanel = new VerticalStackPanel { Spacing = 1 };
        _usersPanel    = new VerticalStackPanel { Spacing = 1 };

        var channelScroll = new ScrollViewer { Width = CHANNEL_WIDTH, Height = PANEL_HEIGHT, Content = _channelsPanel};
        _messageScroll    = new ScrollViewer { Width = MSG_WIDTH,     Height = PANEL_HEIGHT, Content = _messagesPanel, Border = new SolidBrush(new Color(0, 0, 0, MyraStyle.STANDARD_BORDER_ALPHA)), BorderThickness = new Thickness(1)  };
        var userScroll    = new ScrollViewer { Width = USER_WIDTH,    Height = PANEL_HEIGHT, Content = _usersPanel  };

        var row = new HorizontalStackPanel { Spacing = 3 };
        row.Widgets.Add(channelScroll);
        row.Widgets.Add(_messageScroll);
        row.Widgets.Add(userScroll);
        return row;
    }

    private Widget BuildInputRow()
    {
        _chatInput = new MyraInputBox
        {
            HintText = "Type a message...",
            Width = MSG_WIDTH + CHANNEL_WIDTH + 3
        };
        _chatInput.KeyDown += (_, args) => { if (args.Data == Keys.Enter) TrySend(); };

        // Profile? profile = ProfileManager.CurrentProfile;
        // var checkbox = MyraCheckButton.CreateWithCallback(
        //     profile?.ConnectToIrcOnLogin ?? false,
        //     v => { if (profile != null) profile.ConnectToIrcOnLogin = v; },
        //     "Connect on login");

        var row = new HorizontalStackPanel { Spacing = 4 };
        row.Widgets.Add(_chatInput);
        row.Widgets.Add(new MyraButton("Send", TrySend));
        row.Widgets.Add(new MyraButton("Options", () =>
        {
            ShowContextMenu((ContextMenuLabelToggle(!ProfileManager.CurrentProfile.DisableConnectToIrcOnLogin, "Auto connect"), () =>
            {
                ProfileManager.CurrentProfile.DisableConnectToIrcOnLogin = !ProfileManager.CurrentProfile.DisableConnectToIrcOnLogin;
            }));
        }));
        return row;
    }

    private void RebuildChannels()
    {
        if (_channelsPanel == null) return;

        string[] channels = _manager.GetChannels();

        bool autoSelected = false;
        if (string.IsNullOrEmpty(_selectedChannel) && channels.Length > 0)
        {
            _selectedChannel = channels[0];
            autoSelected = true;
        }

        _channelsPanel.Widgets.Clear();
        _channelsPanel.Widgets.Add(new MyraLabel("Channels", MyraLabel.TextStyle.P));

        foreach (string ch in channels)
        {
            string captured = ch;
            bool   selected = ch == _selectedChannel;

            var chRow = new HorizontalStackPanel { Spacing = 2 };
            chRow.Widgets.Add(new MyraButton(selected ? $"[{ch}]" : ch, () =>
            {
                _selectedChannel  = captured;
                _lastMessageCount = -1;
                RebuildChannels();
                RebuildMessages();
                RebuildUsers();
            }) { Width = CHANNEL_WIDTH - 30 });

            chRow.Widgets.Add(MyraStyle.ApplyButtonDangerStyle(new MyraButton("X", () =>
                TazUOChatManager.Instance.LeaveChannel(captured))
            { Tooltip = "Leave channel" }));

            _channelsPanel.Widgets.Add(chRow);
        }

        // Join input row
        var joinBox = new MyraInputBox { HintText = "channel...", Width = CHANNEL_WIDTH - 30 };
        var joinRow = new HorizontalStackPanel { Spacing = 2 };
        joinRow.Widgets.Add(joinBox);
        joinRow.Widgets.Add(new MyraButton("+", () => DoJoin(joinBox))
            { Tooltip = "Join or create a channel" });
        joinBox.KeyDown += (_, args) => { if (args.Data == Keys.Enter) DoJoin(joinBox); };
        _channelsPanel.Widgets.Add(joinRow);

        _lastChannelCount = _manager.TotalChannelCount;

        if (autoSelected)
        {
            RebuildMessages();
            RebuildUsers();
        }
    }

    private void RebuildMessages()
    {
        if (_messagesPanel == null) return;
        _messagesPanel.Widgets.Clear();

        if (!string.IsNullOrEmpty(_selectedChannel))
        {
            string[] messages = _manager.GetMessages(_selectedChannel);
            if (messages.Length == 0)
                _messagesPanel.Widgets.Add(new MyraLabel("No messages yet.", MyraLabel.TextStyle.P));
            else
                foreach (string msg in messages)
                    _messagesPanel.Widgets.Add(new MyraLabel(msg, MyraLabel.TextStyle.P) { MaxWidth = _messagesPanel.MaxWidth });
        }
        else
        {
            _messagesPanel.Widgets.Add(new MyraLabel("Select a channel.", MyraLabel.TextStyle.P));
        }

        _lastMessageCount = _manager.TotalMessageCount;
        _pendingScroll    = true;
    }

    private void RebuildUsers()
    {
        if (_usersPanel == null) return;
        _usersPanel.Widgets.Clear();

        if (!string.IsNullOrEmpty(_selectedChannel))
        {
            string[] users = _manager.GetUsers(_selectedChannel);
            _usersPanel.Widgets.Add(new MyraLabel($"Users ({users.Length})", MyraLabel.TextStyle.P));
            foreach (string user in users)
                _usersPanel.Widgets.Add(new MyraLabel(user == _manager.CurrentNick ? $"/c[green]{user}" : user, MyraLabel.TextStyle.P));
        }
        else
        {
            _usersPanel.Widgets.Add(new MyraLabel("Users", MyraLabel.TextStyle.P));
        }
    }

    private void TrySend()
    {
        if (_chatInput == null || string.IsNullOrEmpty(_selectedChannel)) return;
        string text = _chatInput.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(text)) return;
        _manager.SendMessage(_selectedChannel, text);
        _chatInput.Text = "";
    }

    private void DoJoin(MyraInputBox joinBox)
    {
        string ch = joinBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(ch)) return;
        _manager.JoinChannel(ch);
        joinBox.Text = "";
    }
}
