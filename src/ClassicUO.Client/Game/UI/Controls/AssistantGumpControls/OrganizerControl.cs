using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls.AssistantGumpControls;

public class OrganizerControl : Control
{
    private Profile profile;

    public OrganizerControl(int width, int height)
    {
        profile = ProfileManager.CurrentProfile;
        Width = width;
        Height = height;
        AcceptMouseInput = true;
        CanMove = true;

        BuildOrganizer();
    }

    public void BuildOrganizer()
    {
        Clear();

        // Left side content - organizer list
        var leftSideContent = new BaseOptionsGump.LeftSideMenuRightSideContent(Width, Height, (int)(Width * 0.28));
        Add(leftSideContent);

        // Add new organizer button
        BaseOptionsGump.ModernButton addButton = new(0, 0, leftSideContent.LeftWidth, BaseOptionsGump.ThemeSettings.CHECKBOX_SIZE, ButtonAction.Default, "+ Add Organizer", BaseOptionsGump.ThemeSettings.BUTTON_FONT_COLOR);
        addButton.MouseUp += (sender, e) =>
        {
            var newConfig = OrganizerAgent.Instance?.NewOrganizerConfig();
            if (newConfig != null)
            {
                var configButton = CreateOrganizerConfigButton(newConfig, leftSideContent);
                leftSideContent.AddToLeft(configButton);
                // Auto-select the new config
                SelectOrganizerConfig(newConfig, leftSideContent, configButton);
            }
        };
        leftSideContent.AddToLeft(addButton);

        // Organizer configurations list
        OrganizerConfig firstConfig = null;
        if (OrganizerAgent.Instance?.OrganizerConfigs != null)
        {
            for (int i = 0; i < OrganizerAgent.Instance.OrganizerConfigs.Count; i++)
            {
                var config = OrganizerAgent.Instance.OrganizerConfigs[i];
                if (firstConfig == null) firstConfig = config;

                var configButton = CreateOrganizerConfigButton(config, leftSideContent);
                leftSideContent.AddToLeft(configButton);
            }
        }
    }

    private BaseOptionsGump.ModernButton CreateOrganizerConfigButton(OrganizerConfig config, BaseOptionsGump.LeftSideMenuRightSideContent leftSideContent)
    {
        var button = new BaseOptionsGump.ModernButton(0, 0, leftSideContent.LeftWidth, BaseOptionsGump.ThemeSettings.CHECKBOX_SIZE, ButtonAction.Default, config.Name, BaseOptionsGump.ThemeSettings.BUTTON_FONT_COLOR);
        button.MouseUp += (sender, e) =>
        {
            SelectOrganizerConfig(config, leftSideContent, button);
        };
        return button;
    }

    private void SelectOrganizerConfig(OrganizerConfig config, BaseOptionsGump.LeftSideMenuRightSideContent leftSideContent, BaseOptionsGump.ModernButton button)
    {
        // Clear right side
        leftSideContent.RightArea.Clear();
        leftSideContent.ResetRightSide();

        // Add configuration details to right side
        BuildOrganizerConfigDetails(config, leftSideContent, button);
    }

