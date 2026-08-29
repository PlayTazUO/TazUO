#nullable enable

using System.Collections.Generic;
using System.Text.Json.Nodes;
using ClassicUO.IO.Persistency.Migrations;

namespace ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Migrations;

/// <summary>
/// Every shape change <c>screen_decorations.json</c> has been through, in order. Listed by hand
/// rather than discovered: order is the contract, two branches claiming one version must collide
/// here as a merge conflict, and reflection scanning does not survive trimming.
/// </summary>
internal static class ScreenDecorationsMigrations
{
    private static readonly IReadOnlyList<IConfigMigration<JsonObject>> _migrations =
    [
        new MultiValueTriggerSelectorsMigration()
    ];

    public static ConfigMigrationPipeline<JsonObject> Pipeline { get; } = new(
        new ConfigMigrationSequence<JsonObject>(_migrations),
        new JsonMigrationFormat(ScreenDecorationsJsonContext.JsonOptions)
    );

    public static int LatestVersion => Pipeline.LatestVersion;
}
