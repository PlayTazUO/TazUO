#nullable enable

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
/// Arms the target cursor and reports back what the player picks, so a config screen can point at a
/// world object instead of taking a typed serial.
/// <para>
/// Disarms itself when it leaves the widget tree. Only its own arming - a cursor since claimed by
/// something else is left alone.
/// </para>
/// </summary>
public sealed class TargetSelectionButton : MyraButton
{
    #region Private members

    private readonly Action<uint?> _onTargeted;
    private readonly Func<Entity, bool>? _accepts;

    /// <summary>Reused for every arming: it is the identity
    /// <see cref="TargetManager.IsCurrentTargetingAction" /> matches on.</summary>
    private readonly Action<object> _targetCallback;

    #endregion

    #region Ctor

    /// <summary>Wires the button to arm the target cursor on click.</summary>
    /// <param name="onTargeted">Called with the picked serial, or null when the player cancelled or
    /// picked something <paramref name="accepts" /> rejected.</param>
    /// <param name="accepts">Filters a valid pick - an item-only picker, say. Omitted takes any
    /// entity.</param>
    /// <param name="caption">Button text.</param>
    /// <param name="tooltip">Optional tooltip.</param>
    /// <exception cref="ArgumentNullException"><paramref name="onTargeted" /> is null.</exception>
    public TargetSelectionButton(
        Action<uint?> onTargeted,
        Func<Entity, bool>? accepts = null,
        string? caption = null,
        string? tooltip = null
    ) : base(caption ?? TazLang.Get("targetselectionbutton_target", "Target"))
    {
        ArgumentNullException.ThrowIfNull(onTargeted);

        _onTargeted = onTargeted;
        _accepts = accepts;
        _targetCallback = OnPicked;

        Tooltip = tooltip;
        OnClick = () => World.Instance?.TargetManager.SetTargeting(_targetCallback);

        // MyraButton's caption defaults to left-aligned, which shows once the button is wider than its
        // text - as a fixed-width row of these is.
        Content?.HorizontalAlignment = HorizontalAlignment.Center;
    }

    #endregion

    #region Protected methods

    /// <inheritdoc />
    protected override void OnPlacedChanged()
    {
        base.OnPlacedChanged();

        if (Desktop == null)
            CancelOwnTargeting();
    }

    #endregion

    #region Private methods

    /// <summary>Stands the cursor down if it is still armed for this button. Runs the callback with
    /// null, so a listening owner hears the pick was abandoned.</summary>
    private void CancelOwnTargeting()
    {
        TargetManager? targeting = World.Instance?.TargetManager;

        // Can race, but the worst case is a stray armed cursor.
        if (targeting?.IsCurrentTargetingAction(_targetCallback) == true)
            targeting.CancelTarget();
    }

    private void OnPicked(object? picked)
    {
        if (picked is not Entity entity || (_accepts != null && !_accepts(entity)))
        {
            _onTargeted(null);
            return;
        }

        _onTargeted(entity.Serial);
    }

    #endregion
}
