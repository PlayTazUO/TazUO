#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ClassicUO.Assets;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using static ClassicUO.Game.UI.Gumps.WorldMapGump;

namespace ClassicUO.Game.UI.Gumps;

/// <summary>
///     Outcome of parsing one line of a UOAM <c>.map</c> file.
/// </summary>
public enum UoamLineResult
{
    /// <summary>A marker was produced.</summary>
    Parsed,

    /// <summary>
    ///     Line carries no marker and is not an error: the file's version header, a blank line, or
    ///     any line that never claimed to be a marker. Reported to no one.
    /// </summary>
    Ignored,

    /// <summary>Line was meant to be a marker but could not be parsed.</summary>
    Malformed
}

/// <summary>
///     Reads UOAM-format <c>.map</c> marker files.
/// </summary>
/// <remarks>
///     <para>
///     One marker per line. The first line of a file is a version stamp (<c>3</c> in every known
///     file) and is ignored like any other line carrying no leading sign.
///     </para>
///     <code>
///      -point of interest:  1234  5678  3  Some Marker Name
///      ^ ^^^^^^^^^^^^^^^^ ^  ^^^^  ^^^^  ^  ^^^^^^^^^^^^^^^^
///      |        |         |   |     |    |         |
///      |        |         |   |     |    |         +-- name   optional; taken verbatim, only its
///      |        |         |   |     |    |                    outer padding trimmed
///      |        |         |   |     |    +------------ facet  see table below
///      |        |         |   |     +----------------- Y
///      |        |         |   +---------------------- X
///      |        |         +------------------------- ':' then optional spaces/tabs
///      |        +---------------------------------- icon name; may contain spaces
///      +------------------------------------------ sign: '+' shown, '-' hidden in UOAM
///     </code>
///     <para>
///     A facet is not a map index. UOAM's map list carries one extra leading entry, so every facet
///     sits one above the map it names:
///     </para>
///     <code>
///      facet 0  ->  Felucca AND Trammel   ("Both Felucca and Trammel"; yields two markers)
///      facet 1  ->  map 0   Felucca
///      facet 2  ->  map 1   Trammel
///      facet 3  ->  map 2   Ilshenar
///      facet 4  ->  map 3   Malas
///      facet 5  ->  map 4   Tokuno
///     </code>
///     <para>
///     Free of graphics and of any <see cref="WorldMapGump" /> state by design, so it stays usable
///     headless. Callers resolve the icon texture themselves from
///     <see cref="WMapMarker.MarkerIconName" />.
///     </para>
/// </remarks>
public static partial class UoamMarkerParser
{
    #region Private members

    /// <summary>UOAM files carry no zoom information, so every marker gets the mid zoom level.</summary>
    private const int DEFAULT_ZOOM_INDEX = 3;

    /// <summary>
    ///     Facet UOAM writes for Britannia's shared geometry, drawn on both Felucca and Trammel.
    /// </summary>
    /// <remarks>
    ///     UOAM's facet list is the client's map list with this one extra entry prepended - its
    ///     string table reads "Both Felucca and Trammel", "Felucca", "Trammel", "Ilshenar", "Malas",
    ///     "Tokuno" - which is why every other facet sits exactly one above its map index.
    /// </remarks>
    private const int SHARED_BRITANNIA_FACET = 0;

    private const int FELUCCA_MAP_INDEX = 0;
    private const int TRAMMEL_MAP_INDEX = 1;

    /// <summary>
    ///     A marker line: sign ('-' meaning UOAM had the icon hidden), icon name, separator, three
    ///     integers, then an optional name.
    /// </summary>
    /// <remarks>
    ///     The separator's padding is matched rather than assumed, because a file written without the
    ///     customary single space would otherwise shift the X coordinate. The name is captured verbatim
    ///     so spacing inside it survives; only its outer padding is trimmed. Linear, with no nested
    ///     quantifiers over overlapping alternations - these files are user-supplied, so the pattern
    ///     must not backtrack pathologically.
    /// </remarks>
    private const string MARKER_LINE_PATTERN = "^[+-]" +
                                             "(?<icon>[^:]*):[ \t]*" +
                                             "(?<x>-?[0-9]+)[ \t]+" +
                                             "(?<y>-?[0-9]+)[ \t]+" +
                                             "(?<mapId>-?[0-9]+)" +
                                             "(?:[ \t]+(?<name>.*))?[ \t]*$";

