using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Gumps
{
    public class CoolDownBar : Gump
    {
        public const int COOL_DOWN_WIDTH = 180, COOL_DOWN_HEIGHT = 30;
        public static int DEFAULT_X => ProfileManager.CurrentProfile.CoolDownX;
        public static int DEFAULT_Y => ProfileManager.CurrentProfile.CoolDownY;

        private AlphaBlendControl background, foreground;
        public readonly Label textLabel, cooldownLabel;
        private DateTime expire;
        private TimeSpan duration;
        private int startX, startY;
        private readonly bool isBuffBar;

        private GumpPic gumpPic;

        public BuffIconType buffIconType;

        public CoolDownBar(World world, TimeSpan _duration, string _name, ushort _hue, int x, int y, ushort graphic = ushort.MaxValue, BuffIconType type = BuffIconType.Unknown2, bool isBuffBar = false) : base(world, 0, 0)
        {
            #region VARS
            Width = COOL_DOWN_WIDTH;
            Height = COOL_DOWN_HEIGHT;
            X = x;
            startX = x;
            Y = y;
            startY = y;
            expire = DateTime.Now + _duration;
            duration = _duration;
            CanCloseWithRightClick = true;
            CanMove = true;
            AcceptMouseInput = true;
            buffIconType = type;
            this.isBuffBar = isBuffBar;
            #endregion

            #region BACK/FORE GROUND
            background = new AlphaBlendControl(0.3f);
            background.Width = COOL_DOWN_WIDTH;
            background.Height = COOL_DOWN_HEIGHT;
            background.Hue = _hue;

            foreground = new AlphaBlendControl(0.8f);
            foreground.Width = COOL_DOWN_WIDTH;
            foreground.Height = COOL_DOWN_HEIGHT;
            foreground.Hue = _hue;
            #endregion

            if (graphic != ushort.MaxValue)
            {
                gumpPic = new GumpPic(0, 2, graphic, 0);
                background.X = gumpPic.Width;
                background.Width = COOL_DOWN_WIDTH - gumpPic.Width;

                foreground.X = gumpPic.Width;
                foreground.Width = COOL_DOWN_WIDTH - gumpPic.Width;
            }

            #region LABELS
            if (_name.Length > 17)
            {
                _name = _name.Substring(0, 16) + "..";
            }
            textLabel = new Label(_name, true, _hue, background.Width, style: FontStyle.BlackBorder, align: Assets.TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = background.X
            };

            cooldownLabel = new Label("------", true, _hue, background.Width, style: FontStyle.BlackBorder, align: Assets.TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = background.X,
                Y = 0
            };
            cooldownLabel.Y = COOL_DOWN_HEIGHT - cooldownLabel.Height - 2;
            cooldownLabel.Text = "";
            #endregion

            #region ADD CONTROLS
            if (graphic != ushort.MaxValue)
                Add(gumpPic);
            Add(background);
            Add(foreground);
            Add(textLabel);
            Add(cooldownLabel);
            #endregion
        }

        public override void Update()
        {
            base.Update();

            if (
                !isBuffBar &&
                (ProfileManager.CurrentProfile?.UseLastMovedCooldownPosition ?? false) &&
                (X != startX || Y != startY)
                )
            {
                ProfileManager.CurrentProfile.CoolDownX = X;
                ProfileManager.CurrentProfile.CoolDownY = Y;
                startX = X;
                startY = Y;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
                return false;

            if (DateTime.Now >= expire)
                Dispose();

            TimeSpan remaing = expire - DateTime.Now;

            if (remaing < TimeSpan.FromMinutes(60))
            {
                int offset = 0;
                if (gumpPic != null)
                    offset = gumpPic.Width;
                foreground.Width = (int)((remaing.TotalSeconds / duration.TotalSeconds) * (COOL_DOWN_WIDTH - offset));
                cooldownLabel.Text = ((int)remaing.TotalSeconds).ToString();
            }

            base.Draw(batcher, x, y);

            batcher.DrawRectangle(
                    SolidColorTextureCache.GetTexture(Color.Black),
                    x, y,
                    COOL_DOWN_WIDTH,
                    COOL_DOWN_HEIGHT,
                    ShaderHueTranslator.GetHueVector(background.Hue, false, 1f)
                );
            batcher.DrawRectangle(
                SolidColorTextureCache.GetTexture(Color.Black),
                x + 1, y + 1,
                COOL_DOWN_WIDTH - 2,
                COOL_DOWN_HEIGHT - 2,
                ShaderHueTranslator.GetHueVector(background.Hue, false, 1f)
            );

            return true;
        }

        public class CoolDownConditionData
        {
            public ushort hue;
            public string label;
            public string trigger;
            public int cooldown;
            public int message_type;
            public bool replace_if_exists;
            public bool skip_if_exists;

            /// <summary>
            /// Represents a cooldown bar condition.
            /// Each condition is a standalone 'rule' that determines when a cooldown bar should be displayed.
            /// </summary>
            /// <param name="hue">The bar's hue</param>
            /// <param name="label">The text to render inside the bar</param>
            /// <param name="trigger">The text that triggers the cooldown bar</param>
            /// <param name="cooldown">The duration, in seconds of the cooldown bar</param>
            /// <param name="messageType">The message type that should trigger the cooldown bar. See <see cref="MESSAGE_TYPE"/> for more information</param>
            /// <param name="replaceExisting">
            /// Whether to replace an existing instance of the cooldown bar when triggered.
            /// To clarify, this does not refer to the configuration - this refers to the cooldown bar itself, i.e., whether additional calls replace an already-on-screen instance
            /// </param>
            /// <param name="skipExisting">
            /// Whether to skip (not add) a new cooldown bar when an instance of the same bar is already on-screen, preserving the running countdown.
            /// When set, this takes precedence over <paramref name="replaceExisting"/>.
            /// </param>
            private CoolDownConditionData(
                ushort hue = 42,
                string label = "Label",
                string trigger = "Text to trigger",
                int cooldown = 10,
                int messageType = (int)MESSAGE_TYPE.ALL,
                bool replaceExisting = false,
                bool skipExisting = false
            )
            {
                this.hue = hue;
                this.label = label;
                this.trigger = trigger;
                this.cooldown = cooldown;
                message_type = messageType;
                replace_if_exists = replaceExisting;
                skip_if_exists = skipExisting;
            }

            public static CoolDownConditionData[] GetAllRules()
            {
                List<CooldownBarConfigEntry> bars = CooldownBarsConfig.Current.Bars;
                var data = new CoolDownConditionData[bars.Count];
                for (int i = 0; i < bars.Count; i++)
                    data[i] = FromEntry(bars[i]);

                return data;
            }

            private static CoolDownConditionData FromEntry(CooldownBarConfigEntry entry)
            {
                return new CoolDownConditionData
                {
                    hue = entry.Hue,
                    label = entry.Label,
                    trigger = entry.Trigger,
                    cooldown = entry.Cooldown,
                    message_type = entry.MessageType,
                    replace_if_exists = entry.ReplaceIfExists,
                    skip_if_exists = entry.SkipIfExists
                };
            }

            public static CoolDownConditionData GetConditionData(int key, bool createIfNotExist)
            {
                List<CooldownBarConfigEntry> bars = CooldownBarsConfig.Current.Bars;

                if (key >= 0 && key < bars.Count)
                    return FromEntry(bars[key]);

                var data = new CoolDownConditionData();

                if (createIfNotExist)
                {
                    bars.Add(new CooldownBarConfigEntry
                    {
                        Hue = data.hue,
                        Label = data.label,
                        Trigger = data.trigger,
                        Cooldown = data.cooldown,
                        MessageType = data.message_type,
                        ReplaceIfExists = data.replace_if_exists,
                        SkipIfExists = data.skip_if_exists
                    });
                    CooldownBarsConfig.Current.Save();
                }

                return data;
            }

            public static void SaveCondition(int key, ushort hue, string label, string trigger, int cooldown, bool createIfNotExist, int message_type, bool replace_if_exists, bool skip_if_exists)
            {
                List<CooldownBarConfigEntry> bars = CooldownBarsConfig.Current.Bars;

                if (key >= 0 && key < bars.Count)
                {
                    CooldownBarConfigEntry entry = bars[key];
                    entry.Hue = hue;
                    entry.Label = label;
                    entry.Trigger = trigger;
                    entry.Cooldown = cooldown;
                    entry.MessageType = message_type;
                    entry.ReplaceIfExists = replace_if_exists;
                    entry.SkipIfExists = skip_if_exists;
                }
                else if (createIfNotExist)
                {
                    bars.Add(new CooldownBarConfigEntry
                    {
                        Hue = hue,
                        Label = label,
                        Trigger = trigger,
                        Cooldown = cooldown,
                        MessageType = message_type,
                        ReplaceIfExists = replace_if_exists,
                        SkipIfExists = skip_if_exists
                    });
                }
                else
                {
                    return;
                }

                CooldownBarsConfig.Current.Save();
            }

            public static void RemoveCondition(int key)
            {
                List<CooldownBarConfigEntry> bars = CooldownBarsConfig.Current.Bars;

                if (key >= 0 && key < bars.Count)
                {
                    bars.RemoveAt(key);
                    CooldownBarsConfig.Current.Save();
                }
            }

            /// <summary>
            /// Moves a cooldown condition from one position to another.
            /// </summary>
            /// <param name="oldOrder">Current zero-based index of the condition to move.</param>
            /// <param name="newOrder">Target zero-based index the condition should occupy after the move.</param>
            public static void ReorderCondition(int oldOrder, int newOrder)
            {
                List<CooldownBarConfigEntry> bars = CooldownBarsConfig.Current.Bars;
                int count = bars.Count;

                if (oldOrder == newOrder)
                    return;

                if (oldOrder < 0 || oldOrder >= count || newOrder < 0 || newOrder >= count)
                    return;

                CooldownBarConfigEntry item = bars[oldOrder];
                bars.RemoveAt(oldOrder);
                bars.Insert(newOrder, item);

                CooldownBarsConfig.Current.Save();
            }

            public enum MESSAGE_TYPE
            {
                ALL,
                SELF,
                OTHER
            }

        }
    }
}
