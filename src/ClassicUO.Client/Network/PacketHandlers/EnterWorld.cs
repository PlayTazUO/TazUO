using System;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.IO;
using ClassicUO.Utility;

namespace ClassicUO.Network.PacketHandlers;

internal static class EnterWorld
{
    public static void Receive(World world, ref StackDataReader p)
    {
        uint serial = p.ReadUInt32BE();

        world.CreatePlayer(serial);

        p.Skip(4);
        world.Player.Graphic = p.ReadUInt16BE();
        world.Player.CheckGraphicChange();
        ushort x = p.ReadUInt16BE();
        ushort y = p.ReadUInt16BE();
        sbyte z = (sbyte)p.ReadUInt16BE();

        if (world.Map == null)
            world.MapIndex = 0;

        world.Player.SetInWorldTile(x, y, z);
        world.Player.Direction = (Direction)(p.ReadUInt8() & 0x7);
        world.RangeSize.X = x;
        world.RangeSize.Y = y;

        if (
            ProfileManager.CurrentProfile != null
            && ProfileManager.CurrentProfile.UseCustomLightLevel
        )
            world.Light.Overall =
                ProfileManager.CurrentProfile.LightLevelType == 1
                    ? Math.Min(world.Light.Overall, ProfileManager.CurrentProfile.LightLevel)
                    : ProfileManager.CurrentProfile.LightLevel;

        Client.Game.Audio.UpdateCurrentMusicVolume();

        if (Client.Game.UO.Version >= ClassicUO.Utility.ClientVersion.CV_200)
        {
            if (ProfileManager.CurrentProfile != null)
                AsyncNetClient.Socket.Send_GameWindowSize(
                    (uint)Client.Game.Scene.Camera.Bounds.Width,
                    (uint)Client.Game.Scene.Camera.Bounds.Height
                );

            AsyncNetClient.Socket.Send_Language(Settings.GlobalSettings.Language);
        }

        AsyncNetClient.Socket.Send_ClientVersion(Settings.GlobalSettings.ClientVersion);

        GameActions.SingleClick(world, world.Player);
        AsyncNetClient.Socket.Send_SkillsRequest(world.Player.Serial);

        if (world.Player.IsDead)
            world.ChangeSeason(ClassicUO.Game.Managers.Season.Desolation, 42);

        if (
            Client.Game.UO.Version >= ClassicUO.Utility.ClientVersion.CV_70796
            && ProfileManager.CurrentProfile != null
        )
            AsyncNetClient.Socket.Send_ShowPublicHouseContent(
                ProfileManager.CurrentProfile.ShowHouseContent
            );

        AsyncNetClient.Socket.Send_ToPlugins_AllSkills();
        AsyncNetClient.Socket.Send_ToPlugins_AllSpells();

        if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.WebMapAutoStart &&
            !MapWebServerManager.Instance.IsRunning)
            _ = MapWebServerManager.Instance.Start();
    }
}
