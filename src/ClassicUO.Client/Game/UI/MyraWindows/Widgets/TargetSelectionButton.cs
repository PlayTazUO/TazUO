#nullable enable

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
/// A button that puts the player into targeting mode and reports back whatever they pick - the one
/// way a config screen lets the player point at a world object instead of typing its serial by hand.
/// </summary>
public sealed class TargetSelectionButton : MyraButton
{
    #region Ctor

    /// <param name="onTargeted">
    /// Called with the picked serial, or null if the player cancelled (right-clicked, pressed Esc) or
    /// picked something <paramref name="accepts" /> rejected.
    /// </param>
    /// <param name="accepts">Filters what counts as a valid pick - an item-only picker, say. Anything
    /// the world resolves the serial to is accepted if omitted.</param>
    /// <param name="caption">Button text.</param>
    /// <param name="tooltip">Optional tooltip.</param>
    public TargetSelectionButton(
        Action<uint?> onTargeted,
        Func<Entity, bool>? accepts = null,
        string? caption = null,
        string? tooltip = null
    ) : base(caption ?? TazLang.Get("targetselectionbutton_target", "Target"))
    {
        ArgumentNullException.ThrowIfNull(onTargeted);

        Tooltip = tooltip;
        OnClick = () => World.Instance?.TargetManager.SetTargeting(picked => OnPicked(picked, accepts, onTargeted));
    }

    #endregion

    #region Private methods

    private static void OnPicked(object? picked, Func<Entity, bool>? accepts, Action<uint?> onTargeted)
    {
        if (picked is not Entity entity || (accepts != null && !accepts(entity)))
        {
            onTargeted(null);
            return;
        }

        onTargeted(entity.Serial);
    }

    #endregion
}-559038737
