using ImGuiNET;
using ClassicUO.Game.Managers;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class FriendsListWindow : SingletonImGuiWindow<FriendsListWindow>
    {
        private List<FriendEntry> friendEntries;
        private bool showAddFriendPopup = false;
        private string newFriendName = "";

        private FriendsListWindow() : base("Friends List")
        {
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;
            RefreshFriendsList();
        }

        private void RefreshFriendsList() => friendEntries = FriendsListManager.Instance.GetFriends();

        public override void DrawContent()
        {
            ImGui.Spacing();

            // Header information
            ImGui.Text("Manage your friends list.");
            ImGui.Spacing();

            // Add friend buttons
            if (ImGui.Button("Add by Target"))
            {
                GameActions.Print(World.Instance, "Target a player to add to friends list");
                World.Instance.TargetManager.SetTargeting(targeted =>
                {
                    if (targeted != null && targeted is Mobile mobile)
                    {
                        if (FriendsListManager.Instance.AddFriend(mobile))
                        {
                            GameActions.Print(World.Instance, $"Added {mobile.Name} to friends list");
                            RefreshFriendsList();
                        }
                        else
                        {
                            GameActions.Print(World.Instance, $"Could not add {mobile.Name} - already in friends list");
                        }
                    }
                    else
                    {
                        GameActions.Print(World.Instance, "Invalid target - must be a player");
                    }
                });
            }

            ImGui.SameLine();

            if (ImGui.Button("Add by Name"))
            {
                showAddFriendPopup = true;
            }

            ImGui.Spacing();
            ImGui.SeparatorText("Current Friends:");

            // Display friends list
            if (friendEntries.Count == 0)
            {
                ImGui.Text("No friends added yet.");
            }
            else
            {
                // Table for friends list
                if (ImGui.BeginTable("FriendsTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(0, ImGuiTheme.Dimensions.STANDARD_TABLE_SCROLL_HEIGHT)))
                {
                    ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Serial", ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Date Added", ImGuiTableColumnFlags.WidthFixed, 120);
                    ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 80);
                    ImGui.TableHeadersRow();

                    for (int i = friendEntries.Count - 1; i >= 0; i--)
                    {
                        FriendEntry friend = friendEntries[i];
                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        ImGui.Text(friend.Name ?? "Unknown");

                        ImGui.TableNextColumn();
                        if (friend.Serial != 0)
                        {
                            ImGui.Text(friend.Serial.ToString());
                        }
                        else
                        {
                            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), "N/A");
                        }

                        ImGui.TableNextColumn();
                        ImGui.Text(friend.DateAdded.ToString("yyyy-MM-dd"));

                        ImGui.TableNextColumn();
                        if (ImGui.Button($"Remove##{i}"))
                        {
                            bool removed = friend.Serial != 0
                                ? FriendsListManager.Instance.RemoveFriend(friend.Serial)
                                : FriendsListManager.Instance.RemoveFriend(friend.Name);

                            if (removed)
                            {
                                GameActions.Print(World.Instance, $"Removed {friend.Name} from friends list");
                                RefreshFriendsList();
                            }
                        }
                    }

                    ImGui.EndTable();
                }
            }

            // Add friend by name popup
            if (showAddFriendPopup)
            {
                ImGui.OpenPopup("Add Friend by Name");
                showAddFriendPopup = false;
            }

            if (ImGui.BeginPopupModal("Add Friend by Name"))
            {
                ImGui.Text("Enter friend's name:");
                ImGui.SetNextItemWidth(200);
                ImGui.InputText("##FriendName", ref newFriendName, 100);

                ImGui.Spacing();

                if (ImGui.Button("Add"))
                {
                    if (!string.IsNullOrWhiteSpace(newFriendName))
                    {
                        if (FriendsListManager.Instance.AddFriend(0, newFriendName.Trim()))
                        {
                            GameActions.Print(World.Instance, $"Added {newFriendName.Trim()} to friends list");
                            RefreshFriendsList();
                            newFriendName = "";
                            ImGui.CloseCurrentPopup();
                        }
                        else
                        {
                            GameActions.Print(World.Instance, $"Could not add {newFriendName.Trim()} - already in friends list or invalid name");
                        }
                    }
                }

                ImGui.SameLine();

                if (ImGui.Button("Cancel"))
                {
                    newFriendName = "";
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }
        }
    }
}