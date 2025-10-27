using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    public class CommandsGump : Gump
    {
        public CommandsGump(World world) : base(world, 0, 0)
        {
            X = 300;
            Y = 200;
            Width = 400;
            Height = 500;
            CanCloseWithRightClick = true;
            CanMove = true;

            var bc = new BorderControl(0, 0, Width, Height, 36);
            bc.T_Left = 39925;
            bc.H_Border = 39926;
            bc.T_Right = 39927;
            bc.V_Border = 39928;
            bc.V_Right_Border = 39930;
            bc.B_Left = 39931;
            bc.B_Right = 39933;
            bc.H_Bottom_Border = 39932;

            Add(new GumpPicTiled(39929) { X = bc.BorderSize, Y = bc.BorderSize, Width = Width - (bc.BorderSize * 2), Height = Height - (bc.BorderSize * 2) });

            Add(bc);

            var options = TextBox.RTLOptions.DefaultCentered();
            options.Width = Width;

            TextBox title;
            Add(title = TextBox.GetOne(Language.Instance.CommandGump, TrueTypeLoader.EMBEDDED_FONT, 28, Color.Gold, options));
            title.Y = 5;

            var scroll = new ScrollArea(10, 10 + title.Height, Width - 20, Height - title.Height - 40, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways };

            Add(new AlphaBlendControl(0.45f) { Width = scroll.Width, Height = scroll.Height, X = scroll.X, Y = scroll.Y });

            GenerateEntries(scroll);

            Add(scroll);
        }

        private void GenerateEntries(ScrollArea scroll)
        {
            int y = 0;
            foreach (System.Collections.Generic.KeyValuePair<string, System.Action<string[]>> command in World.CommandManager.Commands)
            {
                var t = TextBox.GetOne(command.Key, TrueTypeLoader.EMBEDDED_FONT, 18, Color.White, TextBox.RTLOptions.Default(scroll.Width));
                t.Y = y;
                scroll.Add(t);
                y += t.Height + 5;
            }
        }
    }
}
