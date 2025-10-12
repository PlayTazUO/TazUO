using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI
{
    /// <summary>
    /// Interface for UI elements that can be drawn in the UIManager's unified rendering pipeline.
    /// This allows both traditional Gumps and ImGui windows to be drawn with proper z-ordering.
    /// </summary>
    public interface IGui
    {
        /// <summary>
        /// Gets whether this UI element is currently disposed.
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Gets whether this UI element is currently visible.
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// Gets the layer order for this UI element (Over, Default, Under).
        /// </summary>
        UILayer LayerOrder { get; }

        /// <summary>
        /// Draws this UI element. For Gumps, this uses the UltimaBatcher2D.
        /// For ImGui windows, this renders the ImGui content.
        /// </summary>
        /// <param name="batcher">The batcher to use for drawing (may be null for ImGui windows)</param>
        void DrawGui(UltimaBatcher2D batcher);
    }
}
