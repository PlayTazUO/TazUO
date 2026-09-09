#nullable enable

using System.Collections.Generic;
using System.Text.Json.Nodes;
using ClassicUO.IO.Persistency.Migrations;

namespace ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Migrations;

/// <summary>
///     Every shape change <c>screen_decorations.json</c> has been through. Listed by hand rather than
///     discovered, because reflection scanning does not survive trimming; the sequence sorts and
///     version-checks the list, so where an entry sits here does not matter.
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
