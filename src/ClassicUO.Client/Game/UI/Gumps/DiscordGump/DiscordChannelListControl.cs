using System.Collections.Generic;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using Discord.Sdk;

namespace ClassicUO.Game.UI.Controls;

public class DiscordChannelListControl : Control
{
    public override bool AcceptMouseInput => true;

    private DataBox _channelList;
    private readonly DiscordGump _discordGump;
    private readonly HashSet<ulong> _currentChanIdList = new();

    public DiscordChannelListControl(int width, int height, DiscordGump gump)
    {
        CanMove = true;
        CanCloseWithRightClick = true;
        Width = width;
        Height = height;
        _discordGump = gump;

        Build();

        DiscordManager.Instance.OnLobbyCreated += OnLobbyCreated;
        DiscordManager.Instance.OnLobbyDeleted += OnLobbyDeleted;
    }

    private void OnLobbyDeleted(object sender) =>
        //TODO: Only delete required lobby instead of rebuilding the entire channel list.
        BuildChannelList();

    public override void Dispose()
    {
        base.Dispose();
        DiscordManager.Instance.OnLobbyCreated -= OnLobbyCreated;
        DiscordManager.Instance.OnLobbyDeleted -= OnLobbyDeleted;
    }
    
    private void OnLobbyCreated(object sender)
    {
        var channel = (LobbyHandle)sender;

        if (channel == null)
            return;
        
        BuildChannelList();
    }

    private void Build()
    {
        ScrollArea scroll = new(0, 0, Width, Height, true)
        {
            ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways
        };

        Add(scroll);

        scroll.Add(_channelList = new DataBox(0, 0, Width, 0));
        BuildChannelList();
    }

    public void BuildChannelList()
    {
        IEnumerable<LobbyHandle> channels = DiscordManager.Instance.GetLobbies();
        
        _channelList.Clear();
        _currentChanIdList.Clear();
        
        foreach (LobbyHandle channel in channels)
        {
            if(channel == null) continue;
            
            if(!_currentChanIdList.Add(channel.Id()))
                continue;
            
            _channelList.Add(new DiscordChannelListItem(_discordGump, channel, Width - 20)); //-20 for scroll bar
        }

        foreach (ulong chanId in DiscordManager.Instance.MessageHistory.Keys)
        {
            if(!_currentChanIdList.Add(chanId))
                continue;

            ChannelHandle channel = DiscordManager.Instance.GetChannel(chanId);
            
            if(channel != null)
                _channelList.Add(new DiscordChannelListItem(_discordGump, channel, Width - 20)); //-20 for scroll bar
        }

        _channelList.ReArrangeChildren();
        UpdateSelectedChannel();
    }

    public void OnChannelActivity(ulong channelId)
    {
        if(!_currentChanIdList.Contains(channelId))
            BuildChannelList();
    }
    
    public void UpdateSelectedChannel()
    {
        foreach (Control child in _channelList.Children)
        {
            if (child is DiscordChannelListItem item)
                item.SetSelected();
        }
    }
}