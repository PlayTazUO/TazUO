using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.IO;

namespace ClassicUO.Network.PacketHandlers;

internal static class DeathScreen
{
    public static void Receive(World world, ref StackDataReader p)
    {
        // todo
        byte action = p.ReadUInt8();

        if (action != 1)
        {
            // Capture the final frame before the death screen is drawn over it.
            if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.ScreenshotOnDeath){
                Client.Game.TakeScreenshot("death");
                GameActions.Print(TazLang.Get("death_screen_disable"), Constants.HUE_WARN);
            }
            world.Weather.Reset();

            Client.Game.Audio.PlayMusic(Client.Game.Audio.DeathMusicIndex, true);

            if (ProfileManager.CurrentProfile.EnableDeathScreen)
                world.Player.DeathScreenTimer = Time.Ticks + Constants.DEATH_SCREEN_TIMER;

            GameActions.RequestWarMode(world.Player, false);
            world.WMapManager._corpse = new WMapEntity(world.Player.Serial)
            {
                X = world.Player.X,
                Y = world.Player.Y,
                HP = 0,
                Map = world.Map.Index,
                LastUpdate = Time.Ticks + 1000 * 60 * 5,
                IsGuild = false,
                Name = "Your Corpse"
            };

            EventSink.InvokeOnPlayerDeath(world.Player, world.Player.Serial);
        }
    }
}
