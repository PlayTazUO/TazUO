#nullable enable
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant;

public static class FriendsListTabContent
{
    public static Widget Build()
    {
        var friendsListPanel = new VerticalStackPanel { Spacing = 4 };

        void BuildFriendsList()
        {
            friendsListPanel.Widgets.Clear();

            List<FriendEntry> friends = FriendsListManager.Instance.GetFriends();

            if (friends.Count == 0)
            {
                friendsListPanel.Widgets.Add(new MyraLabel(TazLang.Get("assistant_friends_empty", "No friends added yet."), MyraLabel.TextStyle.P));
                return;
            }

            friendsListPanel.Widgets.Add(new MyraLabel(TazLang.Get("assistant_friends_title", "Current Friends:"), MyraLabel.TextStyle.H2));

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Numeric(TazLang.Get("assistant_friends_col_serial", "Serial")),
                GridColumnInfo.Fill(TazLang.Get("shared_name", "Name"), 2),
                GridColumnInfo.Auto(TazLang.Get("assistant_friends_col_dateadded", "Date Added")),
                GridColumnInfo.Auto(TazLang.Get("shared_actions", "Actions"))
            );

            int row = 1;
            for (int i = friends.Count - 1; i >= 0; i--)
            {
                FriendEntry f = friends[i];

                grid.AddWidget(new MyraLabel(f.Serial != 0 ? f.Serial.ToString() : TazLang.Get("shared_na", "N/A"), MyraLabel.TextStyle.P, MyraLabel.AlignMode.Right), row, 0);
                grid.AddWidget(new MyraLabel(f.Name ?? TazLang.Get("shared_unknown", "Unknown"), MyraLabel.TextStyle.P), row, 1);
                grid.AddWidget(new MyraLabel(f.DateAdded.ToString("yyyy-MM-dd"), MyraLabel.TextStyle.P), row, 2);
                grid.AddWidget(MyraStyle.ApplyButtonDangerStyle(new MyraButton(TazLang.Get("shared_remove", "Remove"), () =>
                {
                    bool removed = f.Serial != 0
                        ? FriendsListManager.Instance.RemoveFriend(f.Serial)
                        : FriendsListManager.Instance.RemoveFriend(f.Name);

                    if (removed)
                    {
                        GameActions.Print(World.Instance, TazLang.Get("assistant_friends_removed_fmt", "Removed {0} from friends list", new[] { f.Name }));
                        BuildFriendsList();
                    }
                })), row, 3);

                row++;
            }

            friendsListPanel.Widgets.Add(grid);
        }

        BuildFriendsList();

        var root = new VerticalStackPanel { Spacing = 6 };
        root.Widgets.Add(new MyraLabel(TazLang.Get("assistant_friends_desc", "Manage your friends list."), MyraLabel.TextStyle.H3));
        root.Widgets.Add(new MyraButton(TazLang.Get("assistant_friends_add_target", "Add by Target"), () =>
        {
            GameActions.Print(World.Instance, TazLang.Get("assistant_friends_add_target_prompt", "Target a player to add to friends list"));
            World.Instance.TargetManager.SetTargeting(targeted =>
            {
                if (targeted is Mobile mobile)
                {
                    if (FriendsListManager.Instance.AddFriend(mobile))
                    {
                        GameActions.Print(World.Instance, TazLang.Get("assistant_friends_added_fmt", "Added {0} to friends list", new[] { mobile.Name }));
                        BuildFriendsList();
                    }
                    else
                    {
                        GameActions.Print(World.Instance, TazLang.Get("assistant_friends_already_fmt", "Could not add {0} — already in friends list", new[] { mobile.Name }));
                    }
                }
                else
                {
                    GameActions.Print(World.Instance, TazLang.Get("assistant_friends_invalid_target", "Invalid target — must be a player"));
                }
            });
        }));
        root.Widgets.Add(new ScrollViewer { Height = 300, Content = friendsListPanel });

        return root;
    }
}
