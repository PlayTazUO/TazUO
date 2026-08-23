namespace ClassicUO.Game.UI.MyraWindows.Widgets.ArtTexture;

/// <summary>Identifies one baked hue variant of an art graphic.</summary>
/// <param name="Graphic">Art graphic ID, without the 0x4000 offset.</param>
/// <param name="Hue">UO hue baked into the pixels.</param>
/// <param name="PartialHue">Whether the bake recolored only the sprite's gray texels.</param>
internal readonly record struct HuedArtKey(uint Graphic, ushort Hue, bool PartialHue);
