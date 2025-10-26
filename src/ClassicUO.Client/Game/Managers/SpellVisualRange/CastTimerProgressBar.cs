using ClassicUO;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Renderer;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using static ClassicUO.Game.Managers.SpellVisualRangeManager;

public class CastTimerProgressBar : Gump
{
    private Rectangle barBounds, barBoundsF;
    private Texture2D background;
    private Texture2D foreground;
    private bool inCastingPhase = true;
    private DateTime phaseStartTime;

    public CastTimerProgressBar(World world) : base(world, 0, 0)
    {
        CanMove = false;
        AcceptMouseInput = false;
        CanCloseWithEsc = false;
        CanCloseWithRightClick = false;

        ref readonly var gi = ref Client.Game.UO.Gumps.GetGump(0x0805);
        background = gi.Texture;
        barBounds = gi.UV;

        gi = ref Client.Game.UO.Gumps.GetGump(0x0806);
        foreground = gi.Texture;
        barBoundsF = gi.UV;

        inCastingPhase = false;
        IsVisible = false;
    }

    public void OnSpellCastBegin()
    {
        phaseStartTime = DateTime.Now;
        inCastingPhase = true;
        IsVisible = true;
    }

    public override bool Draw(UltimaBatcher2D batcher, int x, int y)
    {
        SpellRangeInfo spell = Instance.GetCurrentSpell();
        if (spell == null)
        {
            IsVisible = false;
            return false;
        }

        IsVisible = true;

        double totalTime = inCastingPhase ? spell.GetEffectiveCastTime() : spell.GetEffectiveRecoveryTime();
        double elapsed = (DateTime.Now - phaseStartTime).TotalSeconds;
        double percent = Math.Min(elapsed / totalTime, 1.0);

        if (percent >= 1.0)
        {
            if (!inCastingPhase)
                IsVisible = false;

            return false;
        }

        Vector3 drawHue = inCastingPhase
            ? ShaderHueTranslator.GetHueVector(0x005F)
            : ShaderHueTranslator.GetHueVector(0x0035);
        DrawProgressBar(batcher, x, y, percent, drawHue);

        return base.Draw(batcher, x, y);
    }

    public void OnRecoveryBegin(SpellRangeInfo spell)
    {
        phaseStartTime = DateTime.Now;
        inCastingPhase = false;
        IsVisible = true;
    }

    private void DrawProgressBar(UltimaBatcher2D batcher, int x, int y, double percent, Vector3 fillHue)
    {
        Mobile m = World.Player;
        Client.Game.UO.Animations.GetAnimationDimensions(
            m.AnimIndex, m.GetGraphicForAnimation(), 0, 0, m.IsMounted, 0,
            out int centerX, out int centerY, out int width, out int height
        );

        WorldViewportGump vp = UIManager.GetGump<WorldViewportGump>();
        x = vp.Location.X + (int)(m.RealScreenPosition.X - (m.Offset.X + 22 + 5));
        y = vp.Location.Y + (int)(m.RealScreenPosition.Y - ((m.Offset.Y - m.Offset.Z) - (height + centerY + 15) +
            (m.IsGargoyle && m.IsFlying ? -22 : !m.IsMounted ? 22 : 0)));

        if (background == null || foreground == null)
            return;

        Vector3 emptyHue = ShaderHueTranslator.GetHueVector(0x0026);
        batcher.Draw(background, new Rectangle(x, y, barBounds.Width, barBounds.Height), barBounds, emptyHue);

        int widthFromPercent = (int)(barBounds.Width * percent);
        if (widthFromPercent > 0)
        {
            batcher.DrawTiled(
                foreground,
                new Rectangle(x, y, widthFromPercent, barBoundsF.Height),
                barBoundsF,
                fillHue
            );
        }
    }
}
