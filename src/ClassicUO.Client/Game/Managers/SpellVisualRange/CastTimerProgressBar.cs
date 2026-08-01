using System;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Managers.SpellVisualRange;

public class CastTimerProgressBar : Gump
{
    private readonly Rectangle _barBounds;
    private readonly Rectangle _barBoundsF;
    private readonly Texture2D _background;
    private readonly Texture2D _foreground;
    private readonly Vector3 _hue = ShaderHueTranslator.GetHueVector(0);


    public CastTimerProgressBar(World world) : base(world, 0, 0)
    {
        CanMove = false;
        AcceptMouseInput = false;
        CanCloseWithEsc = false;
        CanCloseWithRightClick = false;

        ref readonly SpriteInfo gi = ref Client.Game.UO.Gumps.GetGump(0x0805);
        _background = gi.Texture;
        _barBounds = gi.UV;

        gi = ref Client.Game.UO.Gumps.GetGump(0x0806);
        _foreground = gi.Texture;
        _barBoundsF = gi.UV;
    }

    public override bool Draw(UltimaBatcher2D batcher, int x, int y)
    {
        if (SpellVisualRangeManager.Instance.IsCastingWithoutTarget())
        {
            SpellRangeInfo i = SpellVisualRangeManager.Instance.GetCurrentSpell();
            if (i == null || !(i.CastTime > 0)) return base.Draw(batcher, x, y);

            if (_background != null && _foreground != null)
            {
                Mobile m = World.Player;
                Client.Game.UO.Animations.GetAnimationDimensions(
                    m.AnimIndex,
                    m.GetGraphicForAnimation(),
                    0,
                    0,
                    m.IsMounted,
                    0,
                    out int centerX,
                    out int centerY,
                    out int width,
                    out int height
                );

                WorldViewportGump vp = WorldViewportGump.Instance;

                if (vp == null) return false;

                // Follow the player's exact interpolated screen position (RealScreenPosition + Offset),
                // matching how the mobile itself is drawn. Adding Offset keeps the bar glued to the
                // smooth walk animation; using only the tile-based RealScreenPosition made it jump.
                x = vp.Location.X + (int)(m.RealScreenPosition.X + m.Offset.X - (22 + 5));
                y = vp.Location.Y + (int)(m.RealScreenPosition.Y + (m.Offset.Y - m.Offset.Z) + (height + centerY + 15) - (m.IsGargoyle && m.IsFlyingAnimationEnabled ? -22 : !m.IsMounted ? 22 : 0));

                batcher.Draw(_background, new Rectangle(x, y, _barBounds.Width, _barBounds.Height), _barBounds, _hue);

                double percent = (DateTime.Now - SpellVisualRangeManager.Instance.LastSpellTime).TotalSeconds / i.CastTime;

                int widthFromPercent = (int)(_barBounds.Width * percent);
                widthFromPercent = widthFromPercent > _barBounds.Width ? _barBounds.Width : widthFromPercent; //Max width is the bar width

                if (widthFromPercent > 0)
                {
                    batcher.DrawTiled(_foreground, new Rectangle(x, y, widthFromPercent, _barBoundsF.Height), _barBoundsF, _hue);
                }

                if (percent <= 0 && i.FreezeCharacterWhileCasting)
                {
                    World.Player.Flags &= ~Flags.Frozen;
                }
            }
        }
        return base.Draw(batcher, x, y);
    }
}
