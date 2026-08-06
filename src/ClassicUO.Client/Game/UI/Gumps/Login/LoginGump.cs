// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using SDL3;
using ClassicUO.Utility.Platforms;

namespace ClassicUO.Game.UI.Gumps.Login
{
    public class LoginGump : Gump
    {
        private readonly ushort _buttonNormal;
        private readonly ushort _buttonOver;
        private readonly Checkbox _checkboxAutologin;
        private readonly Checkbox _checkboxSaveAccount;
        private readonly Button _nextArrow0;
        private readonly PasswordStbTextBox _passwordFake;
        private readonly StbTextBox _textboxAccount;

        private float _time;

        public static LoginGump Instance { get; private set; }

        public LoginGump(World world, LoginScene scene) : base(world, 0, 0)
        {
            Instance?.Dispose();
            Instance = this;

            CanCloseWithRightClick = false;

            AcceptKeyboardInput = false;

            int offsetX, offsetY, offtextY;
            byte font;
            ushort hue;

            if (Client.Game.UO.Version < ClientVersion.CV_706400)
            {
                _buttonNormal = 0x15A4;
                _buttonOver = 0x15A5;
                const ushort HUE = 0x0386;

                if (Client.Game.UO.Version >= ClientVersion.CV_500A)
                {
                    Add(new GumpPic(0, 0, 0x2329, 0));
                }

                //UO Flag
                Add(new GumpPic(0, 4, 0x15A0, 0) { AcceptKeyboardInput = false });

                // Quit Button
                Add
                (
                    new Button((int)Buttons.Quit, 0x1589, 0x158B, 0x158A)
                    {
                        X = 555,
                        Y = 4,
                        ButtonAction = ButtonAction.Activate
                    }
                );

                //Login Panel
                Add
                (
                    new ResizePic(0x13BE)
                    {
                        X = 128,
                        Y = 288,
                        Width = 451,
                        Height = 157
                    }
                );

                if (Client.Game.UO.Version < ClientVersion.CV_500A)
                {
                    Add(new GumpPic(286, 45, 0x058A, 0));
                }

                // Credits
                Add
                (
                    new Button((int)Buttons.Credits, 0x1583, 0x1585, 0x1584)
                    {
                        X = 60,
                        Y = 385,
                        ButtonAction = ButtonAction.Activate
                    }
                );

                // Login to ultima online
                Add
                (
                    new Label(Client.Game.UO.FileManager.Clilocs.GetString(3000038), false, HUE, font: 2)
                    {
                        X = 253,
                        Y = 305
                    }
                );

                Add
                (
                    new Label(TazLang.Get("account"), false, HUE, font: 2)
                    {
                        X = 183,
                        Y = 345
                    }
                );

                Add
                (
                    new Label(TazLang.Get("password"), false, HUE, font: 2)
                    {
                        X = 183,
                        Y = 385
                    }
                );

                // Arrow Button
                Add
                (
                    _nextArrow0 = new Button((int)Buttons.NextArrow, 0x15A4, 0x15A6, 0x15A5)
                    {
                        X = 610,
                        Y = 445,
                        ButtonAction = ButtonAction.Activate
                    }
                );


                offsetX = 328;
                offsetY = 343;
                offtextY = 40;

                Add
                (
                    _checkboxAutologin = new Checkbox
                    (
                        0x00D2,
                        0x00D3,
                        TazLang.Get("autologin"),
                        1,
                        0x0386,
                        false
                    )
                    {
                        X = 150,
                        Y = 417
                    }
                );

                Add
                (
                    _checkboxSaveAccount = new Checkbox
                    (
                        0x00D2,
                        0x00D3,
                        TazLang.Get("save_account"),
                        1,
                        0x0386,
                        false
                    )
                    {
                        X = _checkboxAutologin.X + _checkboxAutologin.Width + 10,
                        Y = 417
                    }
                );

                font = 1;
                hue = 0x0386;
            }
            else
            {
                _buttonNormal = 0x5CD;
                _buttonOver = 0x5CB;

                Add(new GumpPic(0, 0, 0x014E, 0));

                //// Quit Button
                Add
                (
                    new Button((int)Buttons.Quit, 0x05CA, 0x05C9, 0x05C8)
                    {
                        X = 25,
                        Y = 240,
                        ButtonAction = ButtonAction.Activate
                    }
                );

                //// Credit Button
                Add
                (
                    new Button((int)Buttons.Credits, 0x05D0, 0x05CF, 0x5CE)
                    {
                        X = 530,
                        Y = 125,
                        ButtonAction = ButtonAction.Activate
                    }
                );

                // Arrow Button
                Add
                (
                    _nextArrow0 = new Button((int)Buttons.NextArrow, 0x5CD, 0x5CC, 0x5CB)
                    {
                        X = 280,
                        Y = 365,
                        ButtonAction = ButtonAction.Activate
                    }
                );

                offsetX = 218;
                offsetY = 283;
                offtextY = 50;

                if (Settings.GlobalSettings.CustomServer == Settings.CustomServers.Eventine)
                {
                    Add
                    (
                        new Label(TazLang.Get("eventineshard"), false, 0xFFFF, font: 9)
                        {
                            X = 242,
                            Y = 5
                        }
                    );
                }

                Add
                (
                    _checkboxAutologin = new Checkbox
                    (
                        0x00D2,
                        0x00D3,
                        TazLang.Get("autologin"),
                        9,
                        0x0481,
                        false
                    )
                    {
                        X = 150,
                        Y = 417
                    }
                );

                Add
                (
                    _checkboxSaveAccount = new Checkbox
                    (
                        0x00D2,
                        0x00D3,
                        TazLang.Get("save_account"),
                        9,
                        0x0481,
                        false
                    )
                    {
                        X = _checkboxAutologin.X + _checkboxAutologin.Width + 10,
                        Y = 417
                    }
                );

                font = 9;
                hue = 0x0481;
            }


            // Account Text Input Background
            Add
            (
                new ResizePic(0x0BB8)
                {
                    X = offsetX,
                    Y = offsetY,
                    Width = 210,
                    Height = 30
                }
            );

            // Password Text Input Background
            Add
            (
                new ResizePic(0x0BB8)
                {
                    X = offsetX,
                    Y = offsetY + offtextY,
                    Width = 210,
                    Height = 30
                }
            );

            offsetX += 7;

            // Text Inputs
            Add
            (
                _textboxAccount = new StbTextBox
                (
                    5,
                    16,
                    190,
                    false,
                    hue: 0x034F
                )
                {
                    X = offsetX,
                    Y = offsetY,
                    Width = 190,
                    Height = 25,
                    PlaceHolderText=TazLang.Get("accountname")
                }
            );

            _textboxAccount.SetText(Settings.GlobalSettings.Username);

            Add
            (
                _passwordFake = new PasswordStbTextBox
                (
                    5,
                    16,
                    190,
                    false,
                    hue: 0x034F
                )
                {
                    X = offsetX,
                    Y = offsetY + offtextY + 2,
                    Width = 190,
                    Height = 25
                }
            );

            string[] accts = SimpleAccountManager.GetAccounts();
            if (accts.Length > 0)
            {
                _textboxAccount.ContextMenu = new ContextMenuControl(this);
                foreach (string acct in accts)
                {
                    _textboxAccount.ContextMenu.Add(new ContextMenuItemEntry(acct, () => { _textboxAccount.SetText(acct); }));
                }
                _textboxAccount.SetTooltip(TazLang.Get("accountcontextmenutooltip"));
                _textboxAccount.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Right)
                    {
                        _textboxAccount.ContextMenu.Show();
                        UIManager.ContextMenu.X = _textboxAccount.X + _textboxAccount.Width;
                        UIManager.ContextMenu.Y = _textboxAccount.Y + _textboxAccount.Height;
                    }
                };
            }

