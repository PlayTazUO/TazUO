using ClassicUO.Assets;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Data;

public static class ModernUIConstants
{
    /// <summary>
    /// Standard Modern UI Panel. Used for a general gump background.
    /// Recommended to use with the NineSliceGump class.
    /// </summary>
    public static Texture2D ModernUIPanel { get { PNGLoader.Instance.TryGetEmbeddedTexture("TUOGumpBg.png", out Texture2D texture); return texture; } }

    /// <summary>
    /// Border size of the modern ui panel, used for the NineSliceGump class.
    /// </summary>
    public const int ModernUIPanel_BoderSize = 13;

    /// <summary>
    /// Standard modern ui button. Used for a general button.
    /// See ModernUIButtonDown for "clicked" texture.
    /// Recommended to use with the NineSliceGump class.
    /// </summary>
    public static Texture2D ModernUIButtonUp { get { PNGLoader.Instance.TryGetEmbeddedTexture("TUOUIButtonUp.png", out Texture2D texture); return texture; } }
    public static Texture2D ModernUIButtonDown { get { PNGLoader.Instance.TryGetEmbeddedTexture("TUOUIButtonDown.png", out Texture2D texture); return texture; } }

    public const int ModernUIButton_BorderSize = 4;
}
