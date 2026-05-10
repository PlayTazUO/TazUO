using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.MyraWindows;
using ClassicUO.IO;

namespace ClassicUO.Network.PacketHandlers;

internal static class ASCIIPrompt
{
    public static void Receive(World world, ref StackDataReader p)
    {
        if (!world.InGame)
            return;

        world.MessageManager.PromptData = new PromptData { Prompt = ConsolePrompt.ASCII, Data = p.ReadUInt64BE() };

        if (ProfileManager.CurrentProfile?.UsePromptPopup == true)
            new PromptPopupWindow(world);
    }
}
