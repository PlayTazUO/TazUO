using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Xml;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.ImGuiControls
{
    public abstract class ImGuiWindow : IDisposable
    {
        private bool _isOpen = true;
        private bool _isVisible = true;
        private ImGuiWindowFlags _windowFlags = ImGuiWindowFlags.None;

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

            try
            {
                if (ImGui.Begin(Title, ref _isOpen, _windowFlags))
                {
                    DrawContent();

                    rightclickClose = ImGui.IsMouseClicked(ImGuiMouseButton.Right) && ImGui.IsWindowHovered() && ImGui.IsWindowFocused();
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

        public abstract void DrawContent();

        protected virtual void OnWindowClosed()
        {
        }

        public virtual void Update()
        {
        }

        public virtual void Save(XmlTextWriter xml) => xml.WriteAttributeString("type", GetType().FullName);

        public virtual void Load(XmlElement xml) { }

        public virtual void Dispose()
        {
            OnWindowClosed();

            foreach (KeyValuePair<ushort, ArtPointerStruct> item in _texturePointerCache)
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
            SpriteInfo artInfo = Client.Game.UO.Arts.GetArt(graphic);

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
                nint pnt = ImGuiManager.Renderer.BindTexture(artInfo.Texture);

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
                ImGuiManager.RemoveWindow(Instance);

            Instance = (T)Activator.CreateInstance(typeof(T), true);
            ImGuiManager.AddWindow(Instance);
        }

        public override void Dispose()
        {
            if (Instance == this)
                Instance = null;
            base.Dispose();
        }
    }
}
