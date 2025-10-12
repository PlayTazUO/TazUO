using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Xml;
using ClassicUO.Game.Managers;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public abstract class ImGuiWindow : IDisposable
    {
        private bool _isOpen = true;
        private bool _isVisible = true;
        private ImGuiWindowFlags _windowFlags = ImGuiWindowFlags.None;
        private bool _wasFocused = false;

        protected ImGuiWindow(string title)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
        }

        public string Title { get; protected set; }

        public bool IsOpen
        {
            get => _isOpen;
            set => _isOpen = value;
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => _isVisible = value;
        }

        public bool IsFocused { get; private set; }

        public bool JustGotFocus { get; private set; }

        protected ImGuiWindowFlags WindowFlags
        {
            get => _windowFlags;
            set => _windowFlags = value;
        }

        public void Draw()
        {
            if (!_isVisible || !_isOpen)
                return;

            bool rightclickClose = false;
            JustGotFocus = false;

            try
            {
                if (ImGui.Begin(Title, ref _isOpen, _windowFlags))
                {
                    // Check if window just gained focus
                    // Use IsWindowFocused with any flags to detect any kind of focus (keyboard or mouse interaction)
                    bool isFocusedNow = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);

                    // Also consider the window focused if it's being hovered and clicked
                    if (!isFocusedNow && ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        isFocusedNow = true;
                    }

                    IsFocused = isFocusedNow;
                    if (IsFocused && !_wasFocused)
                    {
                        JustGotFocus = true;
                        OnFocusGained();
                    }
                    _wasFocused = IsFocused;

                    DrawContent();

                    rightclickClose = ImGui.IsMouseClicked(ImGuiMouseButton.Right) && ImGui.IsWindowHovered() && IsFocused;
                }
                else
                {
                    IsFocused = false;
                    _wasFocused = false;
                }
            }
            catch (Exception ex)
            {
                ImGui.Text($"Error in window '{Title}': {ex.Message}");
            }
            finally
            {
                ImGui.End();
            }

            if(rightclickClose)
                Dispose();
        }

        protected virtual void OnFocusGained()
        {
        }

        public abstract void DrawContent();

        protected virtual void OnWindowClosed()
        {
        }

        public virtual void Update()
        {
        }

        public virtual void Save(XmlTextWriter xml)
        {
            xml.WriteAttributeString("type", GetType().FullName);
        }

        public virtual void Load(XmlElement xml) { }

        public virtual void Dispose()
        {
            OnWindowClosed();

            foreach (var item in _texturePointerCache)
                if(item.Value.Pointer != IntPtr.Zero)
                    ImGuiManager.Renderer.UnbindTexture(item.Value.Pointer);

            _texturePointerCache.Clear();

            _isOpen = false;
        }

        protected void SetTooltip(string tooltip)
        {
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);
        }

        private Dictionary<ushort, ArtPointerStruct> _texturePointerCache = new();

        protected bool DrawArt(ushort graphic, Vector2 size, bool useSmallerIfGfxSmaller = true)
        {
            var artInfo = Client.Game.UO.Arts.GetArt(graphic);

            if(useSmallerIfGfxSmaller && artInfo.UV.Width < size.X && artInfo.UV.Height < size.Y)
                size = new Vector2(artInfo.UV.Width, artInfo.UV.Height);

            if (_texturePointerCache.TryGetValue(graphic, out ArtPointerStruct art))
            {
                ImGui.Image(art.Pointer, size, art.UV0, art.UV1);
                return true;
            }

            if(artInfo.Texture != null)
            {
                var uv0 = new Vector2(artInfo.UV.X / (float)artInfo.Texture.Width, artInfo.UV.Y / (float)artInfo.Texture.Height);
                var uv1 = new Vector2((artInfo.UV.X + artInfo.UV.Width) / (float)artInfo.Texture.Width, (artInfo.UV.Y + artInfo.UV.Height) / (float)artInfo.Texture.Height);
                var pnt = ImGuiManager.Renderer.BindTexture(artInfo.Texture);

                _texturePointerCache.Add(graphic, new ArtPointerStruct(pnt, artInfo, uv0, uv1, size));

                ImGui.Image(pnt, size, uv0, uv1);
                return true;
            }

            return false;
        }
    }

    public struct ArtPointerStruct(nint pointer, SpriteInfo spriteInfo, Vector2 uv0, Vector2 uv1, Vector2 size)
    {
        public Vector2 Size = size;
        public IntPtr Pointer = pointer;
        public Vector2 UV0 = uv0;
        public Vector2 UV1 = uv1;
        SpriteInfo SpriteInfo = spriteInfo;
    }

    public abstract class SingletonImGuiWindow<T> : ImGuiWindow where T : SingletonImGuiWindow<T>
    {
        public static T Instance { get; protected set; }

        protected SingletonImGuiWindow(string title = "") : base(title)
        {
        }

        public static SingletonImGuiWindow<T> GetInstance()
        {
            if(Instance != null) return Instance;

            return Instance = (T)Activator.CreateInstance(typeof(T), true);
        }

        public static void Show()
        {
            if (Instance != null)
            {
                ImGuiManager.RemoveWindow(Instance);
                Instance.Dispose();
            }

            Instance = (T)Activator.CreateInstance(typeof(T), true);

            UIManager.Add(new ImGuiGump(Instance));

            //ImGuiManager.AddWindow(Instance);
        }

        public override void Dispose()
        {
            if (Instance == this)
                Instance = null;
            base.Dispose();
        }
    }
}
