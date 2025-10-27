// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Managers
{
    public sealed class HealthLinesManager
    {
        private const int BAR_WIDTH = 34; //28;
        private const int BAR_HEIGHT = 8;
        private const int BAR_WIDTH_HALF = BAR_WIDTH >> 1;
        private const int BAR_HEIGHT_HALF = BAR_HEIGHT >> 1;

        const ushort BACKGROUND_GRAPHIC = 0x1068;
        const ushort HP_GRAPHIC = 0x1069;

        private readonly World _world;

        public HealthLinesManager(World world) { _world = world; }

        public bool IsEnabled =>
            ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.ShowMobilesHP;

        public void Draw(UltimaBatcher2D batcher)
        {
            Camera camera = Client.Game.Scene.Camera;

            // if (SerialHelper.IsMobile(_world.TargetManager.LastTargetInfo.Serial))
            // {
            //     DrawHealthLineWithMath(
            //         batcher,
            //         _world.TargetManager.LastTargetInfo.Serial,
            //         camera.Bounds.Width,
            //         camera.Bounds.Height
            //     );
            //     DrawTargetIndicator(batcher, _world.TargetManager.LastTargetInfo.Serial);
            // }

            // if (SerialHelper.IsMobile(_world.TargetManager.SelectedTarget))
            // {
            //     DrawHealthLineWithMath(
            //         batcher,
            //         _world.TargetManager.SelectedTarget,
            //         camera.Bounds.Width,
            //         camera.Bounds.Height
            //     );
            //     DrawTargetIndicator(batcher, _world.TargetManager.SelectedTarget);
            // }

            // if (SerialHelper.IsMobile(_world.TargetManager.LastAttack))
            // {
            //     DrawHealthLineWithMath(
            //         batcher,
            //         _world.TargetManager.LastAttack,
            //         camera.Bounds.Width,
            //         camera.Bounds.Height
            //     );
            //     DrawTargetIndicator(batcher, _world.TargetManager.LastAttack);
            // }

            if (!IsEnabled)
            {
                return;
            }

            int mode = ProfileManager.CurrentProfile.MobileHPType;

            if (mode < 0)
            {
                return;
            }

            int showWhen = ProfileManager.CurrentProfile.MobileHPShowWhen;
            bool useNewTargetSystem = ProfileManager.CurrentProfile.UseNewTargetSystem;
            Renderer.Animations.Animations animations = Client.Game.UO.Animations;
            bool isEnabled = IsEnabled;

            foreach (Mobile mobile in _world.Mobiles.Values)
            {
                if (mobile.IsDestroyed)
                {
                    continue;
                }

                bool newTargSystem = false;
                bool forceDraw = false;
                bool passive = mobile.Serial != _world.Player.Serial;

                if (_world.TargetManager.LastTargetInfo.Serial == mobile ||
                    _world.TargetManager.LastAttack == mobile ||
                    _world.TargetManager.SelectedTarget == mobile ||
                    _world.TargetManager.NewTargetSystemSerial == mobile)
                {
                    newTargSystem = useNewTargetSystem && _world.TargetManager.NewTargetSystemSerial == mobile;
                    passive = false;
                    forceDraw = true;
                }

                int current = mobile.Hits;
                int max = mobile.HitsMax;

                if (!newTargSystem)
                {
                    if (max == 0)
                    {
                        continue;
                    }

                    if (showWhen == 1 && current == max)
                    {
                        continue;
                    }
                }

                Point p = mobile.RealScreenPosition;
                p.X += (int)mobile.Offset.X + 22 + 5;
                p.Y += (int)(mobile.Offset.Y - mobile.Offset.Z) + 22 + 5;
                int offsetY = 0;

                if (isEnabled)
                {
                    if (mode != 1 && !mobile.IsDead)
                    {
                        if (showWhen == 2 && current != max || showWhen <= 1)
                        {
                            if (mobile.HitsPercentage != 0)
                            {
                                animations.GetAnimationDimensions(
                                    mobile.AnimIndex,
                                    mobile.GetGraphicForAnimation(),
                                    /*(byte) m.GetDirectionForAnimation()*/
                                    0,
                                    /*Mobile.GetGroupForAnimation(m, isParent:true)*/
                                    0,
                                    mobile.IsMounted,
                                    /*(byte) m.AnimIndex*/
                                    0,
                                    out int centerX,
                                    out int centerY,
                                    out int width,
                                    out int height
                                );

                                Point p1 = p;
                                p1.Y -= height + centerY + 8 + 22;

                                if (mobile.IsGargoyle && mobile.IsFlying)
                                {
                                    p1.Y -= 22;
                                }
                                else if (!mobile.IsMounted)
                                {
                                    p1.Y += 22;
                                }

                                p1 = Client.Game.Scene.Camera.WorldToScreen(p1);
                                p1.X -= (mobile.HitsTexture.Width >> 1) + 5;
                                p1.Y -= mobile.HitsTexture.Height;

                                if (mobile.ObjectHandlesStatus == ObjectHandlesStatus.DISPLAYING)
                                {
                                    p1.Y -= Constants.OBJECT_HANDLES_GUMP_HEIGHT + 5;
                                    offsetY += Constants.OBJECT_HANDLES_GUMP_HEIGHT + 5;
                                }

                                if (
                                    !(
                                        p1.X < 0
                                        || p1.X > camera.Bounds.Width - mobile.HitsTexture.Width
                                        || p1.Y < 0
                                        || p1.Y > camera.Bounds.Height
                                    )
                                )
                                {
                                    mobile.HitsTexture.Draw(batcher, p1.X, p1.Y);
                                }

                                if (newTargSystem)
                                {
                                    offsetY += mobile.HitsTexture.Height;
                                }
                            }
                        }
                    }
                }

                p.X -= 5;
                p = Client.Game.Scene.Camera.WorldToScreen(p);
                p.X -= BAR_WIDTH_HALF;
                p.Y -= BAR_HEIGHT_HALF;

                if (p.X < 0 || p.X > camera.Bounds.Width - BAR_WIDTH)
                {
                    continue;
                }

                if (p.Y < 0 || p.Y > camera.Bounds.Height - BAR_HEIGHT)
                {
                    continue;
                }

                if ((isEnabled && mode >= 1) || newTargSystem || forceDraw)
                {
                    DrawHealthLine(batcher, mobile, p.X, p.Y, offsetY, passive, newTargSystem);
                }
            }
        }

        private void DrawTargetIndicator(UltimaBatcher2D batcher, uint serial)
        {
            Entity entity = _world.Get(serial);

            if (entity == null)
            {
                return;
            }
            if (ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.ShowTargetIndicator)
            {
                return;
            }
            ref readonly SpriteInfo indicatorInfo = ref Client.Game.UO.Gumps.GetGump(0x756F);
            if (indicatorInfo.Texture != null)
            {
                Point p = entity.RealScreenPosition;
                p.Y += (int)(entity.Offset.Y - entity.Offset.Z) + 22 + 5;

                p = Client.Game.Scene.Camera.WorldToScreen(p);
                p.Y -= entity.FrameInfo.Height + 25;

                batcher.Draw(
                indicatorInfo.Texture,
                new Rectangle(p.X - 24, p.Y, indicatorInfo.UV.Width, indicatorInfo.UV.Height),
                indicatorInfo.UV,
                ShaderHueTranslator.GetHueVector(0, false, 1.0f)
                );
            }
            else
            {
                ProfileManager.CurrentProfile.ShowTargetIndicator = false; //This sprite doesn't exist for this client, lets avoid checking for it every frame.
            }
        }
        // private void DrawHealthLineWithMath(
        //     UltimaBatcher2D batcher,
        //     uint serial,
        //     int screenW,
        //     int screenH
        // )
        // {
        //     Entity entity = _world.Get(serial);

        //     if (entity == null)
        //     {
        //         return;
        //     }

        //     Point p = entity.RealScreenPosition;
        //     p.X += (int)entity.Offset.X + 22;
        //     p.Y += (int)(entity.Offset.Y - entity.Offset.Z) + 22 + 5;

        //     p = Client.Game.Scene.Camera.WorldToScreen(p);
        //     p.X -= BAR_WIDTH_HALF;
        //     p.Y -= BAR_HEIGHT_HALF;

        //     if (p.X < 0 || p.X > screenW - BAR_WIDTH)
        //     {
        //         return;
        //     }

        //     if (p.Y < 0 || p.Y > screenH - BAR_HEIGHT)
        //     {
        //         return;
        //     }

        //     DrawHealthLine(batcher, entity, p.X, p.Y, false);
        // }

        private void DrawHealthLine(
            UltimaBatcher2D batcher,
            Entity entity,
            int x,
            int y,
            int offsetY,
            bool passive,
            bool newTargetSystem
        )
        {
            if (entity == null)
            {
                return;
            }

            int multiplier = 1;
            if (ProfileManager.CurrentProfile != null)
                multiplier = ProfileManager.CurrentProfile.HealthLineSizeMultiplier;

            int per = (BAR_WIDTH * multiplier) * entity.HitsPercentage / 100;
            int offset = 2;

            if (per >> 2 == 0)
            {
                offset = per;
            }

            var mobile = entity as Mobile;

            float alpha = passive && !newTargetSystem ? 0.5f : 1.0f;
            ushort hue =
                mobile != null
                    ? Notoriety.GetHue(mobile.NotorietyFlag)
                    : Notoriety.GetHue(NotorietyFlag.Gray);

            Vector3 hueVec = ShaderHueTranslator.GetHueVector(hue, false, alpha);

            if (mobile == null)
            {
                y += 22;
            }


            if (newTargetSystem && mobile != null && mobile.Serial != _world.Player.Serial)
            {
                Client.Game.UO.Animations.GetAnimationDimensions(
                    mobile.AnimIndex,
                    mobile.GetGraphicForAnimation(),
                    (byte) mobile.GetDirectionForAnimation(),
                    Mobile.GetGroupForAnimation(mobile, isParent: true),
                    mobile.IsMounted,
                    0, //mobile.AnimIndex,
                    out int centerX,
                    out int centerY,
                    out int width,
                    out int height
                );

                uint topGump;
                uint bottomGump;
                uint gumpHue = 0x7570;
                if (width >= 80)
                {
                    topGump = 0x756D;
                    bottomGump = 0x756A;
                }
                else if (width >= 40)
                {
                    topGump = 0x756E;
                    bottomGump = 0x756B;
                }
                else
                {
                    topGump = 0x756F;
                    bottomGump = 0x756C;
                }

                ref readonly SpriteInfo hueGumpInfo = ref Client.Game.UO.Gumps.GetGump(gumpHue);
                float targetX = x + BAR_WIDTH_HALF - hueGumpInfo.UV.Width / 2f;
                int topTargetY = height + centerY + 8 + 22 + offsetY;

                ref readonly SpriteInfo newTargGumpInfo = ref Client.Game.UO.Gumps.GetGump(topGump);
                if (newTargGumpInfo.Texture != null)
                    batcher.Draw(
                        newTargGumpInfo.Texture,
                        new Vector2(targetX, y - topTargetY),
                        newTargGumpInfo.UV,
                        hueVec
                    );

                if (hueGumpInfo.Texture != null)
                    batcher.Draw(
                        hueGumpInfo.Texture,
                        new Vector2(targetX, y - topTargetY),
                        hueGumpInfo.UV,
                        hueVec
                    );

                y += 7 + newTargGumpInfo.UV.Height / 2 - centerY;

                newTargGumpInfo = ref Client.Game.UO.Gumps.GetGump(bottomGump);
                if (newTargGumpInfo.Texture != null)
                    batcher.Draw(
                        newTargGumpInfo.Texture,
                        new Vector2(targetX, y - 1 - newTargGumpInfo.UV.Height / 2f),
                        newTargGumpInfo.UV,
                        hueVec
                    );
            }


            ref readonly SpriteInfo gumpInfo = ref Client.Game.UO.Gumps.GetGump(BACKGROUND_GRAPHIC);
            Rectangle bounds = gumpInfo.UV;

            if (multiplier > 1)
                x -= (int)(((BAR_WIDTH * multiplier) / 2) - (BAR_WIDTH / 2));

            batcher.Draw(
                gumpInfo.Texture,
                new Rectangle(x, y, gumpInfo.UV.Width * multiplier, gumpInfo.UV.Height * multiplier),
                gumpInfo.UV,
                hueVec
            );

            hueVec.X = 90;

            if (mobile != null)
            {
                if (mobile.IsPoisoned)
                {
                    hueVec.X = 63;
                }
                else if (mobile.IsYellowHits)
                {
                    hueVec.X = 53;
                }
            }

            float hitPerecentage = (float)entity.Hits / (float)entity.HitsMax;

            if (entity.HitsMax == 0)
                hitPerecentage = 1;

            batcher.Draw(
                SolidColorTextureCache.GetTexture(Color.White),
                new Vector2(x + (3 * multiplier), y + (4 * multiplier)),
                new Rectangle(0, 0, (int)(((BAR_WIDTH * multiplier) - (6 * multiplier)) * hitPerecentage), (bounds.Height * multiplier) - (6 * multiplier)),
                hueVec
                );
        }
    }
}
