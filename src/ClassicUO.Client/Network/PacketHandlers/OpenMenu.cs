using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Renderer;

namespace ClassicUO.Network.PacketHandlers;

internal static class OpenMenu
{
    public static void Receive(World world, ref StackDataReader p)
    {
        if (!world.InGame)
            return;

        uint serial = p.ReadUInt32BE();
        ushort id = p.ReadUInt16BE();
        string name = p.ReadASCII(p.ReadUInt8());
        int count = p.ReadUInt8();

        ushort menuid = p.ReadUInt16BE();
        p.Seek(p.Position - 2);

        if (menuid != 0)
        {
            var gump = new MenuGump(world, serial, id, name) { X = 100, Y = 100 };

            int posX = 0;

            for (int i = 0; i < count; i++)
            {
                ushort graphic = p.ReadUInt16BE();
                ushort hue = p.ReadUInt16BE();
                name = p.ReadASCII(p.ReadUInt8());

                ref readonly SpriteInfo artInfo = ref Client.Game.UO.Arts.GetArt(graphic);

                if (artInfo.UV.Width != 0 && artInfo.UV.Height != 0)
                {
                    int posY = artInfo.UV.Height;

                    if (posY >= 47)
                        posY = 0;
                    else
                        posY = (47 - posY) >> 1;

                    gump.AddItem(graphic, hue, name, posX, posY, i + 1);

                    posX += artInfo.UV.Width;
                }
            }

            UIManager.Add(gump);
        }
        else
        {
            var gump = new GrayMenuGump(world, serial, id, name)
            {
                X = (ScaleHelper.LogicalWindowWidth >> 1) - 200,
                Y = (ScaleHelper.LogicalWindowHeight >> 1) - ((121 + count * 21) >> 1)
            };

            int offsetY = 35 + gump.Height;
            int gumpHeight = 70 + offsetY;

            for (int i = 0; i < count; i++)
            {
                p.Skip(4);
                name = p.ReadASCII(p.ReadUInt8());

                int addHeight = gump.AddItem(name, offsetY);

                if (addHeight < 21)
                    addHeight = 21;

                offsetY += addHeight - 1;
                gumpHeight += addHeight;
            }

            offsetY += 5;

            gump.Add(
                new Button(0, 0x1450, 0x1451, 0x1450) { ButtonAction = ButtonAction.Activate, X = 70, Y = offsetY }
            );

            gump.Add(
                new Button(1, 0x13B2, 0x13B3, 0x13B2) { ButtonAction = ButtonAction.Activate, X = 200, Y = offsetY }
            );

            gump.SetHeight(gumpHeight);
            gump.WantUpdateSize = false;
            UIManager.Add(gump);
        }
    }
}
