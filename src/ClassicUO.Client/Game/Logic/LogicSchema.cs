#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.Logic;

/// <summary>
/// One thing a condition may be written about: what it is called, and what kind of value it holds.
/// </summary>
public sealed class LogicField
{
    /// <summary>Persisted by every condition naming this field. Stable across releases.</summary>
    public required string Key { get; init; }

    /// <summary>Name shown in the editor. Localized by whoever builds the schema.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Decides which operators are offered and how the operand is read.</summary>
    public LogicValueKind Kind { get; init; } = LogicValueKind.Text;

    /// <summary>Optional explanation shown on hover in the editor.</summary>
    public string? Description { get; init; }
}

/// <summary>
/// The fields an editor may offer, with no knowledge of what they are read from. This is all the
/// <see cref="ClassicUO.Game.UI.MyraWindows.Widgets.Logic.LogicBuilder" /> needs, which is what keeps
/// the widget free of any one consumer's subject type.
/// </summary>
public interface ILogicSchema
{
    /// <summary>The fields, in the order the editor should offer them.</summary>
    IReadOnlyList<LogicField> Fields { get; }
}

/// <summary>
/// A schema bound to the type it reads from: each field paired with the accessor that pulls its
/// value off a subject.
/// </summary>
/// <typeparam name="TSubject">What conditions built on this schema are evaluated against.</typeparam>
public sealed class LogicSchema<TSubject> : ILogicSchema
{
    #region Public accessors

    /// <inheritdoc />
    public IReadOnlyList<LogicField> Fields { get; }

    #endregion

    #region Private members

    private readonly Dictionary<string, Func<TSubject, object?>> _resolvers;

    #endregion

    #region Ctor

    /// <summary>
    /// Builds a schema from field/accessor pairs.
    /// </summary>
    /// <param name="fields">Each field and the accessor that reads it off a subject.</param>
    /// <exception cref="ArgumentNullException"><paramref name="fields" /> is null.</exception>
    /// <exception cref="ArgumentException">Two fields share a key.</exception>
    public LogicSchema(IEnumerable<(LogicField Field, Func<TSubject, object?> Resolve)> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        (LogicField Field, Func<TSubject, object?> Resolve)[] entries = [.. fields];

        Fields = [.. entries.Select(entry => entry.Field)];

        _resolvers = new Dictionary<string, Func<TSubject, object?>>(entries.Length, StringComparer.OrdinalIgnoreCase);

        foreach ((LogicField field, Func<TSubject, object?> resolve) in entries)
        {
            if (!_resolvers.TryAdd(field.Key, resolve))
                throw new ArgumentException($"Duplicate logic field key '{field.Key}'", nameof(fields));
        }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// The field a condition names, or null for a key this schema does not know.
    /// </summary>
    /// <param name="key">The persisted field key.</param>
    /// <returns>The field, or null.</returns>
    public LogicField? Find(string? key) =>
        key == null ? null : Fields.FirstOrDefault(field => string.Equals(field.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Reads a field off a subject.
    /// </summary>
    /// <param name="key">The persisted field key.</param>
    /// <param name="subject">What to read from.</param>
    /// <param name="value">The value read, or null.</param>
    /// <returns>Whether the key resolved to a field of this schema.</returns>
    public bool TryResolve(string? key, TSubject subject, out object? value)
    {
        value = null;

        if (key == null || !_resolvers.TryGetValue(key, out Func<TSubject, object?>? resolve))
            return false;

        value = resolve(subject);

        return true;
    }

    #endregion
}
