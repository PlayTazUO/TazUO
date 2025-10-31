// SPDX-License-Identifier: BSD-2-Clause
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Gumps
{
    public class NameOverHeadHandlerGump : Gump
    {
        public static Point? LastPosition;

        public override GumpType GumpType => GumpType.NameOverHeadHandler;

        private readonly List<RadioButton> _overheadButtons = new List<RadioButton>();
        private Control _alpha;
        private StbTextBox searchBox;

        public NameOverHeadHandlerGump(World world) : base(world, 0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = false; //Prevent accidentally closing when stay active is enabled

            if (LastPosition == null)
            {
                X = 100;
                Y = 100;
            }
            else
            {
                X = LastPosition.Value.X;
                Y = LastPosition.Value.Y;
            }

            WantUpdateSize = false;

            LayerOrder = UILayer.Over;

            Checkbox stayActive;
            Add
            (
                _alpha = new AlphaBlendControl(0.7f)
                {
                    Hue = 34
                }
            );

            Add
            (
                stayActive = new Checkbox
                (
                    0x00D2,
                    0x00D3,
                    "Stay active",
                    color: 0xFFFF
                )
                {
                    IsChecked = NameOverHeadManager.IsPermaToggled,
                }
            );
            stayActive.ValueChanged += (sender, e) => { World.NameOverHeadManager.SetOverheadToggled(stayActive.IsChecked); CanCloseWithRightClick = stayActive.IsChecked; };


            Checkbox hideFullHp;
            Add
            (
                hideFullHp = new Checkbox
                (
                    0x00D2,
                    0x00D3,
                    color: 0xFFFF
                )
                {
                    IsChecked = ProfileManager.CurrentProfile.NamePlateHideAtFullHealth,
                    X = stayActive.Width + stayActive.X + 5
                }
            );
            hideFullHp.SetTooltip("Hide nameplates above 100% health.");
            hideFullHp.ValueChanged += (sender, e) => { ProfileManager.CurrentProfile.NamePlateHideAtFullHealth = hideFullHp.IsChecked; };


            Checkbox hideInWarmode;
            Add
            (
                hideInWarmode = new Checkbox
                (
                    0x00D2,
                    0x00D3,
                    color: 0xFFFF
                )
                {
                    IsChecked = ProfileManager.CurrentProfile.NamePlateHideAtFullHealthInWarmode,
                    X = hideFullHp.Width + hideFullHp.X + 5
                }
            );
            hideInWarmode.SetTooltip("Only hide 100% hp nameplates in warmode.");
            hideInWarmode.ValueChanged += (sender, e) => { ProfileManager.CurrentProfile.NamePlateHideAtFullHealthInWarmode = hideInWarmode.IsChecked; };



            Add(new AlphaBlendControl() { Y = stayActive.Height + stayActive.Y, Width = 150, Height = 20, Hue = 0x0481 });
            Add(searchBox = new StbTextBox(0, -1, 150, hue: 0xFFFF) { Y = stayActive.Height + stayActive.Y, Width = 150, Height = 20 });
            searchBox.Text = NameOverHeadManager.Search;
            searchBox.TextChanged += (s, e) => { NameOverHeadManager.Search = searchBox.Text; };

            DrawChoiceButtons();
        }

        public void UpdateCheckboxes()
        {
            foreach (RadioButton button in _overheadButtons)
            {
                button.IsChecked = NameOverHeadManager.LastActiveNameOverheadOption.Replace("\\u0026", "&") == button.Text;
            }
        }

        public void RedrawOverheadOptions()
        {
            foreach (RadioButton button in _overheadButtons)
                Remove(button);

            DrawChoiceButtons();
        }

        private void DrawChoiceButtons()
        {
            int biggestWidth = 100;
            List<NameOverheadOption> options = NameOverHeadManager.GetAllOptions();

            for (int i = 0; i < options.Count; i++)
            {
                biggestWidth = Math.Max(biggestWidth, AddOverheadOptionButton(options[i], i).Width);
            }

            _alpha.Width = biggestWidth;
            _alpha.Height = Math.Max(30, options.Count * 20) + 44;

            Width = _alpha.Width;
            Height = _alpha.Height;
        }

        private RadioButton AddOverheadOptionButton(NameOverheadOption option, int index)
        {
            RadioButton button;

            Add
            (
                button = new RadioButton
                (
                    0, 0x00D0, 0x00D1, option.Name,
                    color: 0xFFFF
                )
                {
                    Y = 20 * index + 44,
                    IsChecked = NameOverHeadManager.LastActiveNameOverheadOption.Replace("\\u0026", "&") == option.Name,
                }
            );

            if (button.IsChecked)
            {
                World.NameOverHeadManager.SetActiveOption(option);
            }

            button.ValueChanged += (sender, e) =>
            {
                if (button.IsChecked)
                {
                    World.NameOverHeadManager.SetActiveOption(option);
                }
            };

            _overheadButtons.Add(button);

            return button;
        }

        public override void Dispose()
        {
            NameOverHeadManager.Search = "";
            base.Dispose();
        }

        protected override void OnDragEnd(int x, int y)
        {
            LastPosition = new Point(ScreenCoordinateX, ScreenCoordinateY);

            SetInScreen();

            base.OnDragEnd(x, y);
        }
    }
}
