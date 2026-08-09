// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.Gumps.CharCreation;
using ClassicUO.Game.UI.Gumps.Login;
using ClassicUO.Game.UI.MyraWindows;
using ClassicUO.Network;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Scenes
{
    public enum LoginSteps
    {
        Main,
        Connecting,
        VerifyingAccount,
        ServerSelection,
        LoginInToServer,
        CharacterSelection,
        EnteringBritania,
        CharacterCreation,
        CharacterCreationDone,
        PopUpMessage
    }

    public sealed class LoginScene : Scene
    {
        public static LoginScene Instance { get; private set; }

        private Gump _currentGump;
        private LoginSteps _lastLoginStep;
        private bool _autoLogin;
        private readonly World _world;

        public LoginScene(World world)
        {
            Instance?.Dispose();
            _world = world;
            Instance = this;
            LoginHandshake.Instance.ShouldReconnect = Settings.GlobalSettings.Reconnect;
            LoginHandshake.Instance.LoginStepChanged += OnLoginStepChanged;
            LoginHandshake.Instance.ReceiveCharacterListNotifier += ReceiveCharacterList;
            LoginHandshake.Instance.UpdateCharacterListNotifier += UpdateCharacterList;
            Client.Game.ScaleChanged += GameOnScaleChanged;
        }

        public bool Reconnect
        {
            get => LoginHandshake.Reconnect;
            set => LoginHandshake.Reconnect = value;
        }

        public LoginSteps CurrentLoginStep
        {
            get => LoginHandshake.Instance.CurrentLoginStep;
            set => LoginHandshake.Instance.SetLoginStep(value);
        }

        public ServerListEntry[] Servers => LoginHandshake.Instance.Servers;
        public CityInfo[] Cities
        {
            get => LoginHandshake.Instance.Cities;
            set => LoginHandshake.Instance.Cities = value;
        }
        public string[] Characters => LoginHandshake.Instance.Characters;
        public string PopupMessage { get; set; }
        public byte ServerIndex => LoginHandshake.Instance.ServerIndex;
        public static string Account { get; internal set; }
        private string Password { get; set; }
        public bool CanAutologin => _autoLogin || Reconnect;
        public (int min, int max) LoginDelay => LoginHandshake.Instance.LoginDelay;

        private void GameOnScaleChanged(object sender, float e) => UpdateWindowSize();

        public override void Load()
        {
            base.Load();

            Client.Game.Window.AllowUserResizing = false;

            _autoLogin = Settings.GlobalSettings.AutoLogin;

            UIManager.Add(new LoginBackground(_world));

            if (string.IsNullOrEmpty(Settings.GlobalSettings.IP))
            {
                new PromptPopupWindow("Server IP", "Please enter a server IP to connect to", input =>
                {
                    if (!string.IsNullOrEmpty(input))
                    {
                        if (Settings.GlobalSettings.Port <= 0)
                        {
                            new PromptPopupWindow("Server Port", "Please enter the port for this server", portInput =>
                            {
                                if (!string.IsNullOrEmpty(portInput) && ushort.TryParse(portInput, out ushort p))
                                {
                                    Settings.GlobalSettings.Port = p;
                                }
                                UIManager.Add(_currentGump = new LoginGump(_world, this));
                            }, "Save", "Cancel", () => UIManager.Add(_currentGump = new LoginGump(_world, this)));
                        }
                        else //Port is > 0, possibly valid
                        {
                            UIManager.Add(_currentGump = new LoginGump(_world, this));
                        }
                        Settings.GlobalSettings.IP = input;
                    }
                    else //Cancel ip entry
                    {
                        UIManager.Add(_currentGump = new LoginGump(_world, this));
                    }
                }, "Save", "Cancel", () => UIManager.Add(_currentGump = new LoginGump(_world, this)));
            }
            else
            {
                UIManager.Add(_currentGump = new LoginGump(_world, this));
            }

            Client.Game.Audio.PlayMusic(Client.Game.Audio.LoginMusicIndex, false, true);

            if (CanAutologin && CurrentLoginStep != LoginSteps.Main || CUOEnviroment.SkipLoginScreen && _currentGump != null)
            {
                if (!string.IsNullOrEmpty(Settings.GlobalSettings.Username))
                {
                    // disable if it's the 2nd attempt
                    CUOEnviroment.SkipLoginScreen = false;
                    Connect(Settings.GlobalSettings.Username, Crypter.Decrypt(Settings.GlobalSettings.Password));
                }
            }

            if (Client.Game.IsWindowMaximized())
            {
                Client.Game.RestoreWindow();
            }

            UpdateWindowSize();
        }

        private void UpdateWindowSize() => Client.Game.SetWindowSize((int)(640 * Client.Game.RenderScale), (int)(480 * Client.Game.RenderScale));

        public override void Unload()
        {
            if (IsDestroyed)
            {
                return;
            }

            Client.Game.Audio?.StopMusic();
            Client.Game.Audio?.StopSounds();

            UIManager.GetGump<LoginBackground>()?.Dispose();

            _currentGump?.Dispose();

            Client.Game.UO.GameCursor.IsLoading = false;
            base.Unload();
        }

        private void OnLoginStepChanged(object sender, LoginSteps newStep)
        {
            switch (newStep)
            {
                case LoginSteps.ServerSelection:
                    if (CanAutologin && Servers != null && Servers.Length != 0)
                    {
                        int index = GetServerIndexFromSettings();

                        // Loop through servers to find the one with matching Index property
                        for (int i = 0; i < Servers.Length; i++)
                        {
                            if (Servers[i].Index == index)
                            {
                                SelectServer((byte)index);
                                break;
                            }
                        }
                    }
                    break;
                case LoginSteps.LoginInToServer:
                    Settings.GlobalSettings.LastServerNum = LoginHandshake.Instance.LastServerNum;
                    Settings.GlobalSettings.LastServerName = LoginHandshake.Instance.LastServerName;
                    Settings.GlobalSettings.Save();
                    break;
                case LoginSteps.CharacterSelection:
                    _world.ClientFeatures.SetFlags((CharacterListFlags)LoginHandshake.Instance.CharacterListFlags);
                    break;
                case LoginSteps.PopUpMessage:
                    if(LoginHandshake.Instance.ErrorPacket != byte.MaxValue)
                        PopupMessage = ServerErrorMessages.GetError(LoginHandshake.Instance.ErrorPacket, LoginHandshake.Instance.ErrorCode, LoginDelay);
                    else if(!string.IsNullOrEmpty(LoginHandshake.Instance.ErrorMessage))
                        PopupMessage = LoginHandshake.Instance.ErrorMessage;
                    break;

                case LoginSteps.Main:
                case LoginSteps.Connecting:
                case LoginSteps.VerifyingAccount:
                case LoginSteps.EnteringBritania:
                case LoginSteps.CharacterCreation:
                case LoginSteps.CharacterCreationDone:
                default:
                    break;
            }

            if (_lastLoginStep == newStep)
                return;

            // This trick is to avoid UI flickering
            //
            // Note that this callback may be run from the threadpool so using MT dispatch can help mitigate concurrent modification issues
            //
            // This is a sort-of deferred refresh, not a strict state machine; The MT disposes the previous UI and renders
            // whatever's right for the state that happens to be current when the callback is invoked
            Gump g = _currentGump;
            MainThreadQueue.InvokeOnMainThread(() =>
            {
                // Since this is slightly deferred, we could've been disposed in the time between enqueuing and invocation.
                // We don't wanna mutate UI if that's the case
                if (IsDestroyed)
                    return;

                Client.Game.UO.GameCursor.IsLoading = false;

                Gump next = GetGumpForStep();

                // Dispose any login screens left over from a previous step before showing the next one.
                // Step changes can be dispatched from the network thread, so a racing transition may
                // capture a stale '_currentGump' and orphan the screen an earlier deferred callback
                // created (most visibly the server selection gump lingering behind the login screen).
                // Only one of these interactive screens should ever be visible, so clear the rest.
                DisposeStaleLoginScreens(next);

                UIManager.Add(_currentGump = next);
                g?.Dispose();
            });

            _lastLoginStep = newStep;
        }

        public override void Update()
        {
            base.Update();

            LoginHandshake.Instance.CheckHandshakeTimeout();
            LoginHandshake.Instance.HandleReconnect(Settings.GlobalSettings.ReconnectTime * 1000);
            LoginHandshake.Instance.SendPing();
        }

        /// <summary>
        /// Disposes any lingering login-flow screens that aren't the one we're about to show.
        /// These screens are mutually exclusive, so anything orphaned by a racing step change
        /// (e.g. a server selection gump stuck behind the login screen) gets cleared here.
        /// </summary>
        private static void DisposeStaleLoginScreens(Gump keep)
        {
            DisposeStaleGumpsOfType<LoginGump>(keep);
            DisposeStaleGumpsOfType<ServerSelectionGump>(keep);
            DisposeStaleGumpsOfType<CharacterSelectionGumpBase>(keep);
            DisposeStaleGumpsOfType<CharCreationGump>(keep);
        }

        private static void DisposeStaleGumpsOfType<T>(Gump keep) where T : Gump
        {
            // 'keep' has not been added to the UIManager yet, so GetGump never returns it; the
            // reference check is just a safety net to avoid disposing the incoming screen.
            for (T g = UIManager.GetGump<T>(); g != null && !ReferenceEquals(g, keep); g = UIManager.GetGump<T>())
            {
                g.Dispose();
            }
        }

        private Gump GetGumpForStep()
        {
            foreach (Item item in _world.Items.Values)
            {
                _world.RemoveItem(item);
            }

            foreach (Mobile mobile in _world.Mobiles.Values)
            {
                _world.RemoveMobile(mobile);
            }

            _world.Mobiles.Clear();
            _world.Items.Clear();

            switch (CurrentLoginStep)
            {
                case LoginSteps.Main:
                    PopupMessage = null;

                    return new LoginGump(_world,this);

                case LoginSteps.Connecting:
                case LoginSteps.VerifyingAccount:
                case LoginSteps.LoginInToServer:
                case LoginSteps.EnteringBritania:
                case LoginSteps.PopUpMessage:
                case LoginSteps.CharacterCreationDone:
                    Client.Game.UO.GameCursor.IsLoading = CurrentLoginStep != LoginSteps.PopUpMessage;

                    return GetLoadingScreen();

                case LoginSteps.CharacterSelection: return CreateCharacterSelectionGump();

                case LoginSteps.ServerSelection:
                    return new ServerSelectionGump(_world);

                case LoginSteps.CharacterCreation:
                    return new CharCreationGump(_world,this);
            }

            return null;
        }

        private LoadingGump GetLoadingScreen()
        {
            string labelText = "No Text";
            LoginButtons showButtons = LoginButtons.None;

            if (!string.IsNullOrEmpty(PopupMessage))
            {
                labelText = PopupMessage;
                showButtons = LoginButtons.OK;
                PopupMessage = null;
            }
            else
            {
                switch (CurrentLoginStep)
                {
                    case LoginSteps.Connecting:
                        labelText = Client.Game.UO.FileManager.Clilocs.GetString(3000002, TazLang.Get("connecting")); // "Connecting..."

                        showButtons = LoginButtons.Cancel;

                        break;

                    case LoginSteps.VerifyingAccount:
                        labelText = Client.Game.UO.FileManager.Clilocs.GetString(3000003, TazLang.Get("verifying_account")); // "Verifying Account..."

                        showButtons = LoginButtons.Cancel;

                        break;

                    case LoginSteps.LoginInToServer:
                        labelText = Client.Game.UO.FileManager.Clilocs.GetString(3000053, TazLang.Get("logging_into_shard")); // logging into shard

                        showButtons = LoginButtons.Cancel;
                        break;

                    case LoginSteps.EnteringBritania:
                        labelText = Client.Game.UO.FileManager.Clilocs.GetString(3000001, TazLang.Get("entering_britannia")); // Entering Britania...

                        break;

                    case LoginSteps.CharacterCreationDone:
                        labelText = TazLang.Get("creating_character");

                        break;
                }
            }

            return new LoadingGump(_world, labelText, showButtons, OnLoadingGumpButtonClick);
        }

        private void OnLoadingGumpButtonClick(int buttonId)
        {
            var butt = (LoginButtons)buttonId;

            if (butt == LoginButtons.OK || butt == LoginButtons.Cancel)
            {
                StepBack();
            }
        }

        public void Connect(string account, string password)
        {
            Account = account;
            Password = password;
            LoginHandshake.Instance.Connect(account, password, Settings.GlobalSettings.IP, Settings.GlobalSettings.Port);

            // Save credentials to config file
            if (Settings.GlobalSettings.SaveAccount)
            {
                Settings.GlobalSettings.Username = account;
                Settings.GlobalSettings.Password = Crypter.Encrypt(password);
                SimpleAccountManager.SetAccountPassword(account, Settings.GlobalSettings.Password);
                try
                {
                    Settings.GlobalSettings.Save();
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to save settings: {ex}");
                }
            }
        }

        public int GetServerIndexByName(string name) => LoginHandshake.Instance.GetServerIndexByName(name);

        public int GetServerIndexFromSettings()
        {
            string name = Settings.GlobalSettings.LastServerName;
            int index = GetServerIndexByName(name);

            if (index == -1)
            {
                index = Settings.GlobalSettings.LastServerNum;
            }

            if (Servers == null || index < 0) //Server indexis received from the server, it does not always correlate with the server count/list
            {
                index = 0;
            }

            return index;
        }

        public void SelectServer(byte index)
        {
            if (Servers == null || Servers.Length == 0)
                return;

            // Loop through servers to find the one with matching Index property
            string serverName = "";
            for (int i = 0; i < Servers.Length; i++)
            {
                if (Servers[i].Index == index)
                {
                    serverName = Servers[i].Name;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(serverName))
            {
                LoginHandshake.Instance.SelectServer(index, serverName);
            }
        }

        public void SelectCharacter(uint index)
        {
            if (CurrentLoginStep == LoginSteps.CharacterSelection)
            {
                LastCharacterManager.Save(Account, _world.ServerName, Characters[index]);

                LoginHandshake.Instance.SendSelectCharacter(index);
            }
        }

        public void StartCharCreation()
        {
            if (CurrentLoginStep == LoginSteps.CharacterSelection)
            {
                LoginHandshake.Instance.SetLoginStep(LoginSteps.CharacterCreation);
            }
        }

        public void CreateCharacter(PlayerMobile character, int cityIndex, byte profession)
        {
            int i = 0;

            for (; i < Characters.Length; i++)
            {
                if (string.IsNullOrEmpty(Characters[i]))
                {
                    break;
                }
            }

            LastCharacterManager.Save(Account, _world.ServerName, character.Name);

            //Ideally we want to move this to LoginHandshake, but I want to avoid the Game namespace there.
            AsyncNetClient.Socket.Send_CreateCharacter(character,
                                                  cityIndex,
                                                  AsyncNetClient.Socket.LocalIP,
                                                  ServerIndex,
                                                  (uint)i,
                                                  profession);

            LoginHandshake.Instance.SetLoginStep(LoginSteps.CharacterCreationDone);
        }

        public void DeleteCharacter(uint index) => LoginHandshake.Instance.SendDeleteCharacter(index);

        public void StepBack()
        {
            PopupMessage = null;

            if (Characters != null && CurrentLoginStep != LoginSteps.CharacterCreation && CurrentLoginStep != LoginSteps.ServerSelection)
            {
                LoginHandshake.Instance.SetLoginStep(LoginSteps.LoginInToServer);
            }

            switch (CurrentLoginStep)
            {
                case LoginSteps.Connecting:
                case LoginSteps.VerifyingAccount:
                case LoginSteps.ServerSelection:
                    LoginHandshake.Instance.Disconnect();
                    LoginHandshake.Instance.SetLoginStep(LoginSteps.Main);

                    break;

                case LoginSteps.LoginInToServer:
                    // Stepping back here reconnects and walks the flow back to server selection.
                    // If 'Skip Server Select' is enabled the auto-skip would bounce us straight back to
                    // character selection, so suppress it once to let the user reach the server screen.
                    LoginHandshake.Instance.BypassServerSelectSkipOnce = true;
                    LoginHandshake.Instance.Disconnect();
                    Connect(Account, Password);

                    break;

                case LoginSteps.CharacterCreation:
                    LoginHandshake.Instance.SetLoginStep(LoginSteps.CharacterSelection);

                    break;

                case LoginSteps.PopUpMessage:
                case LoginSteps.CharacterSelection:
                    LoginHandshake.Instance.Disconnect();
                    LoginHandshake.Instance.SetLoginStep(LoginSteps.Main);

                    break;
            }
        }

        public CityInfo GetCity(int index) => LoginHandshake.Instance.GetCity(index);

        private CharacterSelectionGumpBase CreateCharacterSelectionGump()
        {
            return Settings.GlobalSettings.UseCampfireCharacterSelect
                ? new CampfireCharacterSelectionGump(_world)
                : new CharacterSelectionGump(_world);
        }

        /// <summary>
        /// Disposes the active character-selection screen and rebuilds it from the current
        /// style setting. Used by the live style toggle so <see cref="_currentGump"/> stays consistent.
        /// </summary>
        public void RebuildCharacterSelection()
        {
            UIManager.GetGump<CharacterSelectionGumpBase>()?.Dispose();

            _currentGump?.Dispose();

            UIManager.Add(_currentGump = CreateCharacterSelectionGump());
        }

        /// <summary>
        /// Disposes the active login screen and rebuilds it. Used by the live UI language
        /// switch so the freshly loaded strings show immediately and <see cref="_currentGump"/>
        /// stays consistent.
        /// </summary>
        public void RebuildLoginGump()
        {
            if (CurrentLoginStep != LoginSteps.Main)
                return;

            UIManager.GetGump<LoginGump>()?.Dispose();

            _currentGump?.Dispose();

            UIManager.Add(_currentGump = new LoginGump(_world, this));
        }

        private void UpdateCharacterList()
        {
            UIManager.GetGump<CharacterSelectionGumpBase>()?.Dispose();

            _currentGump?.Dispose();

            UIManager.Add(_currentGump = CreateCharacterSelectionGump());
            if (!string.IsNullOrWhiteSpace(PopupMessage))
            {
                Gump g = null;
                g = new LoadingGump(_world, PopupMessage, LoginButtons.OK, (but) => g.Dispose()) { IsModal = true };
                UIManager.Add(g);
                PopupMessage = null;
            }
        }

        private void ReceiveCharacterList()
        {
            uint charToSelect = 0;
            bool haveAnyCharacter = false;
            bool canLogin = CanAutologin;

            if (_autoLogin)
            {
                _autoLogin = false;
            }

            string lastCharName = LastCharacterManager.GetLastCharacter(Account, _world.ServerName);

            if (Characters != null)
            {
                for (byte i = 0; i < Characters.Length; i++)
                {
                    if (Characters[i].Length > 0)
                    {
                        haveAnyCharacter = true;

                        if (Characters[i] == lastCharName)
                        {
                            charToSelect = i;
                            break;
                        }
                    }
                }
            }

            if (canLogin && haveAnyCharacter)
            {
                SelectCharacter(charToSelect);
            }
            else if (!haveAnyCharacter)
            {
                StartCharCreation();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            Client.Game.ScaleChanged -= GameOnScaleChanged;
            LoginHandshake.Instance.LoginStepChanged -= OnLoginStepChanged;
            LoginHandshake.Instance.ReceiveCharacterListNotifier -= ReceiveCharacterList;
            LoginHandshake.Instance.UpdateCharacterListNotifier -= UpdateCharacterList;
            LoginHandshake.Instance?.Dispose();
        }
    }
}
