#nullable enable
using System;

namespace ClassicUO.Game.ScreenDecorations.Shake;

/// <summary>
///     What raised a shake. Each source is capped on its own, so one of them looping cannot evict the
///     other's shakes.
/// </summary>
internal enum ShakeSource
{
    /// <summary>A rule firing, or a preview of the look it raises.</summary>
    Rule,

    /// <summary>A Python script, via the scripting API.</summary>
    Script
}

/// <summary>
///     Who raised a shake: the allowance it bills, and the handle that can cancel it. Separate from
///     <see cref="ShakeRequest" />, which says what the shake looks like - the two are independent, and
///     an envelope is the same envelope whoever asked for it.
///     <para>
///         Reachable only through the two factories, so a scripted shake cannot be built without something
///         to cancel it by. That combination would hold a slot of the script allowance, unstoppable, for its
///         whole duration: <see cref="ScreenShake.Cancel" /> rejects a null owner and
///         <see cref="ScreenShake.CancelByOwner" /> matches by reference.
///     </para>
/// </summary>
internal readonly struct ShakeOrigin
{
    #region Public accessors

    /// <summary>
    ///     The player's own state - a rule firing, or a preview of one. Carries no handle, so
    ///     it ends on its own or with <see cref="ScreenShake.Clear" />.
    /// </summary>
    public static ShakeOrigin Rule => new(ShakeSource.Rule, null);

    /// <summary>A script, which has to hand over something to cancel by.</summary>
    /// <param name="owner">The script raising it.</param>
    /// <returns>The origin.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="owner" /> is null.</exception>
    public static ShakeOrigin Script(object owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        return new ShakeOrigin(ShakeSource.Script, owner);
    }

    /// <summary>Which allowance the shake counts against.</summary>
    public ShakeSource Source { get; }

    /// <summary>What may cancel it, or null for <see cref="Rule" />.</summary>
    public object? Owner { get; }

    #endregion

    #region Ctor

    private ShakeOrigin(ShakeSource source, object? owner)
    {
        Source = source;
        Owner = owner;
    }

    #endregion
}
