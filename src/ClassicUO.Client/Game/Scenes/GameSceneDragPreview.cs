// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Renderer;

namespace ClassicUO.Game.Scenes
{
    public partial class GameScene
    {
        /// <summary>
        /// Maximum Z difference from the player's elevation for which the held-item ground preview
        /// is shown. A floor-to-ceiling in UO is 20 Z, so this lets a ghost appear one floor above
        /// or below the player but not through arbitrary elevation jumps.
        /// </summary>
        private const int DRAG_PREVIEW_MAX_Z_DELTA = 20;

        /// <summary>
        /// Translucency of the held-item ghost on the ground (AlphaHue is 0-255, so this is ~50%).
        /// </summary>
        private const byte DRAG_PREVIEW_ALPHA = 127;

        private Static _dragPreview;

        /// <summary>
        /// Renders a translucent ghost of the held item at the tile it would land on while dragging.
        /// The target selection mirrors the ground-drop targeting in
        /// <see cref="GameSceneInputHandler"/> so the preview tracks the real drop position, and it
        /// is only shown while the cursor is over the world within pickup range.
        /// </summary>
        /// <param name="batcher">The active world batch (camera transform already applied).</param>
        private void DrawDragItemPreview(UltimaBatcher2D batcher)
        {
            ItemHold itemHold = Client.Game.UO.GameCursor.ItemHold;

            if (
                !ProfileManager.GlobalSettings.ShowDragItemPreview
                || !itemHold.Enabled
                || itemHold.Dropped
                || itemHold.IsFixedPosition
                || !UIManager.IsMouseOverWorld
                || SelectedObject.Object is not GameObject target
                || target.Distance > Constants.DRAG_ITEMS_DISTANCE
            )
            {
                return;
            }

            if (!TryGetDragDropTarget(target, itemHold, out int x, out int y, out sbyte z))
            {
                return;
            }

            if (Math.Abs((int)z - _world.Player.Z) > DRAG_PREVIEW_MAX_Z_DELTA)
            {
                return;
            }

            ushort graphic = itemHold.DisplayedGraphic;

            if (graphic == 0xFFFF)
            {
                return;
            }

            _dragPreview ??= new Static(_world);
            _dragPreview.Graphic = graphic;
            _dragPreview.Hue = itemHold.Hue;
            _dragPreview.AlphaHue = DRAG_PREVIEW_ALPHA;
            _dragPreview.X = (ushort)x;
            _dragPreview.Y = (ushort)y;
            _dragPreview.Z = z;
            _dragPreview.PriorityZ = z;
            _dragPreview.UpdateRealScreenPosition(_offset.X, _offset.Y);

            _dragPreview.Draw(
                batcher,
                _dragPreview.RealScreenPosition.X,
                _dragPreview.RealScreenPosition.Y,
                _dragPreview.CalculateDepthZ()
            );
        }

        /// <summary>
        /// Resolves where the held item would land on the ground, matching the drop targeting used
        /// by <see cref="GameSceneInputHandler"/>. Drops that target a container or a surface the
        /// item cannot rest on are rejected (no preview).
        /// </summary>
        private static bool TryGetDragDropTarget(
            GameObject target,
            ItemHold itemHold,
            out int x,
            out int y,
            out sbyte z
        )
        {
            x = 0;
            y = 0;
            z = 0;

            switch (target)
            {
                case Land land:
                    x = land.X;
                    y = land.Y;
                    z = land.Z;

                    return true;

                case Static:
                case Multi:
                    x = target.X;
                    y = target.Y;
                    z = target.Z;

                    ref StaticTiles staticData = ref Client.Game.UO.FileManager.TileData.StaticData[target.Graphic];

                    if (staticData.IsSurface)
                    {
                        z += (sbyte)(staticData.Height == 0xFF ? 0 : staticData.Height);
                    }

                    return true;

                case Item item:
                    if (item.ItemData.IsContainer)
                    {
                        return false;
                    }

                    if (
                        !item.ItemData.IsSurface
                        && !(item.ItemData.IsStackable && item.Graphic == itemHold.Graphic)
                    )
                    {
                        return false;
                    }

                    x = item.X;
                    y = item.Y;
                    z = item.Z;

                    if (item.ItemData.IsSurface)
                    {
                        z += (sbyte)(item.ItemData.Height == 0xFF ? 0 : item.ItemData.Height);
                    }

                    return true;

                default:
                    return false;
            }
        }
    }
}
