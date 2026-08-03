using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using SDL3;

namespace ClassicUO.Game.UI.Gumps.Login
{
    /// <summary>
    /// Shared base for the character selection screens. Holds the view-independent flow
    /// (character enumeration, selection/login/delete, keyboard + controller handling, and
    /// the live style toggle) so the classic list and the campfire arc can both derive from it.
    /// </summary>
    public abstract class CharacterSelectionGumpBase : Gump
    {
        protected const ushort SELECTED_COLOR = 0x0021;
        protected const ushort NORMAL_COLOR = 0x034F;

        protected uint _selectedCharacter;

        /// <summary>Character indices in display order, used for arrow/wheel cycling.</summary>
        protected readonly List<uint> CharOrder = new();

        protected CharacterSelectionGumpBase(World world) : base(world, 0, 0)
        {
            CanCloseWithRightClick = false;
            AcceptKeyboardInput = true;
        }

        protected enum Buttons
        {
            New,
            Delete,
            Next,
            Prev,
            ToggleStyle
        }

        protected struct CharSlot
        {
            public uint Index;
            public string Name;
            public LemCharData? Lem;
        }

        /// <summary>
        /// Defaults <see cref="_selectedCharacter"/> to the last-played character (a saved setting),
        /// so pressing Enter immediately logs into it.
        /// </summary>
        protected void ResolveInitialSelection(LoginScene loginScene)
        {
            string lastCharName = LastCharacterManager.GetLastCharacter(LoginScene.Account, World.ServerName);
            string lastSelected = loginScene.Characters.FirstOrDefault(o => o == lastCharName);

            if (!string.IsNullOrEmpty(lastSelected))
            {
                _selectedCharacter = (uint)Array.IndexOf(loginScene.Characters, lastSelected);
            }
            else if (loginScene.Characters.Length > 0)
            {
                _selectedCharacter = 0;
            }
        }

        /// <summary>
        /// Enumerates the valid (non-empty, slot-allowed) characters honoring MaxChars and the
        /// 6th/7th locked-feature flags, loading each one's last-equipment snapshot.
        /// </summary>
        protected List<CharSlot> EnumerateValidCharacters(LoginScene loginScene)
        {
            var result = new List<CharSlot>();

            for (int i = 0, valid = 0; i < loginScene.Characters.Length; i++)
            {
                string character = loginScene.Characters[i];

                if (!character.NotNullNotEmpty())
                    continue;

                LemCharData? lem = LastEquipmentManager.Load(LoginHandshake.Instance.LastServerName, character, LoginHandshake.Account);

                valid++;

                if (valid > World.ClientFeatures.MaxChars)
                    break;

                if (World.ClientLockedFeatures.Flags != 0 && !World.ClientLockedFeatures.Flags.HasFlag(LockedFeatureFlags.SeventhCharacterSlot))
                    if (valid == 6 && !World.ClientLockedFeatures.Flags.HasFlag(LockedFeatureFlags.SixthCharacterSlot))
                        break;

                result.Add(new CharSlot { Index = (uint)i, Name = character, Lem = lem });
            }

            return result;
        }

        /// <summary>
        /// Builds a placeable paperdoll view from a character's last-equipment snapshot.
        /// </summary>
        protected StaticPaperDollView BuildPaperDoll(LemCharData lem, Vector2 size, bool background)
        {
            var equipment = new Dictionary<Layer, StaticPaperDollView.EquipmentEntry>();

            foreach (KeyValuePair<Layer, LemEquipmentEntry> kvp in lem.Equipment)
            {
                equipment[kvp.Key] = new StaticPaperDollView.EquipmentEntry(
                    kvp.Value.AnimID, kvp.Value.Hue, kvp.Value.IsPartialHue);
            }

            return new StaticPaperDollView(lem.PlayerGraphic, lem.BodyHue, lem.IsFemale, equipment, size, background);
        }

