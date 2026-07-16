using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Gumps
{
    public class SkillProgressBar : Gump
    {
        private long expireAt = long.MaxValue;
        public SkillProgressBar(World world, int skillIndex) : base(world, 0, 0)
        {
            Height = 40;
            Width = 300;

            WorldViewportGump vp = UIManager.GetGump<WorldViewportGump>();

            if (ProfileManager.CurrentProfile == null || ProfileManager.CurrentProfile.SkillProgressBarPosition == Point.Zero)
            {
                if (vp != null)
                {
                    Y = vp.Location.Y + 80;
                    X = (vp.Location.X + (vp.Width / 2)) - (Width / 2);
                }
            }
            else
            {
                Location = ProfileManager.CurrentProfile.SkillProgressBarPosition;
            }

            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            CanMove = true;

            this.skillIndex = skillIndex;

            BuildGump();
        }

        private int skillIndex { get; }

        public void SetDuration(long ms) => expireAt = Time.Ticks + ms;

        protected override void OnMove(int x, int y)
        {
            base.OnMove(x, y);
            ProfileManager.CurrentProfile.SkillProgressBarPosition = Location;
        }

        private void BuildGump()
        {
            if (World.Player.Skills.Length > skillIndex)
            {
                Skill s = World.Player.Skills[skillIndex];

                var tb = TextBox.GetOne
                (
                    string.Format(ProfileManager.CurrentProfile.SkillBarFormat, s.Name, s.Value, s.Cap), ProfileManager.CurrentProfile.GameWindowSideChatFont,
                    ProfileManager.CurrentProfile.GameWindowSideChatFontSize, Color.White, TextBox.RTLOptions.Default()
                );

                tb.X = (Width / 2) - (tb.MeasuredSize.X / 2);

                Add(tb);

                Rectangle barBounds = Client.Game.UO.Gumps.GetGump(0x0805).UV;

                int widthPercent = (int)(barBounds.Width * (s.Value / s.Cap));

                if (widthPercent > barBounds.Width)
                    widthPercent = barBounds.Width;

                Add
                (
                    new GumpPic(0, Height - barBounds.Height, 0x0805, 0)
                    {
                        X = (Width / 2) - (barBounds.Width / 2)
                    }
                ); //Background

                if (widthPercent > 0)
                    Add
                    (
                        new GumpPicTiled(0, Height - barBounds.Height, widthPercent, barBounds.Height, 0x0806)
                        {
                            X = (Width / 2) - (barBounds.Width / 2)
                        }
                    ); //Foreground
            }
        }

        public override void Update()
        {
            base.Update();

            if (Time.Ticks >= expireAt)
            {
                Dispose();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            QueManager.ShowNext(World);
        }

        public static class QueManager
        {
            private static List<SkillProgressBar> skillProgressBars = new List<SkillProgressBar>();
            private static readonly object _lock = new object();
            public static SkillProgressBar CurrentProgressBar;
            private static bool beingReset;


            public static void AddSkill(World world, int skillIndex)
            {
                lock (_lock)
                {
                    // Remove any existing queued updates for the same skill
                    skillProgressBars.RemoveAll(bar => bar.skillIndex == skillIndex);

                    // Add the new skill update
                    skillProgressBars.Add(new SkillProgressBar(world, skillIndex));
                }

                if (CurrentProgressBar == null || CurrentProgressBar.IsDisposed)
                {
                    ShowNext(world);
                }
            }

            public static void ShowNext(World world)
            {
                if (beingReset)
                    return;

                if (!world.InGame)
                    return;

                if (ProfileManager.CurrentProfile != null && !ProfileManager.CurrentProfile.DisplaySkillBarOnChange)
                {
                    Reset();
                    return;
                }

                SkillProgressBar skillProgressBar = null;
                lock (_lock)
                {
                    if (skillProgressBars.Count > 0)
                    {
                        skillProgressBar = skillProgressBars[0];
                        skillProgressBars.RemoveAt(0);
                    }
                }

                if (skillProgressBar == null)
                    return;

                CurrentProgressBar = skillProgressBar;
                skillProgressBar.SetDuration(4000); //Expire in 4 seconds
                UIManager.Add(skillProgressBar, false);
            }

            public static void Reset()
            {
                beingReset = true;

                lock (_lock)
                {
                    foreach (SkillProgressBar skillProgressBar in skillProgressBars)
                    {
                        skillProgressBar?.Dispose();
                    }
                    skillProgressBars.Clear();
                }

                beingReset = false;
            }
        }
    }
}
