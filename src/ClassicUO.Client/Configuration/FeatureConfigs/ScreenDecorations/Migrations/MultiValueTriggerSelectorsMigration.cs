#nullable enable

using System.Text.Json.Nodes;
using ClassicUO.IO.Persistency.Migrations;

namespace ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Migrations;

/// <summary>
/// Moves <c>sound_played</c>'s single <c>sound_index</c> and <c>buff_changed</c>'s single
/// <c>buff_type</c> into the list-valued <c>sound_indexes</c>/<c>buff_types</c> both triggers now
/// match against, so a rule authored before either went multi-select keeps firing on the one value
/// it had.
/// </summary>
internal sealed class MultiValueTriggerSelectorsMigration : IConfigMigration<JsonObject>
{
    private const string SOUND_PLAYED_KIND = "sound_played";
    private const string BUFF_CHANGED_KIND = "buff_changed";

    public int Version => 1;

    public void Up(JsonObject document)
    {
        if (document["overlays"] is not JsonObject overlays || overlays["rules"] is not JsonArray rules)
            return;

        foreach (JsonNode? rule in rules)
        {
            if (rule is not JsonObject ruleObject
                || ruleObject["trigger"] is not JsonObject trigger
                || trigger["parameters"] is not JsonObject parameters
                || parameters["kind"] is not JsonValue kindValue
                || !kindValue.TryGetValue(out string? kind))
                continue;

            switch (kind)
            {
                case SOUND_PLAYED_KIND:
                    Listify(parameters, "sound_index", "sound_indexes");
                    break;

                case BUFF_CHANGED_KIND:
                    Listify(parameters, "buff_type", "buff_types");
                    break;
            }
        }
    }

    /// <summary>Replaces a scalar property with a one-element array under a new name, leaving
    /// <paramref name="parameters" /> untouched where the old key is already gone (a rule authored
    /// after the rename, or a second pass over an already-migrated file).</summary>
    private static void Listify(JsonObject parameters, string oldKey, string newKey)
    {
        if (!parameters.TryGetPropertyValue(oldKey, out JsonNode? value) || value == null)
            return;

        parameters.Remove(oldKey);
        parameters[newKey] = new JsonArray(value);
    }
}
