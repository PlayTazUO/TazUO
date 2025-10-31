using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using Microsoft.Xna.Framework;

namespace ClassicUO.LegionScripting
{
    internal class ScriptingInfoGump : NineSliceGump
    {
        private ModernScrollArea scrollArea;
        private VBoxContainer container;
        private TextBox titleTextBox;
        private static Dictionary<string, object> infoEntries = new();

        private const int MIN_WIDTH = 250;
        private const int MIN_HEIGHT = 200;
        private static int lastX = 200, lastY = 200;
        private static int lastWidth = 300, lastHeight = 400;

        public static ScriptingInfoGump Instance { get; private set; }

        private ScriptingInfoGump() : base(World.Instance, lastX, lastY, lastWidth, lastHeight, ModernUIConstants.ModernUIPanel, ModernUIConstants.ModernUIPanel_BoderSize, true, MIN_WIDTH, MIN_HEIGHT)
        {
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;
            CanMove = true;

            InitializeComponents();
            UpdateUI();
        }

        public static void Show()
        {
            Instance?.Dispose();
            UIManager.Add(Instance = new ScriptingInfoGump());
        }

        private void InitializeComponents()
        {
            // Title
            titleTextBox = TextBox.GetOne("Scripting Info", TrueTypeLoader.EMBEDDED_FONT, 18, Color.DarkOrange, TextBox.RTLOptions.Default(Width - 2 * BorderSize));
            titleTextBox.X = BorderSize;
            titleTextBox.Y = BorderSize;
            titleTextBox.AcceptMouseInput = false;
            Add(titleTextBox);

            // Scroll area
            int scrollAreaY = titleTextBox.Y + titleTextBox.Height + 10;
            int scrollAreaHeight = Height - scrollAreaY - BorderSize;
            scrollArea = new ModernScrollArea(BorderSize, scrollAreaY, Width - 2 * BorderSize, scrollAreaHeight);
            Add(scrollArea);

            // Container for info entries
            container = new VBoxContainer(scrollArea.Width - 20, 5, 5);
            scrollArea.Add(container);
        }

        public static void AddOrUpdateInfo(string key, object value)
        {
            infoEntries[key] = value;
            Instance?.UpdateUI();
        }

        public static void RemoveInfo(string key)
        {
            if (infoEntries.ContainsKey(key))
            {
                infoEntries.Remove(key);
                Instance?.UpdateUI();
            }
        }

        public void UpdateUI()
        {
            container?.Clear();
            foreach (KeyValuePair<string, object> entry in infoEntries)
            {
                container?.Add(new InfoEntry(entry.Key, entry.Value?.ToString() ?? "", container.Width - 10));
            }
        }

        protected override void OnMove(int x, int y)
        {
            lastX = X;
            lastY = Y;
            base.OnMove(x, y);
        }

        public override void Dispose()
        {
            Instance = null;
            base.Dispose();
        }

        protected override void OnResize(int oldWidth, int oldHeight, int newWidth, int newHeight)
        {
            base.OnResize(oldWidth, oldHeight, newWidth, newHeight);
            lastWidth = Width;
            lastHeight = Height;

            // Update title width
            if (titleTextBox != null)
            {
                titleTextBox.Width = Width - 2 * BorderSize;
            }

            // Update scroll area
            if (scrollArea != null)
            {
                int scrollAreaY = titleTextBox.Y + titleTextBox.Height + 10;
                int scrollAreaHeight = Height - scrollAreaY - BorderSize;

                scrollArea.Y = scrollAreaY;
                scrollArea.Width = Width - 2 * BorderSize;
                scrollArea.Height = scrollAreaHeight;
            }

            // Update container width
            if (container != null)
            {
                container.Width = scrollArea.Width - 20;

                // Update all entries width
                foreach (InfoEntry entry in container.Children.OfType<InfoEntry>())
                {
                    entry.UpdateWidth(container.Width - 10);
                }
            }
        }

        private class InfoEntry : Control
        {
            private HBoxContainer hContainer;
            private TextBox keyTextBox;
            private TextBox valueTextBox;
            private string currentValue;

            public InfoEntry(string key, string value, int width)
            {
                AcceptMouseInput = true;
                CanMove = true;
                Width = width;
                Height = 20;
                currentValue = value;

                hContainer = new HBoxContainer(Height, 0, 0);
                hContainer.Width = width;
                Add(hContainer);

                // Key text (non-clickable)
                keyTextBox = TextBox.GetOne($"{key}:", TrueTypeLoader.EMBEDDED_FONT, 14, Color.White, TextBox.RTLOptions.Default(width / 2));
                keyTextBox.AcceptMouseInput = false;
                hContainer.Add(keyTextBox);

                // Value text (clickable for copy)
                valueTextBox = TextBox.GetOne(value, TrueTypeLoader.EMBEDDED_FONT, 14, Color.LightBlue, TextBox.RTLOptions.Default(width / 2).MouseInput());
                valueTextBox.CanMove = true;
                valueTextBox.MouseUp += CopyToClipboard;
                hContainer.Add(valueTextBox);
            }

            public void UpdateWidth(int newWidth)
            {
                Width = newWidth;
                hContainer.Width = newWidth;
                keyTextBox.Width = newWidth / 2;
                valueTextBox.Width = newWidth / 2;
            }

            private void CopyToClipboard(object s, MouseEventArgs e)
            {
                try
                {
                    if (!string.IsNullOrEmpty(currentValue))
                    {
                        SetClipboardText(currentValue);
                        GameActions.Print("Copied to clipboard!");
                    }
                }
                catch (Exception)
                {
                    // Ignore clipboard errors
                }
            }

            private static void SetClipboardText(string text) => SDL3.SDL.SDL_SetClipboardText(text);
        }
    }
}
