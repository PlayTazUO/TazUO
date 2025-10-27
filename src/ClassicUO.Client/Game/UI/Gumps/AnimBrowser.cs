using System;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Utility.Logging;
using SDL3;

namespace ClassicUO.Game.UI.Gumps;

public class AnimBrowser : Gump
{
    private DataBox dataBox = new DataBox(0, 0, 500, 700);
    private AnimationDisplay[] animationDisplays;
    private StbTextBox pageInput;
    public AnimBrowser(World world) : base(world, 0, 0)
    {
        CanMove = true;
        AcceptMouseInput = true;
        Width = 500;
        Height = 700;

        Add(new AlphaBlendControl() { Width = Width, Height = Height });
        Add(dataBox);

        var next = new NiceButton(Width - 100, Height - 20, 100, 20, ButtonAction.Default, ">>");
        next.MouseDown += (s, e) => { Page++; BuildPage(); };
        Add(next);

        var prev = new NiceButton(0, Height - 20, 100, 20, ButtonAction.Default, "<<");
        prev.MouseDown += (s, e) => { Page--; BuildPage(); };
        Add(prev);


        pageInput = new(0xFF, maxWidth: 100, hue: 52, align: Assets.TEXT_ALIGN_TYPE.TS_CENTER) { Width = 100, Height = 20 };
        pageInput.X = (Width - 100) >> 1;
        pageInput.Y = Height - 20;
        pageInput.Text = "0";
        pageInput.TextChanged += (s, e) =>
        {
            if (int.TryParse(pageInput.Text, out int npage))
            {
                if (npage != Page)
                {
                    Page = npage;
                    BuildPage();
                }
            }
        };
        Add(pageInput);

        StbTextBox graphicInput = new(0xFF, maxWidth: 100, hue: 52, align: Assets.TEXT_ALIGN_TYPE.TS_CENTER) { Width = 100, Height = 20, PlaceHolderText = "Enter Graphic" };
        graphicInput.X = (Width - 100) >> 1;
        graphicInput.TextChanged += (s, e) =>
        {
            if (graphicInput.Text.StartsWith("0x"))
            {
                try
                {
                    uint p = uint.Parse(graphicInput.Text.Remove(0, 2), System.Globalization.NumberStyles.HexNumber);
                    Page = (int)p;
                    BuildPage();
                }
                catch (Exception) { }
            }
            else if (uint.TryParse(graphicInput.Text, out uint gfx))
            {
                int maxEntries = (Width / 100) * (Height / 100);
                int pageNumber = (int)(gfx / maxEntries);

                Page = pageNumber;
                BuildPage();
            }
        };
        Add(graphicInput);

        InitialBuild();
        BuildPage();
    }

    private void InitialBuild()
    {
        int maxEntries = (Width / 100) * (Height / 100);
        animationDisplays = new AnimationDisplay[maxEntries];
        for (int i = 0; i < maxEntries; i++)
        {
            animationDisplays[i] = new AnimationDisplay(0, 100, 100)
            { CanMove = true, DrawBorder = true };
            animationDisplays[i].MouseDoubleClick += OnDoubleClick;
            dataBox.Add(animationDisplays[i]);
        }
        dataBox.ReArrangeChildrenGridStyle();
    }

    private void OnDoubleClick(object sender, MouseDoubleClickEventArgs e)
    {
        if (sender is AnimationDisplay rsp)
        {
            SDL.SDL_SetClipboardText(rsp.Graphic.ToString());
            GameActions.Print(World, $"Copied {rsp.Graphic} to clipboard.");
        }
    }

    private void BuildPage()
    {
        if (Page < 0)
            Page = 0;

        pageInput.SetTextInternally = Page.ToString();

        int maxEntries = (Width / 100) * (Height / 100);
        int count = 0;
        uint index = (uint)(Page * maxEntries);
        while (count < maxEntries)
        {
            ref readonly Renderer.SpriteInfo art = ref Client.Game.UO.Arts.GetArt(index);
            //if (art.Texture != null)
            {
                AnimationDisplay c = animationDisplays[count];
                c.UpdateGraphic((ushort)index);
                c.SetTooltip($"Animation: {index}\nDouble click to copy.");
                count++;
            }
            index++;
        }
    }
}