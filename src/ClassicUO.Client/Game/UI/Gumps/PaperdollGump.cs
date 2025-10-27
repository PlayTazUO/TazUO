// SPDX-License-Identifier: BSD-2-Clause


using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System;
using System.Xml;

namespace ClassicUO.Game.UI.Gumps
{
    public class PaperDollGump : TextContainerGump
    {
        private static readonly ushort[] PeaceModeBtnGumps = { 0x07e5, 0x07e6, 0x07e7 };
        private static readonly ushort[] WarModeBtnGumps = { 0x07e8, 0x07e9, 0x07ea };
        private GumpPic _combatBook,
            _racialAbilitiesBook;
        private HitBox _hitBox;
        private bool _isWarMode, _isMinimized;

        private PaperDollInteractable _paperDollInteractable;
        private GumpPic _partyManifestPic;

        private GumpPic _picBase;
        private GumpPic _profilePic;
        private readonly EquipmentSlot[] _slots = new EquipmentSlot[9];
        private readonly EquipmentSlot[] _slots_right = new EquipmentSlot[8];
        private Label _titleLabel;
        private GumpPic _virtueMenuPic;
        private Button _warModeBtn;

        public PaperDollGump(World world) : base(world, 0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;

            if (ProfileManager.CurrentProfile != null)
            {
                X = ProfileManager.CurrentProfile.PaperdollPosition.X;
                Y = ProfileManager.CurrentProfile.PaperdollPosition.Y;
                IsLocked = ProfileManager.CurrentProfile.PaperdollLocked;
            }
        }

        public PaperDollGump(World world, uint serial, bool canLift) : this(world)
        {
            LocalSerial = serial;
            CanLift = canLift;
            if (ProfileManager.CurrentProfile != null)
                Scale = InternalScale = ProfileManager.CurrentProfile.PaperdollScale;
            BuildGump();
        }

        public override GumpType GumpType => GumpType.PaperDoll;

        public bool IsMinimized
        {
            get => _isMinimized;
            set
            {
                if (_isMinimized != value)
                {
                    _isMinimized = value;

                    _picBase.Dispose();
                    _picBase = new GumpPic(0, 0, value ? Settings.Graphic_Button_Minimized : (LocalSerial == World.Player ? Settings.Graphic_Background_Player : Settings.Graphic_Background_Other), 0);
                    _picBase.ScaleWidthAndHeight(Scale).SetInternalScale(Scale);
                    _picBase.MouseDoubleClick += _picBase_MouseDoubleClick;
                    Insert(0, _picBase);

                    foreach (Control c in Children)
                    {
                        c.IsVisible = !value;
                    }

                    _picBase.IsVisible = true;
                    WantUpdateSize = true;
                }
            }
        }

        public bool CanLift { get; set; }

        public override void Dispose()
        {
            UIManager.SavePosition(LocalSerial, Location);

            if (LocalSerial == World.Player)
            {
                if (_virtueMenuPic != null)
                {
                    _virtueMenuPic.MouseDoubleClick -= VirtueMenu_MouseDoubleClickEvent;
                }

                if (_partyManifestPic != null)
                {
                    _partyManifestPic.MouseDoubleClick -= PartyManifest_MouseDoubleClickEvent;
                }
                if (ProfileManager.CurrentProfile != null)
                    ProfileManager.CurrentProfile.PaperdollPosition = Location;
            }

            Clear();
            base.Dispose();
        }

