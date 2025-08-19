using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System.Linq;
using ClassicUO.Game.Data;
using System.Collections.Generic;
using ClassicUO.Configuration;

namespace ClassicUO.Game.UI.Gumps
{
    internal class DressAgentConfigGump : NineSliceGump
    {
        private DressConfig _config;
        private VBoxContainer _buttonContainer;
        private VBoxContainer _itemsList;
        private ModernScrollArea _scrollArea;
        private bool _readOnly;
        private Combobox _configCombobox;
        private List<DressConfig> _allConfigs;

        public DressAgentConfigGump(DressConfig config, bool readOnly = false)
            : base(100, 100, 600, 400, ModernUIConstants.ModernUIPanel, ModernUIConstants.ModernUIPanel_BoderSize, false)
        {
            _config = config;
            _readOnly = readOnly;

            CanMove = true;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;

            BuildGump();
        }

        private void BuildGump()
        {
            Clear();

            // Build list of all configs (current player + other characters)
            _allConfigs = new List<DressConfig>();
            _allConfigs.AddRange(DressAgentManager.Instance.CurrentPlayerConfigs);
            _allConfigs.AddRange(DressAgentManager.Instance.OtherCharacterConfigs);

            // Config selection dropdown
            Add(new Label("Select Config:", true, 0xFFFF, font: 1)
            {
                X = 20,
                Y = 20
            });

            string[] configOptions = _allConfigs.Select(c =>
                c.CharacterName == ProfileManager.CurrentProfile?.CharacterName
                    ? c.Name
                    : $"{c.Name} ({c.CharacterName})"
            ).ToArray();

            int selectedIndex = _allConfigs.FindIndex(c => c == _config);
            if (selectedIndex == -1) selectedIndex = 0;

            _configCombobox = new Combobox(120, 20, 250, configOptions, selectedIndex, emptyString: "No configs available")
            {
                SelectedIndex = selectedIndex
            };
            _configCombobox.OnOptionSelected += OnConfigSelected;
            Add(_configCombobox);

            // Create Config button
            var createButton = new NiceButton(380, 20, 80, 25, ButtonAction.Default, "Create New") { IsSelectable = false, DisplayBorder = true };
            createButton.MouseUp += (s, e) =>
            {
                CreateNewConfig();
            };
            Add(createButton);

            // Title with rename functionality
            Add(new Label("Config Name:", true, 0xFFFF, font: 1)
            {
                X = 20,
                Y = 50
            });

            if (!_readOnly)
            {
                var nameInput = new StbTextBox(1, -1, 250)
                {
                    X = 120,
                    Y = 50,
                    Width = 250,
                    Height = 25,
                    Text = _config.Name,
                    Hue = 42
                };
                nameInput.TextChanged += (s, e) =>
                {
                    _config.Name = nameInput.Text;
                    DressAgentManager.Instance.Save();
                };
                Add(nameInput);
            }
            else
            {
                Add(new Label(_config.Name, true, 0xFFFF, font: 1)
                {
                    X = 120,
                    Y = 50
                });
            }

            // Character info
            Add(new Label($"Character: {_config.CharacterName}", true, 999, font: 1)
            {
                X = 20,
                Y = 75
            });

            // KR Packet option
            if (!_readOnly)
            {
                var krPacketCheckbox = new Checkbox(0x00D2, 0x00D3, "Use KR Equip Packets (faster)", 1, 0xFFFF, true)
                {
                    X = 300,
                    Y = 75,
                    IsChecked = _config.UseKREquipPacket
                };
                krPacketCheckbox.ValueChanged += (s, e) =>
                {
                    _config.UseKREquipPacket = krPacketCheckbox.IsChecked;
                    DressAgentManager.Instance.Save();
                };
                Add(krPacketCheckbox);
            }

            // Left side - Action buttons
            _buttonContainer = new VBoxContainer(180)
            {
                X = 20,
                Y = 110
            };

            if (!_readOnly)
            {
                AddButton("Add Item (Target)", () =>
                {
                    GameActions.Print("Target item to add to dress config");
                    TargetHelper.TargetObject((obj) =>
                    {
                        if (obj != null && SerialHelper.IsItem(obj.Serial))
                        {
                            DressAgentManager.Instance.AddItemToConfig(_config, obj.Serial, obj.Name);
                            RefreshItemsList();
                        }
                    });
                });

                AddButton("Add All Equipped", () =>
                {
                    DressAgentManager.Instance.AddCurrentlyEquippedItems(_config);
                    RefreshItemsList();
                });

                AddButton("Clear All Items", () =>
                {
                    DressAgentManager.Instance.ClearConfig(_config);
                    RefreshItemsList();
                });

                AddButton("Set Undress Bag", () =>
                {
                    GameActions.Print("Target container for undress items");
                    TargetHelper.TargetObject((obj) =>
                    {
                        if (obj != null && SerialHelper.IsItem(obj.Serial))
                        {
                            DressAgentManager.Instance.SetUndressBag(_config, obj.Serial);
                            GameActions.Print($"Undress bag set to: {obj.Name}");
                            RefreshItemsList();
                        }
                    });
                });
            }

            AddButton("Dress", 63, () =>
            {
                DressAgentManager.Instance.DressFromConfig(_config);
            });

            AddButton("Undress", 49, () =>
            {
                DressAgentManager.Instance.UndressFromConfig(_config);
            });

            if(!_readOnly) {
                AddButton("Create Dress Macro", () =>
                {
                    DressAgentManager.Instance.CreateDressMacro(_config.Name);
                    GameActions.Print($"Created dress macro: Dress: {_config.Name}");
                });

                AddButton("Create Undress Macro", () =>
                {
                    DressAgentManager.Instance.CreateUndressMacro(_config.Name);
                    GameActions.Print($"Created undress macro: Undress: {_config.Name}");
                });

                AddButton("Delete Config", 33, () =>
                {
                    DeleteCurrentConfig();
                });
            }

            Add(_buttonContainer);

            // Right side - Items list with scroll area
            _scrollArea = new ModernScrollArea(220, 110, 350, 280)
            {
                AcceptMouseInput = true,
                ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways
            };

            _itemsList = new VBoxContainer(330)
            {
                X = 0,
                Y = 0
            };

            _scrollArea.Add(_itemsList);
            Add(_scrollArea);

            RefreshItemsList();
        }

