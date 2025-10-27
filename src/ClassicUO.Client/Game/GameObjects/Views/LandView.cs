// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace ClassicUO.Game.GameObjects
{
    public sealed partial class Land
    {
        private const float LAND_DEPTH_OFFSET = 0.5f;
        private const int Z_TO_PIXEL_MULTIPLIER = 4;

        private static float _cachedWaterSin;
        private static float _cachedWaterCos;
        private static uint _lastWaterAnimTicks;

        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY, float depth)
        {
            if (!AllowedToDraw || IsDestroyed)
            {
                return false;
            }

            ushort hue = Hue;
            bool isSelected = SelectedObject.Object == this;

            if (isSelected && _profile.HighlightGameObjects)
            {
                hue = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE;
            }
            else if (
                _profile.NoColorObjectsOutOfRange
                && Distance > World.ClientViewRange
            )
            {
                hue = Constants.OUT_RANGE_COLOR;
            }
            else if (World.Player != null && World.Player.IsDead && _profile.EnableBlackWhiteEffect)
            {
                hue = Constants.DEAD_RANGE_COLOR;
            }

            if (isSelected)
            {
                SpellVisualRangeManager.Instance.LastCursorTileLoc = new Vector2(X, Y);
            }

            if (SpellVisualRangeManager.Instance.IsTargetingAfterCasting())
            {
                hue = SpellVisualRangeManager.Instance.ProcessHueForTile(hue, this);
            }

            if (_profile.DisplayRadius && Distance == _profile.DisplayRadiusDistance)
            {
                hue = _profile.DisplayRadiusHue;
            }


            Vector3 hueVec;
            hueVec.Z = 1f;

            if (hue != 0)
            {
                hueVec.X = hue - 1;
                hueVec.Y = IsStretched
                    ? ShaderHueTranslator.SHADER_LAND_HUED
                    : ShaderHueTranslator.SHADER_HUED;
            }
            else
            {
                hueVec.X = 0;
                hueVec.Y = IsStretched
                    ? ShaderHueTranslator.SHADER_LAND
                    : ShaderHueTranslator.SHADER_NONE;
            }

            if (IsStretched)
            {
                posY += Z * Z_TO_PIXEL_MULTIPLIER;

                ref readonly SpriteInfo texmapInfo = ref Client.Game.UO.Texmaps.GetTexmap(
                    Client.Game.UO.FileManager.TileData.LandData[Graphic].TexID
                );

                if (texmapInfo.Texture != null)
                {
                    batcher.DrawStretchedLand(
                        texmapInfo.Texture,
                        new Vector2(posX, posY),
                        texmapInfo.UV,
                        ref YOffsets,
                        ref NormalTop,
                        ref NormalRight,
                        ref NormalLeft,
                        ref NormalBottom,
                        hueVec,
                        depth + LAND_DEPTH_OFFSET
                    );
                }
                else
                {
                    DrawStatic(
                        batcher,
                        Graphic,
                        posX,
                        posY,
                        hueVec,
                        depth,
                        _profile.AnimatedWaterEffect && TileData.IsWet
                    );
                }
            }
            else
            {
                ref readonly SpriteInfo artInfo = ref Client.Game.UO.Arts.GetLand(Graphic);

                if (artInfo.Texture != null)
                {
                    var pos = new Vector2(posX, posY);
                    Vector2 scale = Vector2.One;

                    if (_profile.AnimatedWaterEffect && TileData.IsWet)
                    {
                        // Draw base water layer for depth effect
                        batcher.Draw(
                            artInfo.Texture,
                            pos,
                            artInfo.UV,
                            hueVec,
                            0f,
                            Vector2.Zero,
                            scale,
                            SpriteEffects.None,
                            depth + LAND_DEPTH_OFFSET
                        );

                        // Cache trig calculations per frame to avoid repeated expensive operations
                        if (_lastWaterAnimTicks != Time.Ticks)
                        {
                            _lastWaterAnimTicks = Time.Ticks;
                            _cachedWaterSin = (float)Math.Sin(Time.Ticks / 1000f);
                            _cachedWaterCos = (float)Math.Cos(Time.Ticks / 1000f);
                        }

                        // Calculate animated scale for water surface layer
                        scale = new Vector2(1.1f + _cachedWaterSin * 0.1f, 1.1f + _cachedWaterCos * 0.5f * 0.1f);
                    }

                    // Draw land tile (or animated water surface layer)
                    batcher.Draw(
                        artInfo.Texture,
                        pos,
                        artInfo.UV,
                        hueVec,
                        0f,
                        Vector2.Zero,
                        scale,
                        SpriteEffects.None,
                        depth + LAND_DEPTH_OFFSET
                    );

                }
            }

            return true;
        }

        public override bool CheckMouseSelection()
        {
            if (IsStretched)
            {
                return SelectedObject.IsPointInStretchedLand(
                    ref YOffsets,
                    RealScreenPosition.X,
                    RealScreenPosition.Y + (Z * Z_TO_PIXEL_MULTIPLIER)
                );
            }

            return SelectedObject.IsPointInLand(RealScreenPosition.X, RealScreenPosition.Y);
        }
    }
}
