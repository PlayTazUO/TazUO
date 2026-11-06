#nullable enable

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Tinkerer;

/// <summary>
///     Shared "Target" button for the Tinkerer browser tabs. Arms the target cursor and hands the picked
///     object - an item, mobile, static, or land tile - to the tab so it can jump straight to it.
/// </summary>
internal static class TinkererTargeting
{
    /// <summary>Builds a button that reports the targeted world object to <paramref name="onTargeted" />.</summary>
    /// <param name="onTargeted">Called with the picked object. Not called when the player cancels.</param>
    /// <param name="tooltip">Tooltip describing what the target is used for on this tab.</param>
    public static MyraButton CreateButton(Action<GameObject> onTargeted, string tooltip)
    {
        ArgumentNullException.ThrowIfNull(onTargeted);

        return new TinkererTargetButton(onTargeted, tooltip);
    }

    /// <summary>
    ///     The land graphic at the picked object's tile, falling back to the land underneath it when the
    ///     player targeted something standing on the tile.
    /// </summary>
    public static int GetLandGraphic(GameObject picked)
    {
        if (picked is Land land)
            return land.Graphic;

        return World.Instance?.Map.GetTile(picked.X, picked.Y) is Land under ? under.Graphic : -1;
    }

    private sealed class TinkererTargetButton : MyraButton
    {
        private readonly Action<GameObject> _onTargeted;

        /// <summary>Reused for every arming: it is the identity
        /// <see cref="TargetManager.IsCurrentTargetingAction" /> matches on.</summary>
        private readonly Action<object> _targetCallback;

        public TinkererTargetButton(Action<GameObject> onTargeted, string tooltip)
            : base(TazLang.Get("targetselectionbutton_target", "Target"))
        {
            _onTargeted = onTargeted;
            _targetCallback = OnPicked;

            Tooltip = tooltip;
            OnClick = () => World.Instance?.TargetManager.SetTargeting(_targetCallback);
            Content?.HorizontalAlignment = HorizontalAlignment.Center;
        }

        protected override void OnPlacedChanged()
        {
            base.OnPlacedChanged();

            if (Desktop == null)
                CancelOwnTargeting();
        }

        private void CancelOwnTargeting()
        {
            TargetManager? targeting = World.Instance?.TargetManager;

            if (targeting?.IsCurrentTargetingAction(_targetCallback) == true)
                targeting.CancelTarget();
        }

        private void OnPicked(object? picked)
        {
            if (picked is GameObject gameObject)
                _onTargeted(gameObject);
        }
    }
}
