using ClassicUO.Configuration;
using ClassicUO.Utility.Logging;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Game.UI.ImGuiControls;

namespace ClassicUO.Game.UI
{
    internal static class ImGuiManager
    {
        private static ImGuiRenderer _imGuiRenderer;
        private static bool _isInitialized;
        private static bool _hasWindows;
        private static readonly List<ImGuiWindow> _windows = new();
        private static readonly object _windowsLock = new();
        private static Microsoft.Xna.Framework.Game _game;

        public static bool IsInitialized => _isInitialized;
        public static ImGuiRenderer Renderer => _imGuiRenderer;
        public static ImGuiWindow[] Windows
        {
            get
            {
                lock (_windowsLock)
                {
                    return _windows.ToArray();
                }
            }
        }

        public static void AddWindow(ImGuiWindow window)
        {
            if (window == null) return;
            lock (_windowsLock)
            {
                if (!_windows.Contains(window))
                    _windows.Add(window);
                _hasWindows = _windows.Count > 0;
            }
        }

        public static void RemoveWindow(ImGuiWindow window)
        {
            if (window == null) return;
            lock (_windowsLock)
            {
                _windows.Remove(window);
                _hasWindows = _windows.Count > 0;
            }
        }

        public static void RemoveAllWindows()
        {
            lock (_windowsLock)
            {
                foreach (ImGuiWindow window in _windows)
                {
                    window?.Dispose();
                }
                _windows.Clear();
                _hasWindows = _windows.Count > 0;
            }
        }

        public static void UpdateTheme(float alpha) => ApplyThemeColors(alpha);

        private static void SetTazUOTheme()
        {
            ImGuiIOPtr io = ImGui.GetIO();
            unsafe
            {
                fixed (byte* fontPtr = TrueTypeLoader.Instance.ImGuiFont)
                {
                    ImGui.GetIO().Fonts.AddFontFromMemoryTTF(
                        (IntPtr)fontPtr,
                        TrueTypeLoader.Instance.ImGuiFont.Length,
                        16.0f // font size
                    );
                }
            }

            ImGuiStylePtr style = ImGui.GetStyle();

            // Style settings
            style.WindowRounding = 5.0f;
            style.FrameRounding = 5.0f;
            style.GrabRounding = 5.0f;
            style.TabRounding = 5.0f;
            style.PopupRounding = 5.0f;
            style.ScrollbarRounding = 5.0f;
            style.WindowPadding = new System.Numerics.Vector2(10, 10);
            style.FramePadding = new System.Numerics.Vector2(6, 4);
            style.ItemSpacing = new System.Numerics.Vector2(8, 6);
            style.TabBorderSize = 1.0f;

            float alpha = Client.Settings?.Get(SettingsScope.Global, "imgui_window_alpha", 1.0f) ?? 1.0f;
            ApplyThemeColors(alpha);
        }

