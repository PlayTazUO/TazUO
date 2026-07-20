#nullable enable
using ClassicUO.Renderer;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
/// A Myra Image widget that displays a UO land (terrain) graphic by graphic ID.
/// Uses the correct UV sub-rectangle from the texture atlas so that only
/// the target sprite is rendered. The atlas Texture2D is NOT owned here and
/// must never be disposed — Myra's Image widget does not implement IDisposable,
/// so there is no disposal risk.
/// </summary>
public class MyraLandTexture : Image
{
    public MyraLandTexture(uint graphic, int maxSize = 36)
    {
        SpriteInfo landInfo = Client.Game.UO.Arts.GetLand(graphic);

        if (landInfo.Texture != null)
        {
            // landInfo.UV is the sub-rectangle within the shared atlas texture.
            // Passing just the Texture2D would render the entire atlas page;
            // supplying landInfo.UV scopes it to only this sprite.
            Renderable = new TextureRegion(landInfo.Texture, landInfo.UV);
        }

        MaxWidth = maxSize;
        MaxHeight = maxSize;
    }
}
