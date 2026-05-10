using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.Controls;

public static class MyraExtensions
{
    // These are actually implemented by Myra but are private for some reason
    extension(ScrollViewer scrollViewer)
    {
        /// <summary>
        ///     Computes a <see cref="ScrollViewer" />'s horizontal scroll bar's height based on its current style/texture"/>
        /// </summary>
        /// <returns>An integer representing the scroll bar's height, in pixels</returns>
        public int HorizontalScrollbarHeight()
        {
            int result = 0;
            if (scrollViewer.HorizontalScrollBackground != null)
                result = scrollViewer.HorizontalScrollBackground.Size.Y;

            if (scrollViewer.HorizontalScrollKnob != null && scrollViewer.HorizontalScrollKnob.Size.Y > result)
                result = scrollViewer.HorizontalScrollKnob.Size.Y;

            return result;
        }

        /// <summary>
        ///     Computes a <see cref="ScrollViewer" />'s vertical scroll bar's width based on its current style/texture
        /// </summary>
        /// <returns>An integer representing the scroll bar's width, in pixels</returns>
        public int VerticalScrollbarWidth()
        {
            int result = 0;
            if (scrollViewer.VerticalScrollBackground != null)
                result = scrollViewer.VerticalScrollBackground.Size.X;

            if (scrollViewer.VerticalScrollKnob != null && scrollViewer.VerticalScrollKnob.Size.X > result)
                result = scrollViewer.VerticalScrollKnob.Size.X;

            return result;
        }
    }
}
