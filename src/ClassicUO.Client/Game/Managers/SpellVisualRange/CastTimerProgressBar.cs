using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using ClassicUO.Game.Data;

namespace ClassicUO.Game.Managers.SpellVisualRange;

/// <summary>
/// Displays a progress bar tracking spell casting and recovery phases.
/// </summary>
public class CastTimerProgressBar : Gump
{
    private Rectangle barBounds, barBoundsF;
    private Texture2D background;
    private Texture2D foreground;
    private Vector3 hue = ShaderHueTranslator.GetHueVector(0);


    public CastTimerProgressBar(World world) : base(world, 0, 0)
    {
        CanMove = false;
        AcceptMouseInput = false;
        CanCloseWithEsc = false;
        CanCloseWithRightClick = false;

        ref readonly SpriteInfo gi = ref Client.Game.UO.Gumps.GetGump(0x0805);
        background = gi.Texture;
        barBounds = gi.UV;

        gi = ref Client.Game.UO.Gumps.GetGump(0x0806);
        foreground = gi.Texture;
        barBoundsF = gi.UV;
    }

    public override bool Draw(UltimaBatcher2D batcher, int x, int y)
    {
        if (SpellVisualRangeManager.Instance.IsCastingWithoutTarget())
        {
            SpellRangeInfo i = SpellVisualRangeManager.Instance.GetCurrentSpell();
            if (i != null)
            {
                if (i.CastTime > 0)
                {
                    if (background != null && foreground != null)
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

                        WorldViewportGump vp = UIManager.GetGump<WorldViewportGump>();

                        x = vp.Location.X + (int)(m.RealScreenPosition.X - (m.Offset.X + 22 + 5));
                        y = vp.Location.Y + (int)(m.RealScreenPosition.Y - ((m.Offset.Y - m.Offset.Z) - (height + centerY + 15) + (m.IsGargoyle && m.IsFlying ? -22 : !m.IsMounted ? 22 : 0)));

                        batcher.Draw(background, new Rectangle(x, y, barBounds.Width, barBounds.Height), barBounds, hue);

                        double percent = (DateTime.Now - SpellVisualRangeManager.Instance.LastSpellTime).TotalSeconds / i.GetEffectiveCastTime();

                        int widthFromPercent = (int)(barBounds.Width * percent);
                        widthFromPercent = widthFromPercent > barBounds.Width ? barBounds.Width : widthFromPercent; //Max width is the bar width

                        if (widthFromPercent > 0)
                        {
                            batcher.DrawTiled(foreground, new Rectangle(x, y, widthFromPercent, barBoundsF.Height), barBoundsF, hue);
                        }

                        if (percent <= 0 && i.FreezeCharacterWhileCasting)
                        {
                            World.Player.Flags &= ~Flags.Frozen;
                        }
                    }
                }
            }
        }
        return base.Draw(batcher, x, y);
    }
}
