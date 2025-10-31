// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.CompilerServices;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    public sealed partial class Static
    {
        private const int MAX_Z_DIFFERENCE_FOR_DISPLAY_RADIUS = 11;
        private const int LIGHT_OFFSET = 22;

        private int _canBeTransparent;

        /// <summary>
        /// Gets the display graphic for this static, applying tree-to-stump replacement if enabled.
        /// </summary>
        /// <param name="graphic">The base graphic to process.</param>
        /// <returns>The graphic to display, either the original or replacement graphic.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort GetDisplayGraphic(ushort graphic)
        {
            if (StaticFilters.IsTree(graphic, out _) && _profile?.TreeToStumps == true)
            {
                return Constants.TREE_REPLACE_GRAPHIC;
            }
            return graphic;
        }

        /// <summary>
        /// Determines whether this static object should be rendered with transparency
        /// based on its height relative to the specified Z coordinate.
        /// </summary>
        /// <param name="z">The Z coordinate to test against.</param>
        /// <returns>True if the object should be transparent, false otherwise.</returns>
        public override bool TransparentTest(int z)
        {
            bool r = true;

            if (Z <= z - ItemData.Height)
            {
                r = false;
            }
            else if (z < Z && (_canBeTransparent & 0xFF) == 0)
            {
                r = false;
            }

            return r;
        }

        /// <summary>
        /// Renders the static object to the screen.
        /// </summary>
        /// <param name="batcher">The rendering batch manager.</param>
        /// <param name="posX">The X screen position.</param>
        /// <param name="posY">The Y screen position.</param>
        /// <param name="depth">The rendering depth for sorting.</param>
        /// <returns>True if the object was rendered, false if skipped.</returns>
        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY, float depth)
        {
            if (!AllowedToDraw || IsDestroyed)
            {
                return false;
            }

            GameScene scene = Client.Game.GetScene<GameScene>();

            ushort graphic = Graphic;
            ushort hue = Hue;
            bool partial = ItemData.IsPartialHue;
            bool isSelected = SelectedObject.Object == this;

            if (isSelected && _profile.HighlightGameObjects)
            {
                hue = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE;
                partial = false;
            }
            else if (
                _profile.NoColorObjectsOutOfRange
                && Distance > World.ClientViewRange
            )
            {
                hue = Constants.OUT_RANGE_COLOR;
                partial = false;
            }
            else if (World.Player != null && World.Player.IsDead && _profile.EnableBlackWhiteEffect)
            {
                hue = Constants.DEAD_RANGE_COLOR;
                partial = false;
            }

            if (isSelected)
            {
                SpellVisualRangeManager.Instance.LastCursorTileLoc = new Vector2(X, Y);
            }

            if (SpellVisualRangeManager.Instance.IsTargetingAfterCasting())
            {
                hue = SpellVisualRangeManager.Instance.ProcessHueForTile(hue, this);
            }

            if (_profile.DisplayRadius &&
                Distance == _profile.DisplayRadiusDistance &&
                World.Player != null &&
                Math.Abs(Z - World.Player.Z) < MAX_Z_DIFFERENCE_FOR_DISPLAY_RADIUS)
            {
                hue = _profile.DisplayRadiusHue;
            }

            Vector3 hueVec = ShaderHueTranslator.GetHueVector(hue, partial, AlphaHue / 255f);

            graphic = GetDisplayGraphic(graphic);
            bool isTree = StaticFilters.IsTree(graphic, out _);

            DrawStaticAnimated(
                batcher,
                graphic,
                posX,
                posY,
                hueVec,
                _profile.ShadowsEnabled
                    && _profile.ShadowsStatics
                    && (isTree || ItemData.IsFoliage || StaticFilters.IsRock(graphic)),
                depth,
                _profile.AnimatedWaterEffect && ItemData.IsWet
            );

            if (_isLight && scene != null)
            {
                scene.AddLight(this, this, posX + LIGHT_OFFSET, posY + LIGHT_OFFSET);
            }

            return true;
        }

        /// <summary>
        /// Checks if the mouse cursor is over this static object using pixel-perfect collision detection.
        /// </summary>
        /// <returns>True if the mouse is over a non-transparent pixel of the object, false otherwise.</returns>
        public override bool CheckMouseSelection()
        {
            // Early return if already selected
            if (SelectedObject.Object == this)
            {
                return false;
            }

            // Early return if this is foliage and matches the current foliage index
            GameScene scene = Client.Game.GetScene<GameScene>();
            if (FoliageIndex != -1 && scene != null && scene.FoliageIndex == FoliageIndex)
            {
                return false;
            }

            // Get the display graphic (with tree-to-stump replacement if needed)
            ushort graphic = GetDisplayGraphic(Graphic);

            ref IO.UOFileIndex index = ref Client.Game.UO.FileManager.Arts.File.GetValidRefEntry(graphic + 0x4000);

            Point position = RealScreenPosition;
            position.X -= index.Width;
            position.Y -= index.Height;

            return Client.Game.UO.Arts.PixelCheck(
                graphic,
                SelectedObject.TranslatedMousePositionByViewport.X - position.X,
                SelectedObject.TranslatedMousePositionByViewport.Y - position.Y
            );
        }
    }
}
