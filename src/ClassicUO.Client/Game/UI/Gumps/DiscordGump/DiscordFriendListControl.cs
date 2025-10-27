using System.Linq;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using DiscordSocialSDK.Wrapper;

namespace ClassicUO.Game.UI.Controls;

public class DiscordFriendListControl : Control
{
    private DataBox _friendList;
    private DiscordGump _gump;

    public override bool AcceptMouseInput => true;

    public DiscordFriendListControl(int width, int height, DiscordGump gump)
    {
        Width = width;
        Height = height;
        CanMove = true;
        CanCloseWithRightClick = true;
        _gump = gump;
        Build();
    }

    private void Build()
    {
        ScrollArea scroll = new(0, 0, Width, Height, true)
        {
            ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways
        };

        Add(scroll);

        _friendList = new (0, 0, Width, 0);
        scroll.Add(_friendList);
        BuildFriendsList();
    }
    
    public void BuildFriendsList()
    {
        _friendList.Clear();

        System.Collections.Generic.IEnumerable<Discord.Sdk.RelationshipHandle> friends = DiscordManager.Instance.GetFriends();

        if (friends == null)
            return;

        foreach (Discord.Sdk.RelationshipHandle f in friends.OrderBy(u => u.User()?.IsOnline() != true))
        {
            Discord.Sdk.UserHandle user = f.User();

            if (user == null)
                continue;

            _friendList.Add(new DiscordUserListItem(_gump.World, _gump, user, Width - 20)); //-20 for scroll bar
        }

        _friendList.ReArrangeChildren();
    }

    public void UpdateSelectedFriend()
    {
        foreach (Control child in _friendList.Children)
        {
            if (child is DiscordUserListItem item)
                item.SetSelected();
        }
    }
}