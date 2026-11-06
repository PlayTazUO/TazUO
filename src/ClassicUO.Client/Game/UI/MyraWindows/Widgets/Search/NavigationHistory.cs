#nullable enable

using System;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Search;

/// <summary>
///     Browser-style linear navigation history: a cursor into a list of visited states, where
///     pushing from anywhere other than the end discards the forward tail.
///     <para>
///         Entries must be values describing a location, not live widgets - a consumer that rebuilds
///         its UI (a profile switch, a theme change) would otherwise be holding disposed controls.
///     </para>
/// </summary>
/// <typeparam name="T">The navigation state type. Equality is used to collapse duplicate pushes.</typeparam>
public sealed class NavigationHistory<T>
{
    #region Events

    /// <summary>
    ///     Raised whenever the cursor or the entry list changes, so navigation affordances can
    ///     refresh their enabled state.
    /// </summary>
    public event EventHandler? Changed;

    #endregion

    #region Public Accessors

    /// <summary>Maximum retained entries. Pushing past this drops the oldest.</summary>
    public int Capacity { get; }

    /// <summary>Whether a <see cref="TryGoBack" /> would succeed</summary>
    public bool CanGoBack => _cursor > 0;

    /// <summary>Whether a <see cref="TryGoForward" /> would succeed</summary>
    public bool CanGoForward => _cursor >= 0 && _cursor < _entries.Count - 1;

    /// <summary>The entry at the cursor, or <see langword="default" /> when the history is empty</summary>
    public T? Current => _cursor < 0 ? default : _entries[_cursor];

    /// <summary>Whether any entry has been pushed</summary>
    public bool IsEmpty => _entries.Count == 0;

    #endregion

    #region Members

    #region Consts/Readonly

    private const int DEFAULT_CAPACITY = 50;

    private readonly List<T> _entries = [];
    private readonly IEqualityComparer<T> _comparer;

    #endregion

    #region Mutables

    private int _cursor = -1;

    #endregion

    #endregion

    #region Ctor

    /// <param name="capacity">Maximum retained entries; must be positive</param>
    /// <param name="comparer">
    ///     Equality used to suppress a push that repeats the current entry. Defaults to
    ///     <see cref="EqualityComparer{T}.Default" />.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity" /> is not positive</exception>
    public NavigationHistory(int capacity = DEFAULT_CAPACITY, IEqualityComparer<T>? comparer = null)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");

        Capacity = capacity;
        _comparer = comparer ?? EqualityComparer<T>.Default;
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Appends <paramref name="entry" /> at the cursor, discarding any forward history.
    ///     A push equal to the current entry adds no second copy, so re-selecting where you already
    ///     are doesn't fill the history with repeats - but it still discards the forward tail, since
    ///     arriving somewhere again is a navigation and what lay ahead of the old arrival is no
    ///     longer where the user is heading.
    /// </summary>
    /// <param name="entry">The state to record</param>
    public void Push(T entry)
    {
        bool repeatsCurrent = _cursor >= 0 && _comparer.Equals(_entries[_cursor], entry);
        int forwardStart = _cursor + 1;
        bool hadForward = forwardStart < _entries.Count;

        if (hadForward)
            _entries.RemoveRange(forwardStart, _entries.Count - forwardStart);

        if (repeatsCurrent)
        {
            // The stored entry is kept rather than overwritten: it carries whatever view state was
            // captured into it, which the incoming copy does not have.
            if (hadForward)
                Changed?.Invoke(this, EventArgs.Empty);

            return;
        }

        _entries.Add(entry);

        if (_entries.Count > Capacity)
            _entries.RemoveRange(0, _entries.Count - Capacity);

        _cursor = _entries.Count - 1;

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///     Overwrites the entry at the cursor without moving it. Used for a view whose state evolves
    ///     while the user stays on it - a search that is being retyped should be one history entry,
    ///     not one per keystroke.
    /// </summary>
    /// <param name="entry">The replacement state</param>
    /// <returns>Whether there was a current entry to replace</returns>
    public bool ReplaceCurrent(T entry)
    {
        if (_cursor < 0)
            return false;

        _entries[_cursor] = entry;
        Changed?.Invoke(this, EventArgs.Empty);

        return true;
    }

    /// <summary>Moves the cursor back one entry</summary>
    /// <param name="entry">The entry navigated to, when this returns <see langword="true" /></param>
    /// <returns>Whether the cursor moved</returns>
    public bool TryGoBack(out T entry)
    {
        if (!CanGoBack)
        {
            entry = default!;
            return false;
        }

        entry = _entries[--_cursor];
        Changed?.Invoke(this, EventArgs.Empty);

        return true;
    }

    /// <summary>Moves the cursor forward one entry</summary>
    /// <param name="entry">The entry navigated to, when this returns <see langword="true" /></param>
    /// <returns>Whether the cursor moved</returns>
    public bool TryGoForward(out T entry)
    {
        if (!CanGoForward)
        {
            entry = default!;
            return false;
        }

        entry = _entries[++_cursor];
        Changed?.Invoke(this, EventArgs.Empty);

        return true;
    }

    /// <summary>Drops every entry and resets the cursor</summary>
    public void Clear()
    {
        if (_entries.Count == 0)
            return;

        _entries.Clear();
        _cursor = -1;

        Changed?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}