            _passwordFake.RealText = Crypter.Decrypt(Settings.GlobalSettings.Password);

            _checkboxSaveAccount.IsChecked = Settings.GlobalSettings.SaveAccount;
            _checkboxAutologin.IsChecked = Settings.GlobalSettings.AutoLogin;

            var loginmusic_checkbox = new Checkbox
            (
                0x00D2,
                0x00D3,
                TazLang.Get("music"),
                font,
                hue,
                false
            )
            {
                X = _checkboxSaveAccount.X + _checkboxSaveAccount.Width + 10,
                Y = 417,
                IsChecked = Settings.GlobalSettings.LoginMusic
            };

            Add(loginmusic_checkbox);

            var login_music = new HSliderBar
            (
                loginmusic_checkbox.X + loginmusic_checkbox.Width + 10,
                loginmusic_checkbox.Y + 4,
                80,
                0,
                100,
                Settings.GlobalSettings.LoginMusicVolume,
                HSliderBarStyle.MetalWidgetRecessedBar,
                true,
                font,
                hue,
                false
            );

            Add(login_music);
            login_music.IsVisible = Settings.GlobalSettings.LoginMusic;

            loginmusic_checkbox.ValueChanged += (sender, e) =>
            {
                Settings.GlobalSettings.LoginMusic = loginmusic_checkbox.IsChecked;
                Client.Game.Audio.UpdateCurrentMusicVolume(true);

                login_music.IsVisible = Settings.GlobalSettings.LoginMusic;
            };

