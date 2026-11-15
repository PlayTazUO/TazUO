using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Utility;

/// <summary>
/// Caches compiled regexes by pattern and options.
/// A pattern that does not compile is cached as a regex that never matches, so a bad
/// user-supplied pattern degrades to no match instead of throwing from a gameplay or UI call site.
/// </summary>
public static class RegexHelper
{
    private static readonly Regex _neverMatch = new("(?!)", RegexOptions.Compiled);
    private static readonly ConcurrentDictionary<(string Pattern, RegexOptions Options), Regex> _regexes = new();

    /// <summary>
    /// Returns a compiled regex for <paramref name="pattern"/>, compiling and caching it on first use.
    /// </summary>
    /// <param name="pattern">The regular expression pattern. May be invalid.</param>
    /// <param name="options">
    /// Options for the compiled regex. <see cref="RegexOptions.Compiled"/> is always added.
    /// </param>
    /// <returns>
    /// The cached regex, or a regex that never matches when <paramref name="pattern"/> cannot be compiled.
    /// </returns>
    public static Regex GetRegex(string pattern, RegexOptions options = RegexOptions.Compiled)
    {
        options |= RegexOptions.Compiled;

        return _regexes.GetOrAdd((pattern, options), static key =>
        {
            try
            {
                return new Regex(key.Pattern, key.Options);
            }
            catch (ArgumentException e)
            {
                Log.Warn($"Ignoring invalid regex pattern '{key.Pattern}': {e.Message}");

                return _neverMatch;
            }
        });
    }

    /// <summary>
    /// Checks whether <paramref name="pattern"/> compiles as a regular expression.
    /// </summary>
    /// <param name="pattern">The pattern to check. Null or empty counts as valid - there is nothing to match.</param>
    /// <returns><see langword="true"/> when the pattern is null, empty, or compiles.</returns>
    public static bool IsValid(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return true;

        try
        {
            _ = new Regex(pattern);

            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
