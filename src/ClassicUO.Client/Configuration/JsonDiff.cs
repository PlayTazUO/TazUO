#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace ClassicUO.Configuration;

/// <summary>How a value differs between the on-disk file and the in-memory version.</summary>
public enum JsonChangeKind
{
    /// <summary>The in-memory version has a value the on-disk file does not.</summary>
    Added,

    /// <summary>The on-disk file has a value the in-memory version drops.</summary>
    Removed,

    /// <summary>Present in both, with different values.</summary>
    Changed,
}

/// <summary>
///     One difference between two JSON documents, located by a path that names the property or
///     array index it sits at.
/// </summary>
/// <param name="Path">JSONPath-like location, e.g. <c>$.hotkeys[2].key</c>.</param>
/// <param name="Kind">Which side has the value.</param>
/// <param name="DiskValue">The on-disk rendering, when the disk side has one.</param>
/// <param name="LocalValue">The in-memory rendering, when the in-memory side has one.</param>
public readonly record struct JsonValueChange(string Path, JsonChangeKind Kind, string? DiskValue, string? LocalValue);

/// <summary>
///     Structural comparison of two JSON documents, used to tell the user what differs before they
///     pick a side in a <see cref="JsonSaveConflict"/>.
/// </summary>
internal static class JsonDiff
{
    private const string RootPath = "$";

    /// <summary>Longest value rendered into a change before it is cut off.</summary>
    private const int MAX_VALUE_LENGTH = 50;

    /// <summary>
    ///     Lists the differences between <paramref name="disk" /> (the baseline) and
    ///     <paramref name="local" />. Objects and arrays are walked into, so a nested change is
    ///     reported where it happened rather than as one opaque top-level difference; a value whose
    ///     type changed between the two sides is reported whole.
    /// </summary>
    /// <param name="disk">The on-disk document, the baseline the changes are expressed against.</param>
    /// <param name="local">The in-memory document that would be written.</param>
    /// <returns>Differences in document order; empty when the two are equal.</returns>
    public static IReadOnlyList<JsonValueChange> Compare(JsonNode? disk, JsonNode? local)
    {
        var changes = new List<JsonValueChange>();
        CompareNodes(disk, local, RootPath, changes);

        return changes;
    }

    private static void CompareNodes(JsonNode? disk, JsonNode? local, string path, List<JsonValueChange> changes)
    {
        if (JsonNode.DeepEquals(disk, local))
            return;

        if (disk is JsonObject diskObject && local is JsonObject localObject)
        {
            CompareObjects(diskObject, localObject, path, changes);
            return;
        }

        if (disk is JsonArray diskArray && local is JsonArray localArray)
        {
            CompareArrays(diskArray, localArray, path, changes);
            return;
        }

        // One side is missing, or the two sides are different kinds of value: reported whole.
        changes.Add(new JsonValueChange(path, JsonChangeKind.Changed, Describe(disk), Describe(local)));
    }

    private static void CompareObjects(JsonObject disk, JsonObject local, string path, List<JsonValueChange> changes)
    {
        foreach (KeyValuePair<string, JsonNode?> property in disk)
        {
            string childPath = $"{path}.{property.Key}";

            if (local.TryGetPropertyValue(property.Key, out JsonNode? localValue))
                CompareNodes(property.Value, localValue, childPath, changes);
            else
                changes.Add(new JsonValueChange(childPath, JsonChangeKind.Removed, Describe(property.Value), null));
        }

        foreach (KeyValuePair<string, JsonNode?> property in local)
        {
            if (disk.ContainsKey(property.Key))
                continue;

            changes.Add(new JsonValueChange($"{path}.{property.Key}", JsonChangeKind.Added, null, Describe(property.Value)));
        }
    }

    private static void CompareArrays(JsonArray disk, JsonArray local, string path, List<JsonValueChange> changes)
    {
        int shared = Math.Min(disk.Count, local.Count);

        for (int i = 0; i < shared; i++)
            CompareNodes(disk[i], local[i], $"{path}[{i}]", changes);

        for (int i = shared; i < disk.Count; i++)
            changes.Add(new JsonValueChange($"{path}[{i}]", JsonChangeKind.Removed, Describe(disk[i]), null));

        for (int i = shared; i < local.Count; i++)
            changes.Add(new JsonValueChange($"{path}[{i}]", JsonChangeKind.Added, null, Describe(local[i])));
    }

    /// <summary>Renders a node compactly for one line of a change list, truncating long values.</summary>
    private static string Describe(JsonNode? node)
    {
        if (node == null)
            return "null";

        string text = node.ToJsonString().Replace('\n', ' ').Replace('\r', ' ');

        return text.Length <= MAX_VALUE_LENGTH ? text : string.Concat(text.AsSpan(0, MAX_VALUE_LENGTH), "…");
    }
}
