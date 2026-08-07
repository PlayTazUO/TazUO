using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Renderer;

namespace ClassicUO.Network.PacketHandlers;

internal static class DyeData
{
    public static void Receive(World world, ref StackDataReader p)
    {
        uint serial = p.ReadUInt32BE();
        p.Skip(2);
        ushort graphic = p.ReadUInt16BE();

        ref readonly SpriteInfo gumpInfo = ref Client.Game.UO.Gumps.GetGump(0x0906);

        int x = (Client.Game.Window.ClientBounds.Width >> 1) - (gumpInfo.UV.Width >> 1);
        int y = (Client.Game.Window.ClientBounds.Height >> 1) - (gumpInfo.UV.Height >> 1);

        if (ProfileManager.GlobalSettings.UseModernColorPicker)
        {
            UIManager.GetGump<ModernColorPicker>(serial)?.Dispose();

            UIManager.Add(new ModernColorPicker(
                world,
                hue =>
                {
                    AsyncNetClient.Socket.Send_DyeDataResponse(serial, graphic, hue);
                    UIManager.GetGump<ModernColorPicker>(serial)?.Dispose();
                },
                serial,
                false,
                GetDefaultDyeHues()
            ));
        } 
        else 
        {
            ColorPickerGump gump = UIManager.GetGump<ColorPickerGump>(serial);

            if (gump == null || gump.IsDisposed || gump.Graphic != graphic)
            {
                gump?.Dispose();

                gump = new ColorPickerGump(world, serial, graphic, x, y, null);

                UIManager.Add(gump);
            }
        }
    }

    // Mirrors the default palette of ColorPickerBox (rows=10, columns=20) at graduation 1
    private static ushort[] GetDefaultDyeHues()
    {
        ushort[] hues = new ushort[200];
        for (int i = 0; i < hues.Length; i++)
            hues[i] = (ushort)(3 + i * 5);
        return hues;
    }
}