    private void BuildOrganizerConfigDetails(OrganizerConfig config, BaseOptionsGump.LeftSideMenuRightSideContent leftSideContent, BaseOptionsGump.ModernButton button)
    {
        HBoxContainer box = new HBoxContainer(50);
        // Name input
        BaseOptionsGump.InputFieldWithLabel nameInput = null;
        nameInput = new BaseOptionsGump.InputFieldWithLabel("Name", BaseOptionsGump.ThemeSettings.INPUT_WIDTH, config.Name, false, (s, e) =>
        {
            config.Name = nameInput.Text;
            button.TextLabel.SetText(nameInput.Text);
        });
        leftSideContent.AddToRight(nameInput);

        // Enabled checkbox
        var enabledCheckbox = new BaseOptionsGump.CheckboxWithLabel("Enabled", 0, config.Enabled, b => config.Enabled = b)
        {
            X = nameInput.X + nameInput.Width + 10,
            Y = nameInput.Y
        };
        leftSideContent.AddToRight(enabledCheckbox, false);

        // Run organizer button
        BaseOptionsGump.ModernButton runButton = new(0, 0, 150, BaseOptionsGump.ThemeSettings.CHECKBOX_SIZE, ButtonAction.Default, "Run Organizer", Color.Lime)
        {
            IsSelected = true,
        };
        runButton.MouseUp += (sender, e) =>
        {
            OrganizerAgent.Instance?.RunOrganizer(config.Name);
        };
        leftSideContent.AddToRight(runButton);

        //Dupe button
        BaseOptionsGump.ModernButton dupeButton = new(0, 0, 150, BaseOptionsGump.ThemeSettings.CHECKBOX_SIZE, ButtonAction.Default, "Duplicate", Color.Cyan)
        {
            IsSelected = true,
        };
        dupeButton.MouseUp += (sender, e) =>
        {
            var dupedConfig = OrganizerAgent.Instance?.DupeConfig(config);

            if (dupedConfig != null)
            {
                var configButton = CreateOrganizerConfigButton(dupedConfig, leftSideContent);
                leftSideContent.AddToLeft(configButton);
            }
        };
        leftSideContent.AddToRight(dupeButton);

        //Macro button
        BaseOptionsGump.ModernButton MacroButton = new(0, 0, 150, BaseOptionsGump.ThemeSettings.CHECKBOX_SIZE, ButtonAction.Default, "+ Macro Button", Color.Yellow)
        {
            IsSelected = true,
        };
        MacroButton.MouseUp += (sender, e) =>
        {
            OrganizerAgent.Instance?.CreateOrganizerMacroButton(config.Name);
            GameActions.Print($"Created Organizer Macro: Organizer: {config.Name}");
        };
        leftSideContent.AddToRight(MacroButton);

        // Delete organizer button
        BaseOptionsGump.ModernButton deleteButton = new(0, 0, 150, BaseOptionsGump.ThemeSettings.CHECKBOX_SIZE, ButtonAction.Default, "Delete Organizer", Color.Red)
        {
            IsSelected = true,
            X = dupeButton.X + dupeButton.Width + 60,
            Y = runButton.Y
        };

        deleteButton.MouseUp += (sender, e) =>
        {
            OrganizerAgent.Instance?.DeleteConfig(config);
            button.Dispose();
            leftSideContent.RightArea.Clear();
            leftSideContent.ResetRightSide();
            leftSideContent.RepositionLeftMenuChildren();
        };
        leftSideContent.AddToRight(deleteButton, false);
        // Target container button
        BaseOptionsGump.ModernButton SourceButton = new(0, 0, 150, BaseOptionsGump.ThemeSettings.CHECKBOX_SIZE, ButtonAction.Default, "Set Source", BaseOptionsGump.ThemeSettings.BUTTON_FONT_COLOR)
        {
            IsSelected = true
        };
        SourceButton.MouseUp += (s, e) =>
        {
            GameActions.Print("Select [SOURCE] Container", 82);
            TargetManager.LastTargetInfo.Clear();
            _ = TargetHelper.TargetObject((source) =>
            {
                if (source == null || !SerialHelper.IsItem(source))
                {
                    GameActions.Print("Only items can be selected!");
                    return;
                }
                config.SourceContSerial = source.Serial;
                GameActions.Print($"Source container set to {source.Serial:X}", 63);
                SelectOrganizerConfig(config, leftSideContent, button);
            });
        };
        leftSideContent.AddToRight(SourceButton);

        BaseOptionsGump.ModernButton DestButton = new(0, 0, 150, BaseOptionsGump.ThemeSettings.CHECKBOX_SIZE, ButtonAction.Default, "Set Destination", BaseOptionsGump.ThemeSettings.BUTTON_FONT_COLOR)
        {
            IsSelected = true,
            X = SourceButton.X + SourceButton.Width + 60,
            Y = SourceButton.Y
        };
        DestButton.MouseUp += (s, e) =>
        {
            GameActions.Print("Select [DESTINATION] Container", 82);
            TargetManager.LastTargetInfo.Clear();
            _ = TargetHelper.TargetObject((destination) =>
            {
                if (destination == null || !SerialHelper.IsItem(destination))
                {
                    GameActions.Print("Only items can be selected!");
                    return;
                }
                config.DestContSerial = destination.Serial;
                GameActions.Print($"Destination container set to {destination.Serial:X}", 63);
                SelectOrganizerConfig(config, leftSideContent, button);
            });
        };
        leftSideContent.AddToRight(DestButton,false);


        box.BlankLine();
        // Current source container display
        if (config.SourceContSerial != 0)
        {
            box.Add(TextBox.GetOne($"Source ({World.Items.Get(config.SourceContSerial)?.Name}, {config.SourceContSerial:X})", BaseOptionsGump.ThemeSettings.FONT, BaseOptionsGump.ThemeSettings.STANDARD_TEXT_SIZE - 1, Color.White, TextBox.RTLOptions.Default()));
        }
        else
        {
            box.Add(TextBox.GetOne($"Source (Your backpack)", BaseOptionsGump.ThemeSettings.FONT, BaseOptionsGump.ThemeSettings.STANDARD_TEXT_SIZE - 1, Color.DarkGreen, TextBox.RTLOptions.Default()));
        }
        box.BlankLine();
        // Current destination container display
        if (config.DestContSerial != 0)
        {
            box.Add(TextBox.GetOne($"Dest. ({World.Items.Get(config.DestContSerial)?.Name}, {config.DestContSerial:X})", BaseOptionsGump.ThemeSettings.FONT, BaseOptionsGump.ThemeSettings.STANDARD_TEXT_SIZE - 1, Color.White, TextBox.RTLOptions.Default()));
        }
        leftSideContent.AddToRight(box);
        leftSideContent.BlankLine();

        // Items to organize label
        Control c;
        leftSideContent.AddToRight(c = TextBox.GetOne("Items to organize:                              Amount:", BaseOptionsGump.ThemeSettings.FONT, BaseOptionsGump.ThemeSettings.STANDARD_TEXT_SIZE, Color.White, TextBox.RTLOptions.Default()));
        // Add item button with targeting
        BaseOptionsGump.ModernButton addItemButton = new(0, 0, 120, BaseOptionsGump.ThemeSettings.CHECKBOX_SIZE, ButtonAction.Default, "Add Item", BaseOptionsGump.ThemeSettings.BUTTON_FONT_COLOR)
        {
            X = leftSideContent.RightWidth - 145,
            Y = c.Y - 13
        };
        addItemButton.MouseUp += (sender, e) =>
        {
            TargetHelper.TargetObject((obj) =>
            {
                if (obj == null || !SerialHelper.IsItem(obj))
                {
                    GameActions.Print("Only items can be added!");
                    return;
                }

                var newItemConfig = config.NewItemConfig();
                newItemConfig.Graphic = obj.Graphic;
                newItemConfig.Hue = obj.Hue;

                GameActions.Print($"Added item: Graphic {obj.Graphic:X}, Hue {obj.Hue:X}");

                // Refresh the right side to show the new item
                SelectOrganizerConfig(config, leftSideContent, button);
            });
        };
        leftSideContent.AddToRight(addItemButton, false);

        // Item configurations list
        foreach (var itemConfig in config.ItemConfigs)
        {
            var itemArea = CreateOrganizerItemConfigArea(itemConfig, config, leftSideContent, button);
            leftSideContent.AddToRight(itemArea);
        }
    }

