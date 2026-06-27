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
            private CoolDownConditionData(
                ushort hue = 42,
                string label = "Label",
                string trigger = "Text to trigger",
                int cooldown = 10,
                int messageType = (int)MESSAGE_TYPE.ALL,
                bool replaceExisting = false
            )
            {
                this.hue = hue;
                this.label = label;
                this.trigger = trigger;
                this.cooldown = cooldown;
                message_type = messageType;
                replace_if_exists = replaceExisting;
            }

            public static CoolDownConditionData[] GetAllRules()
            {
                var data = new CoolDownConditionData[ProfileManager.CurrentProfile.CoolDownConditionCount];
                for (int i = 0; i < ProfileManager.CurrentProfile.CoolDownConditionCount; i++)
                    data[i] = GetConditionData(i, false);

                return data;
            }

            public static CoolDownConditionData GetConditionData(int key, bool createIfNotExist)
            {
                var data = new CoolDownConditionData();
                if (ProfileManager.CurrentProfile.CoolDownConditionCount > key)
                {
                    data.hue = ProfileManager.CurrentProfile.Condition_Hue[key];
                    data.label = ProfileManager.CurrentProfile.Condition_Label[key];
                    data.trigger = ProfileManager.CurrentProfile.Condition_Trigger[key];
                    data.cooldown = ProfileManager.CurrentProfile.Condition_Duration[key];
                    data.message_type = ProfileManager.CurrentProfile.Condition_Type[key];

                    if (ProfileManager.CurrentProfile.Condition_ReplaceIfExists.Count > key) //Remove me after a while to prevent index not found
                        data.replace_if_exists = ProfileManager.CurrentProfile.Condition_ReplaceIfExists[key];
                    else
                    {
                        while (ProfileManager.CurrentProfile.Condition_ReplaceIfExists.Count <= key)
                        {
                            ProfileManager.CurrentProfile.Condition_ReplaceIfExists.Add(false);
                        }
                    }
                }
                else if (createIfNotExist)
                {
                    ProfileManager.CurrentProfile.Condition_Hue.Add(data.hue);
                    ProfileManager.CurrentProfile.Condition_Label.Add(data.label);
                    ProfileManager.CurrentProfile.Condition_Trigger.Add(data.trigger);
                    ProfileManager.CurrentProfile.Condition_Duration.Add(data.cooldown);
                    ProfileManager.CurrentProfile.Condition_Type.Add(data.message_type);
                    ProfileManager.CurrentProfile.Condition_ReplaceIfExists.Add(data.replace_if_exists);
                }
                return data;
            }

            public static void SaveCondition(int key, ushort hue, string label, string trigger, int cooldown, bool createIfNotExist, int message_type, bool replace_if_exists)
            {
                if (ProfileManager.CurrentProfile.CoolDownConditionCount > key)
                {
                    ProfileManager.CurrentProfile.Condition_Hue[key] = hue;
                    ProfileManager.CurrentProfile.Condition_Label[key] = label;
                    ProfileManager.CurrentProfile.Condition_Trigger[key] = trigger;
                    ProfileManager.CurrentProfile.Condition_Duration[key] = cooldown;
                    ProfileManager.CurrentProfile.Condition_Type[key] = message_type;

                    if (ProfileManager.CurrentProfile.Condition_ReplaceIfExists.Count > key) //Remove me after a while to prevent index not found
                        ProfileManager.CurrentProfile.Condition_ReplaceIfExists[key] = replace_if_exists;
                    else
                    {
                        while (ProfileManager.CurrentProfile.Condition_ReplaceIfExists.Count <= key)
                        {
                            ProfileManager.CurrentProfile.Condition_ReplaceIfExists.Add(false);
                        }
                        ProfileManager.CurrentProfile.Condition_ReplaceIfExists[key] = replace_if_exists;
                    }
                }
                else if (createIfNotExist)
                {
                    ProfileManager.CurrentProfile.Condition_Hue.Add(hue);
                    ProfileManager.CurrentProfile.Condition_Label.Add(label);
                    ProfileManager.CurrentProfile.Condition_Trigger.Add(trigger);
                    ProfileManager.CurrentProfile.Condition_Duration.Add(cooldown);
                    ProfileManager.CurrentProfile.Condition_Type.Add(message_type);
                    ProfileManager.CurrentProfile.Condition_ReplaceIfExists.Add(createIfNotExist);
                }
            }

            public static void RemoveCondition(int key)
            {
                if (ProfileManager.CurrentProfile.CoolDownConditionCount > key)
                {
                    ProfileManager.CurrentProfile.Condition_Hue.RemoveAt(key);
                    ProfileManager.CurrentProfile.Condition_Label.RemoveAt(key);
                    ProfileManager.CurrentProfile.Condition_Trigger.RemoveAt(key);
                    ProfileManager.CurrentProfile.Condition_Duration.RemoveAt(key);
                    ProfileManager.CurrentProfile.Condition_Type.RemoveAt(key);
                    ProfileManager.CurrentProfile.Condition_ReplaceIfExists.RemoveAt(key);
                }
            }

            /// <summary>
            /// Moves a cooldown condition from one position to another, reordering all associated
            /// profile lists (hue, label, trigger, duration, type, replace-if-exists) atomically.
            /// </summary>
            /// <param name="oldOrder">Current zero-based index of the condition to move.</param>
            /// <param name="newOrder">Target zero-based index the condition should occupy after the move.</param>
            public static void ReorderCondition(int oldOrder, int newOrder)
            {
                Profile profile = ProfileManager.CurrentProfile;
                int count = profile.CoolDownConditionCount;

                if (oldOrder == newOrder)
                    return;

                if (oldOrder < 0 || oldOrder >= count || newOrder < 0 || newOrder >= count)
                    return;

                MoveListItem(profile.Condition_Hue, oldOrder, newOrder);
                MoveListItem(profile.Condition_Label, oldOrder, newOrder);
                MoveListItem(profile.Condition_Trigger, oldOrder, newOrder);
                MoveListItem(profile.Condition_Duration, oldOrder, newOrder);
                MoveListItem(profile.Condition_Type, oldOrder, newOrder);

                while (profile.Condition_ReplaceIfExists.Count < count)
                    profile.Condition_ReplaceIfExists.Add(false);

                MoveListItem(profile.Condition_ReplaceIfExists, oldOrder, newOrder);
            }

            /// <summary>
            /// Relocates the element at <paramref name="oldIndex"/> to <paramref name="newIndex"/>
            /// by removing it and re-inserting it, shifting intermediate elements accordingly.
            /// </summary>
            /// <typeparam name="T">Element type of the list.</typeparam>
            /// <param name="list">List to mutate in place.</param>
            /// <param name="oldIndex">Zero-based source index.</param>
            /// <param name="newIndex">Zero-based destination index.</param>
            private static void MoveListItem<T>(IList<T> list, int oldIndex, int newIndex)
            {
                T item = list[oldIndex];
                list.RemoveAt(oldIndex);
                list.Insert(newIndex, item);
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
