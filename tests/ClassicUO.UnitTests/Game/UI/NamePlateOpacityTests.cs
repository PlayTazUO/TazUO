using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Xunit;

namespace ClassicUO.UnitTests.Game.UI;

[CollectionDefinition("Nameplate drawing", DisableParallelization = true)]
public class NamePlateDrawingCollection;

[Collection("Nameplate drawing")]
public class NamePlateOpacityTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void ZeroOpacityLeavesTheWorldVisible(int radius)
    {
        float[,] pixels = Draw(radius, 0, 0, 0, true);
        foreach (float alpha in pixels)
            Assert.Equal(0, alpha);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 0.5)]
    [InlineData(0, 1)]
    [InlineData(6, 0)]
    [InlineData(6, 0.5)]
    [InlineData(6, 1)]
    public void HalfOpacityDoesNotStackAnOpaqueUnderlayOrMissingFill(int radius, double percent)
    {
        float[,] pixels = Draw(radius, 0, 50, 50, true, percent, applyOverallOpacity: false);
        Assert.Equal(0.5f, pixels[20, 12]); // Filled HP.
        Assert.Equal(0.5f, pixels[80, 12]); // Missing HP.
        Assert.Equal(0.5f, pixels[50, 0]); // Shared nameplate/resource outline.
        foreach (float alpha in pixels)
            Assert.InRange(alpha, 0, 0.5f);
    }

    [Fact]
    public void CombinedHealthBarOpacityControlsTheBackingSurface()
    {
        float[,] pixels = Draw(0, 25, 0, 0, true);
        Assert.Equal(0, pixels[20, 12]);
        Assert.Equal(0, pixels[80, 12]);

        pixels = Draw(0, 25, 50, 0, true);
        Assert.Equal(0.234375f, pixels[20, 12]);
        Assert.Equal(0.234375f, pixels[80, 12]);

        pixels = Draw(0, 0, 50, 0, true);
        Assert.Equal(0, pixels[20, 12]);
        Assert.Equal(0, pixels[80, 12]);
    }

    [Fact]
    public void LegacyMissingHealthShowsOnlyTheConfiguredBackground()
    {
        float[,] pixels = Draw(0, 25, 50, 50, false);
        Assert.Equal(0.234375f, pixels[20, 12]);
        Assert.Equal(0.125f, pixels[80, 12]);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 6)]
    [InlineData(true, 0)]
    [InlineData(true, 6)]
    public void PlayerAndPartyResourceRowsHonorOpacity(bool split, int radius)
    {
        float[,] pixels = Draw(radius, 0, 50, 50, true, barCount: 3, split: split, applyOverallOpacity: false);
        int offset = split ? 24 : 0;
        for (int row = 0; row < 3; row++)
        {
            Assert.Equal(0.5f, pixels[20, offset + row * 8 + 4]);
            Assert.Equal(0.5f, pixels[80, offset + row * 8 + 4]);
        }

        foreach (float alpha in pixels)
            Assert.InRange(alpha, 0, 0.5f);
    }

    // Record real batcher quads before GPU submission. No graphics device or game
    // assets are needed to check the geometry and alpha sent by the nameplate renderer.
    private static float[,] Draw(int radius, byte background, byte health, byte border, bool showMissing,
        double percent = 0.5d, int barCount = 1, bool split = false, bool applyOverallOpacity = true)
    {
        var profile = new Profile
        {
            NamePlateCornerRadius = radius,
            NamePlateOpacity = background,
            NamePlateHealthBarOpacity = health,
            NamePlateBorderOpacity = border,
            NamePlateShowMissingHealth = showMissing
        };
        PropertyInfo currentProfile = typeof(ProfileManager).GetProperty(nameof(ProfileManager.CurrentProfile));
        Profile previousProfile = ProfileManager.CurrentProfile;
        var texture = (Texture2D)RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
        GC.SuppressFinalize(texture);
        var textures = (Dictionary<Color, Texture2D>)typeof(SolidColorTextureCache)
            .GetField("_textures", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        var previousTextures = new Dictionary<Color, Texture2D>(textures);

        try
        {
            currentProfile.SetValue(null, profile);
            textures[Color.Black] = texture;
            textures[new Color(14, 14, 14)] = texture;

            var batcher = (UltimaBatcher2D)RuntimeHelpers.GetUninitializedObject(typeof(UltimaBatcher2D));
            FieldInfo verticesField = Field(typeof(UltimaBatcher2D), "_vertexInfo");
            Array vertices = Array.CreateInstance(verticesField.FieldType.GetElementType(), 2048);
            verticesField.SetValue(batcher, vertices);
            Field(typeof(UltimaBatcher2D), "_textureInfo").SetValue(batcher, new Texture2D[2048]);
            batcher.Begin();

            var gump = (NameOverheadGump)RuntimeHelpers.GetUninitializedObject(typeof(NameOverheadGump));
            gump.Width = 100;
            int totalHeight = split ? 48 : 24;
            gump.Height = totalHeight;
            Field(typeof(NameOverheadGump), "_healthBarWidth").SetValue(gump, 100);
            Field(typeof(NameOverheadGump), "_borderColor").SetValue(gump, texture);
            Field(typeof(NameOverheadGump), "_useSplitLayout").SetValue(gump, split);
            Field(typeof(NameOverheadGump), "_nameBandHeight").SetValue(gump, 23);
            Field(typeof(NameOverheadGump), "_resourceBarHeight").SetValue(gump, 24 / barCount);
            var mobile = (Mobile)RuntimeHelpers.GetUninitializedObject(typeof(Mobile));
            var bounds = new Rectangle(0, 0, 100, 24);

            Invoke(gump, "DrawNamePlateBackground", batcher, mobile, bounds, radius);
            float resourceOpacity = health / 100f;
            if (applyOverallOpacity && !split)
                resourceOpacity *= background / 100f;

            for (int row = 0; row < barCount; row++)
            {
                Rectangle resourceBounds = (Rectangle)typeof(NameOverheadGump)
                    .GetMethod("GetResourceBarBounds", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(gump, [0, 0, row, barCount]);
                Invoke(gump, "DrawResourceBar", batcher, resourceBounds, texture,
                    ShaderHueTranslator.GetHueVector(0, false, resourceOpacity), percent, resourceOpacity);
            }

            var pixels = new float[100, totalHeight];
            int count = (int)Field(typeof(UltimaBatcher2D), "_numSprites").GetValue(batcher);
            for (int i = 0; i < count; i++)
            {
                object quad = vertices.GetValue(i);
                Vector3 start = (Vector3)quad.GetType().GetField("Position0").GetValue(quad);
                Vector3 end = (Vector3)quad.GetType().GetField("Position3").GetValue(quad);
                float alpha = ((Vector3)quad.GetType().GetField("Hue0").GetValue(quad)).Z;
                Assert.InRange((int)start.X, 0, 100);
                Assert.InRange((int)start.Y, 0, totalHeight);
                Assert.InRange((int)end.X, 0, 100);
                Assert.InRange((int)end.Y, 0, totalHeight);
                for (int y = (int)start.Y; y < end.Y; y++)
                for (int x = (int)start.X; x < end.X; x++)
                    pixels[x, y] = alpha + pixels[x, y] * (1 - alpha);
            }

            return pixels;
        }
        finally
        {
            currentProfile.SetValue(null, previousProfile);
            textures.Clear();
            foreach (var pair in previousTextures)
                textures.Add(pair.Key, pair.Value);
        }
    }

    private static FieldInfo Field(Type type, string name) =>
        type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);

    private static void Invoke(NameOverheadGump gump, string method, params object[] args) =>
        typeof(NameOverheadGump).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(gump, args);
}
