using ImGuiNET;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public class ItemDetailWindow : ImGuiWindow
    {
        public static HashSet<ItemInfo> OpenedWindows = new();

        private readonly ItemInfo _itemInfo;

        public ItemDetailWindow(ItemInfo itemInfo) : base($"Item Details - {itemInfo.Name}")
        {
            _itemInfo = itemInfo ?? throw new ArgumentNullException(nameof(itemInfo));
            WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;
            OpenedWindows.Add(itemInfo);
        }

        public override void DrawContent()
        {
            if (_itemInfo == null)
            {
                ImGui.Text("No item information available");
                return;
            }

            ImGui.Spacing();

            // Item graphic display
            DrawItemGraphic();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Basic information
            DrawBasicInfo();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Location and container information
            DrawLocationInfo();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Properties
            DrawProperties();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Action buttons
            DrawActionButtons();
        }

        private void DrawItemGraphic()
        {
            ImGui.Text("Item Graphic:");
            ImGui.SameLine();

            if (_itemInfo.Graphic > 0)
            {
                // Display the item graphic larger for detail view
                if (DrawArt(_itemInfo.Graphic, new Vector2(64, 64)))
                {
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip($"Graphic: {_itemInfo.Graphic} (0x{_itemInfo.Graphic:X4})");
                }
            }
            else
            {
                ImGui.Text("No graphic available");
            }

            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.Text($"Graphic ID: {_itemInfo.Graphic} (0x{_itemInfo.Graphic:X4})");
            if (_itemInfo.Hue > 0)
                ImGui.Text($"Hue: {_itemInfo.Hue} (0x{_itemInfo.Hue:X4})");
            else
                ImGui.Text("Hue: Default");
            ImGui.EndGroup();
        }

        private void DrawBasicInfo()
        {
            ImGui.Text("Basic Information");
            ImGui.Separator();

            ImGui.Text($"Name: {_itemInfo.Name}");
            ImGui.Text($"Serial: 0x{_itemInfo.Serial:X8}");
            ImGui.Text($"Layer: {_itemInfo.Layer} ({(int)_itemInfo.Layer})");

            TimeSpan timeAgo = DateTime.Now - _itemInfo.UpdatedTime;
            string timeText;
            if (timeAgo.TotalDays >= 1)
                timeText = $"{timeAgo.Days}d ago";
            else if (timeAgo.TotalHours >= 1)
                timeText = $"{timeAgo.Hours}h ago";
            else if (timeAgo.TotalMinutes >= 1)
                timeText = $"{(int)timeAgo.TotalMinutes}m ago";
            else
                timeText = "Just now";

            ImGui.Text($"Last seen: {timeText}");
            ImGui.Text($"Character: {_itemInfo.CharacterName}");
            if (!string.IsNullOrEmpty(_itemInfo.ServerName))
                ImGui.Text($"Server: {_itemInfo.ServerName}");
        }

        private void DrawLocationInfo()
        {
            ImGui.Text("Location Information");
            ImGui.Separator();

            if (_itemInfo.OnGround)
            {
                ImGui.Text($"Location: On ground at {_itemInfo.X}, {_itemInfo.Y}");
            }
            else
            {
                ImGui.Text("Location: In container");
                if (_itemInfo.Container != 0)
                {
                    ImGui.Text($"Container: 0x{_itemInfo.Container:X8}");

                    // Check if we can find root container information
                    Item containerItem = Client.Game.UO?.World?.Items?.Get(_itemInfo.Container);
                    if (containerItem != null && containerItem.RootContainer != 0 && containerItem.RootContainer != _itemInfo.Container)
                    {
                        ImGui.Text($"Root Container: 0x{containerItem.RootContainer:X8}");
                    }
                }
            }
        }

        private void DrawProperties()
        {
            ImGui.Text("Properties");
            ImGui.Separator();

            if (!string.IsNullOrEmpty(_itemInfo.Properties))
            {
                // Replace pipe separators with newlines for better display
                string[] properties = _itemInfo.Properties.Split('|');
                foreach (string property in properties)
                {
                    if (!string.IsNullOrWhiteSpace(property))
                    {
                        ImGui.BulletText(property.Trim());
                    }
                }
            }
            else
            {
                ImGui.Text("No properties available");
            }
        }

        private void DrawActionButtons()
        {
            ImGui.Text("Actions");
            ImGui.Separator();

            // Button to open container
            if (!_itemInfo.OnGround && _itemInfo.Container != 0)
            {
                if (ImGui.Button("View Container"))
                {
                    OpenContainerDetailWindow(_itemInfo.Container);
                }
                ImGui.SameLine();
                SetTooltip("View detailed information about the container");

                // Button to open root container if it exists and is different
                Item containerItem = Client.Game.UO?.World?.Items?.Get(_itemInfo.Container);
                if (containerItem != null && containerItem.RootContainer != 0 && containerItem.RootContainer != _itemInfo.Container)
                {
                    if (ImGui.Button("View Root Container"))
                    {
                        OpenContainerDetailWindow(containerItem.RootContainer);
                    }
                    ImGui.SameLine();
                    SetTooltip("View the root container details (usually the character's backpack)");
                }
            }

            // Button to locate item if it exists in the world
            Item worldItem = Client.Game.UO?.World?.Items?.Get(_itemInfo.Serial);
            if (worldItem != null)
            {
                if (ImGui.Button("Use Item"))
                {
                    GameActions.DoubleClick(Client.Game.UO.World, _itemInfo.Serial);
                }
                SetTooltip("Double-click the item to use it");
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));

                ImGui.Button("Item Not Available");
                SetTooltip("This item is not currently visible in the game world");

                ImGui.PopStyleColor(3);
            }

            // Try to locate button
            ImGui.Spacing();
            if (ImGui.Button("Try to Locate"))
            {
                TryToLocateItem();
            }
            SetTooltip("Create a quest arrow pointing to the item's location if known");

            // Close button
            ImGui.SameLine();
            if (ImGui.Button("Close"))
            {
                IsOpen = false;
            }
        }

        private void TryToLocateItem()
        {
            try
            {
                World world = Client.Game.UO?.World;
                if (world?.Player == null)
                {
                    Utility.Logging.Log.Warn("Cannot locate item: Player not available");
                    return;
                }

                int? targetX = null;
                int? targetY = null;

                // First, check if the item is on the ground
                if (_itemInfo.OnGround)
                {
                    targetX = _itemInfo.X;
                    targetY = _itemInfo.Y;
                }
                else if (_itemInfo.Container != 0)
                {
                    // Item is in a container, need to find the root container's location
                    Item containerItem = world.Items?.Get(_itemInfo.Container);
                    if (containerItem != null)
                    {
                        // Check if the root container is the player's backpack
                        if (containerItem.RootContainer == world.Player.Serial)
                        {
                            // Root container is the player - point to player's location
                            targetX = world.Player.X;
                            targetY = world.Player.Y;
                        }
                        else
                        {
                            // Try to find the root container's position
                            Item rootContainer = world.Items?.Get(containerItem.RootContainer);
                            if (rootContainer != null && rootContainer.OnGround)
                            {
                                targetX = rootContainer.X;
                                targetY = rootContainer.Y;
                            }
                            else
                            {
                                // Check if the root container is a Mobile
                                Mobile mobile = world.Mobiles?.Get(containerItem.RootContainer);
                                if (mobile != null)
                                {
                                    targetX = mobile.X;
                                    targetY = mobile.Y;
                                }
                                else
                                {
                                    // Search the database for root container location information
                                    SearchDatabaseForContainerLocation(containerItem.RootContainer);
                                    return; // Async operation, return here
                                }
                            }
                        }
                    }
                    else
                    {
                        // Container not found in world, search database
                        SearchDatabaseForContainerLocation(_itemInfo.Container);
                        return; // Async operation, return here
                    }
                }

                // If we have coordinates, create the quest arrow
                if (targetX.HasValue && targetY.HasValue)
                {
                    CreateQuestArrow(targetX.Value, targetY.Value);
                }
                else
                {
                    Utility.Logging.Log.Info("Cannot determine location for item");
                }
            }
            catch (Exception ex)
            {
                Utility.Logging.Log.Error($"Failed to locate item: {ex.Message}");
            }
        }

        private void SearchDatabaseForContainerLocation(uint containerSerial) => ItemDatabaseManager.Instance.SearchItems(
                results =>
                {
                    // Marshal UI interactions back to the main thread
                    MainThreadQueue.InvokeOnMainThread(() =>
                    {
                        if (results != null && results.Count > 0)
                        {
                            ItemInfo containerInfo = results[0];
                            if (containerInfo.OnGround)
                            {
                                CreateQuestArrow(containerInfo.X, containerInfo.Y);
                            }
                            else
                            {
                                // Container is also in another container, try to find its root
                                World world = Client.Game.UO?.World;
                                if (world?.Player != null && containerInfo.Container == world.Player.Serial)
                                {
                                    // Root is player
                                    CreateQuestArrow(world.Player.X, world.Player.Y);
                                }
                                else
                                {
                                    Utility.Logging.Log.Info($"Container 0x{containerSerial:X8} found but location cannot be determined");
                                }
                            }
                        }
                        else
                        {
                            Utility.Logging.Log.Info($"Container 0x{containerSerial:X8} not found in database");
                        }
                    });
                },
                serial: containerSerial,
                limit: 1
            );

        private void CreateQuestArrow(int x, int y)
        {
            try
            {
                World world = Client.Game.UO?.World;
                if (world == null)
                {
                    Utility.Logging.Log.Warn("Cannot create quest arrow: World not available");
                    return;
                }

                // Remove any existing quest arrow for this item
                QuestArrowGump existingArrow = UIManager.GetGump<QuestArrowGump>(_itemInfo.Serial);
                if (existingArrow != null)
                {
                    existingArrow.Dispose();
                }

                // Create new quest arrow
                var questArrow = new QuestArrowGump(world, _itemInfo.Serial, x, y);
                questArrow.CanCloseWithRightClick = true; // Allow right-click to close
                UIManager.Add(questArrow);

                Utility.Logging.Log.Info($"Quest arrow created pointing to location ({x}, {y})");
            }
            catch (Exception ex)
            {
                Utility.Logging.Log.Error($"Failed to create quest arrow: {ex.Message}");
            }
        }

        private void OpenContainerDetailWindow(uint containerSerial)
        {
            try
            {
                // Search for the container in the database
                ItemDatabaseManager.Instance.SearchItems(
                    results =>
                    {
                        // Marshal UI interactions back to the main thread
                        MainThreadQueue.InvokeOnMainThread(() =>
                        {
                            if (results != null && results.Count > 0)
                            {
                                // If we found the container in the database, open its detail window
                                ItemInfo containerInfo = results[0]; // Take the first (most recent) result
                                var detailWindow = new ItemDetailWindow(containerInfo);
                                ImGuiManager.AddWindow(detailWindow);
                            }
                            else
                            {
                                // Container not found in database - we could show a notification or message
                                Utility.Logging.Log.Warn($"Container 0x{containerSerial:X8} not found in item database");

                                // TODO: Could show a temporary message to the user that the container
                                // information is not available in the database
                            }
                        });
                    },
                    serial: containerSerial,
                    limit: 1
                );
            }
            catch (Exception ex)
            {
                Utility.Logging.Log.Error($"Failed to search for container 0x{containerSerial:X8}: {ex.Message}");
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            OpenedWindows.Remove(_itemInfo);
        }
    }
}
