// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Controls
{
    /// <summary>
    /// Renders an animated in-game character (body + equipment) at actual mobile size.
    /// The character stands still and occasionally plays its idle/fidget animation once, and
    /// turns to face the viewer (down) while selected. Mirrors the frame math used by
    /// <see cref="GameObjects.Views.MobileView"/>.
    /// </summary>
    public class AnimatedCharacterView : Control
    {
        // Player characters are always humanoid, so use the People groups directly
        // (the generic group-classification helper misclassifies some bodies).
        private const byte STAND_GROUP = 4; // PeopleAnimationGroup.Stand
        private const byte IDLE_GROUP = 5;  // PeopleAnimationGroup.Fidget1

        private const float RENDER_SCALE = 1f; // actual in-game mobile size

        public readonly struct EquipEntry
        {
            public readonly ushort AnimID;
            public readonly ushort Hue;
            public readonly bool IsPartialHue;

            public EquipEntry(ushort animID, ushort hue, bool isPartialHue)
            {
                AnimID = animID;
                Hue = hue;
                IsPartialHue = isPartialHue;
            }
        }

        private readonly ushort _bodyGraphic;
        private readonly ushort _bodyHue;
        private readonly byte _baseDirection; // logical 0-7 facing when not selected
        private readonly Dictionary<Layer, EquipEntry> _equipment;
        private readonly uint _frameDelayMs;

        private bool _selected;
        private bool _playingIdle;
        private int _frame;
        private ulong _nextFrame;
        private ulong _nextIdle;

        public AnimatedCharacterView(
            ushort bodyGraphic,
            ushort bodyHue,
            Dictionary<Layer, EquipEntry> equipment,
            byte direction,
            int width,
            int height,
            uint frameDelayMs = 120)
        {
            _bodyGraphic = bodyGraphic;
            _bodyHue = bodyHue;
            _equipment = equipment ?? new Dictionary<Layer, EquipEntry>();
            _baseDirection = direction;
            _frameDelayMs = frameDelayMs;
            _nextIdle = Time.Ticks + RandomIdleDelay();

            Width = width;
            Height = height;

            AcceptMouseInput = false;
            CanMove = false;
        }

        /// <summary>Selected characters turn to face the viewer (down).</summary>
        public void SetSelected(bool value) => _selected = value;

        private static uint RandomIdleDelay() => (uint)Random.Shared.Next(0, 10001); // 0-10s

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            if (_bodyGraphic == 0 || _bodyGraphic >= Client.Game.UO.Animations.MaxAnimationCount)
                return true;

            ulong now = Time.Ticks;

            // Kick off an idle play occasionally; otherwise stay frozen on the stand pose.
            if (!_playingIdle && now >= _nextIdle)
            {
                _playingIdle = true;
                _frame = 0;
                _nextFrame = now + _frameDelayMs;
            }

            byte group = _playingIdle ? IDLE_GROUP : STAND_GROUP;

            byte direction = _selected ? (byte)Direction.Down : _baseDirection;
            byte layerDir = direction;
            byte dir = direction;
            bool mirror = false;
            Client.Game.UO.Animations.GetAnimDirection(ref dir, ref mirror);

            Span<SpriteInfo> bodyFrames = Client.Game.UO.Animations.GetAnimationFrames(
                _bodyGraphic, group, dir, out _, out _, false);

            if (bodyFrames.Length == 0)
                return true;

            // Advance the one-shot idle, reverting to standing when it completes.
            if (_playingIdle && now >= _nextFrame)
            {
                _nextFrame = now + _frameDelayMs;
                _frame++;

                if (_frame >= bodyFrames.Length)
                {
                    _playingIdle = false;
                    _frame = 0;
                    _nextIdle = now + RandomIdleDelay();
                }
            }

            // Anchor the character's feet at the bottom-center of the box.
            int anchorX = x + Width / 2;
            int anchorY = y + Height - 2;

            // Body
            DrawLayer(batcher, _bodyGraphic, _bodyHue, false, false, group, dir, mirror, anchorX, anchorY);

            // Equipment, in the screen-correct order for this direction.
            for (int i = 0; i < Constants.USED_LAYER_COUNT; i++)
            {
                Layer layer = LayerOrder.UsedLayers[layerDir, i];

                if (!_equipment.TryGetValue(layer, out EquipEntry entry) || entry.AnimID == 0)
                    continue;

                ushort graphic = entry.AnimID;

                if (Client.Game.UO.FileManager.Animations.EquipConversions.TryGetValue(
                        _bodyGraphic, out Dictionary<ushort, EquipConvData> map)
                    && map.TryGetValue(entry.AnimID, out EquipConvData data))
                {
                    graphic = data.Graphic;
                }

                DrawLayer(batcher, graphic, entry.Hue, entry.IsPartialHue, true, group, dir, mirror, anchorX, anchorY);
            }

            return true;
        }

        private void DrawLayer(UltimaBatcher2D batcher, ushort graphic, ushort hue, bool partialHue, bool isEquip, byte group, byte dir, bool mirror, int anchorX, int anchorY)
        {
            if (graphic == 0 || graphic >= Client.Game.UO.Animations.MaxAnimationCount)
                return;

            Span<SpriteInfo> frames = Client.Game.UO.Animations.GetAnimationFrames(
                graphic, group, dir, out ushort hueFromFile, out _, isEquip);

            if (frames.Length == 0)
                return;

            ref readonly SpriteInfo sprite = ref frames[_frame % frames.Length];

            if (sprite.Texture == null)
                return;

            ushort finalHue = hue;
            bool finalPartial = partialHue;

            if ((finalHue & 0x8000) != 0)
            {
                finalPartial = true;
                finalHue &= 0x7FFF;
            }

            if (finalHue == 0)
            {
                finalHue = hueFromFile;
                finalPartial = false;
            }

            Vector3 hueVec = ShaderHueTranslator.GetHueVector(finalHue, finalPartial, Alpha, true);

            int w = (int)(sprite.UV.Width * RENDER_SCALE);
            int h = (int)(sprite.UV.Height * RENDER_SCALE);

            int dx = mirror
                ? anchorX - (int)((sprite.UV.Width - sprite.Center.X) * RENDER_SCALE)
                : anchorX - (int)(sprite.Center.X * RENDER_SCALE);

            int dy = anchorY - (int)((sprite.UV.Height + sprite.Center.Y) * RENDER_SCALE);

            batcher.Draw(
                sprite.Texture,
                new Rectangle(dx, dy, w, h),
                sprite.UV,
                hueVec,
                0f,
                Vector2.Zero,
                mirror ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                0f);
        }
    }
}
