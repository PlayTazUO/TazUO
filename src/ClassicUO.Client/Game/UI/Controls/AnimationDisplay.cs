using System;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls;

public class AnimationDisplay : Control
{
    private ushort _graphic;
    public ushort Graphic => _graphic;

    private readonly uint _playspeedMs;

    private ulong _nextFrame = 0;

    private readonly int _mWidth;
    private readonly int _mHeight;

    private byte _animGroup;

    private ushort _lastFrame = 0;

    private Vector3 _hueVector;
    public bool DrawBorder { get; set; }

    public AnimationDisplay(ushort graphic, int width = 100, int height = 100, uint playspeedMs = 650)
    {
        _mWidth = width;
        _mHeight = height;
        UpdateGraphic(graphic);
        _playspeedMs = playspeedMs;
        Width = width;
        Height = height;
    }

    public void UpdateGraphic(ushort graphic)
    {
        if (graphic >= Client.Game.UO.Animations.MaxAnimationCount)
            graphic = 0;

        _graphic = graphic;
        _animGroup = GetAnimGroup(graphic);

        Client.Game.UO.Animations.GetAnimationFrames(graphic, _animGroup, 1, out ushort hue2, out _, true);
        _hueVector = ShaderHueTranslator.GetHueVector(hue2, Client.Game.UO.FileManager.TileData.StaticData[_graphic].IsPartialHue, 1f);
    }

    private static byte GetAnimGroup(ushort graphic)
    {
        AnimationGroupsType groupType = Client.Game.UO.Animations.GetAnimType(graphic);

        switch (Client.Game.UO.FileManager.Animations.GetGroupIndex(graphic, groupType))
        {
            case AnimationGroups.Low: return (byte)LowAnimationGroup.Stand;

            case AnimationGroups.High: return (byte)HighAnimationGroup.Stand;

            case AnimationGroups.People: return (byte)PeopleAnimationGroup.Stand;
        }

        return 0;
    }

    public override void PreDraw()
    {
        base.PreDraw();

        if (_nextFrame <= Time.Ticks)
        {
            _nextFrame = Time.Ticks + _playspeedMs;
            _lastFrame++;
        }
    }

    public override bool Draw(UltimaBatcher2D batcher, int x, int y)
    {
        base.Draw(batcher, x, y);

        Span<SpriteInfo> frames = Client.Game.UO.Animations.GetAnimationFrames(_graphic, _animGroup, 1, out ushort hue2, out _, true);

        if (frames.Length == 0)
            return true;

        if (_lastFrame >= frames.Length)
            _lastFrame = 0;

        ref SpriteInfo spriteInfo = ref frames[_lastFrame];

        if (spriteInfo.Texture != null)
            batcher.Draw(spriteInfo.Texture, new Rectangle(x, y, Math.Min(spriteInfo.UV.Width, _mWidth), Math.Min(spriteInfo.UV.Height, _mHeight)), spriteInfo.UV, _hueVector);

        if (DrawBorder)
            batcher.DrawRectangle(SolidColorTextureCache.GetTexture(Color.Gray), x, y, Width - 1, Height - 1, ShaderHueTranslator.GetHueVector(0, false, Alpha));

        return true;
    }
}