        private void _hitBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtonType.Left && !IsMinimized)
            {
                IsMinimized = true;
            }
        }

        private void BuildGump()
        {
            _picBase?.Dispose();
            _hitBox?.Dispose();

            bool showPaperdollBooks =
                LocalSerial == World.Player && World.ClientFeatures.PaperdollBooks;
            bool showRacialAbilitiesBook =
                showPaperdollBooks && Client.Game.UO.Version >= ClientVersion.CV_7000;

            if (LocalSerial == World.Player)
            {
                Add(_picBase = new GumpPic(0, 0, Settings.Graphic_Background_Player, Settings.Hue_Background_Player));
                _picBase.ScaleWidthAndHeight(Scale).SetInternalScale(Scale);
                _picBase.MouseDoubleClick += _picBase_MouseDoubleClick;

                //HELP BUTTON
                Add(
                    new Button((int)Buttons.Help, Settings.Graphic_Button_Help_Normal, Settings.Graphic_Button_Help_Pressed, Settings.Graphic_Button_Help_Hover)
                    {
                        X = Settings.Position_X_Help,
                        Y = Settings.Position_Y_Help,
                        ButtonAction = ButtonAction.Activate
                    }.ScaleWidthAndHeight(Scale).ScaleXAndY(Scale).SetInternalScale(Scale)
                );

                //OPTIONS BUTTON
                Add(
                    new Button((int)Buttons.Options, Settings.Graphic_Button_Options_Normal, Settings.Graphic_Button_Options_Pressed, Settings.Graphic_Button_Options_Hover)
                    {
                        X = Settings.Position_X_Options,
                        Y = Settings.Position_Y_Options,
                        ButtonAction = ButtonAction.Activate
                    }.ScaleWidthAndHeight(Scale).ScaleXAndY(Scale).SetInternalScale(Scale)
                );

                // LOG OUT BUTTON
                Add(
                    new Button((int)Buttons.LogOut, Settings.Graphic_Button_Logout_Normal, Settings.Graphic_Button_Logout_Pressed, Settings.Graphic_Button_Logout_Hover)
                    {
                        X = Settings.Position_X_Logout,
                        Y = Settings.Position_Y_Logout,
                        ButtonAction = ButtonAction.Activate
                    }.ScaleWidthAndHeight(Scale).ScaleXAndY(Scale).SetInternalScale(Scale)
                );

                if (Client.Game.UO.Version < ClientVersion.CV_500A)
                {
                    // JOURNAL BUTTON
                    Add(
                        new Button((int)Buttons.Journal, Settings.Graphic_Button_Journal_Normal, Settings.Graphic_Button_Journal_Pressed, Settings.Graphic_Button_Journal_Hover)
                        {
                            X = Settings.Position_X_Journal,
                            Y = Settings.Position_Y_Journal,
                            ButtonAction = ButtonAction.Activate
                        }.ScaleWidthAndHeight(Scale).ScaleXAndY(Scale).SetInternalScale(Scale)
                    );
                }
                else
                {
                    // QUESTS BUTTON
                    Add(
                        new Button((int)Buttons.Quests, Settings.Graphic_Button_Quest_Normal, Settings.Graphic_Button_Quest_Pressed, Settings.Graphic_Button_Quest_Hover)
                        {
                            X = Settings.Position_X_Quest,
                            Y = Settings.Position_Y_Quest,
                            ButtonAction = ButtonAction.Activate
                        }.ScaleWidthAndHeight(Scale).ScaleXAndY(Scale).SetInternalScale(Scale)
                    );
                }

                // SKILLS BUTTON
                Add(
                    new Button((int)Buttons.Skills, Settings.Graphic_Button_Skills_Normal, Settings.Graphic_Button_Skills_Pressed, Settings.Graphic_Button_Skills_Hover)
                    {
                        X = Settings.Position_X_Skills,
                        Y = Settings.Position_Y_Skills,
                        ButtonAction = ButtonAction.Activate
                    }.ScaleWidthAndHeight(Scale).ScaleXAndY(Scale).SetInternalScale(Scale)
                );

                // GUILD BUTTON
                Add(
                    new Button((int)Buttons.Guild, Settings.Graphic_Button_Guild_Normal, Settings.Graphic_Button_Guild_Pressed, Settings.Graphic_Button_Guild_Hover)
                    {
                        X = Settings.Position_X_Guild,
                        Y = Settings.Position_Y_Guild,
                        ButtonAction = ButtonAction.Activate
                    }.ScaleWidthAndHeight(Scale).ScaleXAndY(Scale).SetInternalScale(Scale)
                );

                // TOGGLE PEACE/WAR BUTTON
                Mobile mobile = World.Mobiles.Get(LocalSerial);

                _isWarMode = mobile?.InWarMode ?? false;

                if (_isWarMode)
                {
                    Add(
                        _warModeBtn = new Button(
                            (int)Buttons.PeaceWarToggle,
                            Settings.Graphic_Button_Warmode_Normal,
                            Settings.Graphic_Button_Warmode_Pressed,
                            Settings.Graphic_Button_Warmode_Hover
                        )
                        {
                            X = Settings.Position_X_WarMode,
                            Y = Settings.Position_Y_Warmode,
                            ButtonAction = ButtonAction.Activate
                        }
                    );
                }
                else
                {
                    Add(
                        _warModeBtn = new Button(
                            (int)Buttons.PeaceWarToggle,
                            Settings.Graphic_Button_Peacemode_Normal,
                            Settings.Graphic_Button_Peacemode_Pressed,
                            Settings.Graphic_Button_Peacemode_Hover
                        )
                        {
                            X = Settings.Position_X_WarMode,
                            Y = Settings.Position_Y_Warmode,
                            ButtonAction = ButtonAction.Activate
                        }
                    );
                }
                _warModeBtn.ScaleWidthAndHeight(Scale).ScaleXAndY(Scale).SetInternalScale(Scale);

                int profileX = Settings.Position_X_Profile;

                if (showRacialAbilitiesBook)
                {
                    profileX += Settings.Racial_Abilities_Width;
                }

                Add(_profilePic = new GumpPic(profileX, Settings.Position_Y_Profile, Settings.Graphic_Button_Profile, 0));
                profileX += _profilePic.Width;
                _profilePic.ScaleWidthAndHeight(Scale).ScaleXAndY(Scale).SetInternalScale(Scale);
                _profilePic.MouseDoubleClick += Profile_MouseDoubleClickEvent;

                Add(_partyManifestPic = new GumpPic(profileX, Settings.Position_Y_Profile, Settings.Graphic_Button_Party, 0));
                _partyManifestPic.ScaleWidthAndHeight(Scale).ScaleXAndY(Scale).SetInternalScale(Scale);
                _partyManifestPic.MouseDoubleClick += PartyManifest_MouseDoubleClickEvent;

                _hitBox = new HitBox(Settings.Position_X_MinimizeButton, Settings.Position_Y_MinimizeButton, Settings.Size_Width_MinimizeButton, Settings.Size_Height_MinimizeButton, alpha: 0f);
                _hitBox.ScaleWidthAndHeight(Scale).ScaleXAndY(Scale).SetInternalScale(Scale);
                _hitBox.MouseUp += _hitBox_MouseUp;

                Add(_hitBox);
            }
            else
            {
                Add(_picBase = new GumpPic(0, 0, Settings.Graphic_Background_Other, Settings.Hue_Background_Other));
                _picBase.ScaleWidthAndHeight(Scale).SetInternalScale(Scale);
                Add(_profilePic = new GumpPic(Settings.Position_X_Profile, Settings.Position_Y_Profile, Settings.Graphic_Button_Profile, 0));
                _profilePic.ScaleWidthAndHeight(Scale).SetInternalScale(Scale);
                _profilePic.MouseDoubleClick += Profile_MouseDoubleClickEvent;
            }

            // STATUS BUTTON
            Add(
                new Button((int)Buttons.Status, Settings.Graphic_Button_Status_Normal, Settings.Graphic_Button_Status_Pressed, Settings.Graphic_Button_Status_Hover)
                {
                    X = Settings.Position_X_Status,
                    Y = Settings.Position_Y_Status,
                    ButtonAction = ButtonAction.Activate
                }.ScaleWidthAndHeight(Scale).ScaleXAndY(Scale).SetInternalScale(Scale)
            );

            // Virtue menu
            Add(_virtueMenuPic = new GumpPic(Settings.Position_X_Virtue, Settings.Position_Y_Virtue, Settings.Graphic_Button_Virtue, 0));
            _virtueMenuPic.ScaleWidthAndHeight(Scale).ScaleXAndY(Scale).SetInternalScale(Scale);
            _virtueMenuPic.MouseDoubleClick += VirtueMenu_MouseDoubleClickEvent;

            if (LocalSerial == World.Player.Serial)
                Add(new DurabilityGumpMinimized(World)
                {
                    X = Settings.Position_X_Durability,
                    Y = Settings.Position_Y_Durability,
                    Graphic = Settings.Graphic_Button_Durability
                }.ScaleWidthAndHeight(Scale).ScaleXAndY(Scale).SetInternalScale(Scale));

            Mobile mobiles = World.Mobiles.Get(LocalSerial);
            Item twoHandedItem = mobiles.FindItemByLayer(Layer.TwoHanded);

            // Equipment slots for hat/earrings/neck/ring/bracelet
            Add(_slots[0] = new EquipmentSlot(0, Settings.Position_X_LeftSlots, Settings.Position_Y_LeftSlots, Layer.Helmet, this));

            Add(_slots[1] = new EquipmentSlot(0, Settings.Position_X_LeftSlots, Settings.Position_Y_LeftSlots + Settings.Size_Height_LeftSlots, Layer.Earrings, this));

            Add(_slots[2] = new EquipmentSlot(0, Settings.Position_X_LeftSlots, Settings.Position_Y_LeftSlots + Settings.Size_Height_LeftSlots * 2, Layer.Necklace, this));

            Add(_slots[3] = new EquipmentSlot(0, Settings.Position_X_LeftSlots, Settings.Position_Y_LeftSlots + Settings.Size_Height_LeftSlots * 3, Layer.Ring, this));

            Add(_slots[4] = new EquipmentSlot(0, Settings.Position_X_LeftSlots, Settings.Position_Y_LeftSlots + Settings.Size_Height_LeftSlots * 4, Layer.Bracelet, this));

            Add(_slots[5] = new EquipmentSlot(0, Settings.Position_X_LeftSlots, Settings.Position_Y_LeftSlots + Settings.Size_Height_LeftSlots * 5, Layer.Tunic, this));

            Add(_slots[6] = new EquipmentSlot(0, Settings.Position_X_LeftSlots, Settings.Position_Y_LeftSlots + Settings.Size_Height_LeftSlots * 6, Layer.OneHanded, this));

            Add(_slots[7] = new EquipmentSlot(0, Settings.Position_X_LeftSlots, Settings.Position_Y_LeftSlots + Settings.Size_Height_LeftSlots * 7, Layer.TwoHanded, this));

            Add(_slots[8] = new EquipmentSlot(0, Settings.Position_X_LeftSlots, Settings.Position_Y_LeftSlots + Settings.Size_Height_LeftSlots * 8, Layer.Talisman, this));


            foreach (EquipmentSlot slot in _slots)
            {
                slot.ScaleWidthAndHeight(Scale).ScaleXAndY(Scale).SetInternalScale(Scale);
            }

            // Right side equip slots

            Add(_slots_right[0] = new EquipmentSlot(0, Settings.Position_X_RightSlots, Settings.Position_Y_RightSlots, Layer.Robe, this));

            Add(_slots_right[1] = new EquipmentSlot(0, Settings.Position_X_RightSlots, Settings.Position_Y_RightSlots + Settings.Size_Height_RightSlots, Layer.Gloves, this));

            Add(_slots_right[2] = new EquipmentSlot(0, Settings.Position_X_RightSlots, Settings.Position_Y_RightSlots + Settings.Size_Height_RightSlots * 2, Layer.Torso, this));

            Add(_slots_right[3] = new EquipmentSlot(0, Settings.Position_X_RightSlots, Settings.Position_Y_RightSlots + Settings.Size_Height_RightSlots * 3, Layer.Arms, this));

            Add(_slots_right[4] = new EquipmentSlot(0, Settings.Position_X_RightSlots, Settings.Position_Y_RightSlots + Settings.Size_Height_RightSlots * 4, Layer.Pants, this));

            Add(_slots_right[5] = new EquipmentSlot(0, Settings.Position_X_RightSlots, Settings.Position_Y_RightSlots + Settings.Size_Height_RightSlots * 5, Layer.Cloak, this));

            Add(_slots_right[6] = new EquipmentSlot(0, Settings.Position_X_RightSlots, Settings.Position_Y_RightSlots + Settings.Size_Height_RightSlots * 6, Layer.Waist, this));

            Add(_slots_right[7] = new EquipmentSlot(0, Settings.Position_X_RightSlots, Settings.Position_Y_RightSlots + Settings.Size_Height_RightSlots * 7, Layer.Shoes, this));

            foreach (EquipmentSlot slot in _slots_right)
            {
                slot.ScaleWidthAndHeight(Scale).ScaleXAndY(Scale).SetInternalScale(Scale);
            }

            // Paperdoll control!
            _paperDollInteractable = new PaperDollInteractable(Settings.Position_X_Avatar, Settings.Position_Y_Avatar, LocalSerial, this, Scale);
            _paperDollInteractable.ScaleXAndY(Scale);
            Add(_paperDollInteractable);

            if (showPaperdollBooks)
            {
                Add(_combatBook = new GumpPic(Settings.Position_X_CombatBook, Settings.Position_Y_CombatBook, Settings.Graphic_Button_Combat, 0));
                _combatBook.ScaleWidthAndHeight(Scale).ScaleXAndY(Scale).SetInternalScale(Scale);
                _combatBook.MouseDoubleClick += (sender, e) => GameActions.OpenAbilitiesBook(World);

                if (showRacialAbilitiesBook)
                {
                    Add(_racialAbilitiesBook = new GumpPic(Settings.Position_X_RacialAbilities, Settings.Position_Y_RacialAbilities, Settings.Graphic_Button_RacialAbilties, 0));
                    _racialAbilitiesBook.ScaleWidthAndHeight(Scale).ScaleXAndY(Scale).SetInternalScale(Scale);
                    _racialAbilitiesBook.MouseDoubleClick += (sender, e) =>
                    {
                        if (UIManager.GetGump<RacialAbilitiesBookGump>() == null)
                        {
                            UIManager.Add(new RacialAbilitiesBookGump(World, 100, 100));
                        }
                    };
                }
            }

            // Name and title
            _titleLabel = new Label("", false, Settings.Hue_Title, Settings.Size_Width_Hue, font: 1) { X = Settings.Position_X_Title, Y = Settings.Position_Y_Title };
            _titleLabel.ScaleWidthAndHeight(Scale).ScaleXAndY(Scale).SetInternalScale(Scale);

            Add(_titleLabel);

            RequestUpdateContents();

            WantUpdateSize = true;
        }

        private void _picBase_MouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
        {
            if (e.Button == MouseButtonType.Left && IsMinimized)
            {
                IsMinimized = false;
            }
        }

        public void UpdateTitle(string text) => _titleLabel.Text = text;

        private void VirtueMenu_MouseDoubleClickEvent(object sender, MouseDoubleClickEventArgs args)
        {
            if (args.Button == MouseButtonType.Left)
            {
                GameActions.ReplyGump(
                    World,
                    World.Player,
                    0x000001CD,
                    0x00000001,
                    new[] { LocalSerial },
                    new Tuple<ushort, string>[0]
                );
            }
        }

        private void Profile_MouseDoubleClickEvent(object o, MouseDoubleClickEventArgs args)
        {
            if (args.Button == MouseButtonType.Left)
            {
                GameActions.RequestProfile(LocalSerial);
            }
        }

        private void PartyManifest_MouseDoubleClickEvent(
            object sender,
            MouseDoubleClickEventArgs args
        )
        {
            if (args.Button == MouseButtonType.Left)
            {
                PartyGump party = UIManager.GetGump<PartyGump>();

                if (party == null)
                {
                    int x = Client.Game.Window.ClientBounds.Width / 2 - 272;
                    int y = Client.Game.Window.ClientBounds.Height / 2 - 240;
                    UIManager.Add(new PartyGump(World, x, y, World.Party.CanLoot));
                }
                else
                {
                    party.BringOnTop();
                }
            }
        }

        public override void PreDraw()
        {
            if (IsDisposed)
            {
                return;
            }

            Mobile mobile = World.Mobiles.Get(LocalSerial);

            if (mobile != null && mobile.IsDestroyed)
            {
                Dispose();

                return;
            }

            if (LocalSerial == World.Player)
            {
                // This is to update the state of the war mode button.
                if (mobile != null && _isWarMode != mobile.InWarMode)
                {
                    _isWarMode = mobile.InWarMode;
                    ushort[] btngumps = _isWarMode ? WarModeBtnGumps : PeaceModeBtnGumps;
                    _warModeBtn.ButtonGraphicNormal = btngumps[0];
                    _warModeBtn.ButtonGraphicPressed = btngumps[1];
                    _warModeBtn.ButtonGraphicOver = btngumps[2];
                    _warModeBtn.ScaleWidthAndHeight(Scale).SetInternalScale(Scale);
                }

                if(ProfileManager.CurrentProfile != null)
                    if (Location != ProfileManager.CurrentProfile.PaperdollPosition)
                        ProfileManager.CurrentProfile.PaperdollPosition = Location;
            }

            base.PreDraw();

            if (_paperDollInteractable != null && (CanLift || LocalSerial == World.Player.Serial))
            {
                bool force_false =
                    SelectedObject.Object is Item item
                    && (item.Layer == Layer.Backpack || item.ItemData.IsContainer);

                if (
                    _paperDollInteractable.HasFakeItem && !Client.Game.UO.GameCursor.ItemHold.Enabled
                    || force_false
                )
                {
                    _paperDollInteractable.SetFakeItem(false);
                }
                else if (
                    !_paperDollInteractable.HasFakeItem
                    && Client.Game.UO.GameCursor.ItemHold.Enabled
                    && !Client.Game.UO.GameCursor.ItemHold.IsFixedPosition
                    && UIManager.MouseOverControl?.RootParent == this
                )
                {
                    if (Client.Game.UO.GameCursor.ItemHold.ItemData.AnimID != 0)
                    {
                        if (
                            mobile != null
                            && mobile.FindItemByLayer(
                                (Layer)Client.Game.UO.GameCursor.ItemHold.ItemData.Layer
                            ) == null
                        )
                        {
                            _paperDollInteractable.SetFakeItem(true);
                        }
                    }
                }
            }
        }

        protected override void OnMouseExit(int x, int y) => _paperDollInteractable?.SetFakeItem(false);

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);
            if (button == MouseButtonType.Left && World.InGame)
            {
                Mobile container = World.Mobiles.Get(LocalSerial);

                if (Client.Game.UO.GameCursor.ItemHold.Enabled)
                {
                    if (CanLift || LocalSerial == World.Player.Serial)
                    {
                        if (
                            SelectedObject.Object is Item item
                            && (item.Layer == Layer.Backpack || item.ItemData.IsContainer)
                        )
                        {
                            GameActions.DropItem(
                                Client.Game.UO.GameCursor.ItemHold.Serial,
                                0xFFFF,
                                0xFFFF,
                                0,
                                item.Serial
                            );

                            Mouse.CancelDoubleClick = true;
                        }
                        else
                        {
                            if (Client.Game.UO.GameCursor.ItemHold.ItemData.IsWearable)
                            {
                                Item equipment = container.FindItemByLayer(
                                    (Layer)Client.Game.UO.GameCursor.ItemHold.ItemData.Layer
                                );

                                if (equipment == null)
                                {
                                    GameActions.Equip(World,
                                        LocalSerial != World.Player ? container : World.Player
                                    );
                                    Mouse.CancelDoubleClick = true;
                                }
                            }
                        }
                    }
                }
                else if (SelectedObject.Object is Item item)
                {
                    if (World.TargetManager.IsTargeting)
                    {
                        World.TargetManager.Target(item.Serial);
                        Mouse.CancelDoubleClick = true;
                        Mouse.LastLeftButtonClickTime = 0;

                        if (World.TargetManager.TargetingState == CursorTarget.SetTargetClientSide)
                        {
                            UIManager.Add(new InspectorGump(World,item));
                        }
                    }
                    else if (!World.DelayedObjectClickManager.IsEnabled)
                    {
                        Point off = Mouse.LDragOffset;

                        World.DelayedObjectClickManager.Set(
                            item.Serial,
                            Mouse.Position.X - off.X - ScreenCoordinateX,
                            Mouse.Position.Y - off.Y - ScreenCoordinateY,
                            Time.Ticks + Mouse.MOUSE_DELAY_DOUBLE_CLICK
                        );
                    }
                }
            }
            else
            {
                base.OnMouseUp(x, y, button);
            }
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);

            writer.WriteAttributeString("isminimized", IsMinimized.ToString());
            if (LocalSerial == World.Player.Serial && ProfileManager.CurrentProfile != null)
                ProfileManager.CurrentProfile.PaperdollPosition = Location;
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            if (LocalSerial == World.Player)
            {
                BuildGump();

                GameActions.OpenPaperdoll(World, LocalSerial);

                IsMinimized = bool.Parse(xml.GetAttribute("isminimized"));
                if (ProfileManager.CurrentProfile != null)
                {
                    X = ProfileManager.CurrentProfile.PaperdollPosition.X;
                    Y = ProfileManager.CurrentProfile.PaperdollPosition.Y;
                }
            }
            else
            {
                Dispose();
            }
        }

        protected override void UpdateContents()
        {
            Mobile mobile = World.Mobiles.Get(LocalSerial);

            if (mobile != null && mobile.Title != _titleLabel.Text)
            {
                UpdateTitle(mobile.Title);
            }

            _paperDollInteractable.RequestUpdate();

            if (mobile != null)
            {
                for (int i = 0; i < _slots.Length; i++)
                {
                    int idx = (int)_slots[i].Layer;

                    _slots[i].LocalSerial = mobile.FindItemByLayer((Layer)idx)?.Serial ?? 0;
                }

                for (int i = 0; i < _slots_right.Length; i++)
                {
                    int idx = (int)_slots_right[i].Layer;

                    _slots_right[i].LocalSerial = mobile.FindItemByLayer((Layer)idx)?.Serial ?? 0;
                }
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            if (
                Client.Game.UO.GameCursor.ItemHold.Enabled
                && !Client.Game.UO.GameCursor.ItemHold.IsFixedPosition
            )
            {
                OnMouseUp(0, 0, MouseButtonType.Left);

                return;
            }

            switch ((Buttons)buttonID)
            {
                case Buttons.Help:
                    GameActions.RequestHelp();

                    break;

                case Buttons.Options:
                    GameActions.OpenSettings(World);

                    break;

                case Buttons.LogOut:
                    Client.Game.GetScene<GameScene>()?.RequestQuitGame();

                    break;

                case Buttons.Journal:
                    GameActions.OpenJournal(World);

                    break;

                case Buttons.Quests:
                    GameActions.RequestQuestMenu(World);

                    break;

                case Buttons.Skills:
                    GameActions.OpenSkills(World);

                    break;

                case Buttons.Guild:
                    GameActions.OpenGuildGump(World);

                    break;

                case Buttons.PeaceWarToggle:
                    GameActions.ToggleWarMode(World.Player);

                    break;

                case Buttons.Status:

                    if (LocalSerial == World.Player)
                    {
                        UIManager.GetGump<BaseHealthBarGump>(LocalSerial)?.Dispose();

                        var status = StatusGumpBase.GetStatusGump();

                        if (status == null)
                        {
                            UIManager.Add(StatusGumpBase.AddStatusGump(World,ProfileManager.CurrentProfile.StatusGumpPosition.X, ProfileManager.CurrentProfile.StatusGumpPosition.Y));
                        }
                        else
                        {
                            status.BringOnTop();
                        }
                    }
                    else
                    {
                        if (UIManager.GetGump<BaseHealthBarGump>(LocalSerial) != null)
                        {
                            break;
                        }

                        if (ProfileManager.CurrentProfile.CustomBarsToggled)
                        {
                            var bounds = new Rectangle(
                                0,
                                0,
                                HealthBarGumpCustom.HPB_WIDTH,
                                HealthBarGumpCustom.HPB_HEIGHT_SINGLELINE
                            );

                            UIManager.Add(
                                new HealthBarGumpCustom(World, LocalSerial)
                                {
                                    X = Mouse.Position.X - (bounds.Width >> 1),
                                    Y = Mouse.Position.Y - 5
                                }
                            );
                        }
                        else
                        {
                            ref readonly SpriteInfo gumpInfo = ref Client.Game.UO.Gumps.GetGump(0x0804);

                            UIManager.Add(
                                new HealthBarGump(World,LocalSerial)
                                {
                                    X = Mouse.Position.X - (gumpInfo.UV.Width >> 1),
                                    Y = Mouse.Position.Y - 5
                                }
                            );
                        }
                    }

                    break;
            }
        }

        private enum Buttons
        {
            Help,
            Options,
            LogOut,
            Journal,
            Quests,
            Skills,
            Guild,
            PeaceWarToggle,
            Status
        }

        private class EquipmentSlot : Control
        {
            private ItemGumpFixed _itemGump;
            private readonly PaperDollGump _paperDollGump;

            private Control bg, border;
            private double forcedScale = 1f;

            public EquipmentSlot(
                uint serial,
                int x,
                int y,
                Layer layer,
                PaperDollGump paperDollGump
            )
            {
                X = x;
                Y = y;
                LocalSerial = serial;
                Width = 19;
                Height = 20;
                _paperDollGump = paperDollGump;
                Layer = layer;

                Add(bg = new GumpPicTiled(0, 0, 19, 20, 0x243A) { AcceptMouseInput = false });

                Add(border = new GumpPic(0, 0, 0x2344, 0) { AcceptMouseInput = false });

                AcceptMouseInput = true;

                WantUpdateSize = false;
            }

            public override Control ScaleWidthAndHeight(double scale)
            {
                forcedScale = scale;
                bg?.ScaleWidthAndHeight(scale);
                border?.ScaleWidthAndHeight(scale);
                return base.ScaleWidthAndHeight(scale);
            }

            public override Control ScaleXAndY(double scale)
            {
                forcedScale = scale;
                bg?.ScaleXAndY(scale);
                border?.ScaleXAndY(scale);
                return base.ScaleXAndY(scale);
            }

            public Layer Layer { get; }

            public override void PreDraw()
            {
                Item item = _paperDollGump.World.Items.Get(LocalSerial);

                if (item == null || item.IsDestroyed)
                {
                    _itemGump?.Dispose();
                    _itemGump = null;
                }

                Mobile mobile = _paperDollGump.World.Mobiles.Get(_paperDollGump.LocalSerial);

                if (mobile != null)
                {
                    Item it_at_layer = mobile.FindItemByLayer(Layer);

                    if ((it_at_layer != null && _itemGump != null && _itemGump.LocalSerial != it_at_layer.Serial) || _itemGump == null)
                    {
                        if (_itemGump != null)
                        {
                            _itemGump.Dispose();
                            _itemGump = null;
                        }

                        item = it_at_layer;

                        if (item != null)
                        {
                            LocalSerial = it_at_layer.Serial;

                            Add(
                                _itemGump = new ItemGumpFixed(_paperDollGump, item, 18, 18)
                                {
                                    X = 0,
                                    Y = 0,
                                    Width = 18,
                                    Height = 18,
                                    HighlightOnMouseOver = false,
                                    CanPickUp =
                                        _paperDollGump.World.InGame
                                        && (
                                            _paperDollGump.World.Player.Serial == _paperDollGump.LocalSerial
                                            || _paperDollGump.CanLift
                                        )
                                }
                            );
                            _itemGump.ScaleWidthAndHeight(forcedScale).ScaleXAndY(forcedScale);
                        }
                    }
                }

                base.PreDraw();
            }

            private class ItemGumpFixed : ItemGump
            {
                private readonly PaperDollGump _gump;
                private Point originalSize;
                private Point point;
                private readonly Rectangle graphicSize;

                public ItemGumpFixed(PaperDollGump gump, Item item, int w, int h)
                    : base(gump, item.Serial, item.DisplayedGraphic, item.Hue, item.X, item.Y)
                {
                    _gump = gump;
                    Width = w;
                    Height = h;
                    WantUpdateSize = false;

                    graphicSize = Client.Game.UO.Arts.GetRealArtBounds(item.DisplayedGraphic);

                    originalSize.X = Width;
                    originalSize.Y = Height;

                    if (graphicSize.Width < Width)
                    {
                        originalSize.X = graphicSize.Width;
                        point.X = (Width >> 1) - (originalSize.X >> 1);
                    }

                    if (graphicSize.Height < Height)
                    {
                        originalSize.Y = graphicSize.Height;
                        point.Y = (Height >> 1) - (originalSize.Y >> 1);
                    }
                }

                public override Control ScaleWidthAndHeight(double scale)
                {
                    base.ScaleWidthAndHeight(scale);

                    originalSize.X = Width;
                    originalSize.Y = Height;

                    if (graphicSize.Width < Width)
                    {
                        originalSize.X = graphicSize.Width;
                        point.X = (Width >> 1) - (originalSize.X >> 1);
                    }

                    if (graphicSize.Height < Height)
                    {
                        originalSize.Y = graphicSize.Height;
                        point.Y = (Height >> 1) - (originalSize.Y >> 1);
                    }

                    return this;
                }

                public override bool Draw(UltimaBatcher2D batcher, int x, int y)
                {
                    Item item = _gump.World.Items.Get(LocalSerial);

                    if (item == null)
                    {
                        Dispose();
                    }

                    if (IsDisposed)
                    {
                        return false;
                    }

                    Vector3 hueVector = ShaderHueTranslator.GetHueVector(
                        MouseIsOver && HighlightOnMouseOver ? 0x0035 : item.Hue,
                        item.ItemData.IsPartialHue,
                        1,
                        true
                    );

                    ref readonly SpriteInfo artInfo = ref Client.Game.UO.Arts.GetArt(item.DisplayedGraphic);

                    if (artInfo.Texture != null)
                    {
                        batcher.Draw(
                            artInfo.Texture,
                            new Rectangle(
                                x + point.X,
                                y + point.Y,
                                originalSize.X,
                                originalSize.Y
                            ),
                            new Rectangle(
                                artInfo.UV.X + graphicSize.X,
                                artInfo.UV.Y + graphicSize.Y,
                                graphicSize.Width,
                                graphicSize.Height
                            ),
                            hueVector
                        );

                        return true;
                    }

                    return false;
                }

                public override bool Contains(int x, int y) => true;
            }
        }

        public static class Settings
        {
            public static ushort Graphic_Background_Player { get; set; } = 0x07d0;
            public static ushort Graphic_Background_Other { get; set; } = 0x07d1;

            public static ushort Graphic_Button_Help_Normal { get; set; } = 0x07ef;
            public static ushort Graphic_Button_Help_Pressed { get; set; } = 0x07f0;
            public static ushort Graphic_Button_Help_Hover { get; set; } = 0x07f1;

            public static ushort Graphic_Button_Options_Normal { get; set; } = 2006;
            public static ushort Graphic_Button_Options_Pressed { get; set; } = 2007;
            public static ushort Graphic_Button_Options_Hover { get; set; } = 2008;

            public static ushort Graphic_Button_Logout_Normal { get; set; } = 2009;
            public static ushort Graphic_Button_Logout_Pressed { get; set; } = 2010;
            public static ushort Graphic_Button_Logout_Hover { get; set; } = 2011;

            public static ushort Graphic_Button_Journal_Normal { get; set; } = 2012;
            public static ushort Graphic_Button_Journal_Pressed { get; set; } = 2013;
            public static ushort Graphic_Button_Journal_Hover { get; set; } = 2014;

            public static ushort Graphic_Button_Quest_Normal { get; set; } = 22453;
            public static ushort Graphic_Button_Quest_Pressed { get; set; } = 22455;
            public static ushort Graphic_Button_Quest_Hover { get; set; } = 22454;

            public static ushort Graphic_Button_Skills_Normal { get; set; } = 2015;
            public static ushort Graphic_Button_Skills_Pressed { get; set; } = 2016;
            public static ushort Graphic_Button_Skills_Hover { get; set; } = 2017;

            public static ushort Graphic_Button_Guild_Normal { get; set; } = 22450;
            public static ushort Graphic_Button_Guild_Pressed { get; set; } = 22452;
            public static ushort Graphic_Button_Guild_Hover { get; set; } = 22451;

            public static ushort Graphic_Button_Warmode_Normal { get; set; } = 0x07e8;
            public static ushort Graphic_Button_Warmode_Pressed { get; set; } = 0x07e9;
            public static ushort Graphic_Button_Warmode_Hover { get; set; } = 0x07ea;

            public static ushort Graphic_Button_Peacemode_Normal { get; set; } = 0x07e5;
            public static ushort Graphic_Button_Peacemode_Pressed { get; set; } = 0x07e6;
            public static ushort Graphic_Button_Peacemode_Hover { get; set; } = 0x07e7;

            public static ushort Graphic_Button_Status_Normal { get; set; } = 2027;
            public static ushort Graphic_Button_Status_Pressed { get; set; } = 2028;
            public static ushort Graphic_Button_Status_Hover { get; set; } = 2029;

            public static ushort Graphic_Button_Profile { get; set; } = 0x07D2;
            public static ushort Graphic_Button_Party { get; set; } = 0x07D2;

            public static ushort Graphic_Button_Virtue { get; set; } = 0x0071;

            public static ushort Graphic_Button_Durability { get; set; } = 5587;

            public static ushort Graphic_Button_Combat { get; set; } = 0x2B34;

            public static ushort Graphic_Button_RacialAbilties { get; set; } = 0x2B28;

            public static ushort Graphic_Button_Minimized { get; set; } = 0x7EE;

            public static ushort Hue_Background_Player { get; set; } = 0;
            public static ushort Hue_Background_Other { get; set; } = 0;

            public static ushort Hue_Title { get; set; } = 0x0386;
            public static int Size_Width_Hue { get; set; } = 185;

            public static int Position_X_Help { get; set; } = 185;
            public static int Position_Y_Help { get; set; } = 44;

            public static int Position_X_Options { get; set; } = 185;
            public static int Position_Y_Options { get; set; } = 44 + 27 * 1;

            public static int Position_X_Logout { get; set; } = 185;
            public static int Position_Y_Logout { get; set; } = 44 + 27 * 2;

            public static int Position_X_Journal { get; set; } = 185;
            public static int Position_Y_Journal { get; set; } = 44 + 27 * 3;

            public static int Position_X_Quest { get; set; } = 185;
            public static int Position_Y_Quest { get; set; } = 44 + 27 * 3;

            public static int Position_X_Skills { get; set; } = 185;
            public static int Position_Y_Skills { get; set; } = 44 + 27 * 4;

            public static int Position_X_Guild { get; set; } = 185;
            public static int Position_Y_Guild { get; set; } = 44 + 27 * 5;

            public static int Position_X_WarMode { get; set; } = 185;
            public static int Position_Y_Warmode { get; set; } = 44 + 27 * 6;

            public static int Position_X_Status { get; set; } = 185;
            public static int Position_Y_Status { get; set; } = 44 + 27 * 7;

            public static int Position_X_Profile { get; set; } = 25;
            public static int Position_Y_Profile { get; set; } = 196;
            public static int Racial_Abilities_Width { get; set; } = 14;

            public static int Position_X_RacialAbilities { get; set; } = 23;
            public static int Position_Y_RacialAbilities { get; set; } = 200;

            public static int Position_X_Virtue { get; set; } = 80;
            public static int Position_Y_Virtue { get; set; } = 4;

            public static int Position_X_Durability { get; set; } = 0;
            public static int Position_Y_Durability { get; set; } = 40;

            public static int Position_X_LeftSlots { get; set; } = 2;
            public static int Position_Y_LeftSlots { get; set; } = 75;
            public static int Size_Height_LeftSlots { get; set; } = 21;

            public static int Position_X_RightSlots { get; set; } = 166;
            public static int Position_Y_RightSlots { get; set; } = 75;
            public static int Size_Height_RightSlots { get; set; } = 21;

            public static int Position_X_Avatar { get; set; } = 8;
            public static int Position_Y_Avatar { get; set; } = 19;

            public static int Position_X_CombatBook { get; set; } = 156;
            public static int Position_Y_CombatBook { get; set; } = 200;

            public static int Position_X_Title { get; set; } = 39;
            public static int Position_Y_Title { get; set; } = 262;

            public static int Position_X_MinimizeButton { get; set; } = 228;
            public static int Position_Y_MinimizeButton { get; set; } = 260;
            public static int Size_Width_MinimizeButton { get; set; } = 16;
            public static int Size_Height_MinimizeButton { get; set; } = 16;


        }
    }
}
