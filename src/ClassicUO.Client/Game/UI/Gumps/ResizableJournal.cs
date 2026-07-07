using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using ClassicUO.Utility.Logging;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using SDL = SDL3.SDL;

namespace ClassicUO.Game.UI.Gumps
{
    public class ResizableJournal : ResizableGump
    {
        #region VARS
        public static bool ReloadTabs { get; set; } = false;

        private static int BORDER_WIDTH = 4;
        private static int MIN_WIDTH = (BORDER_WIDTH * 2) + (TAB_WIDTH * 4) + 40;
        private const int MIN_HEIGHT = 100;
        private const int SCROLL_BAR_WIDTH = 14;
        private const uint INACTIVE_TRANSPARENCY_DELAY = 3000;
        #region TABS
        private const int TAB_WIDTH = 80;
        private const int TAB_HEIGHT = 30;
        #endregion
        #endregion

        #region CONTROLS

        #region TABS
        private List<NiceButton> _tab = new List<NiceButton>();
        private List<string> _tabName = new List<string>();
        private List<MessageType[]> _tabTypes = new List<MessageType[]>();
        private MessageType[] _currentFilter;
        #endregion

        private AlphaBlendControl _background;
        private JournalEntriesContainer _journalArea;
        private ScrollBar _scrollBarBase;
        private NiceButton _newTabButton;
        private NiceButton _clearJournalButton;
        #endregion

        #region OTHER
        private static int _lastX = 100, _lastY = 100;
        private static int _lastWidth = MIN_WIDTH, _lastHeight = 300;
        private readonly GumpPicTiled _backgroundTexture;
        private World _world;
        private uint _inactiveSince;
        private bool _hiddenForInactivity;
        #endregion
        public ResizableJournal(World world) : base(world, _lastWidth, _lastHeight, MIN_WIDTH, MIN_HEIGHT, 0, 0)
        {
            _world = world;
            AnchorType = ProfileManager.CurrentProfile.JournalAnchorEnabled ? ANCHOR_TYPE.NONE : ANCHOR_TYPE.DISABLED;
            CanMove = true;
            _prevCanMove = true;
            AcceptMouseInput = true;
            WantUpdateSize = true;
            CanCloseWithRightClick = true;
            _prevCloseWithRightClick = true;
            if (ProfileManager.CurrentProfile != null)
            {
                _lastX = ProfileManager.CurrentProfile.JournalPosition.X;
                _lastY = ProfileManager.CurrentProfile.JournalPosition.Y;
                IsLocked = ProfileManager.CurrentProfile.JournalLocked;
            }
            X = _lastX;
            Y = _lastY;


            #region Background
            _background = new AlphaBlendControl((float)ProfileManager.CurrentProfile.JournalOpacity / 100);
            _background.Hue = ProfileManager.CurrentProfile.AltJournalBackgroundHue;
            _background.Width = Width - (BORDER_WIDTH * 2);
            _background.Height = Height - (BORDER_WIDTH * 2);
            _background.X = BORDER_WIDTH;
            _background.Y = BORDER_WIDTH;
            _background.CanCloseWithRightClick = true;
            _background.DragBegin += (sender, e) =>
            {
                InvokeDragBegin(e.Location);
            };

            _backgroundTexture = new GumpPicTiled(0);
            #endregion

            #region Journal Area
            _scrollBarBase = new ScrollBar(
                Width - SCROLL_BAR_WIDTH - BORDER_WIDTH,
                BORDER_WIDTH + TAB_HEIGHT,
                Height - TAB_HEIGHT - (BORDER_WIDTH * 2));

            _journalArea = new JournalEntriesContainer(
                BORDER_WIDTH,
                BORDER_WIDTH + TAB_HEIGHT,
                Width - SCROLL_BAR_WIDTH - (BORDER_WIDTH * 2),
                Height - (BORDER_WIDTH * 2) - TAB_HEIGHT,
                _scrollBarBase,
                this);
            _journalArea.CanCloseWithRightClick = true;
            _journalArea.DragBegin += (sender, e) => { InvokeDragBegin(e.Location); };
            #endregion

            Add(_background);
            Add(_backgroundTexture);
            Add(_scrollBarBase);

            Add(_journalArea);

            Add(_newTabButton = new NiceButton(0, 0, 20, TAB_HEIGHT, ButtonAction.Activate, "+") { IsSelectable = false });
            _newTabButton.SetTooltip("Add a new tab");
            _newTabButton.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    new PromptPopupWindow("New Tab", "Enter a tab name", entry =>
                    {
                        if (!string.IsNullOrEmpty(entry) && !ProfileManager.CurrentProfile.JournalTabs.ContainsKey(entry))
                        {
                            ProfileManager.CurrentProfile.JournalTabs.Add(entry, new MessageType[] { MessageType.Regular });
                            ResizableJournal.ReloadTabs = true;
                        }
                    }, "Save", "Cancel");
                }
            };

