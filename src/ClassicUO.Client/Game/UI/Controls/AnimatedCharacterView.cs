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
    /// Renders an animated in-game character (body + equipment) playing its idle/stand animation
    /// in a chosen facing direction, without requiring a real Mobile/Item game object.
    /// Mirrors the frame math used by <see cref="GameObjects.Views.MobileView"/>.
    /// </summary>
    public class AnimatedCharacterView : Control
    {
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
        private readonly byte _direction; // logical 0-7 facing
        private readonly byte _animGroup;
        private readonly Dictionary<Layer, EquipEntry> _equipment;
        private readonly uint _frameDelayMs;

        private ulong _nextFrame;
        private int _frame;

        public AnimatedCharacterView(
            ushort bodyGraphic,
            ushort bodyHue,
            Dictionary<Layer, EquipEntry> equipment,
            byte direction,
            int width,
            int height,
            uint frameDelayMs = 150)
        {
            _bodyGraphic = bodyGraphic;
            _bodyHue = bodyHue;
            _equipment = equipment ?? new Dictionary<Layer, EquipEntry>();
            _direction = direction;
            _frameDelayMs = frameDelayMs;
            _animGroup = GetStandGroup(bodyGraphic);

            Width = width;
            Height = height;

            AcceptMouseInput = false;
            CanMove = false;
        }

        private static byte GetStandGroup(ushort graphic)
        {
            AnimationGroupsType groupType = Client.Game.UO.Animations.GetAnimType(graphic);

            switch (Client.Game.UO.FileManager.Animations.GetGroupIndex(graphic, groupType))
            {
                case AnimationGroups.Low: return (byte)LowAnimationGroup.Stand;
                case AnimationGroups.High: return (byte)HighAnimationGroup.Stand;
                case AnimationGroups.People: return (byte)PeopleAnimationGroup.Stand;
            }

            return (byte)PeopleAnimationGroup.Stand;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            if (_bodyGraphic == 0 || _bodyGraphic >= Client.Game.UO.Animations.MaxAnimationCount)
                return true;

            // Advance the idle loop over time (no dependency on PreDraw propagation).
            if (_nextFrame <= Time.Ticks)
            {
                _nextFrame = Time.Ticks + _frameDelayMs;
                _frame++;
            }

            byte layerDir = _direction;
            byte dir = _direction;
            bool mirror = false;
            Client.Game.UO.Animations.GetAnimDirection(ref dir, ref mirror);

            // Auto-fit scale from the body frame so the character fills the control box.
            Span<SpriteInfo> bodyFrames = Client.Game.UO.Animations.GetAnimationFrames(
                _bodyGraphic, _animGroup, dir, out _, out _, false);

            if (bodyFrames.Length == 0)
                return true;

            ref readonly SpriteInfo measure = ref bodyFrames[0];
            float scale = 1f;
            if (measure.UV.Width > 0 && measure.UV.Height > 0)
            {
                scale = Math.Min(Width / (float)measure.UV.Width, Height / (float)measure.UV.Height);
                scale = Math.Clamp(scale, 0.5f, 2.2f);
            }

            // Anchor the character's feet at the bottom-center of the box.
            int anchorX = x + Width / 2;
            int anchorY = y + Height - 2;

            // Body
            DrawLayer(batcher, _bodyGraphic, _bodyHue, false, false, dir, mirror, scale, anchorX, anchorY);

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

                DrawLayer(batcher, graphic, entry.Hue, entry.IsPartialHue, true, dir, mirror, scale, anchorX, anchorY);
            }

            return true;
        }

        private void DrawLayer(UltimaBatcher2D batcher, ushort graphic, ushort hue, bool partialHue, bool isEquip, byte dir, bool mirror, float scale, int anchorX, int anchorY)
        {
            if (graphic == 0 || graphic >= Client.Game.UO.Animations.MaxAnimationCount)
                return;

            Span<SpriteInfo> frames = Client.Game.UO.Animations.GetAnimationFrames(
                graphic, _animGroup, dir, out ushort hueFromFile, out _, isEquip);

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

            int w = (int)(sprite.UV.Width * scale);
            int h = (int)(sprite.UV.Height * scale);

            int dx = mirror
                ? anchorX - (int)((sprite.UV.Width - sprite.Center.X) * scale)
                : anchorX - (int)(sprite.Center.X * scale);

            int dy = anchorY - (int)((sprite.UV.Height + sprite.Center.Y) * scale);

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