    [GeneratedRegex(MARKER_LINE_PATTERN, RegexOptions.CultureInvariant)]
    private static partial Regex MarkerLineRegex();

    #endregion

    #region Public methods

    /// <summary>
    ///     Loads every marker from a UOAM <c>.map</c> file.
    /// </summary>
    /// <param name="mapFilePath">Full path of the file to read.</param>
    /// <param name="skippedLines">Count of marker lines that were malformed and therefore dropped.</param>
    /// <returns>Markers in file order; empty when the file holds none.</returns>
    /// <exception cref="IOException">The file could not be read.</exception>
    public static List<WMapMarker> LoadFile(string mapFilePath, out int skippedLines)
    {
        var markers = new List<WMapMarker>();
        skippedLines = 0;

        using var reader = new StreamReader(
            File.Open(mapFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
        );

        while (!reader.EndOfStream)
        {
            string? line = reader.ReadLine();

            if (line is null)
                continue;

            // We explicitly ignore the 'Ignore' result - We don't even want to log non-marker lines.
            if (ParseLine(line, markers, out string? failureReason) == UoamLineResult.Malformed)
            {
                skippedLines++;
                Log.Warn($"Skipping malformed marker line in {Path.GetFileName(mapFilePath)}: \"{line}\" ({failureReason})");
            }
        }

        return markers;
    }

    /// <summary>
    ///     Parses one UOAM line, shaped <c>+Icon Name: X Y MapId Marker Name</c>, appending every
    ///     marker it yields.
    /// </summary>
    /// <param name="line">Line to parse; blank lines and the file header are reported as ignorable.</param>
    /// <param name="markers">
    ///     Receives the parsed markers, appended in map order and left untouched unless the result is
    ///     <see cref="UoamLineResult.Parsed" />. One line usually yields one marker, but UOAM's shared
    ///     Britannia facet yields two - the same place on Felucca and on Trammel. Each marker's
    ///     <see cref="WMapMarker.MarkerIcon" /> is left null for the caller to resolve.
    /// </param>
    /// <param name="failureReason">
    ///     Why the line was rejected, set only when the result is <see cref="UoamLineResult.Malformed" />.
    /// </param>
    /// <returns>Whether the line produced markers, carried none, or was malformed.</returns>
    public static UoamLineResult ParseLine(string line, ICollection<WMapMarker> markers, out string? failureReason)
    {
        failureReason = null;

        // Guard only against indexing/matching nothing; the pattern itself rejects non-marker lines.
        if (string.IsNullOrEmpty(line))
            return UoamLineResult.Ignored;

        Match match = MarkerLineRegex().Match(line);

        if (!match.Success)
            return ClassifyUnmatchedLine(line, out failureReason);

        // The pattern guarantees digits, so a failure here can only be an out-of-range value.
        if (!int.TryParse(match.Groups["x"].ValueSpan, out int x)
            || !int.TryParse(match.Groups["y"].ValueSpan, out int y)
            || !int.TryParse(match.Groups["mapId"].ValueSpan, out int facet))
        {
            failureReason = "coordinate or map ID outside the range of a 32-bit integer";

            return UoamLineResult.Malformed;
        }

        if (!TryResolveMapIndex(facet, out int mapIndex, out bool sharedAcrossFelAndTram))
        {
            failureReason = $"facet {facet} names no map this client knows (expected 0-{MapLoader.MAPS_COUNT})";

            return UoamLineResult.Malformed;
        }

        var marker = new WMapMarker
        {
            X = x,
            Y = y,
            MapId = mapIndex,
            Name = match.Groups["name"].Value.Trim(),
            Color = Color.White,
            ZoomIndex = DEFAULT_ZOOM_INDEX,
            // Kept whole: UOAM icon names are multi-word ("point of interest"), and cutting at
            // the first space makes them unmatchable against the icon files named after them.
            MarkerIconName = MarkerIconKey.Normalize(match.Groups["icon"].Value)
        };

        markers.Add(marker);

        // A marker holds a single map, so the facet covering both Britannia maps needs a second one.
        if (sharedAcrossFelAndTram)
            markers.Add(CopyOntoMap(marker, TRAMMEL_MAP_INDEX));

        return UoamLineResult.Parsed;
    }

    #endregion

    #region Private methods

    /// <summary>
    ///     Translates a UOAM facet into the map index the client uses.
    /// </summary>
    /// <param name="facet">Facet field as written in the marker line.</param>
    /// <param name="mapIndex">
    ///     Resolved map index; Felucca for <see cref="SHARED_BRITANNIA_FACET" />, whose Trammel twin is
    ///     reported through <paramref name="sharedAcrossFelAndTram" /> instead.
    /// </param>
    /// <param name="sharedAcrossFelAndTram">True when the facet covers both Britannia maps.</param>
    /// <returns>
    ///     False when the facet names no map this client has, which keeps a bad facet from becoming an
    ///     out-of-range map index downstream.
    /// </returns>
    private static bool TryResolveMapIndex(int facet, out int mapIndex, out bool sharedAcrossFelAndTram)
    {
        sharedAcrossFelAndTram = facet == SHARED_BRITANNIA_FACET;

        // The shared facet occupies slot 0 of UOAM's list, so every real facet is one above its map.
        mapIndex = sharedAcrossFelAndTram ? FELUCCA_MAP_INDEX : facet - 1;

        return mapIndex >= 0 && mapIndex < MapLoader.MAPS_COUNT;
    }

    /// <summary>
    ///     Copies a marker onto another map, used to give the shared Britannia facet its second home.
    /// </summary>
    /// <param name="source">Marker to copy.</param>
    /// <param name="mapIndex">Map index the copy belongs to.</param>
    /// <returns>An independent marker identical to <paramref name="source" /> but on another map.</returns>
    private static WMapMarker CopyOntoMap(WMapMarker source, int mapIndex) => new()
    {
        X = source.X,
        Y = source.Y,
        MapId = mapIndex,
        Name = source.Name,
        Color = source.Color,
        ColorName = source.ColorName,
        ZoomIndex = source.ZoomIndex,
        MarkerIcon = source.MarkerIcon,
        MarkerIconName = source.MarkerIconName
    };

    /// <summary>
    ///     Decides why a line failed <see cref="MARKER_LINE_PATTERN" />, separating lines that never
    ///     claimed to be markers from markers the file got wrong, and names the defect for the latter.
    /// </summary>
    /// <param name="line">The non-matching line; must be non-empty.</param>
    /// <param name="failureReason">Set when the verdict is <see cref="UoamLineResult.Malformed" />.</param>
    /// <returns><see cref="UoamLineResult.Ignored" /> or <see cref="UoamLineResult.Malformed" />.</returns>
    private static UoamLineResult ClassifyUnmatchedLine(string line, out string? failureReason)
    {
        failureReason = null;

        // Anything that doesnt start with '+' or '-' is not a marker and should be ignored
        if (line[0] != '+' && line[0] != '-')
            return UoamLineResult.Ignored;

        int separatorIndex = line.IndexOf(':');
        if (separatorIndex < 1 || separatorIndex + 1 >= line.Length)
        {
            failureReason = "missing coordinate separator";
            return UoamLineResult.Malformed;
        }

        // A payload of at most one token carries no coordinate triplet to be wrong about.
        ReadOnlySpan<char> payload = line.AsSpan(separatorIndex + 1).Trim();

        if (payload.IndexOfAny(' ', '\t') < 0)
            return UoamLineResult.Ignored;

        failureReason = "expected \"<icon>: X Y MapId [name]\" with three integer fields";

        return UoamLineResult.Malformed;
    }

    #endregion
}