            Add(_clearJournalButton = new NiceButton(0, 0, 20, TAB_HEIGHT, ButtonAction.Activate, "X") { IsSelectable = false });
            _clearJournalButton.SetTooltip("Clear journal entries");
            _clearJournalButton.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    _journalArea.ClearEntries();
                }
            };

            BuildTabs();

            InitJournalEntries();
            ResizeWindow(ProfileManager.CurrentProfile.ResizeJournalSize);
            BuildBorder();
            EventSink.JournalEntryAdded += EventSink_EntryAdded;
        }

        private void EventSink_EntryAdded(object sender, JournalEntry e) => AddJournalEntry(e);

        public override GumpType GumpType => GumpType.Journal;

        public enum BorderStyle
        {
            Default,
            Style1,
            Style2,
            Style3,
            Style4,
            Style5,
            Style6,
            Style7
        }

        private void BuildTabs()
        {
            foreach (NiceButton tab in _tab)
            {
                tab.Dispose();
            }

            _tab.Clear();
            _tabName.Clear();
            _tabTypes.Clear();

            foreach (KeyValuePair<string, MessageType[]> tab in ProfileManager.CurrentProfile.JournalTabs)
            {
                AddTab(tab.Key, tab.Value);
            }
            if (ProfileManager.CurrentProfile.LastJournalTab < _tab.Count)
            {
                _tab[ProfileManager.CurrentProfile.LastJournalTab].IsSelected = true;
                OnButtonClick(ProfileManager.CurrentProfile.LastJournalTab); //Simulate selecting a tab
            }

            for (int i = 0; i < _tab.Count; i++)
                Add(_tab[i]);

            _newTabButton.X = (_tab.Count * TAB_WIDTH) + 4;
            _clearJournalButton.X = _newTabButton.X + _newTabButton.Width + 4;
        }

        public void BuildBorder()
        {
            int graphic = 0, borderSize = 0;
            switch ((BorderStyle)ProfileManager.CurrentProfile.JournalStyle)
            {
                case BorderStyle.Style1:
                    graphic = 3500; borderSize = 26;
                    break;
                case BorderStyle.Style2:
                    graphic = 5054; borderSize = 12;
                    break;
                case BorderStyle.Style3:
                    graphic = 5120; borderSize = 10;
                    break;
                case BorderStyle.Style4:
                    graphic = 9200; borderSize = 7;
                    break;
                case BorderStyle.Style5:
                    graphic = 9270; borderSize = 10;
                    break;
                case BorderStyle.Style6:
                    graphic = 9300; borderSize = 4;
                    break;
                case BorderStyle.Style7:
                    graphic = 9260; borderSize = 17;
                    break;

                default:
                case BorderStyle.Default:
                    BorderControl.DefaultGraphics();
                    _backgroundTexture.IsVisible = false;
                    _background.IsVisible = true;
                    BORDER_WIDTH = 4;
                    break;
            }

            if ((BorderStyle)ProfileManager.CurrentProfile.JournalStyle != BorderStyle.Default)
            {
                BorderControl.T_Left = (ushort)graphic;
                BorderControl.H_Border = (ushort)(graphic + 1);
                BorderControl.T_Right = (ushort)(graphic + 2);
                BorderControl.V_Border = (ushort)(graphic + 3);

                _backgroundTexture.Graphic = (ushort)(graphic + 4);
                _backgroundTexture.IsVisible = true;
                _backgroundTexture.Hue = _background.Hue;
                BorderControl.Hue = _background.Hue;
                BorderControl.Alpha = (float)ProfileManager.CurrentProfile.JournalOpacity / 100;
                _background.IsVisible = false;

                BorderControl.V_Right_Border = (ushort)(graphic + 5);
                BorderControl.B_Left = (ushort)(graphic + 6);
                BorderControl.H_Bottom_Border = (ushort)(graphic + 7);
                BorderControl.B_Right = (ushort)(graphic + 8);
                BorderControl.BorderSize = borderSize;
                BORDER_WIDTH = borderSize;
            }
            Reposition();

            if (ProfileManager.CurrentProfile.HideJournalBorder)
                BorderControl.IsVisible = false;
            else
                BorderControl.IsVisible = true;
        }

        private void Reposition()
        {
            if (IsDisposed)
                return;
            _background.X = BORDER_WIDTH;
            _background.Y = BORDER_WIDTH;
            _background.Width = Width - (BORDER_WIDTH * 2);
            _background.Height = Height - (BORDER_WIDTH * 2);

            _backgroundTexture.X = _background.X;
            _backgroundTexture.Y = _background.Y;
            _backgroundTexture.Width = _background.Width;
            _backgroundTexture.Height = _background.Height;
            _backgroundTexture.Alpha = (float)ProfileManager.CurrentProfile.JournalOpacity / 100;
            BorderControl.Alpha = (float)ProfileManager.CurrentProfile.JournalOpacity / 100;

            _journalArea.X = BORDER_WIDTH;
            _journalArea.Y = TAB_HEIGHT;
            _journalArea.Width = Width - SCROLL_BAR_WIDTH - (BORDER_WIDTH * 2);
            _journalArea.Height = Height - BORDER_WIDTH - TAB_HEIGHT;

            _lastWidth = Width;
            _lastHeight = Height;

            _scrollBarBase.X = Width - SCROLL_BAR_WIDTH - BORDER_WIDTH;
            _scrollBarBase.Y = _journalArea.Y;
            _scrollBarBase.Height = Height - BORDER_WIDTH - TAB_HEIGHT;
            ProfileManager.CurrentProfile.ResizeJournalSize = new Point(Width, Height);
        }

        public void UpdateOptions()
        {
            _backgroundTexture.Alpha = (float)ProfileManager.CurrentProfile.JournalOpacity / 100;
            BorderControl.Alpha = (float)ProfileManager.CurrentProfile.JournalOpacity / 100;
            _background.Hue = ProfileManager.CurrentProfile.AltJournalBackgroundHue;
            AnchorType = ProfileManager.CurrentProfile.JournalAnchorEnabled ? ANCHOR_TYPE.NONE : ANCHOR_TYPE.DISABLED;

            BuildBorder();
        }

        public static void UpdateJournalOptions() => UIManager.ForEach<ResizableJournal>(p => p.UpdateOptions());

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("rw", Width.ToString());
            writer.WriteAttributeString("rh", Height.ToString());

            int c = 0;
            foreach (NiceButton tab in _tab)
            {
                if (tab.IsSelected)
                {
                    writer.WriteAttributeString("tab", c.ToString());
                    break;
                }
                c++;
            }
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            var savedSize = new Microsoft.Xna.Framework.Point(Width, Height);

            if (int.TryParse(xml.GetAttribute("rw"), out int width) && width > 0)
            {
                savedSize.X = width;
            }
            if (int.TryParse(xml.GetAttribute("rh"), out int height) && height > 0)
            {
                savedSize.Y = height;
            }
            if (int.TryParse(xml.GetAttribute("tab"), out int tab))
            {
                OnButtonClick(tab); //Simulate selecting a tab
            }

            ResizeWindow(savedSize);
            BuildBorder();
        }

        public override void OnMouseWheel(MouseEventType delta)
        {
            base.OnMouseWheel(delta);
            if (_scrollBarBase != null)
                _scrollBarBase.InvokeMouseWheel(delta);
        }

        public override void OnButtonClick(int buttonID)
        {
            if (_tab.Count > buttonID)
            {
                _tab[buttonID].IsSelected = true;
                _currentFilter = _tabTypes[buttonID];
                _journalArea.CalculateScrollBarMaxValue();
                _journalArea.Update();
                _scrollBarBase.Value = _scrollBarBase.MaxValue;
                ProfileManager.CurrentProfile.LastJournalTab = buttonID;
            }
        }

        private void AddTab(string Name, MessageType[] filters)
        {
            NiceButton nb;
            _tab.Add(nb = new NiceButton((_tab.Count * TAB_WIDTH) + 4, 0, TAB_WIDTH, TAB_HEIGHT, ButtonAction.Activate, Name, 1)
            {
                ButtonParameter = _tab.Count,
                IsSelectable = true,
                CanCloseWithRightClick = false,
                ContextMenu = new TabContextEntry(this, Name)
            });

            nb.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Right)
                {
                    nb.ContextMenu.Show();
                }
            };
            _tabName.Add(Name);
            _tabTypes.Add(filters);
        }

        private void AddJournalEntry(JournalEntry journalEntry)
        {
            if (journalEntry == null)
                return;
            if (!string.IsNullOrEmpty(journalEntry.Name) && World.IgnoreManager.IgnoredCharsList.Contains(journalEntry.Name))
                return;

            string text;
            if (string.IsNullOrEmpty(journalEntry.Name) || string.Equals(journalEntry.Name, journalEntry.Text.Trim())) //Text is apparently prepended with a space from servers.
            {
                text = journalEntry.Text;
            }
            else
            {
                text = $"{journalEntry.Name}: {journalEntry.Text}";
            }
            _journalArea.AddEntry(text, journalEntry.Hue, journalEntry.Time, journalEntry.TextType, journalEntry.MessageType);
        }

        private void InitJournalEntries()
        {
            foreach (JournalEntry entry in JournalManager.Entries)
            {
                if (entry == null)
                    continue;
                AddJournalEntry(entry);
            }
        }

        protected override void UpdateContents()
        {
            base.UpdateContents();
            _background.Alpha = (float)ProfileManager.CurrentProfile.JournalOpacity / 100;
        }

        public override void Update()
        {
            base.Update();

            if (IsDisposed) { return; }

            if (X != _lastX || Y != _lastY)
            {
                _lastX = X;
                _lastY = Y;
                ProfileManager.CurrentProfile.JournalPosition = Location;
            }
            if (((Width != _lastWidth || Height != _lastHeight) && !Mouse.LButtonPressed))
                Reposition();

            if (ReloadTabs)
            {
                ReloadTabs = false;

                BuildTabs();
            }

            UpdateInactiveTransparency();
        }

        private void UpdateInactiveTransparency()
        {
            if (ProfileManager.CurrentProfile == null || !ProfileManager.CurrentProfile.JournalTransparencyWhenInactive)
            {
                if (_hiddenForInactivity)
                    RestoreFromInactivity();

                _inactiveSince = 0;
                return;
            }

            if (IsMouseOverJournal() || IsTopMostGump())
            {
                _inactiveSince = 0;

                if (_hiddenForInactivity)
                    RestoreFromInactivity();

                return;
            }

            if (_inactiveSince == 0)
                _inactiveSince = Time.Ticks;

            if (!_hiddenForInactivity && Time.Ticks - _inactiveSince >= INACTIVE_TRANSPARENCY_DELAY)
                ApplyInactivity();

            if (_hiddenForInactivity) //The journal area keeps re-enabling the scroll bar, so keep it hidden every frame.
                _scrollBarBase.IsVisible = false;
        }

        private bool IsMouseOverJournal()
        {
            //MouseIsOver only covers the gump itself, so also treat hovering any child (text area, tabs, scroll bar) as being over the journal.
            return MouseIsOver || UIManager.MouseOverControl?.RootParent == this;
        }

        private bool IsTopMostGump()
        {
            foreach (IGui gump in UIManager.Gumps)
            {
                if (gump.IsDisposed || !gump.IsVisible || gump.LayerOrder != UILayer.Default)
                    continue;

                return gump == this;
            }

            return false;
        }

        private void ApplyInactivity()
        {
            _hiddenForInactivity = true;

            ShowBorder = false; //Hides the border and the resize handle.
            _background.IsVisible = false;
            _backgroundTexture.IsVisible = false;
            _scrollBarBase.IsVisible = false;

            foreach (NiceButton tab in _tab)
                tab.IsVisible = false;

            _newTabButton.IsVisible = false;
            _clearJournalButton.IsVisible = false;
        }

        private void RestoreFromInactivity()
        {
            _hiddenForInactivity = false;

            foreach (NiceButton tab in _tab)
                tab.IsVisible = true;

            _newTabButton.IsVisible = true;
            _clearJournalButton.IsVisible = true;
            ResizeButton.IsVisible = !IsLocked;

            BuildBorder(); //Restores border and background visibility based on the current profile/style.

            if (IsLocked)
                ShowBorder = false;
        }

        public override void Dispose()
        {
            EventSink.JournalEntryAdded -= EventSink_EntryAdded;
            _journalArea.Dispose();
            base.Dispose();
        }

        private class JournalEntriesContainer : Control
        {
            private const int SelectionAutoScrollMargin = 16;
            private const int SelectionAutoScrollStep = 12;
            private static readonly Color SelectionBackgroundColor = new Color(56, 112, 220);

            private Deque<JournalData> journalDatas = new Deque<JournalData>();


            private readonly ScrollBarBase _scrollBar;
            private int lastWidth = 0, lastHeight = 0;
            private int _nextEntryOrder;
            private ResizableJournal _resizableJournal;
            private SelectionPoint? _selectionAnchor;
            private SelectionPoint? _selectionActive;
            private bool _isSelecting;

            public JournalEntriesContainer(int x, int y, int width, int height, ScrollBarBase scrollBarControl, ResizableJournal resizableJournal)
            {
                _resizableJournal = resizableJournal;
                _scrollBar = scrollBarControl;
                _scrollBar.IsVisible = false;
                AcceptMouseInput = true;
                AcceptKeyboardInput = true;
                HandlesKeyboardFocus = true;
                CanMove = true;

                X = x;
                Y = y;
                Width = lastWidth = width;
                Height = lastHeight = height;

                WantUpdateSize = false;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                base.Draw(batcher, x, y);
                int my = y;
                bool hideTimestamp = ProfileManager.CurrentProfile.HideJournalTimestamp;

                if (batcher.ClipBegin(x, y, Width, Height))
                {
                    foreach (JournalData journalEntry in journalDatas)
                    {
                        if (journalEntry == null || journalEntry.EntryText == null || journalEntry.TimeStamp == null)
                            continue;

                        if (!CanBeDrawn(journalEntry.TextType, journalEntry.MessageType))
                            continue;

                        if (my + journalEntry.EntryText.Height - y >= _scrollBar.Value && my - y <= _scrollBar.Value + _scrollBar.Height)
                        {
                            int textX = hideTimestamp ? x : x + journalEntry.TimeStamp.Width + 5;
                            int textY = my - _scrollBar.Value;

                            DrawSelection(batcher, journalEntry, textX, textY);

                            if (!hideTimestamp)
                                journalEntry.TimeStamp.Draw(batcher, x, textY);

                            journalEntry.EntryText.Draw(batcher, textX, textY);
                        }
                        my += journalEntry.EntryText.Height;
                    }

                    batcher.ClipEnd();
                }
                return true;
            }

            public override void Update()
            {
                base.Update();

                if (!IsVisible)
                    return;

                if (_isSelecting)
                {
                    if (Mouse.LButtonPressed)
                    {
                        UpdateSelectionFromMouse(Mouse.Position.X - ScreenCoordinateX, Mouse.Position.Y - ScreenCoordinateY);
                    }
                    else
                    {
                        _isSelecting = false;
                        CanMove = true;
                    }
                }

                _scrollBar.IsVisible = _scrollBar.MaxValue > _scrollBar.MinValue;
                if (Width != lastWidth || Height != lastHeight)
                {
                    lastWidth = Width;
                    lastHeight = Height;

                    foreach (JournalData _ in journalDatas)
                    {
                        if (_ is null)
                            continue;
                        _.EntryText.Width = Width - BORDER_WIDTH - (ProfileManager.CurrentProfile.HideJournalTimestamp ? 0 : _.TimeStamp.Width);
                        _.EntryText.Update(); //Because this control isn't a child of any gump, it doesn't get updated
                    }

                    CalculateScrollBarMaxValue();
                }

            }

            public void CalculateScrollBarMaxValue()
            {
                bool maxValue = _scrollBar.Value == _scrollBar.MaxValue;
                int height = 0;

                foreach (JournalData _ in journalDatas)
                {
                    if (_ != null)
                        if (CanBeDrawn(_.TextType, _.MessageType))
                            height += _.EntryText.Height;
                }


                height -= _scrollBar.Height;

                if (height > 0)
                {
                    _scrollBar.MaxValue = height;

                    if (maxValue)
                    {
                        _scrollBar.Value = _scrollBar.MaxValue;
                    }
                }
                else
                {
                    _scrollBar.MaxValue = 0;
                    _scrollBar.Value = 0;
                }
            }

            public void AddEntry(string text, ushort hue, DateTime time, TextType text_type, MessageType messageType)
            {
                bool maxScroll = _scrollBar.Value == _scrollBar.MaxValue;

                while (journalDatas.Count > (ProfileManager.CurrentProfile == null ? 200 : ProfileManager.CurrentProfile.MaxJournalEntries))
                {
                    JournalData removed = journalDatas.RemoveFromFront();

                    if (_selectionAnchor?.Entry == removed || _selectionActive?.Entry == removed)
                    {
                        ClearSelection();
                    }

                    removed.Destroy();
                }

                string timestampText = $"{time:t}";
                var timeS = TextBox.GetOne(timestampText, ProfileManager.CurrentProfile.SelectedTTFJournalFont, ProfileManager.CurrentProfile.SelectedJournalFontSize - 2, 1150, TextBox.RTLOptions.Default());
                var je = TextBox.GetOne(text, ProfileManager.CurrentProfile.SelectedTTFJournalFont, ProfileManager.CurrentProfile.SelectedJournalFontSize, hue,
                    new TextBox.RTLOptions() { Width = Width - (ProfileManager.CurrentProfile.HideJournalTimestamp ? 0 : timeS.Width), CalculateGlyphs = true });

                journalDatas.AddToBack(
                    new JournalData(
                        je,
                        timeS,
                        text,
                        timestampText,
                        _nextEntryOrder++,
                        text_type,
                        messageType
                    ));

                if (maxScroll)
                {
                    _scrollBar.Value = _scrollBar.MaxValue;
                }
                CalculateScrollBarMaxValue();
            }

            public override void OnMouseDown(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left)
                {
                    if (TryGetSelectionPointFromText(x, y, out SelectionPoint point))
                    {
                        SetKeyboardFocus();
                        CanMove = false;
                        _isSelecting = true;
                        _selectionAnchor = point;
                        _selectionActive = point;
                        return;
                    }

                    CanMove = true;
                }

                base.OnMouseDown(x, y, button);
            }

            public override void OnMouseOver(int x, int y)
            {
                if (_isSelecting && Mouse.LButtonPressed)
                {
                    UpdateSelectionFromMouse(x, y);
                    return;
                }

                base.OnMouseOver(x, y);
            }

            public override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left && _isSelecting)
                {
                    UpdateSelectionFromMouse(x, y);
                    _isSelecting = false;
                    CanMove = true;
                    return;
                }

                base.OnMouseUp(x, y, button);
            }

            public override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
            {
                switch (key)
                {
                    case SDL.SDL_Keycode.SDLK_C when Keyboard.Ctrl:
                        CopySelection();
                        return;

                    case SDL.SDL_Keycode.SDLK_A when Keyboard.Ctrl:
                        SelectAllVisible();
                        return;

                    case SDL.SDL_Keycode.SDLK_ESCAPE:
                        ClearSelection();
                        return;
                }

                base.OnKeyDown(key, mod);
            }

            private void UpdateSelectionFromMouse(int x, int y)
            {
                if (y < SelectionAutoScrollMargin)
                {
                    _scrollBar.Value = Math.Max(_scrollBar.MinValue, _scrollBar.Value - SelectionAutoScrollStep);
                }
                else if (y > Height - SelectionAutoScrollMargin)
                {
                    _scrollBar.Value = Math.Min(_scrollBar.MaxValue, _scrollBar.Value + SelectionAutoScrollStep);
                }

                _selectionActive = GetSelectionPoint(x, y);
            }

            private bool TryGetSelectionPointFromText(int x, int y, out SelectionPoint point)
            {
                point = default;

                int contentY = y + _scrollBar.Value;
                int rowY = 0;
                bool hideTimestamp = ProfileManager.CurrentProfile.HideJournalTimestamp;

                foreach (JournalData entry in journalDatas)
                {
                    if (entry == null || !CanBeDrawn(entry.TextType, entry.MessageType))
                    {
                        continue;
                    }

                    int rowHeight = entry.EntryText.Height;

                    if (contentY < rowY)
                    {
                        return false;
                    }

                    if (contentY <= rowY + rowHeight)
                    {
                        int textX = hideTimestamp ? 0 : entry.TimeStamp.Width + 5;
                        int localX = x - textX;
                        int localY = contentY - rowY;

                        if (!IsInsideTextLine(entry, localX, localY))
                        {
                            return false;
                        }

                        point = new SelectionPoint(entry, GetTextIndexAt(entry, localX, localY));
                        return true;
                    }

                    rowY += rowHeight;
                }

                return false;
            }

            private static bool IsInsideTextLine(JournalData entry, int x, int y)
            {
                if (x < 0 || y < 0 || y >= entry.EntryText.Height)
                {
                    return false;
                }

                var line = entry.EntryText.RTL.GetLineByY(y);

                return line != null && line.Count > 0 && x <= line.Size.X;
            }

            private SelectionPoint GetSelectionPoint(int x, int y)
            {
                JournalData nearest = null;
                int nearestIndex = 0;
                int contentY = y + _scrollBar.Value;
                int rowY = 0;
                bool hideTimestamp = ProfileManager.CurrentProfile.HideJournalTimestamp;

                foreach (JournalData entry in journalDatas)
                {
                    if (entry == null || !CanBeDrawn(entry.TextType, entry.MessageType))
                    {
                        continue;
                    }

                    int rowHeight = entry.EntryText.Height;

                    if (contentY < rowY)
                    {
                        return new SelectionPoint(entry, 0);
                    }

                    nearest = entry;
                    nearestIndex = entry.TextLength;

                    if (contentY <= rowY + rowHeight)
                    {
                        int textX = hideTimestamp ? 0 : entry.TimeStamp.Width + 5;
                        int localX = Math.Max(0, x - textX);
                        int localY = Math.Clamp(contentY - rowY, 0, Math.Max(0, rowHeight - 1));

                        return new SelectionPoint(entry, GetTextIndexAt(entry, localX, localY));
                    }

                    rowY += rowHeight;
                }

                return nearest == null ? default : new SelectionPoint(nearest, nearestIndex);
            }

            private static int GetTextIndexAt(JournalData entry, int x, int y)
            {
                var line = entry.EntryText.RTL.GetLineByY(y);

                if (line == null)
                {
                    return y < 0 ? 0 : entry.TextLength;
                }

                int lineIndex = line.GetGlyphIndexByX(x) ?? (x <= 0 ? 0 : line.Count);
                int lineStart = entry.GetTextIndexFromRawOffset(line.TextStartIndex);

                return Math.Clamp(lineStart + lineIndex, 0, entry.TextLength);
            }

            private void DrawSelection(UltimaBatcher2D batcher, JournalData entry, int x, int y)
            {
                if (!TryGetSelectionRange(out SelectionPoint start, out SelectionPoint end))
                {
                    return;
                }

                if (entry.Order < start.Entry.Order || entry.Order > end.Entry.Order)
                {
                    return;
                }

                int selectionStart = entry == start.Entry ? start.Index : 0;
                int selectionEnd = entry == end.Entry ? end.Index : entry.TextLength;

                if (selectionStart >= selectionEnd)
                {
                    return;
                }

                foreach (var line in entry.EntryText.RTL.Lines)
                {
                    int lineStart = entry.GetTextIndexFromRawOffset(line.TextStartIndex);
                    int lineEnd = lineStart + line.Count;
                    int startIndex = Math.Max(selectionStart, lineStart);
                    int endIndex = Math.Min(selectionEnd, lineEnd);

                    if (startIndex >= endIndex)
                    {
                        continue;
                    }

                    int startX = GetLineX(entry, line, startIndex);
                    int endX = GetLineX(entry, line, endIndex);

                    if (endX <= startX)
                    {
                        continue;
                    }

                    batcher.Draw(
                        SolidColorTextureCache.GetTexture(SelectionBackgroundColor),
                        new Rectangle(x + startX, y + GetLineY(line), endX - startX, Math.Max(1, line.Size.Y)),
                        ShaderHueTranslator.GetHueVector(0, false, 0.42f)
                    );
                }
            }

            private static int GetLineX(JournalData entry, FontStashSharp.RichText.TextLine line, int textIndex)
            {
                int lineStart = entry.GetTextIndexFromRawOffset(line.TextStartIndex);
                int lineIndex = Math.Clamp(textIndex - lineStart, 0, line.Count);

                if (lineIndex >= line.Count)
                {
                    return line.Size.X;
                }

                FontStashSharp.RichText.TextChunkGlyph? glyph = line.GetGlyphInfoByIndex(lineIndex);
                return glyph?.Bounds.X ?? 0;
            }

            private static int GetLineY(FontStashSharp.RichText.TextLine line)
            {
                FontStashSharp.RichText.TextChunkGlyph? glyph = line.GetGlyphInfoByIndex(0);
                return glyph?.LineTop ?? 0;
            }

            private bool TryGetSelectionRange(out SelectionPoint start, out SelectionPoint end)
            {
                start = default;
                end = default;

                if (!_selectionAnchor.HasValue || !_selectionActive.HasValue)
                {
                    return false;
                }

                SelectionPoint a = _selectionAnchor.Value;
                SelectionPoint b = _selectionActive.Value;

                if (a.Entry == null || b.Entry == null || a.Entry == b.Entry && a.Index == b.Index)
                {
                    return false;
                }

                if (a.Entry.Order < b.Entry.Order || a.Entry == b.Entry && a.Index <= b.Index)
                {
                    start = a;
                    end = b;
                }
                else
                {
                    start = b;
                    end = a;
                }

                return true;
            }

            private void CopySelection()
            {
                if (!TryGetSelectionRange(out SelectionPoint start, out SelectionPoint end))
                {
                    return;
                }

                StringBuilder sb = new StringBuilder();
                bool hideTimestamp = ProfileManager.CurrentProfile.HideJournalTimestamp;

                foreach (JournalData entry in journalDatas)
                {
                    if (entry == null || !CanBeDrawn(entry.TextType, entry.MessageType))
                    {
                        continue;
                    }

                    if (entry.Order < start.Entry.Order || entry.Order > end.Entry.Order)
                    {
                        continue;
                    }

                    int selectionStart = entry == start.Entry ? start.Index : 0;
                    int selectionEnd = entry == end.Entry ? end.Index : entry.TextLength;

                    selectionStart = Math.Clamp(selectionStart, 0, entry.TextLength);
                    selectionEnd = Math.Clamp(selectionEnd, 0, entry.TextLength);

                    if (selectionStart >= selectionEnd)
                    {
                        continue;
                    }

                    if (sb.Length > 0)
                    {
                        sb.AppendLine();
                    }

                    if (!hideTimestamp)
                    {
                        sb.Append(entry.TimestampText);
                        sb.Append(' ');
                    }

                    int rawSelectionStart = entry.GetRawTextOffset(selectionStart);
                    int rawSelectionEnd = entry.GetRawTextOffset(selectionEnd);

                    if (rawSelectionStart >= rawSelectionEnd)
                    {
                        continue;
                    }

                    sb.Append(entry.RawText, rawSelectionStart, rawSelectionEnd - rawSelectionStart);
                }

                if (sb.Length > 0)
                {
                    ClassicUO.Utility.Clipboard.SetClipboardText(sb.ToString());
                }
            }

            private void SelectAllVisible()
            {
                JournalData first = null;
                JournalData last = null;

                foreach (JournalData entry in journalDatas)
                {
                    if (entry == null || !CanBeDrawn(entry.TextType, entry.MessageType))
                    {
                        continue;
                    }

                    first ??= entry;
                    last = entry;
                }

                if (first == null || last == null)
                {
                    ClearSelection();
                    return;
                }

                _selectionAnchor = new SelectionPoint(first, 0);
                _selectionActive = new SelectionPoint(last, last.TextLength);
            }

            private void ClearSelection()
            {
                _selectionAnchor = null;
                _selectionActive = null;
                _isSelecting = false;
                CanMove = true;
            }

            private bool CanBeDrawn(TextType type, MessageType messageType)
            {
                if (_resizableJournal._currentFilter != null)
                {
                    for (int i = 0; i < _resizableJournal._currentFilter.Length; i++)
                    {
                        MessageType currentfilter = _resizableJournal._currentFilter[i];

                        if (messageType == MessageType.ChatSystem && currentfilter == MessageType.ChatSystem)
                            return true;

                        if (type == TextType.SYSTEM && currentfilter == MessageType.System)
                            return true;

                        if (type == TextType.SYSTEM && currentfilter != MessageType.System)
                            continue;

                        if (currentfilter == messageType)
                            return true;
                    }
                    return false;
                }

                return true;
            }

            public void ClearEntries()
            {
                Reset();
                CalculateScrollBarMaxValue();
            }

            private void Reset()
            {
                foreach (JournalData _ in journalDatas)
                    _.Destroy();

                journalDatas.Clear();
                ClearSelection();
            }

            public override void Dispose()
            {
                Reset();

                base.Dispose();
            }

        public class JournalData
        {
            private readonly int[] _textIndexToRawOffset;

            public JournalData(TextBox textBox, TextBox timeStamp, string rawText, string timestampText, int order, TextType textType, MessageType messageType)
            {
                EntryText = textBox;
                TimeStamp = timeStamp;
                RawText = rawText ?? string.Empty;
                TimestampText = timestampText;
                Order = order;
                TextType = textType;
                MessageType = messageType;
                _textIndexToRawOffset = BuildTextIndexToRawOffset(RawText);
            }

                public void Destroy()
                {
                    EntryText?.Dispose();
                    TimeStamp?.Dispose();
                }

                public TextBox EntryText { get; }
                public TextBox TimeStamp { get; }
            public string RawText { get; }
            public string TimestampText { get; }
            public int Order { get; }
            public int TextLength => _textIndexToRawOffset.Length - 1;
            public TextType TextType { get; }
            public MessageType MessageType { get; }

            public int GetRawTextOffset(int textIndex)
            {
                return _textIndexToRawOffset[Math.Clamp(textIndex, 0, TextLength)];
            }

            public int GetTextIndexFromRawOffset(int rawOffset)
            {
                rawOffset = Math.Clamp(rawOffset, 0, RawText.Length);

                int index = Array.BinarySearch(_textIndexToRawOffset, rawOffset);

                return index >= 0 ? index : ~index;
            }

            private static int[] BuildTextIndexToRawOffset(string text)
            {
                List<int> offsets = new List<int>(text.Length + 1) { 0 };

                for (int i = 0; i < text.Length;)
                {
                    i += char.IsSurrogatePair(text, i) ? 2 : 1;
                    offsets.Add(i);
                }

                return offsets.ToArray();
            }
        }

            private readonly struct SelectionPoint
            {
                public SelectionPoint(JournalData entry, int index)
                {
                    Entry = entry;
                    Index = entry == null ? 0 : Math.Clamp(index, 0, entry.TextLength);
                }

                public JournalData Entry { get; }
                public int Index { get; }
            }
        }

        private class TabContextEntry : ContextMenuControl
        {
            public TabContextEntry(Gump gump, string name) : base(gump)
            {
                if (ProfileManager.CurrentProfile.JournalTabs.ContainsKey(name))
                {
                    MessageType[] selectedTypes = ProfileManager.CurrentProfile.JournalTabs[name];

                    foreach (MessageType item in Enum.GetValues<MessageType>())
                    {
                        string entryName = string.Empty;
                        switch (item)
                        {
                            case MessageType.Regular:
                                entryName = "Regular";
                                break;
                            case MessageType.System:
                                entryName = "System";
                                break;
                            case MessageType.Emote:
                                entryName = "Emote";
                                break;
                            case MessageType.Limit3Spell:
                                entryName = "Limit3Spell(Sphere)";
                                break;
                            case MessageType.Label:
                                entryName = "Label";
                                break;
                            case MessageType.Focus:
                                entryName = "Focus";
                                break;
                            case MessageType.Whisper:
                                entryName = "Whisper";
                                break;
                            case MessageType.Yell:
                                entryName = "Yell";
                                break;
                            case MessageType.Spell:
                                entryName = "Spell";
                                break;
                            case MessageType.Guild:
                                entryName = "Guild";
                                break;
                            case MessageType.Alliance:
                                entryName = "Alliance";
                                break;
                            case MessageType.Command:
                                entryName = "Command";
                                break;
                            case MessageType.Encoded:
                                entryName = "Encoded";
                                break;
                            case MessageType.ChatSystem:
                                entryName = "Global Chat";
                                break;
                            case MessageType.Party:
                                entryName = "Party";
                                break;
                            case MessageType.Damage:
                                entryName = "Damage";
                                break;
                            case MessageType.Discord:
                                entryName = "Discord";
                                break;
                        }

                        Add(entryName,
                            () =>
                            {
                                if (ProfileManager.CurrentProfile.JournalTabs.ContainsKey(name))
                                {
                                    MessageType[] selectedTypes = ProfileManager.CurrentProfile.JournalTabs[name];

                                    if (selectedTypes.Contains(item))
                                    {
                                        ProfileManager.CurrentProfile.JournalTabs[name] = RemoveType(selectedTypes, item);
                                        ResizableJournal.ReloadTabs = true;
                                    }
                                    else
                                    {
                                        ProfileManager.CurrentProfile.JournalTabs[name] = AddType(selectedTypes, item);
                                        ResizableJournal.ReloadTabs = true;
                                    }
                                }
                            },
                            true,
                            selectedTypes.Contains(item));
                    }
                }

                Add("X Delete Tab", () =>
                {
                    UIManager.Add(new QuestionGump(gump.World, $"Delete [{name}] tab?", (yes) =>
                    {
                        if (yes)
                        {
                            if (ProfileManager.CurrentProfile.JournalTabs.ContainsKey(name))
                            {
                                ProfileManager.CurrentProfile.JournalTabs.Remove(name);
                                ResizableJournal.ReloadTabs = true;
                            }
                        }
                    }));
                });
            }

            private static MessageType[] RemoveType(MessageType[] array, MessageType removeMe)
            {
                var modifiedList = new List<MessageType>();
                foreach (MessageType item in array)
                {
                    if (item != removeMe)
                    {
                        modifiedList.Add(item);
                    }
                }

                return modifiedList.ToArray();
            }

            private static MessageType[] AddType(MessageType[] array, MessageType addMe)
            {
                var modifiedList = new List<MessageType>();

                modifiedList.AddRange(array);

                modifiedList.Add(addMe);

                return modifiedList.ToArray();
            }
        }
    }
}
