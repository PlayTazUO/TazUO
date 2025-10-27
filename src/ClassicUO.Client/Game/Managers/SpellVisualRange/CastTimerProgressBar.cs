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
    private bool inCastingPhase;
    private DateTime phaseStartTime;
    private static readonly Vector3 CastingHue = ShaderHueTranslator.GetHueVector(0x005F); // 95
    private static readonly Vector3 RecoveryHue = ShaderHueTranslator.GetHueVector(0x0035); // 53
    private static readonly Vector3 EmptyHue = ShaderHueTranslator.GetHueVector(0x0026);
    private const int OffsetX = 27;   // 22 + 5
    private const int OffsetY = 15;
    private const int FlyingAdjust = 22;
    private const int MountedAdjust = 22;


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

    /// <summary>
    /// Initiates the casting phase progress visualization.
    /// </summary>
    public void OnSpellCastBegin() => MainThreadQueue.InvokeOnMainThread(() =>
    {
        phaseStartTime = DateTime.Now;
        inCastingPhase = true;
        IsVisible = true;
    });

    /// <summary>
    /// Initiates the recovery phase progress visualization after a spell cast completes.
    /// </summary>
    /// <param name="spell">The spell that just finished casting.</param>
    public void OnRecoveryBegin() => MainThreadQueue.InvokeOnMainThread(() =>
        {
            phaseStartTime = DateTime.Now;
            inCastingPhase = false;
            IsVisible = true;
        });

    private void DrawProgressBar(UltimaBatcher2D batcher, int x, int y, double percent, Vector3 fillHue)
    {
        Mobile m = World.Player;
        Client.Game.UO.Animations.GetAnimationDimensions(
            m.AnimIndex, m.GetGraphicForAnimation(), 0, 0, m.IsMounted, 0,
            out int centerX, out int centerY, out int width, out int height
        );

        WorldViewportGump vp = UIManager.GetGump<WorldViewportGump>();
        if (vp == null)
            return;

        x = vp.Location.X + (int)(m.RealScreenPosition.X - (m.Offset.X + OffsetX));
        y = vp.Location.Y + (int)(m.RealScreenPosition.Y - ((m.Offset.Y - m.Offset.Z) - (height + centerY + OffsetY) +
            (m.IsGargoyle && m.IsFlying ? -FlyingAdjust : !m.IsMounted ? MountedAdjust : 0)));

        if (background == null || foreground == null)
            return;

        batcher.Draw(background, new Rectangle(x, y, barBounds.Width, barBounds.Height), barBounds, EmptyHue);

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

    public override bool Draw(UltimaBatcher2D batcher, int x, int y)
    {
        if (World?.Player == null || SpellVisualRangeManager.Instance == null)
        {
            IsVisible = false;
            return false;
        }

        SpellRangeInfo spell = SpellVisualRangeManager.Instance.GetCurrentSpell();
        if (spell == null)
        {
            IsVisible = false;
            return false;
        }

        IsVisible = true;

        double totalTime = inCastingPhase ? spell.GetEffectiveCastTime() : spell.GetEffectiveRecoveryTime();
        if (totalTime <= 0)
        {
            IsVisible = false;
            return false;
        }
        double elapsed = (DateTime.Now - phaseStartTime).TotalSeconds;
        double percent = Math.Min(elapsed / totalTime, 1.0);

        if (percent >= 1.0)
        {
            IsVisible = false;
            return false;
        }
        IsVisible = true;
        Vector3 drawHue = inCastingPhase ? CastingHue : RecoveryHue;
        DrawProgressBar(batcher, x, y, percent, drawHue);

        return base.Draw(batcher, x, y);
    }
}