    private Control CreateOrganizerItemConfigArea(OrganizerItemConfig itemConfig, OrganizerConfig parentConfig, BaseOptionsGump.LeftSideMenuRightSideContent leftSideContent, BaseOptionsGump.ModernButton button)
    {
        var itemArea = new Area() { Width = leftSideContent.RightArea.Width - 20, AcceptMouseInput = false };
        Control c;

        var rsp = new ResizableStaticPic(itemConfig.Graphic, 50, 50) { Hue = (ushort)(itemConfig.Hue == ushort.MaxValue ? 0 : itemConfig.Hue) };
        itemArea.Add(rsp);

        // Item info display
        var itemText = $"Graphic: {itemConfig.Graphic:X4}, Hue: {(itemConfig.Hue == ushort.MaxValue ? "ANY" : itemConfig.Hue.ToString())}";
        itemArea.Add(c = TextBox.GetOne(itemText, BaseOptionsGump.ThemeSettings.FONT, BaseOptionsGump.ThemeSettings.STANDARD_TEXT_SIZE, Color.White, TextBox.RTLOptions.Default()));
        c.X = rsp.Width + 1;
        c.Y = (rsp.Height - c.Height) / 2;

        BaseOptionsGump.InputField input = new BaseOptionsGump.InputField(100, BaseOptionsGump.ThemeSettings.CHECKBOX_SIZE, 100, 60000, itemConfig.Amount.ToString(), true,
        onTextChanges: (s, e) =>
        {
            if (ushort.TryParse(((BaseOptionsGump.InputField.StbTextBox)s).Text, out ushort amount))
            {
                if (amount <= 0) amount = 0;
                itemConfig.Amount = amount;
            }
        })
        {
            X = 240,
            Y = 10
        };

        input.SetTooltip("Amount of item quantity to move, 0 = All");

        itemArea.Add(input);
        // Enabled checkbox
        var enabledCheckbox = new BaseOptionsGump.CheckboxWithLabel("Enabled", 0, itemConfig.Enabled, b => itemConfig.Enabled = b)
        {
            X = itemArea.Width - 130
        };
        enabledCheckbox.Y = (rsp.Height - enabledCheckbox.Height) / 2;
        itemArea.Add(enabledCheckbox);

        // Delete button
        BaseOptionsGump.ModernButton deleteButton = new(0, 0, 20, BaseOptionsGump.ThemeSettings.CHECKBOX_SIZE, ButtonAction.Default, "X", Color.Red)
        {
            X = enabledCheckbox.X + enabledCheckbox.Width + 10
        };
        deleteButton.Y = (rsp.Height - deleteButton.Height) / 2;
        deleteButton.MouseUp += (sender, e) =>
        {
            parentConfig.DeleteItemConfig(itemConfig);
            // Refresh the right side to remove the deleted item
            SelectOrganizerConfig(parentConfig, leftSideContent, button);
        };
        itemArea.Add(deleteButton);
        itemArea.ForceSizeUpdate();
        return itemArea;

    }
}
