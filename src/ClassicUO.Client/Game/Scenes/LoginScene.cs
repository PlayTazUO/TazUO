// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.Gumps.CharCreation;
using ClassicUO.Game.UI.Gumps.Login;
using ClassicUO.Network;
using ClassicUO.Resources;
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


        public override void Load()
        {
            base.Load();

            Client.Game.Window.AllowUserResizing = false;

            _autoLogin = Settings.GlobalSettings.AutoLogin;

            UIManager.Add(new LoginBackground(_world));

            if (string.IsNullOrEmpty(Settings.GlobalSettings.IP))
            {
                UIManager.Add(new InputRequest(_world, "Please enter a server IP to connect to", "Save", "Cancel", (result, input) =>
                {
                    if (result == InputRequest.Result.BUTTON1 && !string.IsNullOrEmpty(input))
                    {
                        if (Settings.GlobalSettings.Port <= 0)
                        {
                            UIManager.Add(new InputRequest(_world, "Please enter the port for this server", "Save", "Cancel", (result, input) =>
                            {
                                if (result == InputRequest.Result.BUTTON1 && !string.IsNullOrEmpty(input))
                                {
                                    if (ushort.TryParse(input, out ushort p))
                                    {
                                        Settings.GlobalSettings.Port = p;
                                    }
                                }
                                UIManager.Add(_currentGump = new LoginGump(_world, this));
                            })
                            { X = 130, Y = 150 });
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
                })
                { X = 130, Y = 150 });
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

            Client.Game.SetWindowSize(640, 480);
        }


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
                case LoginSteps.Main:
                    break;
                case LoginSteps.Connecting:
                    break;
                case LoginSteps.VerifyingAccount:
                    break;
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
                case LoginSteps.EnteringBritania:
                    break;
                case LoginSteps.CharacterCreation:
                    break;
                case LoginSteps.CharacterCreationDone:
                    break;
                case LoginSteps.PopUpMessage:
                    if(LoginHandshake.Instance.ErrorPacket != byte.MaxValue)
                        PopupMessage = ServerErrorMessages.GetError(LoginHandshake.Instance.ErrorPacket, LoginHandshake.Instance.ErrorCode, LoginDelay);
                    else if(!string.IsNullOrEmpty(LoginHandshake.Instance.ErrorMessage))
                        PopupMessage = LoginHandshake.Instance.ErrorMessage;
                    break;
            }

            if (_lastLoginStep != newStep)
            {
                Client.Game.UO.GameCursor.IsLoading = false;

                // this trick avoid the flickering
                Gump g = _currentGump;
                UIManager.Add(_currentGump = GetGumpForStep());
                g?.Dispose();

                _lastLoginStep = newStep;
            }
        }

        public override void Update()
        {
            base.Update();

            LoginHandshake.Instance.HandleReconnect(Settings.GlobalSettings.ReconnectTime * 1000);
            LoginHandshake.Instance.SendPing();
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

                case LoginSteps.CharacterSelection: return new CharacterSelectionGump(_world);

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
                        labelText = Client.Game.UO.FileManager.Clilocs.GetString(3000002, ResGeneral.Connecting); // "Connecting..."

                        showButtons = LoginButtons.Cancel;

                        break;

                    case LoginSteps.VerifyingAccount:
                        labelText = Client.Game.UO.FileManager.Clilocs.GetString(3000003, ResGeneral.VerifyingAccount); // "Verifying Account..."

                        showButtons = LoginButtons.Cancel;

                        break;

                    case LoginSteps.LoginInToServer:
                        labelText = Client.Game.UO.FileManager.Clilocs.GetString(3000053, ResGeneral.LoggingIntoShard); // logging into shard

                        showButtons = LoginButtons.Cancel;
                        break;

                    case LoginSteps.EnteringBritania:
                        labelText = Client.Game.UO.FileManager.Clilocs.GetString(3000001, ResGeneral.EnteringBritannia); // Entering Britania...

                        break;

                    case LoginSteps.CharacterCreationDone:
                        labelText = ResGeneral.CreatingCharacter;

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

            if (Servers == null || index < 0 || index > Servers.Length)
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
                _world.ServerName = serverName;
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

        private void UpdateCharacterList()
        {
            UIManager.GetGump<CharacterSelectionGump>()?.Dispose();

            _currentGump?.Dispose();

            UIManager.Add(_currentGump = new CharacterSelectionGump(_world));
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
            LoginHandshake.Instance.LoginStepChanged -= OnLoginStepChanged;
            LoginHandshake.Instance.ReceiveCharacterListNotifier -= ReceiveCharacterList;
            LoginHandshake.Instance.UpdateCharacterListNotifier -= UpdateCharacterList;
            LoginHandshake.Instance?.Dispose();
        }
    }
}
