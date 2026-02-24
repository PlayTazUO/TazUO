#nullable enable
using System.Collections.Generic;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant;

public static class FriendsListTabContent
{
    public static Widget Build()
    {
        var friendsListPanel = new VerticalStackPanel { Spacing = 2 };

        void BuildFriendsList()
        {
            friendsListPanel.Widgets.Clear();

            List<FriendEntry> friends = FriendsListManager.Instance.GetFriends();

            if (friends.Count == 0)
            {
                friendsListPanel.Widgets.Add(new MyraLabel("No friends added yet.", MyraLabel.Style.P));
                return;
            }

            friendsListPanel.Widgets.Add(new MyraLabel("Current Friends:", MyraLabel.Style.H2));

            var grid = new MyraGrid();
            grid.AddColumn(null, 4);
            grid.Border = new SolidBrush(MyraStyle.GridBorderColor);
            grid.BorderThickness = new Thickness(1);
            grid.GridLinesColor = MyraStyle.GridBorderColor;
            grid.ShowGridLines = true;

            grid.AddWidget(new MyraLabel("Name", MyraLabel.Style.H3), 0, 0);
            grid.AddWidget(new MyraLabel("Serial", MyraLabel.Style.H3), 0, 1);
            grid.AddWidget(new MyraLabel("Date Added", MyraLabel.Style.H3), 0, 2);

            int row = 1;
            for (int i = friends.Count - 1; i >= 0; i--)
            {
                FriendEntry f = friends[i];

                grid.AddWidget(new MyraLabel(f.Name ?? "Unknown", MyraLabel.Style.P), row, 0);
                grid.AddWidget(new MyraLabel(f.Serial != 0 ? f.Serial.ToString() : "N/A", MyraLabel.Style.P), row, 1);
                grid.AddWidget(new MyraLabel(f.DateAdded.ToString("yyyy-MM-dd"), MyraLabel.Style.P), row, 2);
                grid.AddWidget(new MyraButton("Remove", () =>
                {
                    bool removed = f.Serial != 0
                        ? FriendsListManager.Instance.RemoveFriend(f.Serial)
                        : FriendsListManager.Instance.RemoveFriend(f.Name);

                    if (removed)
                    {
                        GameActions.Print(World.Instance, $"Removed {f.Name} from friends list");
                        BuildFriendsList();
                    }
                }), row, 3);

                row++;
            }

            friendsListPanel.Widgets.Add(grid);
        }

        BuildFriendsList();

        var root = new VerticalStackPanel { Spacing = 6 };
        root.Widgets.Add(new MyraLabel("Manage your friends list.", MyraLabel.Style.P));
        root.Widgets.Add(new MyraButton("Add by Target", () =>
        {
            GameActions.Print(World.Instance, "Target a player to add to friends list");
            World.Instance.TargetManager.SetTargeting(targeted =>
            {
                if (targeted is Mobile mobile)
                {
                    if (FriendsListManager.Instance.AddFriend(mobile))
                    {
                        GameActions.Print(World.Instance, $"Added {mobile.Name} to friends list");
                        BuildFriendsList();
                    }
                    else
                    {
                        GameActions.Print(World.Instance, $"Could not add {mobile.Name} — already in friends list");
                    }
                }
                else
                {
                    GameActions.Print(World.Instance, "Invalid target — must be a player");
                }
            });
        }));
        root.Widgets.Add(new ScrollViewer { Height = 300, Content = friendsListPanel });

        return root;
    }
}
