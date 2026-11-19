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

        /// <summary>
        /// Last weather that actually started. Survives <see cref="Reset"/>, which the client also runs on a player
        /// update or a denied walk, so the server resending that weather doesn't repeat its message.
        /// </summary>
        public WeatherType? LastWeather { get; private set; }

        // The same weather returning this soon after a stop is the same weather going on: servers with weather by map
        // sector (e.g. Sphere) send stop and start again as the player crosses a short clear stretch.
        private const uint SAME_WEATHER_QUIET_MS = 60_000;

        private bool _isStopped;
        private uint _stoppedAt;

        /// <summary>Whether starting <paramref name="type"/> should print its start message.</summary>
        /// <param name="type">Weather being started.</param>
        /// <returns>False when it continues the last weather: no stop since, or back shortly after one.</returns>
        protected bool ShouldPrintStartMessage(WeatherType type)
        {
            if (LastWeather != type)
            {
                return true;
            }

            return _isStopped && Time.Ticks - _stoppedAt >= SAME_WEATHER_QUIET_MS;
        }

        /// <summary>Records a weather start, for <see cref="ShouldPrintStartMessage"/>.</summary>
        /// <param name="type">Weather that started.</param>
        protected void OnWeatherStarted(WeatherType type)
        {
            LastWeather = type;
            _isStopped = false;
        }

        /// <summary>
        /// Records a server stop. Only the first stop after a weather counts: the server resends "stop" on every
        /// resync while the sky is already clear, and that must not restart the quiet window.
        /// </summary>
        protected void OnWeatherStopped()
        {
            if (_isStopped || !LastWeather.HasValue)
            {
                return;
            }

            _isStopped = true;
            _stoppedAt = Time.Ticks;
        }
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
