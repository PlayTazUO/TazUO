#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;

namespace ClassicUO.Game.ScreenDecorations.Manager;

/// <summary>
/// One effect a script has asked for, and the terms it asked on.
/// </summary>
/// <param name="Handle">What the script holds and stops it by. Monotonic, never reused.</param>
/// <param name="Slot">Compositor slot it occupies. Slots are Guid-keyed, so a scripted effect needs
/// one of its own - it has no rule to borrow an id from.</param>
/// <param name="Profile">The look to draw.</param>
/// <param name="SourceProfileId">
/// The library composition this came from, or <see cref="Guid.Empty"/> for one the script built.
/// What a rule's claim is tested against, so a script raising "poison" suppresses the rule raising
/// it rather than compositing a second copy at doubled alpha. Script-built compositions have no
/// source and so never claim, which is why two identical hand-built ones do double up.
/// </param>
/// <param name="Intensity">Strength the script asked for, 0-1.</param>
/// <param name="ExpiresAt">When it lapses, or null to run until stopped.</param>
/// <param name="Owner">The script's API instance, for teardown when the script stops.</param>
internal readonly record struct ScriptedDemand(
    uint Handle,
    Guid Slot,
    EffectProfile Profile,
    Guid SourceProfileId,
    float Intensity,
    DateTime? ExpiresAt,
    object Owner
);

/// <summary>
/// The effects scripts currently hold, in the order they asked for them, with the handle-to-slot
/// mapping that lets a script stop one without knowing anything about compositor slots.
/// <para>
/// Insertion order is kept rather than inferred from a dictionary, because it decides which of two
/// demands on one look draws: the first to arrive claims it and the second is inert until the first
/// lapses. Hash order would make that arbitrary and make it change between passes.
/// </para>
/// <para>
/// Not thread safe. Owned by <see cref="ScreenOverlayManager"/>, which is main thread only.
/// </para>
/// </summary>
internal sealed class ScriptedOverlayRegistry
{
    #region Public accessors

    /// <summary>The live demands, oldest first.</summary>
    public IReadOnlyList<ScriptedDemand> Entries => _entries;

    public int Count => _entries.Count;

    #endregion

    #region Private members

    private readonly List<ScriptedDemand> _entries = [];

    /// <summary>
    /// The compositor slots scripts share, handed out round-robin and reused.
    /// <para>
    /// A fixed set rather than a fresh <see cref="Guid.NewGuid"/> per demand, because a slot the
    /// compositor has been told to hide stays in its active set until the look's fade-out finishes,
    /// and nothing counts those: with a new slot each time, a start-stop loop on a long fade-out
    /// grows that set without bound, and the compositor walks and sorts all of it every frame. A
    /// pool of <see cref="ScreenOverlayManager.ScriptCap"/> caps it, because reusing a vacated slot
    /// makes the next start replace the retiring overlay instead of adding another beside it.
    /// </para>
    /// <para>
    /// Round-robin rather than lowest-free so the slot reused is the one vacated longest ago, whose
    /// fade is most likely already done - reusing one mid-fade means the next effect inherits its
    /// residual envelope rather than fading in from nothing.
    /// </para>
    /// </summary>
    private readonly Guid[] _slots = BuildSlots();

    /// <summary>
    /// Handles are never reused, so a script holding one that has already lapsed cannot stop
    /// whatever took its place. 0 is reserved for "the user has the system switched off".
    /// </summary>
    private uint _nextHandle;

    /// <summary>Where in <see cref="_slots"/> the search for a free slot starts.</summary>
    private int _nextSlot;

    #endregion

    #region Public methods

    /// <summary>
    /// Records a demand and hands back the handle the script stops it by.
    /// </summary>
    /// <param name="profile">The look to draw.</param>
    /// <param name="sourceProfileId">The library composition it came from, or
    /// <see cref="Guid.Empty"/> for a script-built one.</param>
    /// <param name="intensity">Strength asked for, 0-1.</param>
    /// <param name="expiresAt">When it lapses, or null to run until stopped.</param>
    /// <param name="owner">The script's API instance.</param>
    /// <returns>The handle.</returns>
    /// <exception cref="InvalidOperationException">Every slot is held, which the caller is expected
    /// to have ruled out by checking <see cref="Count"/> against the cap.</exception>
    public uint Add(EffectProfile profile, Guid sourceProfileId, float intensity, DateTime? expiresAt, object owner)
    {
        uint handle = ++_nextHandle;

        _entries.Add(new ScriptedDemand(handle, TakeSlot(), profile, sourceProfileId, intensity, expiresAt, owner));

        return handle;
    }

