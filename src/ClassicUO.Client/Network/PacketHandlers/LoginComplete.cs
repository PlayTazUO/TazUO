using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.Gumps.Login;
using ClassicUO.IO;
using ClassicUO.Utility;

namespace ClassicUO.Network.PacketHandlers;

internal static class LoginComplete
{
    public static void Receive(World world, ref StackDataReader p)
    {
        if (world.Player != null && Client.Game.Scene is LoginScene)
        {
            var scene = new GameScene(world);
            Client.Game.SetScene(scene);
            LoginScene.Instance?.Dispose();
            LoginGump.Instance?.Dispose();

            GameActions.RequestMobileStatus(world, world.Player);
            AsyncNetClient.Socket.Send_OpenChat("");

            AsyncNetClient.Socket.Send_SkillsRequest(world.Player);
            ObjectActionQueue.Instance.Enqueue(ObjectActionQueueItem.DoubleClick(world.Player | 0x8000_0000), ActionPriority.UseItem);

            if (Client.Game.UO.Version >= ClassicUO.Utility.ClientVersion.CV_306E)
                AsyncNetClient.Socket.Send_ClientType();

            if (Client.Game.UO.Version >= ClassicUO.Utility.ClientVersion.CV_305D)
                AsyncNetClient.Socket.Send_ClientViewRange(world.ClientViewRange);

            // Reset the global action cooldown here because, for some reason, immediately
            // sending multiple actions (e.g. reopening paperdoll and reopening containers)
            // results in the server telling the client it must wait to perform actions.
            GlobalActionCooldown.BeginCooldown();
            List<Gump> gumps = ProfileManager.CurrentProfile.ReadGumps(
                world,
                ProfileManager.ProfilePath
            );

            if (gumps != null)
                foreach (Gump gump in gumps)
                    UIManager.Add(gump);
        }
    }
}
