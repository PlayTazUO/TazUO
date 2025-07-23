using ClassicUO.Assets;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Data;

public static class ModernUIConstants
{
    /// <summary>
    /// Standard Modern UI Panel. Used for a general gump background.
    /// Recommended to use with the NineSliceGump class.
    /// </summary>
    public static Texture2D ModernUIPanel => PNGLoader.Instance.EmbeddedArt["TUOGumpBg.png"];

    /// <summary>
    /// Border size of the modern ui panel, used for the NineSliceGump class.
    /// </summary>
    public const int ModernUIPanel_BoderSize = 13;

    /// <summary>
    /// Standard modern ui button. Used for a general button.
    /// See ModernUIButtonDown for "clicked" texture.
    /// Recommended to use with the NineSliceGump class.
    /// </summary>
    public static Texture2D ModernUIButtonUp => PNGLoader.Instance.EmbeddedArt["TUOUIButtonUp.png"];
    public static Texture2D ModernUIButtonDown => PNGLoader.Instance.EmbeddedArt["TUOUIButtonDown.png"];

    public const int ModernUIButton_BorderSize = 4;
}