        private static void ApplyThemeColors(float alpha)
        {
            // TazUO color scheme
            RangeAccessor<System.Numerics.Vector4> colors = ImGui.GetStyle().Colors;

            // Primary background - apply alpha
            System.Numerics.Vector4 windowBg = ImGuiTheme.Colors.Base100;
            windowBg.W = alpha;
            colors[(int)ImGuiCol.WindowBg] = windowBg;

            colors[(int)ImGuiCol.MenuBarBg] = ImGuiTheme.Colors.Primary;

            System.Numerics.Vector4 popupBg = ImGuiTheme.Colors.Base100;
            popupBg.W = alpha;
            colors[(int)ImGuiCol.PopupBg] = popupBg;

            // Headers - apply alpha to backgrounds
            System.Numerics.Vector4 header = ImGuiTheme.Colors.Base100;
            header.W = alpha;
            colors[(int)ImGuiCol.Header] = header;

            System.Numerics.Vector4 headerHovered = ImGuiTheme.Colors.Base100;
            headerHovered.W = alpha;
            colors[(int)ImGuiCol.HeaderHovered] = headerHovered;

            colors[(int)ImGuiCol.HeaderActive] = ImGuiTheme.Colors.Primary;

            // Buttons
            colors[(int)ImGuiCol.Button] = ImGuiTheme.Colors.Primary;
            colors[(int)ImGuiCol.ButtonHovered] = ImGuiTheme.Colors.Base200;
            colors[(int)ImGuiCol.ButtonActive] = ImGuiTheme.Colors.Primary;

            // Frame BG
            colors[(int)ImGuiCol.FrameBg] = ImGuiTheme.Colors.Base200;
            colors[(int)ImGuiCol.FrameBgHovered] = ImGuiTheme.Colors.Base300;
            colors[(int)ImGuiCol.FrameBgActive] = ImGuiTheme.Colors.Primary;

            // Tabs - apply alpha to backgrounds
            System.Numerics.Vector4 tab = ImGuiTheme.Colors.Base100;
            tab.W = alpha;
            colors[(int)ImGuiCol.Tab] = tab;

            colors[(int)ImGuiCol.TabHovered] = ImGuiTheme.Colors.Primary;
            colors[(int)ImGuiCol.TabSelected] = ImGuiTheme.Colors.Primary;

            // Title - apply alpha to backgrounds
            System.Numerics.Vector4 titleBg = ImGuiTheme.Colors.Base100;
            titleBg.W = alpha;
            colors[(int)ImGuiCol.TitleBg] = titleBg;

            colors[(int)ImGuiCol.TitleBgActive] = ImGuiTheme.Colors.Primary;

            System.Numerics.Vector4 titleBgCollapsed = ImGuiTheme.Colors.Base100;
            titleBgCollapsed.W = alpha;
            colors[(int)ImGuiCol.TitleBgCollapsed] = titleBgCollapsed;

            // Borders
            colors[(int)ImGuiCol.Border] = ImGuiTheme.Colors.Primary;
            colors[(int)ImGuiCol.BorderShadow] = ImGuiTheme.Colors.BorderShadow;

            // Text
            colors[(int)ImGuiCol.Text] = ImGuiTheme.Colors.BaseContent;
            colors[(int)ImGuiCol.TextDisabled] = ImGuiTheme.Colors.Base300;

            // Highlights
            colors[(int)ImGuiCol.CheckMark] = ImGuiTheme.Colors.Primary;
            colors[(int)ImGuiCol.SliderGrab] = ImGuiTheme.Colors.Primary;
            colors[(int)ImGuiCol.SliderGrabActive] = ImGuiTheme.Colors.Base100;
            colors[(int)ImGuiCol.ResizeGrip] = ImGuiTheme.Colors.Primary;
            colors[(int)ImGuiCol.ResizeGripHovered] = ImGuiTheme.Colors.Primary;
            colors[(int)ImGuiCol.ResizeGripActive] = ImGuiTheme.Colors.Primary;

            // Scrollbar
            colors[(int)ImGuiCol.ScrollbarBg] = ImGuiTheme.Colors.ScrollbarBg;
            colors[(int)ImGuiCol.ScrollbarGrab] = ImGuiTheme.Colors.ScrollbarGrab;
            colors[(int)ImGuiCol.ScrollbarGrabHovered] = ImGuiTheme.Colors.ScrollbarGrabHovered;
            colors[(int)ImGuiCol.ScrollbarGrabActive] = ImGuiTheme.Colors.ScrollbarGrabActive;
        }

        public static void Initialize(Microsoft.Xna.Framework.Game game)
        {
            //return; //Disable for now, basic implementation is done

            _game = game;
            try
            {
                _imGuiRenderer = new ImGuiRenderer(game);
                SetTazUOTheme();
                _imGuiRenderer.RebuildFontAtlas();

                _isInitialized = true;
                Log.Info("ImGui initialized successfully");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to initialize ImGui: {ex.Message}");
                _isInitialized = false;
            }
        }

        public static void Update(GameTime gameTime)
        {
            if (!_isInitialized || !_hasWindows)
                return;

            try
            {
                if (!_imGuiRenderer.BeforeLayout(gameTime))
                    return;

                DrawImGui();

                _imGuiRenderer.AfterLayout();
            }
            catch (Exception ex)
            {
                Log.Error($"ImGui update error: {ex.Message}");
            }
        }

        private static void DrawImGui()
        {
            // Draw managed windows
            lock (_windowsLock)
            {
                for (int i = _windows.Count - 1; i >= 0; i--)
                {
                    ImGuiWindow window = _windows[i];
                    if (window != null)
                    {
                        if (window.IsOpen)
                        {
                            window.Update();
                            window.Draw();
                        }
                        else
                        {
                            window.Dispose();
                            _windows.RemoveAt(i);
                        }
                    }
                    else
                    {
                        _windows.RemoveAt(i);
                    }
                }
            }
        }

        public static void Dispose()
        {
            RemoveAllWindows();
            _imGuiRenderer?.Dispose();
            _imGuiRenderer = null;
            _isInitialized = false;
            Log.Info("ImGui disposed");
        }
    }
}