        private void AddButton(string text, System.Action action, ushort hue = ushort.MaxValue)
        {
            AddButton(text, hue, action);
        }

        private void AddButton(string text, ushort hue, System.Action action)
        {
            var button = new NiceButton(0, 0, 170, 25, ButtonAction.Default, text, hue: hue) { IsSelectable = false, DisplayBorder = true };
            button.MouseUp += (s, e) => action();
            _buttonContainer.Add(button);
        }

        private void OnConfigSelected(object sender, int selectedIndex)
        {
            if (selectedIndex >= 0 && selectedIndex < _allConfigs.Count)
            {
                var newConfig = _allConfigs[selectedIndex];

                // Check if this is a config from another character (read-only)
                bool isOtherCharacter = newConfig.CharacterName != ProfileManager.CurrentProfile?.CharacterName;

                _config = newConfig;
                _readOnly = isOtherCharacter;

                // Rebuild the gump with the new config
                BuildGump();
            }
        }

        private void RefreshConfigDropdown()
        {
            // Rebuild the dropdown options to reflect any name changes
            if (_configCombobox != null && _allConfigs != null)
            {
                string[] configOptions = _allConfigs.Select(c =>
                    c.CharacterName == ProfileManager.CurrentProfile?.CharacterName
                        ? c.Name
                        : $"{c.Name} ({c.CharacterName})"
                ).ToArray();

                int currentIndex = _allConfigs.FindIndex(c => c == _config);
                if (currentIndex != -1)
                {
                    _configCombobox.SelectedIndex = currentIndex;
                }
            }
        }

