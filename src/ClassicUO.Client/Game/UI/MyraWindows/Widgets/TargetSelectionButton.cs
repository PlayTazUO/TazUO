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
/// Disarms itself when it leaves the widget tree, so closing a screen mid-pick does not leave the
/// cursor armed against it. Only its own arming is cancelled - a cursor since claimed by something
/// else is left alone.
/// </para>
/// </summary>
public sealed class TargetSelectionButton : MyraButton
{
    #region Private members

    private readonly Action<uint?> _onTargeted;
    private readonly Func<Entity, bool>? _accepts;

    /// <summary>Built once and reused for every arming, because it is the identity
    /// <see cref="TargetManager.IsCurrentTargetingAction" /> matches on. A fresh lambda per click
    /// would leave nothing to recognise this button's arming by.</summary>
    private readonly Action<object> _targetCallback;

    #endregion

    #region Ctor

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

        // MyraButton's own caption label defaults to left-aligned, which only shows once a caller
        // gives the button more width than its text needs - as a fixed-width row of these does.
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

    /// <summary>
    /// Stands the cursor down if it is still armed for this button. Cancelling runs the callback with
    /// null, so an owner still listening hears that the pick was abandoned rather than waiting on one
    /// that can no longer arrive.
    /// </summary>
    private void CancelOwnTargeting()
    {
        TargetManager? targeting = World.Instance?.TargetManager;

        // Could technically race but this is a nitpick anyway, just trying to keep things tidy.
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
