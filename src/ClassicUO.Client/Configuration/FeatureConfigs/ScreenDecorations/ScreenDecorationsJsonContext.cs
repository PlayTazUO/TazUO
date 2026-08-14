#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Effects;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Rules;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Triggers;
using ClassicUO.Configuration.Json;
using ClassicUO.Game.Logic;
using ClassicUO.Renderer.Effects;

namespace ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;

[JsonSerializable(typeof(ScreenDecorations), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(EffectProfile), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(OverlayRule), GenerationMode = JsonSourceGenerationMode.Metadata)]
// The two polymorphic hierarchies. Their subtypes are reached through the [JsonDerivedType]
// attributes on these bases, which is also what writes and reads the discriminator.
[JsonSerializable(typeof(LayerEffect), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(TriggerParameters), GenerationMode = JsonSourceGenerationMode.Metadata)]
// Named outright rather than left to be discovered through the parameters that hold one: the tree
// is polymorphic in its own right, and its subtypes are reached through its [JsonDerivedType]s.
[JsonSerializable(typeof(LogicNode), GenerationMode = JsonSourceGenerationMode.Metadata)]
// OverlayShape, OverlayNoise and the structs under them are public fields, not properties. Without
// IncludeFields the whole layer stack serializes as a row of empty objects.
[JsonSourceGenerationOptions(IncludeFields = true)]
internal sealed partial class ScreenDecorationsJsonContext : JsonSerializerContext
{
    private static Lazy<JsonSerializerOptions> JsonOptions { get; } = new(() =>
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true,
            PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance,
            Converters = { new ColorJsonConverter(), new JsonStringEnumConverter() }
        };

        return options;
    });

    public static ScreenDecorationsJsonContext DefaultToUse { get; } = new(JsonOptions.Value);
}