        /// <summary>Cycles the current selection through <see cref="CharOrder"/> with wrap-around.</summary>
        protected void CycleSelection(int delta)
        {
            if (CharOrder.Count == 0)
                return;

            int i = CharOrder.IndexOf(_selectedCharacter);
            if (i < 0)
                i = 0;

            i = (i + delta) % CharOrder.Count;
            if (i < 0)
                i += CharOrder.Count;

            SelectCharacter(CharOrder[i]);
        }

        protected virtual void SelectCharacter(uint index) => _selectedCharacter = index;

        protected void LoginCharacter(uint index)
        {
            LoginScene loginScene = Client.Game.GetScene<LoginScene>();

            if (loginScene.Characters != null && loginScene.Characters.Length > index && !string.IsNullOrEmpty(loginScene.Characters[index]))
            {
                loginScene.SelectCharacter(index);
            }
        }

        protected bool CanCreateChar(LoginScene scene)
        {
            if (scene.Characters != null)
            {
                int empty = scene.Characters.Count(string.IsNullOrEmpty);

                if (empty >= 0 && scene.Characters.Length - empty < World.ClientFeatures.MaxChars)
                {
                    return true;
                }
            }

            return false;
        }

        protected void DeleteCharacter(LoginScene loginScene)
        {
            string charName = loginScene.Characters[_selectedCharacter];

            if (!string.IsNullOrEmpty(charName))
            {
                LoadingGump existing = Children.OfType<LoadingGump>().FirstOrDefault();

                if (existing != null)
                {
                    Remove(existing);
                }

                Add
                (
                    new LoadingGump
                    (
                        World,
                        string.Format(TazLang.Get("permanently_delete0"), charName),
                        LoginButtons.OK | LoginButtons.Cancel,
                        buttonID =>
                        {
                            if (buttonID == (int)LoginButtons.OK)
                            {
                                loginScene.DeleteCharacter(_selectedCharacter);
                            }
                            else
                            {
                                ChangePage(1);
                            }
                        }
                    ),
                    2
                );

                ChangePage(2);
            }
        }

        /// <summary>Flips the SQL-backed style setting and rebuilds the screen live.</summary>
        protected void ToggleSelectionStyle()
        {
            Settings.GlobalSettings.UseCampfireCharacterSelect = !Settings.GlobalSettings.UseCampfireCharacterSelect;
            Client.Game.GetScene<LoginScene>().RebuildCharacterSelection();
        }

        protected override void OnControllerButtonUp(SDL.SDL_GamepadButton button)
        {
            base.OnControllerButtonUp(button);

            if (button == SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH)
            {
                LoginCharacter(_selectedCharacter);
            }
        }

        public override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            switch (key)
            {
                case SDL.SDL_Keycode.SDLK_RETURN:
                case SDL.SDL_Keycode.SDLK_KP_ENTER:
                    LoginCharacter(_selectedCharacter);
                    break;

                case SDL.SDL_Keycode.SDLK_UP:
                case SDL.SDL_Keycode.SDLK_LEFT:
                    CycleSelection(-1);
                    break;

                case SDL.SDL_Keycode.SDLK_DOWN:
                case SDL.SDL_Keycode.SDLK_RIGHT:
                    CycleSelection(1);
                    break;
            }
        }

        public override void OnMouseWheel(MouseEventType delta)
        {
            base.OnMouseWheel(delta);

            if (MouseEventType.WheelScrollUp == delta)
                CycleSelection(-1);
            else
                CycleSelection(1);
        }

        public override void OnButtonClick(int buttonID)
        {
            LoginScene loginScene = Client.Game.GetScene<LoginScene>();

            switch ((Buttons)buttonID)
            {
                case Buttons.Delete:
                    DeleteCharacter(loginScene);
                    break;

                case Buttons.New when CanCreateChar(loginScene):
                    loginScene.StartCharCreation();
                    break;

                case Buttons.Next:
                    LoginCharacter(_selectedCharacter);
                    break;

                case Buttons.Prev:
                    loginScene.StepBack();
                    break;

                case Buttons.ToggleStyle:
                    ToggleSelectionStyle();
                    break;
            }

            base.OnButtonClick(buttonID);
        }
    }
}
