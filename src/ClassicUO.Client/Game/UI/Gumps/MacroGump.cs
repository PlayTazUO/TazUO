
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.Game.UI.Gumps
{
    public sealed class MacroGump : Gump
    {
        public MacroGump(World world, string name) : base(world, 0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;
            Renderer.Camera camera = Client.Game.Scene.Camera;

            var macroGumpBackground = new AlphaBlendControl
            {
                Width = 360,
                Height = 200,
                X = camera.Bounds.Width / 2 - 125,
                Y = 150,
                Alpha = 0.8f
            };

            var text = new Label($"Edit macro: {name}", true, 15)
            {
                X = camera.Bounds.Width / 2 - 105,
                Y = macroGumpBackground.Y + 2
            };

            Add(macroGumpBackground);
            Add(text);

            Add
            (
                new MacroControl(this, name, true)
                {
                    X = macroGumpBackground.X + 20,
                    Y = macroGumpBackground.Y + 20,
                }
            );

            SetInScreen();
        }
    }
}
