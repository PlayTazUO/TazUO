#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassicUO.Configuration.Json;
using ClassicUO.Renderer.Effects;

namespace ClassicUO.Configuration.FeatureConfigs.ScreenOverlays;

[JsonSerializable(typeof(ScreenOverlays), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(OverlayEffectProfile), GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(List<OverlayLayer>), GenerationMode = JsonSourceGenerationMode.Metadata)]
// OverlayParams and the structs under it are public fields, not properties. Without IncludeFields
// the whole layer stack serializes as a row of empty objects.
[JsonSourceGenerationOptions(IncludeFields = true)]
internal sealed partial class ScreenOverlaysJsonContext : JsonSerializerContext
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

    public static ScreenOverlaysJsonContext DefaultToUse { get; } = new(JsonOptions.Value);
}
