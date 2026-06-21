#nullable enable
using ClassicUO.Renderer;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
/// A Myra Image widget that displays a UO texmap (the stretched land texture)
/// by texture ID. Uses the correct UV sub-rectangle from the texture atlas so
/// that only the target sprite is rendered. The atlas Texture2D is NOT owned
/// here and must never be disposed — Myra's Image widget does not implement
/// IDisposable, so there is no disposal risk.
/// </summary>
public class MyraTexmapTexture : Image
{
    public MyraTexmapTexture(uint texId, int maxSize = 36)
    {
        SpriteInfo texInfo = Client.Game.UO.Texmaps.GetTexmap(texId);

        if (texInfo.Texture != null)
        {
            // texInfo.UV is the sub-rectangle within the shared atlas texture.
            // Passing just the Texture2D would render the entire atlas page;
            // supplying texInfo.UV scopes it to only this sprite.
            Renderable = new TextureRegion(texInfo.Texture, texInfo.UV);
        }

        MaxWidth = maxSize;
        MaxHeight = maxSize;
    }
}
