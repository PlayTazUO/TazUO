using System;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers.Hotkeys;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using SDL3;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// Bridges counter-bar cell hotkeys onto the central <see cref="HotKeys"/> registry. Each hotkeyed
    /// cell is a normal <see cref="HotKeyEntry"/> (id <c>counterbar:&lt;index&gt;</c>) so dispatch,
    /// conflict detection and the shared capture UI all work exactly like every other hotkey.
    ///
    /// Unlike most consumers, the binding's source of truth is the counter bar's gump XML (saved with
    /// the cell), not <c>hotkeys.json</c>: on restore the XML binding is re-applied over the entry
    /// (<see cref="SetBinding"/> registers then overwrites), so the XML always wins. The copy the
    /// central system writes to <c>hotkeys.json</c> is just an incidental runtime mirror.
    ///
    /// Key bindings fire through the entry's OnPressed callback (dispatched by
    /// <see cref="HotKeys.HandleKeyDown"/>, already focus-gated and repeat-ignoring). Mouse and
    /// controller bindings have no such dispatch, so we listen to the raw button-down events and use
    /// the matching cell when <c>HotKeys.Get(id).IsPressed()</c> reports it active.
    /// </summary>
    internal static class CounterBarHotkeysManager
    {
        private const string IdPrefix = "counterbar:";
        private const string Category = "Counter Bar";

        private static bool _subscribed;

        public static string MakeId(int index) => IdPrefix + index;

        /// <summary>Current binding for the cell at <paramref name="index"/>, or an empty binding when unset.</summary>
        public static HotkeyBinding GetBinding(int index)
        {
            HotKeyEntry entry = HotKeys.Get(MakeId(index));
            return entry?.Binding?.Clone() ?? new HotkeyBinding();
        }

        /// <summary>
        /// Set the hotkey for the cell at <paramref name="index"/> (or clear it when the binding isn't
        /// usable). Registers the entry with the central hotkey system; the caller is responsible for
        /// persisting the binding into the gump XML.
        /// </summary>
        public static void SetBinding(int index, HotkeyBinding binding)
        {
            EnsureSubscribed();

            string id = MakeId(index);

            // Only bindings we can actually trigger are accepted: a key (OnPressed dispatch) or a mouse
            // button / controller button (button-down listeners). Empty, wheel and modifier-only
            // bindings can't reliably fire a cell, so treat them as a clear.
            if (!IsUsable(binding))
            {
                HotKeys.Unregister(id);
                return;
            }

            HotKeyEntry entry = HotKeys.Register(id, DisplayName(index), new HotkeyBinding(), Category, () => Activate(index));
            // The just-set binding (from capture or from the gump XML) wins over any stale hotkeys.json value.
            entry.Binding = binding.Clone();
        }

        /// <summary>Remove the hotkey bound to the cell at <paramref name="index"/>.</summary>
        public static void ClearBinding(int index) => HotKeys.Unregister(MakeId(index));

        /// <summary>
        /// Drop any registered cell hotkeys at or beyond <paramref name="count"/>. Called when the
        /// counter bar shrinks so removed cells don't keep firing or linger in the registry.
        /// </summary>
        public static void PruneFrom(int count)
        {
            foreach ((string id, int index) in RegisteredCells())
                if (index >= count)
                    HotKeys.Unregister(id);
        }

        private static void EnsureSubscribed()
        {
            if (_subscribed)
                return;

            Mouse.ButtonDownEvent += OnMouseButtonDown;
            Controller.ButtonDownEvent += OnControllerButtonDown;
            _subscribed = true;
        }

        // Match the specific button that fired so an unrelated click/press can't retrigger a cell whose
        // (different) bound button merely happens to still be held.
        private static void OnMouseButtonDown(MouseButtonType button)
            => FirePressed(b => b.HasMouseButton && b.MouseButton == button);

        private static void OnControllerButtonDown(SDL.SDL_GamepadButton button)
            => FirePressed(b => b.HasController && b.ControllerButtons != null && Array.IndexOf(b.ControllerButtons, button) >= 0);

        private static void FirePressed(Func<HotkeyBinding, bool> isKind)
        {
            // Mirror the keyboard path's gate: only dispatch while the game world owns input, so a
            // button can't fire a cell while it is being (re)bound in the capture box or a window has focus.
            if (!WorldHasInputFocus() || HotKeys.GloballyDisabled)
                return;

            foreach ((string id, int index) in RegisteredCells())
            {
                HotKeyEntry entry = HotKeys.Get(id);
                // Exact modifier match (like HotKeys.HandleKeyDown) so a no-modifier button binding
                // doesn't also fire while modifiers are held.
                if (entry != null && entry.Enabled && entry.Binding != null && isKind(entry.Binding) && entry.IsPressed(allowAdditionalModifiers: false))
                    Activate(index);
            }
        }

        /// <summary>Enumerates the currently registered counter-bar cell entries as (id, cell index) pairs.</summary>
        private static System.Collections.Generic.IEnumerable<(string id, int index)> RegisteredCells()
        {
            foreach (HotKeyEntry entry in HotKeys.AllRegistered().ToArray())
            {
                if (!entry.Id.StartsWith(IdPrefix, StringComparison.Ordinal))
                    continue;

                if (int.TryParse(entry.Id.AsSpan(IdPrefix.Length), out int index))
                    yield return (entry.Id, index);
            }
        }

        // Equivalent to GameSceneInputHandler.CanExecuteMacro: input belongs to the world (the system
        // chat box holds focus and isn't mid-compose), not to a gump/window such as the capture box.
        private static bool WorldHasInputFocus()
        {
            SystemChatControl chat = UIManager.SystemChat;
            return chat != null
                   && UIManager.KeyboardFocusControl == chat.TextBoxControl
                   && chat.Mode >= ChatMode.Default;
        }

        private static void Activate(int index)
        {
            // Don't fire while the counter bar is toggled off (the gump is kept but disabled/hidden).
            if (CounterBarGump.CurrentCounterBarGump is { IsEnabled: true } gump)
                gump.GetCounterItem(index)?.ActivateFromHotkey();
        }

        private static string DisplayName(int index) => TazLang.Get("counterbar_slot", new[] { (index + 1).ToString() });

        // A binding can fire a cell only when it has a key (OnPressed dispatch) or a mouse/controller
        // button (button-down listeners). Wheel and modifier-only bindings can't.
        private static bool IsUsable(HotkeyBinding binding)
            => binding != null && (binding.HasKey || binding.HasMouseButton || binding.HasController);
    }
}