            login_music.ValueChanged += (sender, e) =>
            {
                Settings.GlobalSettings.LoginMusicVolume = login_music.Value;
                Client.Game.Audio.UpdateCurrentMusicVolume(true);
            };


            if (!string.IsNullOrEmpty(_textboxAccount.Text))
            {
                _passwordFake.SetKeyboardFocus();
            }
            else
            {
                _textboxAccount.SetKeyboardFocus();
            }

#if DEBUG
            var loadTimeLabel = new Label
            (
                $"Asset load: {Client.Game.UO.FileManager.LoadTime.TotalMilliseconds:F0} ms",
                false,
                0x034E,
                font: 9
            )
            {
                Y = 5
            };
            loadTimeLabel.X = 640 - loadTimeLabel.Width - 5;
            Add(loadTimeLabel);
#endif

            Add
            (
                new Label(TazLang.Get("uoversion", [Settings.GlobalSettings.ClientVersion]), false, 0x034E, font: 9)
                {
                    X = 286,
                    Y = 453
                }
            );

            Add
            (
                new Label(TazLang.Get("tazuoversion", [CUOEnviroment.Version]), false, 0x034E, font: 9)
                {
                    X = 286,
                    Y = 465
                }
            );

            var optionsButton = new NiceButton(5, 5, 80, 30, ButtonAction.Default, TazLang.Get("options")) { IsSelectable = false, BackgroundColor = new Color(0.7f, 0.7f, 0.7f, 0.7f) };
            optionsButton.MouseDown += (s,e) =>
            {
                ContextMenuControl c = GenOptionsContext();
                optionsButton.ContextMenu = c;
                c.Show();
            };
            Add(optionsButton);
        }

        private ContextMenuControl GenOptionsContext()
        {
            var c = new ContextMenuControl(this);
            c.Add(new ContextMenuItemEntry(TazLang.Get("skipserverselectdesc"), () =>
            {
                Settings.GlobalSettings.SkipServerSelect = !Settings.GlobalSettings.SkipServerSelect;
                _ = Client.Settings.SetAsync(SettingsScope.Global, Constants.SqlSettings.SKIP_SERVER_SELECTION, Settings.GlobalSettings.SkipServerSelect);
            }, true, Settings.GlobalSettings.SkipServerSelect));

            c.Add(new ContextMenuItemEntry(TazLang.Get("editsettings"), OpenEditSettings, true, false));

            c.Add(new ContextMenuItemEntry(TazLang.Get("tuowebsite"), () =>
            {
                PlatformHelper.LaunchBrowser("https://tazuo.org");
            }, true, false));

            c.Add(new ContextMenuItemEntry(TazLang.Get("tuodiscord"), () =>
            {
                PlatformHelper.LaunchBrowser("https://discord.gg/QvqzkB95G4");
            }, true, false));

            c.Add(new ContextMenuItemEntry(TazLang.Get("cuowebsite"), () =>
            {
                PlatformHelper.LaunchBrowser("https://www.classicuo.eu");
            }, true, false));

            return c;
        }

        private void OpenEditSettings()
        {
            var existing = QuickOptionsWindow.GetExisting(TazLang.Get("editsettings"));
            if (existing != null)
            {
                existing.CenterInScreen();
                existing.BringOnTop();
                return;
            }

            Settings s = Settings.GlobalSettings;

            var w = new QuickOptionsWindow(TazLang.Get("editsettings"));

            w.AddInput(TazLang.Get("ipentry"), s.IP, v => { s.IP = v; s.Save(); }, 200, TazLang.Get("iporhostnamedesc"));

            w.AddInput(TazLang.Get("portentry"), s.Port.ToString(), v =>
            {
                if (ushort.TryParse(v, out ushort port) && port >= 1)
                {
                    s.Port = port;
                    s.Save();
                }
            }, 80, TazLang.Get("serverporttooltip"));

            w.AddCheckbox(TazLang.Get("autologin"), s.AutoLogin, v => { s.AutoLogin = v; s.Save(); },
                TazLang.Get("autologintooltip"));

            w.AddCheckbox(TazLang.Get("reconnect"), s.Reconnect, v => { s.Reconnect = v; s.Save(); },
                TazLang.Get("autoreconnecttooltip"));

            w.AddInput(TazLang.Get("reconnecttimeentry"), s.ReconnectTime.ToString(), v =>
            {
                if (int.TryParse(v, out int time) && time >= 0)
                {
                    s.ReconnectTime = time;
                    s.Save();
                }
            }, 80, TazLang.Get("reconnecttooltip"));

            w.AddInput(TazLang.Get("forcedriver"), s.ForceDriver.ToString(), v =>
            {
                if (byte.TryParse(v, out byte driver))
                {
                    s.ForceDriver = driver;
                    s.Save();
                }
            }, 80, TazLang.Get("forcedrivertooltip"));

            w.AddLabel(TazLang.Get("forcedriverwarning"));

            string[] langs = TazLang.GetAvailableLanguages();
            int langIdx = System.Array.IndexOf(langs, s.UILanguage ?? "EN");
            w.AddDropdown(
                TazLang.Get("uilangentry"),
                langs,
                langIdx >= 0 ? langIdx : 0,
                i =>
                {
                    s.UILanguage = langs[i];
                    s.Save();

                    // Reload the language strings and rebuild the login screen so the
                    // selection takes effect live without requiring a restart.
                    TazLang.Load(langs[i]);
                    Client.Game.GetScene<LoginScene>()?.RebuildLoginGump();
                },
                TazLang.Get("uilangtooltip")
            );

            w.CenterInScreen();
        }

        protected override void OnControllerButtonUp(SDL.SDL_GamepadButton button)
        {
            base.OnControllerButtonUp(button);
            if (button == SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH)
            {
                SaveCheckboxStatus();
                LoginScene ls = Client.Game.GetScene<LoginScene>();

                if (ls.CurrentLoginStep == LoginSteps.Main)
                {
                    ls.Connect(_textboxAccount.Text, _passwordFake.RealText);
                }
            }
        }

        public override void OnKeyboardReturn(int textID, string text)
        {
            SaveCheckboxStatus();
            LoginScene ls = Client.Game.GetScene<LoginScene>();

            if (ls.CurrentLoginStep == LoginSteps.Main)
            {
                ls.Connect(_textboxAccount.Text, _passwordFake.RealText);
            }
        }

        private void SaveCheckboxStatus()
        {
            Settings.GlobalSettings.SaveAccount = _checkboxSaveAccount.IsChecked;
            Settings.GlobalSettings.AutoLogin = _checkboxAutologin.IsChecked;
        }

        public override void Update()
        {
            if (IsDisposed)
            {
                return;
            }

            if (World.Instance != null && World.Instance.InGame)
            {
                Dispose();
                return;
            }

            base.Update();

            if (_time < Time.Ticks)
            {
                _time = (float)Time.Ticks + 1000;

                _nextArrow0.ButtonGraphicNormal = _nextArrow0.ButtonGraphicNormal == _buttonNormal ? _buttonOver : _buttonNormal;
            }

            if (_passwordFake.HasKeyboardFocus)
            {
                if (_passwordFake.Hue != 0x0021)
                {
                    _passwordFake.Hue = 0x0021;
                }
            }
            else if (_passwordFake.Hue != 0)
            {
                _passwordFake.Hue = 0;
            }

            if (_textboxAccount.HasKeyboardFocus)
            {
                if (_textboxAccount.Hue != 0x0021)
                {
                    _textboxAccount.Hue = 0x0021;
                }
            }
            else if (_textboxAccount.Hue != 0)
            {
                _textboxAccount.Hue = 0;
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons)buttonID)
            {
                case Buttons.NextArrow:
                    SaveCheckboxStatus();

                    if (!_textboxAccount.IsDisposed)
                    {
                        Client.Game.GetScene<LoginScene>().Connect(_textboxAccount.Text, _passwordFake.RealText);
                    }

                    break;

                case Buttons.Quit:
                    Client.Game.Exit();

                    break;

                case Buttons.Credits:
                    UIManager.Add(new CreditsGump(World));

                    break;
            }
        }

        private class PasswordStbTextBox : StbTextBox
        {
            private new Point _caretScreenPosition;
            private new readonly RenderedText _rendererCaret;

            private new readonly RenderedText _rendererText;

            public PasswordStbTextBox
            (
                byte font,
                int max_char_count = -1,
                int maxWidth = 0,
                bool isunicode = true,
                FontStyle style = FontStyle.None,
                ushort hue = 0,
                TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT
            ) : base
            (
                font,
                max_char_count,
                maxWidth,
                isunicode,
                style,
                hue,
                align
            )
            {
                _rendererText = RenderedText.Create
                (
                    string.Empty,
                    hue,
                    font,
                    isunicode,
                    style,
                    align,
                    maxWidth
                );

                _rendererCaret = RenderedText.Create
                (
                    "_",
                    hue,
                    font,
                    isunicode,
                    (style & FontStyle.BlackBorder) != 0 ? FontStyle.BlackBorder : FontStyle.None,
                    align
                );

                NoSelection = true;
            }

            internal string RealText
            {
                get => Text;
                set => SetText(value);
            }

            public new ushort Hue
            {
                get => _rendererText.Hue;
                set
                {
                    if (_rendererText.Hue != value)
                    {
                        _rendererText.Hue = value;
                        _rendererCaret.Hue = value;

                        _rendererText.CreateTexture();
                        _rendererCaret.CreateTexture();
                    }
                }
            }

            protected override void DrawCaret(UltimaBatcher2D batcher, int x, int y)
            {
                if (HasKeyboardFocus)
                {
                    _rendererCaret.Draw(batcher, x + _caretScreenPosition.X, y + _caretScreenPosition.Y);
                }
            }

            public override void OnMouseDown(int x, int y, MouseButtonType button)
            {
                base.OnMouseDown(x, y, button);

                if (button == MouseButtonType.Left)
                {
                    UpdateCaretScreenPosition();
                }
            }

            public override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
            {
                base.OnKeyDown(key, mod);
                UpdateCaretScreenPosition();
            }

            public override void Dispose()
            {
                _rendererText?.Destroy();
                _rendererCaret?.Destroy();

                base.Dispose();
            }

            protected override void OnTextInput(string c) => base.OnTextInput(c);

            protected override void OnTextChanged()
            {
                if (Text.Length > 0)
                {
                    _rendererText.Text = new string('*', Text.Length);
                }
                else
                {
                    _rendererText.Text = string.Empty;
                }

                base.OnTextChanged();
                UpdateCaretScreenPosition();
            }

            public override void OnFocusEnter()
            {
                base.OnFocusEnter();
                CaretIndex = Text?.Length ?? 0;
                UpdateCaretScreenPosition();
            }

            private new void UpdateCaretScreenPosition() => _caretScreenPosition = _rendererText.GetCaretPosition(Stb.CursorIndex);

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (batcher.ClipBegin(x, y, Width, Height))
                {
                    DrawSelection(batcher, x, y);

                    _rendererText.Draw(batcher, x, y);

                    DrawCaret(batcher, x, y);
                    batcher.ClipEnd();
                }

                return true;
            }
        }


        private enum Buttons
        {
            NextArrow,
            Quit,
            Credits
        }
    }
}
