using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Controls.ResizableComponents;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Network;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace ClassicUO.Game.UI.MyraWindows;

/// <summary>
/// Myra-based replacement for the legacy <c>ChatGump</c>. Lists the channels the server has
/// joined us to, lets one be selected and joined, and offers leave/create actions.
/// </summary>
public class ChatWindow : MyraControl
{
    private const int DEFAULT_WIDTH = 345;
    private const int DEFAULT_HEIGHT = 390;

    private readonly World _world;
    private readonly ListView _channelList;
    private readonly MyraLabel _currentChannelLabel;

    private string _selectedChannelName;
    private bool _rebuildingList;

    // Packets arrive off the UI thread, so they only flag a refresh. The widget tree is rebuilt
    // on the next PreDraw instead of being touched from the packet handler.
    private volatile bool _pendingRefresh;
    private volatile bool _pendingConferenceUpdate;

    public ChatWindow(World world) : base(TazLang.Get("chat"))
    {
        _world = world;

        _rootWindow.Props.Resize.ScrollerMode = ScrollViewerMode.None;
        _rootWindow.Props.Resize.MinWidth = 280;
        _rootWindow.Props.Resize.MinHeight = 320;
        _rootWindow.Width = DEFAULT_WIDTH;
        _rootWindow.Height = DEFAULT_HEIGHT;

        var layout = new MyraGrid();
        layout.AddColumn(new Proportion(ProportionType.Fill));
        layout.AddRow();                                      // Channels heading
        layout.AddRow(new Proportion(ProportionType.Fill));   // Channel list
        layout.AddRow();                                      // Current channel heading
        layout.AddRow();                                      // Current channel name
        layout.AddRow();                                      // Buttons

        layout.AddWidget(
            new MyraLabel(TazLang.Get("channels"), MyraLabel.TextStyle.H4)
            {
                HorizontalAlignment = HorizontalAlignment.Center
            },
            0, 0
        );

        _channelList = new ListView
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        ApplyChannelListStyle();
        _channelList.SelectedIndexChanged += OnChannelSelected;
        layout.AddWidget(_channelList, 1, 0);

        layout.AddWidget(
            new MyraLabel(TazLang.Get("your_current_channel"), MyraLabel.TextStyle.P)
            {
                HorizontalAlignment = HorizontalAlignment.Center
            },
            2, 0
        );

        _currentChannelLabel = new MyraLabel(string.Empty, MyraLabel.TextStyle.P)
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        layout.AddWidget(_currentChannelLabel, 3, 0);

        var buttons = new HorizontalStackPanel
        {
            Spacing = MyraStyle.STANDARD_SPACING,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        buttons.Widgets.Add(new MyraButton(TazLang.Get("join"), JoinSelectedChannel));
        buttons.Widgets.Add(new MyraButton(TazLang.Get("leave"), LeaveChannel));
        buttons.Widgets.Add(new MyraButton(TazLang.Get("create"), ShowCreateChannelDialog));
        layout.AddWidget(buttons, 4, 0);

        SetRootContent(layout);

        RefreshChannelList();
        UpdateCurrentChannelLabel();
        CenterInViewPort();
    }

    /// <summary>Called when the server adds or removes a channel.</summary>
    public void RequestUpdateContents() => _pendingRefresh = true;

    /// <summary>Called when the server tells us which channel we are now in.</summary>
    public void UpdateConference() => _pendingConferenceUpdate = true;

    public override void PreDraw()
    {
        base.PreDraw();

        if (IsDisposed)
            return;

        if (_pendingRefresh)
        {
            _pendingRefresh = false;
            RefreshChannelList();
        }

        if (_pendingConferenceUpdate)
        {
            _pendingConferenceUpdate = false;
            UpdateCurrentChannelLabel();
        }
    }

    /// <summary>
    /// Give the list the same skin as the rest of the Myra UI instead of Myra's default
    /// button-styled list background.
    /// </summary>
    private void ApplyChannelListStyle()
    {
        ListBoxStyle baseStyle = Stylesheet.Current.ComboBoxStyle?.ListBoxStyle ?? Stylesheet.Current.ListBoxStyle;
        if (baseStyle == null)
            return;

        var style = (ListBoxStyle)baseStyle.Clone();
        style.Background = new SolidBrush(new Color(0, 0, 0, 75));
        style.Border = new SolidBrush(MyraStyle.GridBorderColor);
        style.BorderThickness = new Thickness(1);

        _channelList.ApplyListBoxStyle(style);
    }

    private void RefreshChannelList()
    {
        string previousSelection = _selectedChannelName;

        _rebuildingList = true;
        try
        {
            _channelList.Widgets.Clear();
            _selectedChannelName = null;

            int selectedIndex = -1;
            int index = 0;

            foreach (KeyValuePair<string, ChatChannel> entry in _world.ChatManager.Channels)
            {
                string channelName = entry.Key;

                var row = new MyraLabel(channelName, MyraLabel.TextStyle.P) { Tag = channelName };
                row.TouchDoubleClick += (_, _) => JoinChannel(channelName);
                _channelList.Widgets.Add(row);

                if (channelName == previousSelection)
                    selectedIndex = index;

                index++;
            }

            if (selectedIndex >= 0)
            {
                _selectedChannelName = previousSelection;
                _channelList.SelectedIndex = selectedIndex;
            }
        }
        finally
        {
            _rebuildingList = false;
        }
    }

    private void OnChannelSelected(object sender, EventArgs e)
    {
        if (_rebuildingList)
            return;

        _selectedChannelName = _channelList.SelectedItem?.Tag as string;
    }

    private void UpdateCurrentChannelLabel() =>
        _currentChannelLabel.Text = _world.ChatManager.CurrentChannelName ?? string.Empty;

    private void JoinSelectedChannel() => JoinChannel(_selectedChannelName);

    private static void JoinChannel(string channelName)
    {
        if (!string.IsNullOrEmpty(channelName))
            AsyncNetClient.Socket.Send_ChatJoinCommand(channelName);
    }

    private static void LeaveChannel() => AsyncNetClient.Socket.Send_ChatLeaveChannelCommand();

    private void ShowCreateChannelDialog()
    {
        var nameBox = new MyraInputBox { HintText = TazLang.Get("name"), Width = 220 };

        var content = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
        content.Widgets.Add(new MyraLabel(TazLang.Get("create_achannel"), MyraLabel.TextStyle.P));
        content.Widgets.Add(nameBox);

        new MyraDialog(TazLang.Get("create"), content, ok =>
        {
            if (ok && !string.IsNullOrWhiteSpace(nameBox.Text))
                AsyncNetClient.Socket.Send_ChatCreateChannelCommand(nameBox.Text);
        });
    }
}
