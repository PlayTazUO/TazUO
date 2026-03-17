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
    public const int ModernUIPanel_BorderSize = 13;

    /// <summary>
    /// Standard modern ui button. Used for a general button.
    /// See ModernUIButtonDown for "clicked" texture.
    /// Recommended to use with the NineSliceGump class.
    /// </summary>
    public static Texture2D ModernUIButtonUp { get { PNGLoader.Instance.TryGetEmbeddedTexture("TUOUIButtonUp.png", out Texture2D texture); return texture; } }
    public static Texture2D ModernUIButtonDown { get { PNGLoader.Instance.TryGetEmbeddedTexture("TUOUIButtonDown.png", out Texture2D texture); return texture; } }
    public static Texture2D ModernUIButtonDangerUp { get { PNGLoader.Instance.TryGetEmbeddedTexture("TUOUIButtonDangerUp.png", out Texture2D texture); return texture; } }
    public static Texture2D ModernUIButtonDangerDown { get { PNGLoader.Instance.TryGetEmbeddedTexture("TUOUIButtonDangerDown.png", out Texture2D texture); return texture; } }
    public static Texture2D ModernUICheckBoxChecked { get { PNGLoader.Instance.TryGetEmbeddedTexture("TUOUICheckBoxChecked.png", out Texture2D texture); return texture; } }
    public static Texture2D ModernUICheckBoxUnChecked { get { PNGLoader.Instance.TryGetEmbeddedTexture("TUOUICheckBoxUnChecked.png", out Texture2D texture); return texture; } }

    public static Texture2D ModernUISkillUp { get { PNGLoader.Instance.TryGetEmbeddedTexture("upicon.png", out Texture2D texture); return texture; } }
    public static Texture2D ModernUISkillDown { get { PNGLoader.Instance.TryGetEmbeddedTexture("downicon.png", out Texture2D texture); return texture; } }
    public static Texture2D ModernUISkillLock { get { PNGLoader.Instance.TryGetEmbeddedTexture("lockicon.png", out Texture2D texture); return texture; } }

    public const int ModernUIButton_BorderSize = 4;

    public static Texture2D ModernUIVerticalScrollbar { get { PNGLoader.Instance.TryGetEmbeddedTexture("scroll.png", out Texture2D texture); return texture; } }
    public static Texture2D ModernUIVerticalScrollbarKnob { get { PNGLoader.Instance.TryGetEmbeddedTexture("scroll-knob-vertical.png", out Texture2D texture); return texture; } }
}
