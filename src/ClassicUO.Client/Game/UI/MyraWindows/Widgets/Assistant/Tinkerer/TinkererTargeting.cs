#nullable enable

using System;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Tinkerer;

/// <summary>
///     Shared "Target" button for the Tinkerer browser tabs. Arms the target cursor, resolves the picked
///     entity, and hands it to the tab so it can jump straight to that object's graphic.
/// </summary>
internal static class TinkererTargeting
{
    /// <summary>Builds a button that reports the targeted world entity to <paramref name="onTargeted" />.</summary>
    /// <param name="onTargeted">Called with the picked entity. Not called when the player cancels.</param>
    /// <param name="tooltip">Tooltip describing what the target is used for on this tab.</param>
    public static TargetSelectionButton CreateButton(Action<Entity> onTargeted, string tooltip)
    {
        ArgumentNullException.ThrowIfNull(onTargeted);

        return new TargetSelectionButton(
            serial =>
            {
                if (serial is { } picked && World.Instance?.Get(picked) is { } entity)
                    onTargeted(entity);
            },
            tooltip: tooltip
        );
    }
}
