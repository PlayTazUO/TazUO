// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.CompilerServices;
using ClassicUO.Configuration;
using ClassicUO.IO;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.GameObjects
{
    public enum ObjectHandlesStatus
    {
        NONE,
        OPEN,
        CLOSED,
        DISPLAYING
    }

    public abstract partial class GameObject
    {
        // Constants for tile positioning and depth calculations
        private const int TILE_CENTER_OFFSET = 22;
        private const int TILE_HEIGHT_OFFSET = 44;
        private const int ART_STATIC_OFFSET = 0x4000;
        private const float DEPTH_Z_SCALE = 0.01f;
        private const int DEPTH_Z_OFFSET = 127;
        private const float DEPTH_RENDER_OFFSET = 0.5f;
        private const float DEPTH_WET_BASE_OFFSET = 0.49f;
        private const float DEPTH_SHADOW_OFFSET = 0.25f;

        // Water animation caching for performance
        private static float _cachedWaterSin;
        private static float _cachedWaterCos;
        private static uint _lastWaterAnimTicks;

        public byte AlphaHue;
        public bool AllowedToDraw = true;
        public ObjectHandlesStatus ObjectHandlesStatus;
        public Rectangle FrameInfo;
        protected bool IsFlipped;

        public abstract bool Draw(UltimaBatcher2D batcher, int posX, int posY, float depth);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float CalculateDepthZ()
        {
            int x = X;
            int y = Y;
            int z = PriorityZ;

            // Offsets are in SCREEN coordinates
            // if (Offset.X > 0 && Offset.Y < 0)
            // {
            //     // North
            // }
            // else
            if (Offset.X > 0 && Offset.Y == 0)
            {
                // Northeast
                x++;
            }
            else if (Offset.X > 0 && Offset.Y > 0)
            {
                // East
                z += Math.Max(0, (int)Offset.Z);
                x++;
            }
            else if (Offset.X == 0 && Offset.Y > 0)
            {
                // Southeast
                x++;
                y++;
            }
            else if (Offset.X < 0 && Offset.Y > 0)
            {
                // South
                z += Math.Max(0, (int)Offset.Z);
                y++;
            }
            else if (Offset.X < 0 && Offset.Y == 0)
            {
                // Southwest
                y++;
            }
            // else if (Offset.X < 0 && Offset.Y < 0)
            // {
            //     // West
            // }
            // else if (Offset.X == 0 && Offset.Y < 0)
            // {
            //     // Northwest
            // }

            return (x + y) + (DEPTH_Z_OFFSET + z) * DEPTH_Z_SCALE;
        }

        public Rectangle GetOnScreenRectangle()
        {
            Rectangle prect = Rectangle.Empty;

            prect.X = (int)(RealScreenPosition.X - FrameInfo.X + TILE_CENTER_OFFSET + Offset.X);
            prect.Y = (int)(RealScreenPosition.Y - FrameInfo.Y + TILE_CENTER_OFFSET + (Offset.Y - Offset.Z));
            prect.Width = FrameInfo.Width;
            prect.Height = FrameInfo.Height;

            return prect;
        }

        /// <summary>
        /// Tests if the object is transparent at the given Z coordinate.
        /// Default implementation treats objects as opaque.
        /// </summary>
        /// <param name="z">The Z coordinate to test</param>
        /// <returns>True if transparent at this Z level, false otherwise</returns>
        public virtual bool TransparentTest(int z) => false;

        /// <summary>
        /// Updates cached water animation values if needed and returns the current scale.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector2 GetWaterAnimationScale()
        {
            if (_lastWaterAnimTicks != Time.Ticks)
            {
                _lastWaterAnimTicks = Time.Ticks;
                _cachedWaterSin = (float)Math.Sin(Time.Ticks / 1000f);
                _cachedWaterCos = (float)Math.Cos(Time.Ticks / 1000f);
            }
            return new Vector2(1.1f + _cachedWaterSin * 0.1f, 1.1f + _cachedWaterCos * 0.5f * 0.1f);
        }

        protected static void DrawStatic(
            UltimaBatcher2D batcher,
            ushort graphic,
            int x,
            int y,
            Vector3 hue,
            float depth,
            bool isWet = false
        )
        {
            ref readonly SpriteInfo artInfo = ref Client.Game.UO.Arts.GetArt(graphic);

            if (artInfo.Texture != null)
            {
                ref UOFileIndex index = ref Client.Game.UO.FileManager.Arts.File.GetValidRefEntry(graphic + ART_STATIC_OFFSET);
                index.Width = (short)((artInfo.UV.Width >> 1) - TILE_CENTER_OFFSET);
                index.Height = (short)(artInfo.UV.Height - TILE_HEIGHT_OFFSET);

                x -= index.Width;
                y -= index.Height;

                var pos = new Vector2(x, y);
                float renderDepth = depth + DEPTH_RENDER_OFFSET;

                if (isWet)
                {
                    // Draw base layer at slightly lower depth to prevent z-fighting
                    batcher.Draw(
                        artInfo.Texture,
                        pos,
                        artInfo.UV,
                        hue,
                        0f,
                        Vector2.Zero,
                        Vector2.One,
                        SpriteEffects.None,
                        depth + DEPTH_WET_BASE_OFFSET
                    );

                    // Draw animated water layer on top
                    Vector2 scale = GetWaterAnimationScale();
                    batcher.Draw(
                        artInfo.Texture,
                        pos,
                        artInfo.UV,
                        hue,
                        0f,
                        Vector2.Zero,
                        scale,
                        SpriteEffects.None,
                        renderDepth
                    );
                }
                else
                {
                    batcher.Draw(
                        artInfo.Texture,
                        pos,
                        artInfo.UV,
                        hue,
                        0f,
                        Vector2.Zero,
                        Vector2.One,
                        SpriteEffects.None,
                        renderDepth
                    );
                }
            }
        }

        protected static void DrawGump(
            UltimaBatcher2D batcher,
            ushort graphic,
            int x,
            int y,
            Vector3 hue,
            float depth
        )
        {
            ref readonly SpriteInfo gumpInfo = ref Client.Game.UO.Gumps.GetGump(graphic);

            if (gumpInfo.Texture != null)
            {
                float renderDepth = depth + DEPTH_RENDER_OFFSET;
                batcher.Draw(
                    gumpInfo.Texture,
                    new Vector2(x, y),
                    gumpInfo.UV,
                    hue,
                    0f,
                    Vector2.Zero,
                    1f,
                    SpriteEffects.None,
                    renderDepth
                );
            }
        }

        protected static void DrawStaticRotated(
            UltimaBatcher2D batcher,
            ushort graphic,
            int x,
            int y,
            float angle,
            Vector3 hue,
            float depth,
            bool isWet = false
        )
        {
            ref readonly SpriteInfo artInfo = ref Client.Game.UO.Arts.GetArt(graphic);

            if (artInfo.Texture != null)
            {
                ref UOFileIndex index = ref Client.Game.UO.FileManager.Arts.File.GetValidRefEntry(graphic + ART_STATIC_OFFSET);
                index.Width = (short)((artInfo.UV.Width >> 1) - TILE_CENTER_OFFSET);
                index.Height = (short)(artInfo.UV.Height - TILE_HEIGHT_OFFSET);

                float renderDepth = depth + DEPTH_RENDER_OFFSET;

                batcher.Draw(
                    artInfo.Texture,
                    new Rectangle(
                        x - index.Width,
                        y - index.Height,
                        artInfo.UV.Width,
                        artInfo.UV.Height
                    ),
                    artInfo.UV,
                    hue,
                    angle,
                    Vector2.Zero,
                    SpriteEffects.None,
                    renderDepth
                );
            }
        }

        protected static void DrawStaticAnimated(
            UltimaBatcher2D batcher,
            ushort graphic,
            int x,
            int y,
            Vector3 hue,
            bool shadow,
            float depth,
            bool isWet = false
        )
        {
            ref UOFileIndex index = ref Client.Game.UO.FileManager.Arts.File.GetValidRefEntry(graphic + ART_STATIC_OFFSET);

            graphic = (ushort)(graphic + index.AnimOffset);

            ref readonly SpriteInfo artInfo = ref Client.Game.UO.Arts.GetArt(graphic);

            if (artInfo.Texture != null)
            {
                index = ref Client.Game.UO.FileManager.Arts.File.GetValidRefEntry(graphic + ART_STATIC_OFFSET);
                index.Width = (short)((artInfo.UV.Width >> 1) - TILE_CENTER_OFFSET);
                index.Height = (short)(artInfo.UV.Height - TILE_HEIGHT_OFFSET);

                x -= index.Width;
                y -= index.Height;

                var pos = new Vector2(x, y);
                float renderDepth = depth + DEPTH_RENDER_OFFSET;

                if (shadow)
                {
                    batcher.DrawShadow(artInfo.Texture, pos, artInfo.UV, false, depth + DEPTH_SHADOW_OFFSET);
                }

                if (isWet)
                {
                    // Draw base layer at slightly lower depth to prevent z-fighting
                    batcher.Draw(
                        artInfo.Texture,
                        pos,
                        artInfo.UV,
                        hue,
                        0f,
                        Vector2.Zero,
                        Vector2.One,
                        SpriteEffects.None,
                        depth + DEPTH_WET_BASE_OFFSET
                    );

                    // Draw animated water layer on top
                    Vector2 scale = GetWaterAnimationScale();
                    batcher.Draw(
                        artInfo.Texture,
                        pos,
                        artInfo.UV,
                        hue,
                        0f,
                        Vector2.Zero,
                        scale,
                        SpriteEffects.None,
                        renderDepth
                    );
                }
                else
                {
                    batcher.Draw(
                        artInfo.Texture,
                        pos,
                        artInfo.UV,
                        hue,
                        0f,
                        Vector2.Zero,
                        Vector2.One,
                        SpriteEffects.None,
                        renderDepth
                    );
                }
            }
        }
    }
}
