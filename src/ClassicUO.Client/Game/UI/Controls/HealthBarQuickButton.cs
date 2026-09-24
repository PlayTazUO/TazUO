using ClassicUO.Common.Enums;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;

namespace ClassicUO.Game.UI.Controls
{
    /// <summary>
    /// Heal or cure button shown on health-bar gumps. Keeps the standard heal/cure hand icon but its
    /// tooltip names the action currently configured for the slot (<see cref="Profile.QuickHealAction"/>
    /// or <see cref="Profile.QuickCureAction"/>), and clicking performs that action on the bar's target.
    /// The tooltip refreshes automatically if the configured action changes while the bar is open.
    /// </summary>
    public class HealthBarQuickButton : Button
    {
        public const ushort HealGraphic = 0x0938;
        public const ushort CureGraphic = 0x0939;
        public const ushort PressedGraphic = 0x093A;

        /// <summary>Which of the two health-bar quick action slots this button drives.</summary>
        public enum Slot
        {
            Heal,
            Cure
        }

        private readonly World _world;
        private readonly uint _targetSerial;
        private readonly Slot _slot;
        private HealthBarQuickAction _shownAction;

        /// <summary>
        /// Creates a quick action button.
        /// </summary>
        /// <param name="world">The world the action is performed in.</param>
        /// <param name="targetSerial">Serial of the mobile the action is applied to.</param>
        /// <param name="slot">Whether this is the heal or cure slot.</param>
        /// <param name="x">Horizontal position within the parent gump.</param>
        /// <param name="y">Vertical position within the parent gump.</param>
        public HealthBarQuickButton(World world, uint targetSerial, Slot slot, int x, int y)
            : base(
                (int)slot,
                slot == Slot.Heal ? HealGraphic : CureGraphic,
                PressedGraphic,
                slot == Slot.Heal ? HealGraphic : CureGraphic)
        {
            _world = world;
            _targetSerial = targetSerial;
            _slot = slot;

            X = x;
            Y = y;
            ButtonAction = ButtonAction.Activate;

            Refresh();
        }

        private bool IsHeal => _slot == Slot.Heal;

        private HealthBarQuickAction Action =>
            IsHeal ? ProfileManager.CurrentProfile.QuickHealAction : ProfileManager.CurrentProfile.QuickCureAction;

        /// <summary>Rebuilds the tooltip to name the action currently configured for this slot.</summary>
        public void Refresh()
        {
            _shownAction = Action;

            string slot = TazLang.Get(IsHeal ? "healthbar_quickheal" : "healthbar_quickcure");
            SetTooltip($"{slot}: {_shownAction.GetDisplayName()}");
        }

        public override void Update()
        {
            base.Update();

            if (Action != _shownAction)
                Refresh();
        }

        public override void OnButtonClick(int buttonID) =>
            GameActions.QuickAction(_world, _targetSerial, Action);
    }
}
