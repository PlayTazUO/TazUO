using System;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Managers.EnhancedMap;

/// <summary>
/// Draws EnhancedMap alerts over the game world at their reported positions. Purely a visual
/// layer: it takes no mouse or keyboard input and is never persisted.
/// </summary>
/// <remarks>
/// Positions come from tile deltas converted with the same isometric step the client uses
/// (<c>22</c> half-tile units) and then run through the camera transform. Alerts carry no Z or
/// entity, so markers sit at ground level, which is accurate enough for an overlay.
/// </remarks>
internal sealed class EnhancedMapOverlayGump : Gump
{
    private const int TileStep = 22;
    private const int DotSize = 5;
    private const int DotHalf = DotSize / 2;

    public EnhancedMapOverlayGump(World world) : base(world, 0, 0)
    {
        CanMove = false;
        CanCloseWithRightClick = false;
        AcceptMouseInput = false;
        AcceptKeyboardInput = false;
        WantUpdateSize = false;
    }

    /// <inheritdoc/>
    public override bool ShouldBeSaved => false;

    public override bool Draw(UltimaBatcher2D batcher, int x, int y)
    {
        World world = World;
        GameScene scene = Client.Game?.GetScene<GameScene>();

        if (world?.Player == null || scene?.Camera == null || EnhancedMapManager.Config is not { ShowOverlay: true })
            return false;

        EnhancedMapManager manager = EnhancedMapManager.Instance;
        Camera camera = scene.Camera;
        Vector3 hue = ShaderHueTranslator.GetHueVector(0);

        foreach (EnhancedMapAlert alert in manager.Alerts.Values)
        {
            if (TryProject(world, camera, alert.X, alert.Y, out Point screen))
            {
                DrawMarker(batcher, camera, screen, Color.Red, hue);
                DrawLabel(batcher, camera, screen, alert.Name);
            }
        }

        return true;
    }

    /// <summary>Projects world tile coordinates to screen space and rejects off-camera positions.</summary>
    private static bool TryProject(World world, Camera camera, int worldX, int worldY, out Point screen)
    {
        int deltaX = world.Player.X - worldX;
        int deltaY = world.Player.Y - worldY;

        int px = (camera.Bounds.Width >> 1) - (deltaX - deltaY) * TileStep;
        int py = (camera.Bounds.Height >> 1) - (deltaX + deltaY) * TileStep;

        px -= (int)world.Player.Offset.X;
        py -= (int)(world.Player.Offset.Y - world.Player.Offset.Z);

        screen = camera.WorldToScreen(new Point(px, py));
        screen.X += camera.Bounds.X;
        screen.Y += camera.Bounds.Y;

        return screen.X >= camera.Bounds.X
               && screen.X <= camera.Bounds.Right
               && screen.Y >= camera.Bounds.Y
               && screen.Y <= camera.Bounds.Bottom;
    }

    private static void DrawMarker(UltimaBatcher2D batcher, Camera camera, Point screen, Color color, Vector3 hue)
    {
        if (screen.X < camera.Bounds.X || screen.X + DotSize > camera.Bounds.Right)
            return;

        if (screen.Y < camera.Bounds.Y || screen.Y + DotSize > camera.Bounds.Bottom)
            return;

        batcher.Draw(
            SolidColorTextureCache.GetTexture(color),
            new Rectangle(screen.X - DotHalf, screen.Y - DotHalf, DotSize, DotSize),
            hue
        );
    }

    private static void DrawLabel(UltimaBatcher2D batcher, Camera camera, Point screen, string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        int tx = Math.Clamp(screen.X + DotHalf + 2, camera.Bounds.X, Math.Max(camera.Bounds.X, camera.Bounds.Right - 1));
        int ty = Math.Clamp(screen.Y - 8, camera.Bounds.Y, Math.Max(camera.Bounds.Y, camera.Bounds.Bottom - 1));

        batcher.DrawString(Fonts.Regular, text, tx, ty, ShaderHueTranslator.GetHueVector(0));
    }
}
