using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;

namespace ClassicUO.Game.UI.Gumps
{
    public class Supporters : Gump
    {
        private const int WIDTH = 512;
        private const int HEIGHT = 512;

        private AlphaBlendControl _background;

        private Texture2D image = PNGLoader.Instance.GetImageTexture(Path.Combine(CUOEnviroment.ExecutablePath, "ExternalImages", "tazuo.png"));

        private Label[] supporterLabels;

        private Line line;

        private double offset = 0.0;

        public Supporters(World world) : base(world, 0, 0)
        {
            Width = WIDTH;
            Height = HEIGHT;
            X = (Client.Game.Window.ClientBounds.Width - Width) >> 1;
            Y = (Client.Game.Window.ClientBounds.Height - Height) >> 1;

            CanCloseWithEsc = true;
            CanCloseWithRightClick = true;
            CanMove = true;
            AcceptMouseInput = true;

            _background = new AlphaBlendControl();
            _background.Width = WIDTH;
            _background.Height = HEIGHT;
            _background.X = 1;
            _background.Y = 1;
            Add(_background);

            var title = new Label(TazLang.Get("supporters_title", "TazUO supporters and honorable mentions<br>And a special thanks to all the ClassicUO devs that made this possible!"), true, 0xffff, WIDTH, 255, FontStyle.BlackBorder, Assets.TEXT_ALIGN_TYPE.TS_CENTER, true);
            title.Y = 1;
            Add(title);

            line = new Line(0, title.Height, WIDTH, 2, Color.Gray.PackedValue);
            Add(line);

            int y = line.Y + line.Height + 1;
            string[] supporters = new[]
            {
                TazLang.Get("supporters_entry_0", "TazmanianTad - Developer"),
                TazLang.Get("supporters_entry_1", "Doskan - Random coffee bringer"),
                TazLang.Get("supporters_entry_2", "Auburok - Don't leave Brit Bank without TazUO"),
                TazLang.Get("supporters_entry_3", "IDiivil - Happily Organized Now"),
                TazLang.Get("supporters_entry_4", "Avernal"),
                TazLang.Get("supporters_entry_5", "d6punk - UO for life!"),
                TazLang.Get("supporters_entry_6", "Eora - Always looking for interesting adventures")
            };
            supporterLabels = new Label[supporters.Length];
            for (int i = 0; i < supporters.Length; i++)
            {
                var l = new Label(supporters[i], true, 0xffff, WIDTH, 255, FontStyle.BlackBorder, Assets.TEXT_ALIGN_TYPE.TS_CENTER, true);
                l.Y = y;
                y += l.Height + 1;
                Add(l);
                supporterLabels[i] = l;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (image != null)
            {
                batcher.Draw(
                    image,
                    new Rectangle(x, y, image.Bounds.Width, image.Bounds.Height),
                    new Vector3(0, 0, 1)
                    );
            }

            offset += 0.9;

            int newY = line.Y + line.Height + 1;
            for (int i = 0; i < supporterLabels.Length; i++)
            {
                if (supporterLabels[i].Y <= line.Y + line.Height)
                    supporterLabels[i].IsVisible = false;
                else
                {
                    supporterLabels[i].Y -= (int)offset;
                    if (supporterLabels[i].Y < Height - supporterLabels[i].Height - 1)
                        supporterLabels[i].IsVisible = true;
                }
            }

            if (supporterLabels[supporterLabels.Length - 1].Y <= line.Y + line.Height)
                for (int ii = 0; ii < supporterLabels.Length; ii++)
                {
                    supporterLabels[ii].Y = Height + ((supporterLabels[ii].Height - 1) * ii);
                    supporterLabels[ii].IsVisible = false;
                }
            if (offset >= 1)
                offset = 0;

            Vector3 hue = ShaderHueTranslator.GetHueVector(0);
            batcher.DrawRectangle
            (
                SolidColorTextureCache.GetTexture(Color.Gray),
                x,
                y,
                Width - 3,
                Height + 1,
                hue
            );
            return base.Draw(batcher, x, y);
        }
    }
}
