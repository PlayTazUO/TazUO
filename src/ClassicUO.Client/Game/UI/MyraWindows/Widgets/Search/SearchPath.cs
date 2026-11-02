#nullable enable

using System;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Search;

/// <summary>
///     An ordered chain of labels locating something inside a nested UI, e.g.
///     <c>Video › Visual Effects › Overlay Rules</c>. Immutable; <see cref="Push" /> and
///     <see cref="Take" /> return new instances.
///     <para>
///         Segments are display labels, so two distinct nodes that happen to share a label chain
///         compare equal. Consumers that need identity beyond the visible path must carry it
///         separately.
///     </para>
/// </summary>
public readonly struct SearchPath : IEquatable<SearchPath>
{
    #region Members

    /// <summary>Separator used by <see cref="Text" /> and <see cref="ToString" /></summary>
    public const string SEPARATOR = " › ";

    /// <summary>The empty path, matching a node with no ancestors</summary>
    public static readonly SearchPath Empty = new([], string.Empty);

    private readonly string[]? _segments;
    private readonly string? _text;

    #endregion

    #region Public Accessors

    /// <summary>Number of segments in this path</summary>
    public int Length => _segments?.Length ?? 0;

    /// <summary>Whether this path has no segments</summary>
    public bool IsEmpty => Length == 0;

    /// <summary>The joined path, e.g. <c>Video › Lighting</c>. Empty string for an empty path.</summary>
    public string Text => _text ?? string.Empty;

    /// <summary>The last segment, or <see langword="null" /> when the path is empty</summary>
    public string? Leaf => Length == 0 ? null : _segments![^1];

    /// <summary>The segments, outermost first</summary>
    public ReadOnlySpan<string> Segments => _segments ?? [];

    /// <summary>The segment at <paramref name="index" />, outermost first</summary>
    /// <exception cref="IndexOutOfRangeException">Thrown when <paramref name="index" /> is out of range</exception>
    public string this[int index] => (_segments ?? throw new IndexOutOfRangeException())[index];

    #endregion

    #region Ctor

    private SearchPath(string[] segments, string text)
    {
        _segments = segments;
        _text = text;
    }

    /// <summary>Creates a path from <paramref name="segments" />, outermost first</summary>
    /// <param name="segments">The segment labels. Null and whitespace entries are dropped.</param>
    public SearchPath(params string?[] segments)
    {
        List<string> kept = [];

        foreach (string? segment in segments)
        {
            if (!string.IsNullOrWhiteSpace(segment))
                kept.Add(segment!);
        }

        _segments = kept.ToArray();
        _text = string.Join(SEPARATOR, _segments);
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Returns this path with <paramref name="segment" /> appended. A null or whitespace
    ///     segment is ignored, which lets callers push an unlabelled node's name unconditionally.
    /// </summary>
    /// <param name="segment">The segment to append</param>
    /// <returns>The extended path, or this path unchanged when the segment is blank</returns>
    public SearchPath Push(string? segment)
    {
        if (string.IsNullOrWhiteSpace(segment))
            return this;

        int length = Length;
        var extended = new string[length + 1];

        if (length > 0)
            Array.Copy(_segments!, extended, length);

        extended[length] = segment!;

        return new SearchPath(extended, length == 0 ? segment! : $"{_text}{SEPARATOR}{segment}");
    }

    /// <summary>
    ///     Returns the first <paramref name="count" /> segments. Used to resolve a click on an
    ///     individual breadcrumb segment into the path it stands for.
    /// </summary>
    /// <param name="count">Number of leading segments to keep; clamped to the available range</param>
    /// <returns>The truncated path</returns>
    public SearchPath Take(int count)
    {
        count = Math.Clamp(count, 0, Length);

        if (count == 0)
            return Empty;

        if (count == Length)
            return this;

        var head = new string[count];
        Array.Copy(_segments!, head, count);

        return new SearchPath(head, string.Join(SEPARATOR, head));
    }

    /// <summary>
    ///     Whether this path starts with every segment of <paramref name="other" />. An empty
    ///     path is a prefix of everything.
    /// </summary>
    /// <param name="other">The candidate prefix</param>
    /// <returns>Whether <paramref name="other" /> is a prefix of this path</returns>
    public bool StartsWith(SearchPath other)
    {
        if (other.Length > Length)
            return false;

        for (int i = 0; i < other.Length; i++)
        {
            if (!string.Equals(_segments![i], other._segments![i], StringComparison.Ordinal))
                return false;
        }

        return true;
    }

    /// <summary>Number of leading segments this path shares with <paramref name="other" /></summary>
    /// <param name="other">The path to compare against</param>
    /// <returns>The count of common leading segments</returns>
    public int CommonPrefixLength(SearchPath other)
    {
        int max = Math.Min(Length, other.Length);
        int shared = 0;

        while (shared < max && string.Equals(_segments![shared], other._segments![shared], StringComparison.Ordinal))
            shared++;

        return shared;
    }

    /// <inheritdoc />
    public bool Equals(SearchPath other) => Length == other.Length && StartsWith(other);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is SearchPath other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Text);

    /// <inheritdoc />
    public override string ToString() => Text;

    public static bool operator ==(SearchPath left, SearchPath right) => left.Equals(right);

    public static bool operator !=(SearchPath left, SearchPath right) => !left.Equals(right);

    #endregion
}
