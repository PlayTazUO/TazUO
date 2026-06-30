using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace ClassicUO.Game
{
    public sealed class Weather : WeatherBase
    {
        private const int MAX_WEATHER_EFFECT = 70;
        private const float SIMULATION_TIME = 37.0f;

        private readonly WeatherEffect[] _effects = new WeatherEffect[MAX_WEATHER_EFFECT];
        private readonly Texture2D _rainImage = PNGLoader.Instance.GetImageTexture(
            System.IO.Path.Combine(CUOEnviroment.ExecutablePath, "ExternalImages", "rain.png"));

        public Weather(World world) : base(world)
        {
        }

        public override void Reset()
        {
            Type = 0;
            Count = CurrentCount = Temperature = 0;
            Wind = 0;
            _windTimer = _timer = 0;
            CurrentWeather = null;
        }

        public override void Generate(WeatherType type, byte count, byte temp)
        {
            if (CurrentWeather.HasValue && CurrentWeather == type)
            {
                return;
            }

            Reset();

            Type = type;
            Count = (byte)Math.Min(MAX_WEATHER_EFFECT, (int)count);
            Temperature = temp;
            _timer = Time.Ticks + Constants.WEATHER_TIMER;

            _lastTick = Time.Ticks;

            if (Type == WeatherType.WT_INVALID_0 || Type == WeatherType.WT_INVALID_1)
            {
                _timer = 0;
                CurrentWeather = null;

                return;
            }

            bool showMessage = Count > 0;

            switch (type)
            {
                case WeatherType.WT_RAIN:
                    if (showMessage)
                    {
                        GameActions.Print
                        (
                            _world,
                            ResGeneral.ItBeginsToRain,
                            1154,
                            MessageType.System,
                            3,
                            false
                        );

                        CurrentWeather = type;
                    }

                    break;

                case WeatherType.WT_STORM_APPROACH:
                    if (showMessage)
                    {
                        GameActions.Print
                        (
                            _world,
                            ResGeneral.AFierceStormApproaches,
                            1154,
                            MessageType.System,
                            3,
                            false
                        );

                        CurrentWeather = type;

                        PlayThunder();
                    }

                    break;

                case WeatherType.WT_SNOW:
                    if (showMessage)
                    {
                        GameActions.Print
                        (
                            _world,
                            ResGeneral.ItBeginsToSnow,
                            1154,
                            MessageType.System,
                            3,
                            false
                        );

                        CurrentWeather = type;

                        PlayWind();
                    }

                    break;

                case WeatherType.WT_STORM_BREWING:
                    if (showMessage)
                    {
                        GameActions.Print
                        (
                            _world,
                            ResGeneral.AStormIsBrewing,
                            1154,
                            MessageType.System,
                            3,
                            false
                        );

                        CurrentWeather = type;

                        PlayThunder();
                    }

                    break;
            }

            _windTimer = 0;

            while (CurrentCount < Count)
            {
                ref WeatherEffect effect = ref _effects[CurrentCount++];
                effect.X = RandomHelper.GetValue(0, Client.Game.Scene.Camera.Bounds.Width);
                effect.Y = RandomHelper.GetValue(0, Client.Game.Scene.Camera.Bounds.Height);
            }
        }

        public override void UpdateAudio()
        {
            if (IsWeatherDisabled)
            {
                if (CurrentWeather.HasValue || CurrentCount > 0)
                {
                    Reset();
                }
            }
        }

        public override void Draw(UltimaBatcher2D batcher, int x, int y, float layerDepth)
        {
            if (IsWeatherDisabled)
            {
                if (CurrentWeather.HasValue || CurrentCount > 0)
                {
                    Reset();
                }

                return;
            }

            bool removeEffects = false;

            if (_timer < Time.Ticks)
            {
                if (CurrentCount == 0)
                {
                    return;
                }

                removeEffects = true;
            }
            else if (Type == WeatherType.WT_INVALID_0 || Type == WeatherType.WT_INVALID_1)
            {
                return;
            }

            uint passed = Time.Ticks - _lastTick;

            if (passed > 7000)
            {
                _lastTick = Time.Ticks;
                passed = 25;
            }

            bool windChanged = false;

            if (_windTimer < Time.Ticks)
            {
                if (_windTimer == 0)
                {
                    windChanged = true;
                }

                _windTimer = Time.Ticks + (uint)(RandomHelper.GetValue(13, 19) * 1000);

                sbyte lastWind = Wind;

                Wind = (sbyte)RandomHelper.GetValue(0, 4);

                if (RandomHelper.GetValue(0, 2) != 0)
                {
                    Wind *= -1;
                }

                if (Wind < 0 && lastWind > 0)
                {
                    Wind = 0;
                }
                else if (Wind > 0 && lastWind < 0)
                {
                    Wind = 0;
                }

                if (lastWind != Wind)
                {
                    windChanged = true;
                }
            }

            Point winsize = new Point(Client.Game.Scene.Camera.Bounds.Width, Client.Game.Scene.Camera.Bounds.Height);

            Rectangle snowRect = new Rectangle(0, 0, 2, 2);

            for (int i = 0; i < CurrentCount; i++)
            {
                ref WeatherEffect effect = ref _effects[i];

                if (effect.X < x || effect.X > x + winsize.X || effect.Y < y || effect.Y > y + winsize.Y)
                {
                    if (removeEffects)
                    {
                        if (CurrentCount > 0)
                        {
                            CurrentCount--;
                        }
                        else
                        {
                            CurrentCount = 0;
                        }

                        continue;
                    }

                    effect.X = x + RandomHelper.GetValue(0, winsize.X);
                    effect.Y = y + RandomHelper.GetValue(0, winsize.Y);
                }

                switch (Type)
                {
                    case WeatherType.WT_RAIN:
                        float scaleRation = effect.ScaleRatio;
                        effect.SpeedX = -4.5f - scaleRation;
                        effect.SpeedY = 5.0f + scaleRation;

                        break;

                    case WeatherType.WT_STORM_BREWING:
                        effect.SpeedX = Wind * 1.5f;
                        effect.SpeedY = 1.5f;

                        if (windChanged)
                        {
                            PlayThunder();
                        }

                        break;

                    case WeatherType.WT_SNOW:
                    case WeatherType.WT_STORM_APPROACH:

                        if (Type == WeatherType.WT_SNOW)
                        {
                            effect.SpeedX = Wind;
                            effect.SpeedY = 1.0f;
                        }
                        else
                        {
                            effect.SpeedX = Wind;
                            effect.SpeedY = 6.0f;
                        }

                        if (windChanged)
                        {
                            effect.SpeedAngle = MathHelper.ToDegrees((float)Math.Atan2(effect.SpeedX, effect.SpeedY));

                            effect.SpeedMagnitude = (float)Math.Sqrt(Math.Pow(effect.SpeedX, 2) + Math.Pow(effect.SpeedY, 2));

                            if (Type == WeatherType.WT_SNOW)
                            {
                                PlayWind();
                            }
                            else
                            {
                                PlayThunder();
                            }
                        }

                        float speedAngle = effect.SpeedAngle;
                        float speedMagnitude = effect.SpeedMagnitude;

                        speedMagnitude += effect.ScaleRatio;

                        speedAngle += SinOscillate(0.4f, 20, Time.Ticks + effect.ID);

                        float rad = MathHelper.ToRadians(speedAngle);
                        effect.SpeedX = speedMagnitude * (float)Math.Sin(rad);
                        effect.SpeedY = speedMagnitude * (float)Math.Cos(rad);

                        break;
                }

                float speedOffset = passed / SIMULATION_TIME;

                switch (Type)
                {
                    case WeatherType.WT_RAIN:
                    case WeatherType.WT_STORM_APPROACH:

                        int oldX = (int)effect.X;
                        int oldY = (int)effect.Y;

                        float ofsx = effect.SpeedX * speedOffset;
                        float ofsy = effect.SpeedY * speedOffset;

                        effect.X += ofsx;
                        effect.Y += ofsy;

                        const float MAX_OFFSET_XY = 5.0f;

                        if (ofsx >= MAX_OFFSET_XY)
                        {
                            oldX = (int)(effect.X - MAX_OFFSET_XY);
                        }
                        else if (ofsx <= -MAX_OFFSET_XY)
                        {
                            oldX = (int)(effect.X + MAX_OFFSET_XY);
                        }

                        if (ofsy >= MAX_OFFSET_XY)
                        {
                            oldY = (int)(effect.Y - MAX_OFFSET_XY);
                        }
                        else if (oldY <= -MAX_OFFSET_XY)
                        {
                            oldY = (int)(effect.Y + MAX_OFFSET_XY);
                        }

                        if (_rainImage != null)
                        {
                            Vector3 hue = ShaderHueTranslator.GetHueVector(0);
                            batcher.Draw(_rainImage, new Rectangle(x + oldX, y + oldY, 80, 80), new Rectangle(x, y, 1000, 1000), hue);
                        }
                        else
                        {
                            var start = new Vector2(x + oldX, y + oldY);
                            var end = new Vector2(x + effect.X, y + effect.Y);

                            batcher.DrawLine
                            (
                                SolidColorTextureCache.GetTexture(Color.Blue),
                                start,
                                end,
                                Vector3.UnitZ,
                                2,
                                layerDepth
                            );
                        }

                        break;

                    case WeatherType.WT_SNOW:

                        effect.X += effect.SpeedX * speedOffset;
                        effect.Y += effect.SpeedY * speedOffset;

                        snowRect.X = x + (int)effect.X;
                        snowRect.Y = y + (int)effect.Y;

                        batcher.Draw
                        (
                            SolidColorTextureCache.GetTexture(Color.White),
                            snowRect,
                            Vector3.UnitZ,
                            layerDepth
                        );

                        break;
                }
            }

            _lastTick = Time.Ticks;
        }

        private struct WeatherEffect
        {
            public float SpeedX, SpeedY, X, Y, ScaleRatio, SpeedAngle, SpeedMagnitude;
            public uint ID;
        }
    }
}
