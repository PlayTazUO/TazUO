using System;
using ClassicUO.Configuration;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace ClassicUO.Game
{
    public enum WeatherType
    {
        WT_RAIN = 0,
        WT_STORM_APPROACH,
        WT_SNOW,
        WT_STORM_BREWING,

        WT_INVALID_0 = 0xFE,
        WT_INVALID_1 = 0xFF
    }

    public abstract class WeatherBase
    {
        protected uint _timer;
        protected uint _windTimer;
        protected uint _lastTick;
        protected readonly World _world;

        protected WeatherBase(World world)
        {
            _world = world;
        }

        public WeatherType? CurrentWeather { get; protected set; }
        public WeatherType Type { get; protected set; }
        public byte Count { get; protected set; }
        public byte CurrentCount { get; protected set; }
        public byte Temperature { get; protected set; }
        public sbyte Wind { get; protected set; }

        protected static bool IsWeatherDisabled =>
            ProfileManager.CurrentProfile?.DisableWeather == true;

        protected static float SinOscillate(float freq, int range, uint current_tick)
        {
            float anglef = (int)(current_tick / 2.7777f * freq) % 360;

            return Math.Sign(MathHelper.ToRadians(anglef)) * range;
        }

        public abstract void Reset();
        public abstract void Generate(WeatherType type, byte count, byte temp);
        public abstract void UpdateAudio();
        public abstract void Draw(UltimaBatcher2D batcher, int x, int y, float layerDepth);

        protected void PlayWind()
        {
            PlaySound(RandomHelper.RandomList(0x014, 0x015, 0x016));
        }

        protected void PlayThunder()
        {
            PlaySound(RandomHelper.RandomList(0x028, 0x206));
        }

        protected void PlaySound(int sound)
        {
            if (IsWeatherDisabled)
            {
                return;
            }

            if (!_world.InGame || _world.Player == null)
            {
                return;
            }

            int randX = RandomHelper.GetValue(10, 18);
            if (RandomHelper.RandomBool())
            {
                randX *= -1;
            }

            int randY = RandomHelper.GetValue(10, 18);
            if (RandomHelper.RandomBool())
            {
                randY *= -1;
            }

            Client.Game.Audio.PlaySoundWithDistance(_world, sound, _world.Player.X + randX, _world.Player.Y + randY);
        }
    }
}
