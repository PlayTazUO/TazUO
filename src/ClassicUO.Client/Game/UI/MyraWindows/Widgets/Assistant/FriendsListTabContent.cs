#nullable enable
using System.Collections.Generic;
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
                friendsListPanel.Widgets.Add(new MyraLabel("尚未添加好友。", MyraLabel.TextStyle.P));
                return;
            }

            friendsListPanel.Widgets.Add(new MyraLabel("当前好友:", MyraLabel.TextStyle.H2));

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Numeric("序列号"),
                GridColumnInfo.Fill("名称", 2),
                GridColumnInfo.Auto("添加日期"),
                GridColumnInfo.Auto("")
            );

            int row = 1;
            for (int i = friends.Count - 1; i >= 0; i--)
            {
                FriendEntry f = friends[i];

                grid.AddWidget(new MyraLabel(f.Serial != 0 ? f.Serial.ToString() : "N/A", MyraLabel.TextStyle.P, MyraLabel.AlignMode.Right), row, 0);
                grid.AddWidget(new MyraLabel(f.Name ?? "Unknown", MyraLabel.TextStyle.P), row, 1);
                grid.AddWidget(new MyraLabel(f.DateAdded.ToString("yyyy-MM-dd"), MyraLabel.TextStyle.P), row, 2);
                grid.AddWidget(MyraStyle.ApplyButtonDangerStyle(new MyraButton("移除", () =>
                {
                    bool removed = f.Serial != 0
                        ? FriendsListManager.Instance.RemoveFriend(f.Serial)
                        : FriendsListManager.Instance.RemoveFriend(f.Name);

                    if (removed)
                    {
                        GameActions.Print(World.Instance, $"已将 {f.Name} 从好友列表中移除");
                        BuildFriendsList();
                    }
                })), row, 3);

                row++;
            }

            friendsListPanel.Widgets.Add(grid);
        }

        BuildFriendsList();

        var root = new VerticalStackPanel { Spacing = 6 };
        root.Widgets.Add(new MyraLabel("Manage your friends list.", MyraLabel.TextStyle.H3));
        root.Widgets.Add(new MyraButton("Add by Target", () =>
        {
            GameActions.Print(World.Instance, "选择玩家以添加至好友列表");
            World.Instance.TargetManager.SetTargeting(targeted =>
            {
                if (targeted is Mobile mobile)
                {
                    if (FriendsListManager.Instance.AddFriend(mobile))
                    {
                        GameActions.Print(World.Instance, $"已将 {mobile.Name} 添加到好友列表");
                        BuildFriendsList();
                    }
                    else
                    {
                        GameActions.Print(World.Instance, $"无法添加 {mobile.Name} — 已在好友列表中");
                    }
                }
                else
                {
                    GameActions.Print(World.Instance, "无效目标 — 必须是玩家");
                }
            });
        }));
        root.Widgets.Add(new ScrollViewer { Height = 300, Content = friendsListPanel });

        return root;
    }
}
