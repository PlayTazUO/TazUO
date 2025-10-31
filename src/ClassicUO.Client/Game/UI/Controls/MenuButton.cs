using System;

namespace ClassicUO.Game.UI.Controls
{
    public class MenuButton : Control
    {
        public MenuButton(int width, uint hue, float alpha, string tooltip = "", uint linehue = UInt32.MaxValue)
        {
            Width = width;
            Height = 16;
            AcceptMouseInput = true;
            var _ = new Area(true, (int)hue) { Width = Width, Height = Height, AcceptMouseInput = false };
            _.Add(new AlphaBlendControl(0.25f) { Width = Width, Height = Height });

            if(linehue != UInt32.MaxValue)
                hue = linehue;
            
            Add(_);
            Add(new Line(0, 2, Width, 2, hue) { Alpha = alpha, AcceptMouseInput = false });
            Add(new Line(0, 7, Width, 2, hue) { Alpha = alpha, AcceptMouseInput = false });
            Add(new Line(0, 12, Width, 2, hue) { Alpha = alpha, AcceptMouseInput = false });
            SetTooltip(tooltip);
            //_.SetTooltip(tooltip);
        }

        public override bool Contains(int x, int y) => true;
    }
}
