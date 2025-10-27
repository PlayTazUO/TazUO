// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.Managers;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using SDL3;

namespace ClassicUO.Game.Scenes
{
    public abstract class Scene : IDisposable
    {
        public bool IsDestroyed { get; private set; }
        public bool IsLoaded { get; private set; }
        public int RenderedObjectsCount { get; protected set; }
        public Camera Camera { get; } = new Camera(0.3f, 3.0f, 0.05f);



        public virtual void Dispose()
        {
            if (IsDestroyed)
            {
                return;
            }

            Unload();
            IsDestroyed = true;
        }

        public virtual void Update() => Camera.Update(true, Time.Delta, Mouse.Position);

        public virtual bool Draw(UltimaBatcher2D batcher) => true;


        public virtual void Load() => IsLoaded = true;

        public virtual void Unload() => IsLoaded = false;


        internal virtual bool OnMouseUp(MouseButtonType button) => false;
        internal virtual bool OnMouseDown(MouseButtonType button) => false;
        internal virtual bool OnMouseDoubleClick(MouseButtonType button) => false;
        internal virtual bool OnMouseWheel(bool up) => false;
        internal virtual bool OnMouseDragging() => false;

        internal virtual void OnControllerButtonDown(SDL.SDL_GamepadButtonEvent e) { }
        internal virtual void OnControllerButtonUp(SDL.SDL_GamepadButtonEvent e) { }

        internal virtual void OnTextInput(string text)
        {
        }

        internal virtual void OnKeyDown(SDL.SDL_KeyboardEvent e)
        {
        }

        internal virtual void OnKeyUp(SDL.SDL_KeyboardEvent e)
        {
        }
    }
}
