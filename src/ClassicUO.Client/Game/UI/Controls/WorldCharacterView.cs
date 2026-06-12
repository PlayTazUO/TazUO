using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Controls
{
    /// <summary>
    /// Renders a real in-game <see cref="Mobile"/> (body + equipped <see cref="Item"/>s) through the
    /// actual <see cref="Mobile"/> draw path, at native mobile size, for the login character preview.
    /// The mobile is created in the supplied (login) world and removed again on dispose. It uses the
    /// mobile's built-in idle/fidget animation (driven via <see cref="Mobile.Update"/>) and turns to
    /// face the viewer (down) while selected.
    /// </summary>
    public class WorldCharacterView : Control
    {
        private readonly byte _baseDirection;
        private Mobile _mobile;

        public WorldCharacterView(World world, LemCharData lem, byte baseDirection, int width, int height, uint previewIndex)
        {
            _baseDirection = baseDirection;

            Width = width;
            Height = height;
            AcceptMouseInput = false;
            CanMove = false;

            _mobile = BuildMobile(world, lem, baseDirection, previewIndex);
        }

        private static Mobile BuildMobile(World world, LemCharData lem, byte baseDirection, uint previewIndex)
        {
            // Dedicated preview serial ranges: mobiles stay in the mobile range (< 0x40000000),
            // items in the item range (>= 0x40000000), distinct from char-creation's 0x4000_0000 items.
            Mobile mobile = world.GetOrCreateMobile(0x3F00_0000u + previewIndex);
            mobile.Graphic = lem.PlayerGraphic;
            mobile.Hue = lem.BodyHue;
            // Mobiles are normally ramped to opaque by the world's ProcessAlpha; preview mobiles
            // never run through it, so force full opacity or only their shadow would render.
            mobile.AlphaHue = 255;

            if (lem.IsFemale)
                mobile.Flags |= Flags.Female;
            else
                mobile.Flags &= ~Flags.Female;

            mobile.Direction = (Direction)baseDirection;

            foreach (System.Collections.Generic.KeyValuePair<Layer, LemEquipmentEntry> kvp in lem.Equipment)
            {
                Layer layer = kvp.Key;
                LemEquipmentEntry e = kvp.Value;

                if (e.Graphic == 0 || layer == Layer.Mount)
                    continue;

                Item item = world.GetOrCreateItem(0x7F00_0000u + (previewIndex << 8) + (uint)layer);
                item.Graphic = e.Graphic; // item graphic; ItemData.AnimID is derived from this
                item.Hue = e.Hue;
                item.Layer = layer;
                item.Container = mobile.Serial;
                mobile.PushToBack(item);
            }

            return mobile;
        }

        /// <summary>Selected characters turn to face the viewer (down).</summary>
        public void SetSelected(bool value)
        {
            if (_mobile != null && !_mobile.IsDestroyed)
                _mobile.Direction = value ? Direction.Down : (Direction)_baseDirection;

            if (value)
            {
                _mobile.SetAnimation(
                    Mobile.GetReplacedObjectAnimation(_mobile.Graphic, (ushort)PeopleAnimationGroup.WalkArmed),
                    interval: 0,
                    frameCount: 0,
                    repeatCount: 0,
                    repeat: false,
                    forward: true,
                    fromServer: true);
            } else
            {
                _mobile.SetIdleAnimation();
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            if (_mobile == null || _mobile.IsDestroyed)
                return true;

            // Drive the mobile's animation (idle/fidget) each frame from Draw, which is guaranteed
            // to run (Control.Update early-returns for childless controls).
            _mobile.Update();

            // Anchor the character's feet at the bottom-center of the box. MobileView.Draw applies
            // posY -= 3 and +22 offsets internally, so undo them here.
            int posX = x + Width / 2 - 22;
            int posY = y + Height - 22 + 3;

            _mobile.Draw(batcher, posX, posY, 0f);

            return true;
        }

        public override void Dispose()
        {
            if (_mobile != null && !_mobile.IsDestroyed)
                _mobile.World.RemoveMobile(_mobile.Serial, true);

            _mobile = null;

            base.Dispose();
        }
    }
}