        private void CreateNewConfig()
        {
            // Generate a unique name for the new config
            string baseName = "New Config";
            string newName = baseName;
            int counter = 1;

            while (DressAgentManager.Instance.CurrentPlayerConfigs.Any(c => c.Name.Equals(newName, System.StringComparison.OrdinalIgnoreCase)))
            {
                newName = $"{baseName} {counter}";
                counter++;
            }

            // Create the new config
            var newConfig = DressAgentManager.Instance.CreateNewConfig(newName);

            // Switch to the new config
            _config = newConfig;
            _readOnly = false;

            // Rebuild the gump to reflect the new config
            BuildGump();

            GameActions.Print($"Created new dress config: {newName}");
        }

        private void DeleteCurrentConfig()
        {
            // Check if there are other configs to switch to
            var availableConfigs = DressAgentManager.Instance.CurrentPlayerConfigs.Where(c => c != _config).ToList();

            if (availableConfigs.Count > 0)
            {
                // Delete the current config
                DressAgentManager.Instance.DeleteConfig(_config);

                // Switch to the first available config
                _config = availableConfigs[0];
                _readOnly = false;

                // Rebuild the gump with the new config
                BuildGump();

                GameActions.Print($"Deleted config. Switched to: {_config.Name}");
            }
            else
            {
                // No other configs exist, check if there are other character configs to view
                if (DressAgentManager.Instance.OtherCharacterConfigs.Count > 0)
                {
                    // Delete current config
                    DressAgentManager.Instance.DeleteConfig(_config);

                    // Switch to first other character config (read-only)
                    _config = DressAgentManager.Instance.OtherCharacterConfigs[0];
                    _readOnly = true;

                    // Rebuild the gump
                    BuildGump();

                    GameActions.Print($"Deleted config. Switched to: {_config.Name} ({_config.CharacterName}) - Read Only");
                }
                else
                {
                    // No configs left at all, dispose the gump
                    DressAgentManager.Instance.DeleteConfig(_config);
                    GameActions.Print("Deleted last config. Closing dress agent.");
                    Dispose();
                }
            }
        }

        private void RefreshItemsList()
        {
            _itemsList.Clear();

            // Items header
            _itemsList.Add(new Label($"Items ({_config.Items.Count}):", true, 0xFFFF, font: 1));

            if (_config.Items.Count > 0)
            {
                foreach (var item in _config.Items)
                {
                    var itemArea = new Area { Width = 330, Height = 25 };

                    // Item name label with layer info
                    string layerName = ((Layer)item.Layer).ToString();
                    var nameLabel = new Label($"{item.Name} [{layerName}] ({item.Serial})", true, 0xFFFF, font: 1)
                    {
                        X = 5,
                        Y = 5
                    };
                    itemArea.Add(nameLabel);

                    // Delete button
                    if (!_readOnly)
                    {
                        var deleteButton = new NiceButton(itemArea.Width - 25, 2, 20, 20, ButtonAction.Default, "X") { IsSelectable = false, DisplayBorder = true };
                        deleteButton.MouseUp += (s, e) =>
                        {
                            DressAgentManager.Instance.RemoveItemFromConfig(_config, item.Serial);
                            RefreshItemsList();
                        };
                        itemArea.Add(deleteButton);
                    }

                    _itemsList.Add(itemArea);
                }
            }
            else
            {
                _itemsList.Add(new Label("No items configured.", true, 0xFFFF, font: 1));
                if (!_readOnly)
                {
                    _itemsList.Add(new Label("Use the buttons on the left to add items.", true, 0xFFFF, font: 1));
                }
            }

            // Undress bag info
            if (_config.UndressBagSerial != 0)
            {
                var bagItem = World.Items.TryGetValue(_config.UndressBagSerial, out var item) ? item : null;
                string bagName = bagItem?.Name ?? "Unknown";
                _itemsList.Add(new Label($"Undress Bag: {bagName} ({_config.UndressBagSerial})", true, 53, font: 1));
            }
            else
            {
                _itemsList.Add(new Label("Undress Bag: Player Backpack (default)", true, 0xFFFF, font: 1));
            }
        }
    }
}