    /// <summary>
    /// Drops the demand behind <paramref name="handle"/>, if <paramref name="owner"/> is the script
    /// that asked for it.
    /// </summary>
    /// <param name="handle">The handle to release. Unknown, expired and already-released handles,
    /// and 0, are ignored.</param>
    /// <param name="owner">The script's API instance. A handle belonging to another script is
    /// ignored, so one script cannot stop another's effects by guessing at the handle range.</param>
    /// <returns>True if a demand was dropped.</returns>
    public bool Remove(uint handle, object owner)
    {
        if (handle == 0)
            return false;

        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Handle != handle || !ReferenceEquals(_entries[i].Owner, owner))
                continue;

            _entries.RemoveAt(i);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Drops everything <paramref name="owner"/> asked for.
    /// </summary>
    /// <param name="owner">The script's API instance.</param>
    /// <returns>True if anything was dropped.</returns>
    public bool RemoveByOwner(object owner)
    {
        int removed = _entries.RemoveAll(entry => ReferenceEquals(entry.Owner, owner));

        return removed > 0;
    }

    /// <summary>
    /// Drops every demand whose declared span has run out.
    /// </summary>
    /// <param name="now">The instant to judge against.</param>
    /// <returns>True if anything lapsed.</returns>
    public bool RemoveLapsed(DateTime now)
    {
        int removed = _entries.RemoveAll(entry => entry.ExpiresAt is { } at && at <= now);

        return removed > 0;
    }

    /// <summary>
    /// The script behind the demand occupying <paramref name="slot"/>, for tagging the shake its
    /// look fires on arrival.
    /// </summary>
    /// <param name="slot">The compositor slot to look up.</param>
    /// <returns>The owner, or null if nothing holds that slot.</returns>
    public object? FindOwner(Guid slot)
    {
        foreach (ScriptedDemand entry in _entries)
        {
            if (entry.Slot == slot)
                return entry.Owner;
        }

        return null;
    }

    public void Clear() => _entries.Clear();

    /// <summary>
    /// When the soonest demand lapses, or null if none of them will.
    /// </summary>
    /// <returns>The instant.</returns>
    public DateTime? NextExpiry()
    {
        DateTime? earliest = null;

        foreach (ScriptedDemand entry in _entries)
        {
            if (entry.ExpiresAt is not { } at)
                continue;

            if (earliest == null || at < earliest)
                earliest = at;
        }

        return earliest;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// The next slot no live demand holds, searching round-robin from where the last search left
    /// off.
    /// </summary>
    /// <returns>The slot.</returns>
    /// <exception cref="InvalidOperationException">Every slot is held.</exception>
    private Guid TakeSlot()
    {
        for (int offset = 0; offset < _slots.Length; offset++)
        {
            Guid candidate = _slots[(_nextSlot + offset) % _slots.Length];

            if (IsHeld(candidate))
                continue;

            _nextSlot = (_nextSlot + offset + 1) % _slots.Length;

            return candidate;
        }

        throw new InvalidOperationException(
            $"All {_slots.Length} scripted overlay slots are held; the cap should have been checked first."
        );
    }

    private bool IsHeld(Guid slot)
    {
        foreach (ScriptedDemand entry in _entries)
        {
            if (entry.Slot == slot)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Mints the pool. Per instance rather than static so two registries - the live one and a test's
    /// - cannot hand the compositor the same slot ids.
    /// </summary>
    /// <returns>The slots.</returns>
    private static Guid[] BuildSlots()
    {
        var slots = new Guid[ScreenOverlayManager.ScriptCap];

        for (int i = 0; i < slots.Length; i++)
            slots[i] = Guid.NewGuid();

        return slots;
    }

    #endregion
}
