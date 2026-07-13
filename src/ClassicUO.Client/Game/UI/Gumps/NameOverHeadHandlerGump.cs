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

        private const int OPTIONS_TOP_OFFSET = 66;

        private readonly List<RadioButton> _overheadButtons = new List<RadioButton>();
        private Control _alpha;
        private StbTextBox searchBox;
        private StbTextBox negativeSearchBox;
        private int _topContentWidth;

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

            // Track how wide the checkbox row is so the background can be sized to contain it.
            _topContentWidth = Math.Max(_topContentWidth, hideInWarmode.X + hideInWarmode.Width);



            int searchY = stayActive.Height + stayActive.Y;
            _topContentWidth = Math.Max(_topContentWidth, 150);
            Add(new AlphaBlendControl() { Y = searchY, Width = 150, Height = 20, Hue = 0x0481 });
            Add(searchBox = new StbTextBox(0, -1, 134, hue: 0xFFFF) { Y = searchY, Width = 134, Height = 20 });
            searchBox.Text = NameOverHeadManager.Search;
            searchBox.SetTooltip("Only show nameplates matching this text.\nSeparate multiple terms with ';'");
            searchBox.TextChanged += (s, e) => { NameOverHeadManager.Search = searchBox.Text; };

            Label clearSearch;
            Add
            (
                clearSearch = new Label("X", true, 0xFFFF, font: 1)
                {
                    AcceptMouseInput = true,
                    X = 138
                }
            );
            clearSearch.Y = searchY + ((20 - clearSearch.Height) >> 1);
            _topContentWidth = Math.Max(_topContentWidth, clearSearch.X + clearSearch.Width);
            clearSearch.SetTooltip("Clear search");
            clearSearch.MouseUp += (s, e) =>
            {
                searchBox.Text = "";
                NameOverHeadManager.Search = "";
                UIManager.KeyboardFocusControl = searchBox; //Return focus to the input in case clicking the X moved it
            };

            int negativeSearchY = searchY + 22;
            Add(new AlphaBlendControl(0.4f) { Y = negativeSearchY, Width = 150, Height = 20, Hue = 0x0481 });
            Add(negativeSearchBox = new StbTextBox(0, -1, 134, hue: 0xFFFF) { Y = negativeSearchY, Width = 134, Height = 20 });
            negativeSearchBox.Text = NameOverHeadManager.NegativeSearch;
            negativeSearchBox.SetTooltip("Hide nameplates matching this text (opposite of search).\nSeparate multiple terms with ';'");
            negativeSearchBox.TextChanged += (s, e) => { NameOverHeadManager.NegativeSearch = negativeSearchBox.Text; };

            Label clearNegativeSearch;
            Add
            (
                clearNegativeSearch = new Label("X", true, 0xFFFF, font: 1)
                {
                    AcceptMouseInput = true,
                    X = 138
                }
            );
            clearNegativeSearch.Y = negativeSearchY + ((20 - clearNegativeSearch.Height) >> 1);
            _topContentWidth = Math.Max(_topContentWidth, clearNegativeSearch.X + clearNegativeSearch.Width);
            clearNegativeSearch.SetTooltip("Clear hide search");
            clearNegativeSearch.MouseUp += (s, e) =>
            {
                negativeSearchBox.Text = "";
                NameOverHeadManager.NegativeSearch = "";
                UIManager.KeyboardFocusControl = negativeSearchBox; //Return focus to the input in case clicking the X moved it
            };

            DrawChoiceButtons();
        }

        /// <summary>Refreshes the search input boxes to reflect the active nameplate option's saved filters.</summary>
        public void UpdateSearchBoxes()
        {
            if (searchBox != null)
                searchBox.Text = NameOverHeadManager.Search;

            if (negativeSearchBox != null)
                negativeSearchBox.Text = NameOverHeadManager.NegativeSearch;
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
            int biggestWidth = Math.Max(100, _topContentWidth);
            List<NameOverheadOption> options = NameOverHeadManager.GetAllOptions();

            for (int i = 0; i < options.Count; i++)
            {
                biggestWidth = Math.Max(biggestWidth, AddOverheadOptionButton(options[i], i).Width);
            }

            _alpha.Width = biggestWidth;
            _alpha.Height = Math.Max(30, options.Count * 20) + OPTIONS_TOP_OFFSET;

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
                    Y = 20 * index + OPTIONS_TOP_OFFSET,
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

        protected override void OnDragEnd(int x, int y)
        {
            LastPosition = new Point(ScreenCoordinateX, ScreenCoordinateY);

            SetInScreen();

            base.OnDragEnd(x, y);
        }
    }
}
