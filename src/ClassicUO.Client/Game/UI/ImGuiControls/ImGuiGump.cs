using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.ImGuiControls
{
    /// <summary>
    /// Gump wrapper that allows ImGuiWindow to participate in the unified UIManager rendering pipeline.
    /// This enables proper z-ordering between Gumps and ImGui windows.
    /// </summary>
    public class ImGuiGump : Gump
    {
        private readonly ImGuiWindow _window;
        private bool _needsBringToFront;

        public ImGuiGump(ImGuiWindow window, UILayer layerOrder = UILayer.Default) : base(World.Instance, 0, 0)
        {
            _window = window;
            LayerOrder = layerOrder;
            CanMove = false;
            AcceptMouseInput = false;
            AcceptKeyboardInput = false;
        }

        public ImGuiWindow Window => _window;

        public override bool IsDisposed => base.IsDisposed || _window == null || !_window.IsOpen;

        public override bool IsVisible
        {
            get => _window != null && _window.IsVisible && _window.IsOpen;
            set { } // Ignore - visibility controlled by ImGuiWindow
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (_window != null && _window.IsOpen && _window.IsVisible)
            {
                _window.Draw();

                // Set flag if focus was gained this frame
                if (_window.JustGotFocus)
                {
                    _needsBringToFront = true;
                }

                return true;
            }
            return false;
        }

        public override void Dispose()
        {
            _window?.Dispose();
            base.Dispose();
        }

        public override void Update()
        {
            if (_window != null && _window.IsOpen && _window.IsVisible)
            {
                _window.Update();
            }
        }

        public override void PreDraw()
        {
            if (_window == null || !_window.IsOpen)
            {
                Dispose();
                return;
            }

            if (_needsBringToFront)
            {
                _needsBringToFront = false;
                Managers.UIManager.MakeTopMostGump(this);
            }
        }
    }
}
