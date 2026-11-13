#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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
///     Free of graphics and of any <see cref="WorldMapGump" /> state by design, so it stays usable
///     headless. Callers resolve the icon texture themselves from
///     <see cref="WMapMarker.MarkerIconName" />.
/// </remarks>
public static partial class UoamMarkerParser
{
    #region Private members

    /// <summary>UOAM files carry no zoom information, so every marker gets the mid zoom level.</summary>
    private const int DEFAULT_ZOOM_INDEX = 3;

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

            UoamLineResult result = ParseLine(line, out WMapMarker? marker, out string? failureReason);

            // We explicitly ignore the 'Ignore' result - We don't even want to log non-marker lines.
            if (result == UoamLineResult.Parsed)
                markers.Add(marker!);
            else if (result == UoamLineResult.Malformed)
            {
                skippedLines++;
                Log.Warn($"Skipping malformed marker line in {Path.GetFileName(mapFilePath)}: \"{line}\" ({failureReason})");
            }
        }

        return markers;
    }

    /// <summary>
    ///     Parses one UOAM line, shaped <c>+Icon Name: X Y MapId Marker Name</c>.
    /// </summary>
    /// <param name="line">Line to parse; blank lines and the file header are reported as ignorable.</param>
    /// <param name="marker">
    ///     Parsed marker, set only when the result is <see cref="UoamLineResult.Parsed" />. Its
    ///     <see cref="WMapMarker.MarkerIcon" /> is left null for the caller to resolve.
    /// </param>
    /// <param name="failureReason">
    ///     Why the line was rejected, set only when the result is <see cref="UoamLineResult.Malformed" />.
    /// </param>
    /// <returns>Whether the line produced a marker, carried none, or was malformed.</returns>
    public static UoamLineResult ParseLine(string line, out WMapMarker? marker, out string? failureReason)
    {
        marker = null;
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
            || !int.TryParse(match.Groups["mapId"].ValueSpan, out int mapId))
        {
            failureReason = "coordinate or map ID outside the range of a 32-bit integer";

            return UoamLineResult.Malformed;
        }

        marker = new WMapMarker
        {
            X = x,
            Y = y,
            MapId = mapId,
            Name = match.Groups["name"].Value.Trim(),
            Color = Color.White,
            ZoomIndex = DEFAULT_ZOOM_INDEX,
            // Kept whole: UOAM icon names are multi-word ("point of interest"), and cutting at
            // the first space makes them unmatchable against the icon files named after them.
            MarkerIconName = MarkerIconKey.Normalize(match.Groups["icon"].Value)
        };

        return UoamLineResult.Parsed;
    }

    #endregion

    #region Private methods

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
