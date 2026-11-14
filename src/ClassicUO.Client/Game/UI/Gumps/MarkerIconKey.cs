#nullable enable

namespace ClassicUO.Game.UI.Gumps;

/// <summary>
/// Turns a raw world-map marker icon name into the key the icon lookup is keyed by.
/// </summary>
/// <remarks>
/// Every producer and consumer of an icon name goes through here, so a new marker format cannot
/// introduce a key that disagrees with the rest. The fold must stay locale-independent: Turkish
/// and Azeri lowercase 'I' to a dotless 'i', which no <see cref="System.StringComparer"/> treats
/// as equal to 'i', so a culture-sensitive fold orphans icons on those machines alone - and the
/// keys outlive the session, being written into marker files.
/// </remarks>
public static class MarkerIconKey
{
    /// <summary>
    /// Produces the lookup key for an icon name.
    /// </summary>
    /// <param name="iconName">
    /// Raw name from a marker file or from an icon's file name; may be null or padded.
    /// </param>
    /// <returns>Trimmed, invariantly lowercased key, or empty when there is no usable name.</returns>
    public static string Normalize(string? iconName) =>
        string.IsNullOrWhiteSpace(iconName) ? string.Empty : iconName.Trim().ToLowerInvariant();
}
