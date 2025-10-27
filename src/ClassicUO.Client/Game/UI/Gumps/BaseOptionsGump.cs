using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL3;
using StbTextEditSharp;

namespace ClassicUO.Game.UI.Gumps;

public class BaseOptionsGump : Gump
{
    //TODO: Instanced search
    public static string SearchText { get; protected set; } = string.Empty;

    public static event EventHandler SearchValueChanged;

    protected LeftSideMenuRightSideContent MainContent;

    public override bool AcceptMouseInput { get; set; } = true;

    public BaseOptionsGump(World world, int width, int height, string title) : base(world, 0, 0)
    {
        CanMove = true;
        CanCloseWithRightClick = true;
        Width = width;
        Height = height;

        Build(title);
    }

    private void Build(string title)
    {
        Add
        (
            new ColorBox(Width, Height, ThemeSettings.BACKGROUND)
            {
                AcceptMouseInput = true,
                CanMove = true,
                Alpha = 0.85f
            }
        );

        Add
        (
            new ColorBox(Width, 40, ThemeSettings.SEARCH_BACKGROUND)
            {
                AcceptMouseInput = true,
                CanMove = true,
                Alpha = 0.85f
            }
        );

        var titleTextBox = TextBox.GetOne(title, ThemeSettings.FONT, 30, Color.White, TextBox.RTLOptions.Default());
        titleTextBox.X = 10;
        titleTextBox.Y = 7;
        Add(titleTextBox);

        Control c = TextBox.GetOne("Search", ThemeSettings.FONT, 30, Color.White, TextBox.RTLOptions.Default());
        c.Y = 7;
        Add(c);

        InputField search;

        Add
        (
            search = new InputField(400, 30)
            {
                X = Width - 405,
                Y = 5
            }
        );

        search.TextChanged += (s, e) =>
        {
            SearchText = search.Text;
            SearchValueChanged.Raise();
        };

        c.X = search.X - c.Width - 5;

        Add
        (
            MainContent = new LeftSideMenuRightSideContent(Width, Height - 40, (int)(Width * 0.23))
            {
                Y = 40
            }
        );

        MainContent.RightArea.ToggleScrollBarVisibility(false);
        MainContent.RightArea.GetScrollBar.Dispose();
        MainContent.RightArea.GetScrollBar = null;
    }

    public override void OnPageChanged()
    {
        base.OnPageChanged();

        MainContent.ActivePage = ActivePage;
    }

    public override void ChangePage(int pageIndex)
    {
        base.ChangePage(pageIndex);

        foreach (Control mb in MainContent.LeftArea.Children)
        {
            if (mb is ModernButton button && button.ButtonParameter == pageIndex && button.IsSelectable)
            {
                button.IsSelected = true;

                break;
            }
        }
    }

    public string GetPageString()
    {
        string page = MainContent.ActivePage.ToString();

        foreach (Control c in MainContent.RightArea.Children)
        {
            if (c is Area && c.Page == MainContent.ActivePage)
            {
                foreach (Control c2 in c.Children)
                {
                    if (c2 is LeftSideMenuRightSideContent)
                    {
                        page += ":" + c2.ActivePage;

                        return page;
                    }
                }
            }
        }

        return page;
    }

    public void GoToPage(string pageString)
    {
        string[] parts = pageString.Split(':');

        if (parts.Length >= 1)
        {
            if (int.TryParse(parts[0], out int p))
            {
                ChangePage(p);

                if (parts.Length >= 2)
                {
                    if (int.TryParse(parts[1], out int pp))
                    {
                        foreach (Control c in MainContent.RightArea.Children)
                        {
                            if (c is Area && c.Page == p)
                            {
                                foreach (Control c2 in c.Children)
                                {
                                    if (c2 is LeftSideMenuRightSideContent lsc)
                                    {
                                        lsc.ActivePage = pp;

                                        foreach (Control mb in lsc.LeftArea.Children)
                                        {
                                            if (mb is ModernButton button && button.ButtonParameter == pp && button.IsSelectable)
                                            {
                                                button.IsSelected = true;

                                                break;
                                            }
                                        }

                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    protected static void SetParentsForMatchingSearch(Control c, int page)
    {
        for (Control p = c.Parent; p != null; p = p.Parent)
        {
            if (p is LeftSideMenuRightSideContent content)
            {
                content.SetMatchingButton(page);
            }
        }
    }

    #region Sub-Classes

    protected ModernButton CategoryButton(string text, int page, int width, int height = 40) => new ModernButton(0, 0, width, height, ButtonAction.SwitchPage, text, ThemeSettings.BUTTON_FONT_COLOR)
    {
        ButtonParameter = page,
        FullPageSwitch = true
    };

    protected ModernButton SubCategoryButton(string text, int page, int width, int height = 40) => new ModernButton(0, 0, width, height, ButtonAction.SwitchPage, text, ThemeSettings.BUTTON_FONT_COLOR)
    {
        ButtonParameter = page
    };

    public static class ThemeSettings
    {
        public static int SLIDER_WIDTH { get; set; } = 150;
        public static int COMBO_BOX_WIDTH { get; set; } = 225;
        public static int SCROLL_BAR_WIDTH { get; set; } = 15;
        public static int INPUT_WIDTH { get; set; } = 200;
        public static int TOP_PADDING { get; set; } = 5;
        public static int INDENT_SPACE { get; set; } = 40;
        public static int BLANK_LINE { get; set; } = 20;
        public static int HORIZONTAL_SPACING_CONTROLS { get; set; } = 20;
        public static int STANDARD_TEXT_SIZE
        {
            get
            {
                if (ProfileManager.CurrentProfile != null)
                    return ProfileManager.CurrentProfile.OptionsFontSize;

                return 20;
            }
        }
        public static float NO_MATCH_SEARCH { get; set; } = 0.5f;
        public static ushort BACKGROUND { get; set; } = 897;
        public static ushort SEARCH_BACKGROUND { get; set; } = 899;
        public static ushort CHECKBOX { get; set; } = 899;
        public static int CHECKBOX_SIZE { get; set; } = 30;
        public static ushort BLACK { get; set; } = 0;
        public static Color DROPDOWN_OPTION_NORMAL_HUE { get; set; } = Color.White;
        public static Color DROPDOWN_OPTION_HOVER_HUE { get; set; } = Color.AntiqueWhite;
        public static Color DROPDOWN_OPTION_SELECTED_HUE { get; set; } = Color.CadetBlue;
        public static Color BUTTON_FONT_COLOR { get; set; } = Color.White;
        public static Color TEXT_FONT_COLOR { get; set; } = Color.White;
        public static string FONT {
            get
            {
                if (ProfileManager.CurrentProfile != null)
                    return ProfileManager.CurrentProfile.OptionsFont;

                return TrueTypeLoader.EMBEDDED_FONT;
            }
        }
    }


    protected interface SearchableOption
    {
        public bool Search(string text);

        public void OnSearchMatch();
    }

    protected static class PositionHelper
    {
        public static int X, Y = ThemeSettings.TOP_PADDING, LAST_Y = ThemeSettings.TOP_PADDING;

        public static void BlankLine()
        {
            LAST_Y = Y;
            Y += ThemeSettings.BLANK_LINE;
        }

        public static void Indent() => X += ThemeSettings.INDENT_SPACE;

        public static void RemoveIndent() => X -= ThemeSettings.INDENT_SPACE;

        public static Control PositionControl(Control c)
        {
            c.X = X;
            c.Y = Y;

            LAST_Y = Y;
            Y += c.Height + ThemeSettings.TOP_PADDING;

            return c;
        }

        public static Control ToRightOf(Control c, Control other, int padding = 5)
        {
            c.Y = other.Y;
            c.X = other.X + other.Width + padding;

            return c;
        }

        public static void PositionExact(Control c, int x, int y)
        {
            c.X = x;
            c.Y = y;
        }

        public static void Reset()
        {
            X = 0;
            Y = ThemeSettings.TOP_PADDING;
            LAST_Y = Y;
        }
    }

    protected class SettingsOption
    {
        public SettingsOption(string optionLabel, Control control, int maxTotalWidth, int optionsPage, int x = 0, int y = 0)
        {
            OptionLabel = optionLabel;
            OptionControl = control;
            OptionsPage = optionsPage;

            FullControl = new Area(false)
            {
                AcceptMouseInput = true,
                CanMove = true,
                CanCloseWithRightClick = true
            };

            if (!string.IsNullOrEmpty(OptionLabel))
            {
                Control labelTextBox = TextBox.GetOne(OptionLabel, ThemeSettings.FONT, 20, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default(maxTotalWidth));
                FullControl.Add(labelTextBox, optionsPage);

                if (labelTextBox.Width > maxTotalWidth)
                {
                    labelTextBox.Width = maxTotalWidth;
                }

                if (OptionControl != null)
                {
                    if (labelTextBox.Width + OptionControl.Width + 5 > maxTotalWidth)
                    {
                        OptionControl.Y = labelTextBox.Height + 5;
                        OptionControl.X = 15;
                    }
                    else
                    {
                        OptionControl.X = labelTextBox.Width + 5;
                    }
                }

                FullControl.Width += labelTextBox.Width + 5;
                FullControl.Height = labelTextBox.Height;
            }

            if (OptionControl != null)
            {
                FullControl.Add(OptionControl, optionsPage);
                FullControl.Width += OptionControl.Width;
                FullControl.ActivePage = optionsPage;

                if (OptionControl.Height > FullControl.Height)
                {
                    FullControl.Height = OptionControl.Height;
                }
            }

            FullControl.X = x;
            FullControl.Y = y;

            int scrollAreaHeight = 100;

            var scrollArea = new ScrollArea(x, y, FullControl.Width, scrollAreaHeight, FullControl.Height)
            {
                AcceptMouseInput = true
            };

            scrollArea.Add(FullControl);

            FullControl.X = 0;
            FullControl.Y = 0;

            ScrollContainer = scrollArea;
        }

        public string OptionLabel { get; }
        public Control OptionControl { get; }
        public int OptionsPage { get; }
        public Area FullControl { get; }
        public ScrollArea ScrollContainer { get; }
    }

    public class LeftSideMenuRightSideContent : Control
    {
        private ScrollArea left, right;
        private int leftY, rightY = ThemeSettings.TOP_PADDING, leftX, rightX;

        public ScrollArea LeftArea => left;
        public ScrollArea RightArea => right;

        public new int ActivePage
        {
            get => base.ActivePage;
            set
            {
                base.ActivePage = value;
                right.ActivePage = value;
            }
        }

        public LeftSideMenuRightSideContent(int width, int height, int leftWidth, int page = 0)
        {
            Width = width;
            Height = height;
            CanMove = true;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;

            Add
            (
                new AlphaBlendControl()
                {
                    Width = leftWidth,
                    Height = Height,
                    CanMove = true
                }, page
            );

            Add
            (
                left = new ScrollArea(0, 0, leftWidth, height)
                {
                    CanMove = true,
                    AcceptMouseInput = true
                }, page
            );

            Add
            (
                right = new ScrollArea(leftWidth, 0, Width - leftWidth, height)
                {
                    CanMove = true,
                    AcceptMouseInput = true
                }, page
            );

            LeftWidth = leftWidth - ThemeSettings.SCROLL_BAR_WIDTH;
            RightWidth = Width - leftWidth;
        }

        public int LeftWidth { get; }
        public int RightWidth { get; }

        public void AddToLeft(Control c, bool autoPosition = true, int page = 0)
        {
            if (autoPosition)
            {
                c.Y = leftY;
                c.X = leftX;
                leftY += c.Height;
            }

            left.Add(c, page);
        }

        public void RepositionLeftMenuChildren()
        {
            leftY = 0;
            leftX = 0;

            foreach (Control c in left.Children)
            {
                if (c == null || c.IsDisposed || c is not ModernButton)
                    continue;

                c.Y = leftY;
                c.X = leftX;
                leftY += c.Height;
            }
        }

        public void AddToRight(Control c, bool autoPosition = true, int page = 0)
        {
            if (autoPosition)
            {
                c.Y = rightY;
                c.X = rightX;
                rightY += c.Height + ThemeSettings.TOP_PADDING;
            }

            right.Add(c, page);
        }

        public void BlankLine() => rightY += ThemeSettings.BLANK_LINE;

        public void Indent() => rightX += ThemeSettings.INDENT_SPACE;

        public void RemoveIndent()
        {
            rightX -= ThemeSettings.INDENT_SPACE;

            if (rightX < 0)
            {
                rightX = 0;
            }
        }

        /// <summary>
        /// Resets x and y positions of the right side content. Use this for each new page.
        /// </summary>
        public void ResetRightSide()
        {
            rightY = ThemeSettings.TOP_PADDING;
            rightX = 0;
        }

        public void SetMatchingButton(int page)
        {
            foreach (Control c in left.Children)
            {
                if (c is ModernButton button && button.ButtonParameter == page)
                {
                    ((SearchableOption)button).OnSearchMatch();
                    int p = Parent == null ? Page : Parent.Page;
                    SetParentsForMatchingSearch(this, p);
                }
            }
        }
    }

    protected class HotkeyBox : Control
    {
        private bool _actived;
        private readonly ModernButton _buttonOK, _buttonCancel;
        private readonly TextBox _label;

        public HotkeyBox()
        {
            CanMove = false;
            AcceptMouseInput = true;
            AcceptKeyboardInput = true;

            Width = 300;
            Height = 40;

            var bg = new AlphaBlendControl()
            {
                Width = 150,
                Height = 40,
                AcceptMouseInput = true
            };

            Add(bg);
            bg.MouseUp += LabelOnMouseUp;

            Add(_label = TextBox.GetOne("None", ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.DefaultCentered(150)));
            _label.Y = (bg.Height >> 1) - (_label.Height >> 1);

            _label.MouseUp += LabelOnMouseUp;

            Add
            (
                _buttonOK = new ModernButton(152, 0, 75, 40, ButtonAction.Activate, "Save", ThemeSettings.BUTTON_FONT_COLOR)
                {
                    ButtonParameter = (int)ButtonState.Ok
                }
            );

            Add
            (
                _buttonCancel = new ModernButton(_buttonOK.Bounds.Right + 5, 0, 75, 40, ButtonAction.Activate, "Cancel", ThemeSettings.BUTTON_FONT_COLOR)
                {
                    ButtonParameter = (int)ButtonState.Cancel
                }
            );

            WantUpdateSize = false;
            IsActive = false;
        }

        public SDL.SDL_Keycode Key { get; private set; }
        public SDL.SDL_GamepadButton[] Buttons { get; private set; }
        public MouseButtonType MouseButton { get; private set; }
        public bool WheelScroll { get; private set; }
        public bool WheelUp { get; private set; }
        public SDL.SDL_Keymod Mod { get; private set; }

        public bool IsActive
        {
            get => _actived;
            set
            {
                _actived = value;

                if (value)
                {
                    _buttonOK.IsVisible = _buttonCancel.IsVisible = true;
                    _buttonOK.IsEnabled = _buttonCancel.IsEnabled = true;
                }
                else
                {
                    _buttonOK.IsVisible = _buttonCancel.IsVisible = false;
                    _buttonOK.IsEnabled = _buttonCancel.IsEnabled = false;
                }
            }
        }

        public event EventHandler HotkeyChanged, HotkeyCancelled;

        protected override void OnControllerButtonDown(SDL.SDL_GamepadButton button)
        {
            if (IsActive)
            {
                SetButtons(Controller.PressedButtons());
            }
        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            if (IsActive)
            {
                SetKey(key, mod);
            }
        }

        public void SetButtons(SDL.SDL_GamepadButton[] buttons)
        {
            ResetBinding();
            Buttons = buttons;
            _label.Text = Controller.GetButtonNames(buttons);
        }

        public void SetKey(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            if (key == SDL.SDL_Keycode.SDLK_UNKNOWN && mod == SDL.SDL_Keymod.SDL_KMOD_NONE)
            {
                ResetBinding();

                Key = key;
                Mod = mod;
            }
            else
            {
                string newvalue = KeysTranslator.TryGetKey(key, mod);

                if (!string.IsNullOrEmpty(newvalue) && key != SDL.SDL_Keycode.SDLK_UNKNOWN)
                {
                    ResetBinding();

                    Key = key;
                    Mod = mod;
                    _label.Text = newvalue;
                }
            }
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Middle || button == MouseButtonType.XButton1 || button == MouseButtonType.XButton2)
            {
                SDL.SDL_Keymod mod = SDL.SDL_Keymod.SDL_KMOD_NONE;

                if (Keyboard.Alt)
                {
                    mod |= SDL.SDL_Keymod.SDL_KMOD_ALT;
                }

                if (Keyboard.Shift)
                {
                    mod |= SDL.SDL_Keymod.SDL_KMOD_SHIFT;
                }

                if (Keyboard.Ctrl)
                {
                    mod |= SDL.SDL_Keymod.SDL_KMOD_CTRL;
                }

                SetMouseButton(button, mod);
            }
        }

        public void SetMouseButton(MouseButtonType button, SDL.SDL_Keymod mod)
        {
            string newvalue = KeysTranslator.GetMouseButton(button, mod);

            if (!string.IsNullOrEmpty(newvalue) && button != MouseButtonType.None)
            {
                ResetBinding();

                MouseButton = button;
                Mod = mod;
                _label.Text = newvalue;
            }
        }

        protected override void OnMouseWheel(MouseEventType delta)
        {
            SDL.SDL_Keymod mod = SDL.SDL_Keymod.SDL_KMOD_NONE;

            if (Keyboard.Alt)
            {
                mod |= SDL.SDL_Keymod.SDL_KMOD_ALT;
            }

            if (Keyboard.Shift)
            {
                mod |= SDL.SDL_Keymod.SDL_KMOD_SHIFT;
            }

            if (Keyboard.Ctrl)
            {
                mod |= SDL.SDL_Keymod.SDL_KMOD_CTRL;
            }

            if (delta == MouseEventType.WheelScrollUp)
            {
                SetMouseWheel(true, mod);
            }
            else if (delta == MouseEventType.WheelScrollDown)
            {
                SetMouseWheel(false, mod);
            }
        }

        public void SetMouseWheel(bool wheelUp, SDL.SDL_Keymod mod)
        {
            string newvalue = KeysTranslator.GetMouseWheel(wheelUp, mod);

            if (!string.IsNullOrEmpty(newvalue))
            {
                ResetBinding();

                WheelScroll = true;
                WheelUp = wheelUp;
                Mod = mod;
                _label.Text = newvalue;
            }
        }

        private void ResetBinding()
        {
            Key = 0;
            MouseButton = MouseButtonType.None;
            WheelScroll = false;
            Mod = 0;
            _label.Text = "None";
            Buttons = null;
        }

        private void LabelOnMouseUp(object sender, MouseEventArgs e)
        {
            IsActive = true;
            SetKeyboardFocus();
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((ButtonState)buttonID)
            {
                case ButtonState.Ok: HotkeyChanged.Raise(this); break;

                case ButtonState.Cancel:
                    _label.Text = "None";

                    HotkeyCancelled.Raise(this);

                    Key = SDL.SDL_Keycode.SDLK_UNKNOWN;
                    Mod = SDL.SDL_Keymod.SDL_KMOD_NONE;

                    break;
            }

            IsActive = false;
        }

        private enum ButtonState
        {
            Ok,
            Cancel
        }
    }

    public class InputField : Control
    {
        private readonly StbTextBox _textbox;

        private AlphaBlendControl _background;

        public StbTextBox Stb => _textbox;

        public event EventHandler TextChanged
        {
            add { _textbox.TextChanged += value; }
            remove { _textbox.TextChanged -= value; }
        }

        public event EventHandler EnterPressed;

        public InputField(int width, int height, int maxWidthText = 0, int maxCharsCount = -1, string text = "", bool numbersOnly = false, EventHandler onTextChanges = null)
        {
            WantUpdateSize = false;

            Width = width;
            Height = height;

            _textbox = new StbTextBox(maxCharsCount, maxWidthText)
            {
                X = 4,
                Width = width - 8,
            };

            _textbox.Y = (height >> 1) - (_textbox.Height >> 1);
            _textbox.Text = text;
            _textbox.NumbersOnly = numbersOnly;

            Add
            (
                _background = new AlphaBlendControl()
                {
                    Width = Width,
                    Height = Height
                }
            );

            Add(_textbox);

            if (onTextChanges != null)
            {
                TextChanged += onTextChanges;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (batcher.ClipBegin(x, y, Width, Height))
            {
                base.Draw(batcher, x, y);

                batcher.ClipEnd();
            }

            return true;
        }


        public void UpdateBackground()
        {
            _background.Width = Width;
            _background.Height = Height;
        }

        public string Text => _textbox.Text;

        public override bool AcceptKeyboardInput
        {
            get => _textbox.AcceptKeyboardInput;
            set => _textbox.AcceptKeyboardInput = value;
        }

        public bool NumbersOnly
        {
            get => _textbox.NumbersOnly;
            set => _textbox.NumbersOnly = value;
        }


        public void SetText(string text) => _textbox.SetText(text);

        public void SetTooltip(string text)
        {
            base.SetTooltip(text);
            _textbox.SetTooltip(text);
        }

        public override void OnKeyboardReturn(int textID, string text)
        {
            base.OnKeyboardReturn(textID, text);
            EnterPressed?.Invoke(this, EventArgs.Empty);
        }


        public class StbTextBox : Control, ITextEditHandler
        {
            protected static readonly Color SELECTION_COLOR = new Color()
            {
                PackedValue = 0x80a06020
            };
            private const int FONT_SIZE = 20;
            private readonly int _maxCharCount = -1;


            public StbTextBox(int max_char_count = -1, int maxWidth = 0)
            {
                AcceptKeyboardInput = true;
                AcceptMouseInput = true;
                CanMove = false;
                IsEditable = true;

                _maxCharCount = max_char_count;

                Stb = new TextEdit(this);
                Stb.SingleLine = true;

                _rendererText = TextBox.GetOne
                (
                    string.Empty, ThemeSettings.FONT, FONT_SIZE, ThemeSettings.TEXT_FONT_COLOR,
                    TextBox.RTLOptions.Default(maxWidth > 0 ? maxWidth : null).MouseInput().EnableGlyphCalculation().IgnoreColors().DisableCommands()
                );

                _rendererCaret = TextBox.GetOne
                    ("_", ThemeSettings.FONT, FONT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default().MouseInput().EnableGlyphCalculation());

                Height = _rendererCaret.Height;
                LoseFocusOnEscapeKey = true;
            }

            protected TextEdit Stb { get; }

            public override bool AcceptKeyboardInput => base.AcceptKeyboardInput && IsEditable;

            public bool AllowTAB { get; set; }
            public bool NoSelection { get; set; }

            public bool LoseFocusOnEscapeKey { get; set; }

            public int CaretIndex
            {
                get => Stb.CursorIndex;
                set
                {
                    Stb.CursorIndex = value;
                    UpdateCaretScreenPosition();
                }
            }

            public bool Multiline
            {
                get => !Stb.SingleLine;
                set => Stb.SingleLine = !value;
            }

            public bool NumbersOnly { get; set; }

            public int SelectionStart
            {
                get => Stb.SelectStart;
                set
                {
                    if (AllowSelection)
                    {
                        Stb.SelectStart = value;
                    }
                }
            }

            public int SelectionEnd
            {
                get => Stb.SelectEnd;
                set
                {
                    if (AllowSelection)
                    {
                        Stb.SelectEnd = value;
                    }
                }
            }

            public bool AllowSelection { get; set; } = true;

            internal int TotalHeight => _rendererText.Height;

            public string Text
            {
                get => _rendererText.Text;

                set
                {
                    if (_maxCharCount > 0)
                    {
                        if (value != null && value.Length > _maxCharCount)
                        {
                            value = value.Substring(0, _maxCharCount);
                        }
                    }

                    _rendererText.Text = value;

                    if (!_is_writing)
                    {
                        OnTextChanged();
                    }
                }
            }

            public int Length => Text?.Length ?? 0;

            public float GetWidth(int index)
            {
                if (Text != null)
                {
                    if (index < _rendererText.Text.Length)
                    {
                        FontStashSharp.RichText.TextChunkGlyph? glyphRender = _rendererText.RTL.GetGlyphInfoByIndex(index);

                        if (glyphRender != null)
                        {
                            return glyphRender.Value.Bounds.Width;
                        }
                    }
                }

                return 0;
            }

            public TextEditRow LayoutRow(int startIndex)
            {
                var r = new TextEditRow()
                {
                    num_chars = _rendererText.Text.Length
                };

                int sx = ScreenCoordinateX;
                int sy = ScreenCoordinateY;

                r.x0 += sx;
                r.x1 += sx;
                r.ymin += sy;
                r.ymax += sy;

                return r;
            }

            protected Point _caretScreenPosition;
            protected bool _is_writing;
            protected bool _leftWasDown, _fromServer;
            protected TextBox _rendererText, _rendererCaret;

            public event EventHandler TextChanged;

            public void SelectAll()
            {
                if (AllowSelection)
                {
                    Stb.SelectStart = 0;
                    Stb.SelectEnd = Length;
                }
            }

            protected void UpdateCaretScreenPosition() => _caretScreenPosition = GetCoordsForIndex(Stb.CursorIndex);

            protected Point GetCoordsForIndex(int index)
            {
                int x = 0, y = 0;

                if (Text != null)
                {
                    if (index < Text.Length)
                    {
                        FontStashSharp.RichText.TextChunkGlyph? glyphRender = _rendererText.RTL.GetGlyphInfoByIndex(index);

                        if (glyphRender != null)
                        {
                            x += glyphRender.Value.Bounds.Left;
                            y += glyphRender.Value.LineTop;
                        }
                    }
                    else if (_rendererText.RTL.Lines != null && _rendererText.RTL.Lines.Count > 0)
                    {
                        // After last glyph
                        FontStashSharp.RichText.TextLine lastLine = _rendererText.RTL.Lines[_rendererText.RTL.Lines.Count - 1];

                        if (lastLine.Count > 0)
                        {
                            FontStashSharp.RichText.TextChunkGlyph? glyphRender = lastLine.GetGlyphInfoByIndex(lastLine.Count - 1);

                            x += glyphRender.Value.Bounds.Right;
                            y += glyphRender.Value.LineTop;
                        }
                        else if (_rendererText.RTL.Lines.Count > 1)
                        {
                            FontStashSharp.RichText.TextLine previousLine = _rendererText.RTL.Lines[_rendererText.RTL.Lines.Count - 2];

                            if (previousLine.Count > 0)
                            {
                                FontStashSharp.RichText.TextChunkGlyph? glyphRender = previousLine.GetGlyphInfoByIndex(0);
                                y += glyphRender.Value.LineTop + lastLine.Size.Y + _rendererText.RTL.VerticalSpacing;
                            }
                        }
                    }
                }

                return new Point(x, y);
            }

            protected int GetIndexFromCoords(Point coords)
            {
                if (Text != null)
                {
                    FontStashSharp.RichText.TextLine line = _rendererText.RTL.GetLineByY(coords.Y);

                    if (line != null)
                    {
                        int? index = line.GetGlyphIndexByX(coords.X);

                        if (index != null)
                        {
                            return (int)index;
                        }
                    }
                }

                return 0;
            }

            protected Point GetCoordsForClick(Point clicked)
            {
                if (Text != null)
                {
                    FontStashSharp.RichText.TextLine line = _rendererText.RTL.GetLineByY(clicked.Y);

                    if (line != null)
                    {
                        int? index = line.GetGlyphIndexByX(clicked.X);

                        if (index != null)
                        {
                            return GetCoordsForIndex((int)index);
                        }
                    }
                }

                return Point.Zero;
            }

            private ControlKeys ApplyShiftIfNecessary(ControlKeys k)
            {
                if (Keyboard.Shift && !NoSelection)
                {
                    k |= ControlKeys.Shift;
                }

                return k;
            }

            private bool IsMaxCharReached(int count) => _maxCharCount >= 0 && Length + count >= _maxCharCount;

            protected virtual void OnTextChanged()
            {
                TextChanged?.Raise(this);

                UpdateCaretScreenPosition();
            }

            internal override void OnFocusEnter()
            {
                base.OnFocusEnter();
                CaretIndex = Text?.Length ?? 0;
            }

            internal override void OnFocusLost()
            {
                if (Stb != null)
                    Stb.SelectStart = Stb.SelectEnd = 0;

                base.OnFocusLost();
            }

            protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
            {
                ControlKeys? stb_key = null;
                bool update_caret = false;

                switch (key)
                {
                    case SDL.SDL_Keycode.SDLK_TAB:
                        if (AllowTAB)
                        {
                            // UO does not support '\t' char in its fonts
                            OnTextInput("   ");
                        }
                        else
                        {
                            Parent?.KeyboardTabToNextFocus(this);
                        }

                        break;

                    case SDL.SDL_Keycode.SDLK_A when Keyboard.Ctrl && !NoSelection: SelectAll(); break;

                    case SDL.SDL_Keycode.SDLK_ESCAPE:
                        if (LoseFocusOnEscapeKey && SelectionStart == SelectionEnd)
                        {
                            UIManager.KeyboardFocusControl = null;
                        }

                        SelectionStart = 0;
                        SelectionEnd = 0;

                        break;

                    case SDL.SDL_Keycode.SDLK_INSERT when IsEditable: stb_key = ControlKeys.InsertMode; break;

                    case SDL.SDL_Keycode.SDLK_C when Keyboard.Ctrl && !NoSelection:
                        int selectStart = Math.Min(Stb.SelectStart, Stb.SelectEnd);
                        int selectEnd = Math.Max(Stb.SelectStart, Stb.SelectEnd);

                        if (selectStart < selectEnd && selectStart >= 0 && selectEnd - selectStart <= Text.Length)
                        {
                            SDL.SDL_SetClipboardText(Text.Substring(selectStart, selectEnd - selectStart));
                        }

                        break;

                    case SDL.SDL_Keycode.SDLK_X when Keyboard.Ctrl && !NoSelection:
                        selectStart = Math.Min(Stb.SelectStart, Stb.SelectEnd);
                        selectEnd = Math.Max(Stb.SelectStart, Stb.SelectEnd);

                        if (selectStart < selectEnd && selectStart >= 0 && selectEnd - selectStart <= Text.Length)
                        {
                            SDL.SDL_SetClipboardText(Text.Substring(selectStart, selectEnd - selectStart));

                            if (IsEditable)
                            {
                                Stb.Cut();
                            }
                        }

                        break;

                    case SDL.SDL_Keycode.SDLK_V when Keyboard.Ctrl && IsEditable: OnTextInput(StringHelper.GetClipboardText(Multiline)); break;

                    case SDL.SDL_Keycode.SDLK_Z when Keyboard.Ctrl && IsEditable: stb_key = ControlKeys.Undo; break;

                    case SDL.SDL_Keycode.SDLK_Y when Keyboard.Ctrl && IsEditable: stb_key = ControlKeys.Redo; break;

                    case SDL.SDL_Keycode.SDLK_LEFT:
                        if (Keyboard.Ctrl && Keyboard.Shift)
                        {
                            if (!NoSelection)
                            {
                                stb_key = ControlKeys.Shift | ControlKeys.WordLeft;
                            }
                        }
                        else if (Keyboard.Shift)
                        {
                            if (!NoSelection)
                            {
                                stb_key = ControlKeys.Shift | ControlKeys.Left;
                            }
                        }
                        else if (Keyboard.Ctrl)
                        {
                            stb_key = ControlKeys.WordLeft;
                        }
                        else
                        {
                            stb_key = ControlKeys.Left;
                        }

                        update_caret = true;

                        break;

                    case SDL.SDL_Keycode.SDLK_RIGHT:
                        if (Keyboard.Ctrl && Keyboard.Shift)
                        {
                            if (!NoSelection)
                            {
                                stb_key = ControlKeys.Shift | ControlKeys.WordRight;
                            }
                        }
                        else if (Keyboard.Shift)
                        {
                            if (!NoSelection)
                            {
                                stb_key = ControlKeys.Shift | ControlKeys.Right;
                            }
                        }
                        else if (Keyboard.Ctrl)
                        {
                            stb_key = ControlKeys.WordRight;
                        }
                        else
                        {
                            stb_key = ControlKeys.Right;
                        }

                        update_caret = true;

                        break;

                    case SDL.SDL_Keycode.SDLK_UP:
                        stb_key = ApplyShiftIfNecessary(ControlKeys.Up);
                        update_caret = true;

                        break;

                    case SDL.SDL_Keycode.SDLK_DOWN:
                        stb_key = ApplyShiftIfNecessary(ControlKeys.Down);
                        update_caret = true;

                        break;

                    case SDL.SDL_Keycode.SDLK_BACKSPACE when IsEditable:
                        stb_key = ApplyShiftIfNecessary(ControlKeys.BackSpace);
                        update_caret = true;

                        break;

                    case SDL.SDL_Keycode.SDLK_DELETE when IsEditable:
                        stb_key = ApplyShiftIfNecessary(ControlKeys.Delete);
                        update_caret = true;

                        break;

                    case SDL.SDL_Keycode.SDLK_HOME:
                        if (Keyboard.Ctrl && Keyboard.Shift)
                        {
                            if (!NoSelection)
                            {
                                stb_key = ControlKeys.Shift | ControlKeys.TextStart;
                            }
                        }
                        else if (Keyboard.Shift)
                        {
                            if (!NoSelection)
                            {
                                stb_key = ControlKeys.Shift | ControlKeys.LineStart;
                            }
                        }
                        else if (Keyboard.Ctrl)
                        {
                            stb_key = ControlKeys.TextStart;
                        }
                        else
                        {
                            stb_key = ControlKeys.LineStart;
                        }

                        update_caret = true;

                        break;

                    case SDL.SDL_Keycode.SDLK_END:
                        if (Keyboard.Ctrl && Keyboard.Shift)
                        {
                            if (!NoSelection)
                            {
                                stb_key = ControlKeys.Shift | ControlKeys.TextEnd;
                            }
                        }
                        else if (Keyboard.Shift)
                        {
                            if (!NoSelection)
                            {
                                stb_key = ControlKeys.Shift | ControlKeys.LineEnd;
                            }
                        }
                        else if (Keyboard.Ctrl)
                        {
                            stb_key = ControlKeys.TextEnd;
                        }
                        else
                        {
                            stb_key = ControlKeys.LineEnd;
                        }

                        update_caret = true;

                        break;

                    case SDL.SDL_Keycode.SDLK_KP_ENTER:
                    case SDL.SDL_Keycode.SDLK_RETURN:
                        if (IsEditable)
                        {
                            if (Multiline)
                            {
                                if (!_fromServer && !IsMaxCharReached(0))
                                {
                                    OnTextInput("\n");
                                }
                            }
                            else
                            {
                                Parent?.OnKeyboardReturn(0, Text);

                                if (UIManager.SystemChat != null && UIManager.SystemChat.TextBoxControl != null && IsFocused)
                                {
                                    if (!IsFromServer || !UIManager.SystemChat.TextBoxControl.IsVisible)
                                    {
                                        OnFocusLost();
                                        OnFocusEnter();
                                    }
                                    else if (UIManager.KeyboardFocusControl == null || UIManager.KeyboardFocusControl != UIManager.SystemChat.TextBoxControl)
                                    {
                                        UIManager.SystemChat.TextBoxControl.SetKeyboardFocus();
                                    }
                                }
                            }
                        }

                        break;
                }

                if (stb_key != null)
                {
                    Stb.Key(stb_key.Value);
                }

                if (update_caret)
                {
                    UpdateCaretScreenPosition();
                }

                base.OnKeyDown(key, mod);
            }

            public void SetText(string text)
            {
                if (string.IsNullOrEmpty(text))
                {
                    ClearText();
                }
                else
                {
                    if (_maxCharCount > 0)
                    {
                        if (text.Length > _maxCharCount)
                        {
                            text = text.Substring(0, _maxCharCount);
                        }
                    }

                    Stb.ClearState(!Multiline);
                    Text = text;

                    Stb.CursorIndex = Length;

                    if (!_is_writing)
                    {
                        OnTextChanged();
                    }
                }
            }

            public void ClearText()
            {
                if (Length != 0)
                {
                    SelectionStart = 0;
                    SelectionEnd = 0;
                    Stb.Delete(0, Length);

                    if (!_is_writing)
                    {
                        OnTextChanged();
                    }
                }
            }

            public void AppendText(string text) => Stb.Paste(text);

            protected override void OnTextInput(string c)
            {
                if (c == null || !IsEditable)
                {
                    return;
                }

                _is_writing = true;

                if (SelectionStart != SelectionEnd)
                {
                    Stb.DeleteSelection();
                }

                int count;

                if (_maxCharCount > 0)
                {
                    int remains = _maxCharCount - Length;

                    if (remains <= 0)
                    {
                        _is_writing = false;

                        return;
                    }

                    count = Math.Min(remains, c.Length);

                    if (remains < c.Length && count > 0)
                    {
                        c = c.Substring(0, count);
                    }
                }
                else
                {
                    count = c.Length;
                }

                if (count > 0)
                {
                    if (NumbersOnly)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            if (!char.IsNumber(c[i]))
                            {
                                _is_writing = false;

                                return;
                            }
                        }

                        if (_maxCharCount > 0 && int.TryParse(Stb.text + c, out int val))
                        {
                            if (val > _maxCharCount)
                            {
                                _is_writing = false;
                                SetText(_maxCharCount.ToString());

                                return;
                            }
                        }
                    }


                    if (count > 1)
                    {
                        Stb.Paste(c);
                        OnTextChanged();
                    }
                    else
                    {
                        Stb.InputChar(c[0]);
                        OnTextChanged();
                    }
                }

                _is_writing = false;
            }

            private int GetXOffset()
            {
                if (_caretScreenPosition.X > Width)
                {
                    return _caretScreenPosition.X - Width + 5;
                }

                return 0;
            }

            public void Click(Point pos)
            {
                pos = new Point((pos.X - ScreenCoordinateX), pos.Y - ScreenCoordinateY);
                CaretIndex = GetIndexFromCoords(pos);
                SelectionStart = 0;
                SelectionEnd = 0;
                Stb.HasPreferredX = false;
            }

            public void Drag(Point pos)
            {
                pos = new Point((pos.X - ScreenCoordinateX), pos.Y - ScreenCoordinateY);

                if (SelectionStart == SelectionEnd)
                {
                    SelectionStart = CaretIndex;
                }

                CaretIndex = SelectionEnd = GetIndexFromCoords(pos);
            }

            private protected void DrawSelection(UltimaBatcher2D batcher, int x, int y)
            {
                if (!AllowSelection)
                {
                    return;
                }

                int selectStart = Math.Min(SelectionStart, SelectionEnd);
                int selectEnd = Math.Max(SelectionStart, SelectionEnd);

                if (selectStart < selectEnd)
                {
                    //Show selection
                    Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, 0.5f);

                    Point start = GetCoordsForIndex(selectStart);
                    Point size = GetCoordsForIndex(selectEnd);
                    size = new Point(size.X - start.X, _rendererText.Height);

                    batcher.Draw(SolidColorTextureCache.GetTexture(SELECTION_COLOR), new Rectangle(x + start.X, y + start.Y, size.X, size.Y), hueVector);
                }
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                int slideX = x - GetXOffset();

                if (batcher.ClipBegin(x, y, Width, Height))
                {
                    base.Draw(batcher, x, y);
                    DrawSelection(batcher, slideX, y);
                    _rendererText.Draw(batcher, slideX, y);
                    DrawCaret(batcher, slideX, y);
                    batcher.ClipEnd();
                }

                return true;
            }

            protected virtual void DrawCaret(UltimaBatcher2D batcher, int x, int y)
            {
                if (HasKeyboardFocus)
                {
                    _rendererCaret.Draw(batcher, x + _caretScreenPosition.X, y + _caretScreenPosition.Y);
                }
            }

            protected override void OnMouseDown(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left && IsEditable)
                {
                    if (!NoSelection)
                    {
                        _leftWasDown = true;
                    }

                    Click(new Point(x + ScreenCoordinateX + GetXOffset(), y + ScreenCoordinateY));
                }

                base.OnMouseDown(x, y, button);
            }

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left)
                {
                    _leftWasDown = false;
                }

                base.OnMouseUp(x, y, button);
            }

            protected override void OnMouseOver(int x, int y)
            {
                base.OnMouseOver(x, y);

                if (!_leftWasDown)
                {
                    return;
                }

                Drag(new Point(x + ScreenCoordinateX + GetXOffset(), y + ScreenCoordinateY));
            }

            public override void Dispose()
            {
                _rendererText?.Dispose();
                _rendererCaret?.Dispose();

                base.Dispose();
            }

            protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
            {
                if (!NoSelection && CaretIndex < Text.Length && CaretIndex >= 0 && !char.IsWhiteSpace(Text[CaretIndex]))
                {
                    int idx = CaretIndex;

                    if (idx - 1 >= 0 && char.IsWhiteSpace(Text[idx - 1]))
                    {
                        ++idx;
                    }

                    SelectionStart = Stb.MoveToPreviousWord(idx);
                    SelectionEnd = Stb.MoveToNextWord(idx);

                    if (SelectionEnd < Text.Length)
                    {
                        --SelectionEnd;
                    }

                    return true;
                }

                return base.OnMouseDoubleClick(x, y, button);
            }
        }
    }

    public class ModernButton : HitBox, SearchableOption
    {
        private readonly ButtonAction _action;
        private readonly int _groupnumber;
        private bool _isSelected;

        public bool DisplayBorder;

        public bool FullPageSwitch;

        public ModernButton
        (
            int x, int y, int w, int h, ButtonAction action, string text, Color fontColor, int groupnumber = 0,
            FontStashSharp.RichText.TextHorizontalAlignment align = FontStashSharp.RichText.TextHorizontalAlignment.Center
        ) : base(x, y, w, h)
        {
            _action = action;

            Add(TextLabel = TextBox.GetOne(text, ThemeSettings.FONT, 20, fontColor, TextBox.RTLOptions.Default(w).Alignment(align)));

            TextLabel.Y = (h - TextLabel.Height) >> 1;
            _groupnumber = groupnumber;

            SearchValueChanged += ModernOptionsGump_SearchValueChanged;
        }

        public override void Dispose()
        {
            base.Dispose();
            SearchValueChanged -= ModernOptionsGump_SearchValueChanged;
        }

        private void ModernOptionsGump_SearchValueChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(SearchText))
            {
                if (Search(SearchText))
                {
                    OnSearchMatch();
                    SetParentsForMatchingSearch(this, Page);
                }
                else
                {
                    TextLabel.Alpha = ThemeSettings.NO_MATCH_SEARCH;
                }
            }
            else
            {
                TextLabel.Alpha = 1f;
            }
        }

        internal TextBox TextLabel { get; }

        public int ButtonParameter { get; set; }

        public bool IsSelectable { get; set; } = true;

        public bool IsSelected
        {
            get => _isSelected && IsSelectable;
            set
            {
                if (!IsSelectable)
                {
                    return;
                }

                _isSelected = value;

                if (value)
                {
                    Control p = Parent;

                    if (p == null)
                    {
                        return;
                    }

                    IEnumerable<ModernButton> list = p.FindControls<ModernButton>();

                    foreach (ModernButton b in list)
                    {
                        if (b != this && b._groupnumber == _groupnumber)
                        {
                            b.IsSelected = false;
                        }
                    }
                }
            }
        }

        internal static ModernButton GetSelected(Control p, int group)
        {
            IEnumerable<ModernButton> list = p.FindControls<ModernButton>();

            foreach (ModernButton b in list)
            {
                if (b._groupnumber == group && b.IsSelected)
                {
                    return b;
                }
            }

            return null;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                IsSelected = true;

                if (_action == ButtonAction.SwitchPage)
                {
                    if (!FullPageSwitch)
                    {
                        if (Parent != null)
                        {
                            //Scroll area
                            Parent.ActivePage = ButtonParameter;

                            if (Parent.Parent != null && Parent.Parent is LeftSideMenuRightSideContent)
                            {
                                //LeftSideMenuRightSideContent
                                ((LeftSideMenuRightSideContent)Parent.Parent).ActivePage = ButtonParameter;
                            }
                        }
                    }
                    else
                    {
                        ChangePage(ButtonParameter);
                    }
                }
                else
                {
                    OnButtonClick(ButtonParameter);
                }
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsSelected)
            {
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, Alpha);

                batcher.Draw(_texture, new Vector2(x, y), new Rectangle(0, 0, Width, Height), hueVector);
            }

            if (DisplayBorder)
            {
                batcher.DrawRectangle(SolidColorTextureCache.GetTexture(Color.LightGray), x, y, Width, Height, ShaderHueTranslator.GetHueVector(0, false, Alpha));
            }

            return base.Draw(batcher, x, y);
        }

        public bool Search(string text) => TextLabel.Text.ToLower().Contains(text.ToLower());

        public void OnSearchMatch() => TextLabel.Alpha = 1f;
    }

    public class ScrollArea : Control
    {
        private ScrollBar _scrollBar;

        public ScrollBar GetScrollBar
        {
            get { return _scrollBar; }
            set { _scrollBar = value; }
        }

        public override bool AcceptMouseInput { get; set; } = true;

        public ScrollArea(int x, int y, int w, int h, int scrollMaxHeight = -1)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;

            _scrollBar = new ScrollBar(Width - ThemeSettings.SCROLL_BAR_WIDTH, 0, Height);

            ScrollMaxHeight = scrollMaxHeight;

            _scrollBar.MinValue = 0;
            _scrollBar.MaxValue = scrollMaxHeight >= 0 ? scrollMaxHeight : Height;
            _scrollBar.Parent = this;
            _scrollBar.IsVisible = true;

            WantUpdateSize = false;
            CanMove = true;
        }

        public int ScrollMaxHeight { get; set; } = -1;
        public int ScrollValue => _scrollBar.Value;
        public int ScrollMinValue => _scrollBar.MinValue;
        public int ScrollMaxValue => _scrollBar.MaxValue;

        public void ToggleScrollBarVisibility(bool visible = true)
        {
            if (_scrollBar != null)
            {
                _scrollBar.IsVisible = visible;
            }
        }

        public override void Update()
        {
            base.Update();

            CalculateScrollBarMaxValue();
        }

        public void Scroll(bool isup)
        {
            if (isup)
            {
                _scrollBar.Value -= _scrollBar.ScrollStep;
            }
            else
            {
                _scrollBar.Value += _scrollBar.ScrollStep;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            int sbar = 0, start = 0;

            if (_scrollBar != null)
            {
                _scrollBar.Draw(batcher, x + _scrollBar.X, y + _scrollBar.Y);
                sbar = _scrollBar.Width;
                start = 1;
            }

            if (batcher.ClipBegin(x, y, Width - sbar, Height))
            {
                for (int i = start; i < Children.Count; i++)
                {
                    Control child = Children[i];

                    if (!child.IsVisible || (child.Page != ActivePage && child.Page != 0))
                    {
                        continue;
                    }

                    int finalY = y + child.Y - (_scrollBar == null ? 0 : _scrollBar.Value);

                    child.Draw(batcher, x + child.X, finalY);
                }

                batcher.ClipEnd();
            }

            return true;
        }

        protected override void OnMouseEnter(int x, int y)
        {
            base.OnMouseEnter(x, y);

            if (UIManager.KeyboardFocusControl == UIManager.SystemChat.TextBoxControl || UIManager.KeyboardFocusControl == null)
            {
                UIManager.KeyboardFocusControl = this; //Dirty fix for mouse wheel macros
            }
        }

        protected override void OnMouseWheel(MouseEventType delta)
        {
            if (IsDisposed || _scrollBar == null)
            {
                return;
            }

            switch (delta)
            {
                case MouseEventType.WheelScrollUp: _scrollBar.Value -= _scrollBar.ScrollStep; break;

                case MouseEventType.WheelScrollDown: _scrollBar.Value += _scrollBar.ScrollStep; break;
            }
        }

        public override void Clear()
        {
            for (int i = 1; i < Children.Count; i++)
            {
                Children[i].Dispose();
            }
        }

        private void CalculateScrollBarMaxValue()
        {
            if (_scrollBar == null)
            {
                return;
            }

            _scrollBar.Height = ScrollMaxHeight >= 0 ? ScrollMaxHeight : Height;
            bool maxValue = _scrollBar.Value == _scrollBar.MaxValue && _scrollBar.MaxValue != 0;

            int startY = 0, endY = 0;

            for (int i = 1; i < Children.Count; i++)
            {
                Control c = Children[i];

                if (c.IsVisible && !c.IsDisposed && (c.Page == 0 || c.Page == ActivePage))
                {
                    if (c.Y < startY)
                    {
                        startY = c.Y;
                    }

                    if (c.Bounds.Bottom > endY)
                    {
                        endY = c.Bounds.Bottom;
                    }
                }
            }

            int height = Math.Abs(startY) + Math.Abs(endY) - _scrollBar.Height;
            height = Math.Max(0, height);

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
                _scrollBar.Value = _scrollBar.MaxValue = 0;
            }

            _scrollBar.UpdateOffset(0, Offset.Y);

            for (int i = 1; i < Children.Count; i++)
            {
                Children[i].UpdateOffset(0, -_scrollBar.Value);
            }
        }

        public class ScrollBar : ScrollBarBase
        {
            private Rectangle _rectSlider, _emptySpace;

            private Vector3 _hueVector = ShaderHueTranslator.GetHueVector(ThemeSettings.BACKGROUND, false, 0.75f);
            private Vector3 _hueVectorForeground = ShaderHueTranslator.GetHueVector(ThemeSettings.BLACK, false, 0.75f);
            private Texture2D _whiteTexture = SolidColorTextureCache.GetTexture(Color.White);

            public override bool AcceptMouseInput { get; set; } = true;

            public ScrollBar(int x, int y, int height)
            {
                Height = height;
                Location = new Point(x, y);

                Width = ThemeSettings.SCROLL_BAR_WIDTH;

                _rectSlider = new Rectangle(0, _sliderPosition, Width, 20);

                _emptySpace.X = 0;
                _emptySpace.Y = 0;
                _emptySpace.Width = Width;
                _emptySpace.Height = Height;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                if (Height <= 0 || !IsVisible || IsDisposed)
                {
                    return false;
                }

                // draw scrollbar background
                batcher.Draw(_whiteTexture, new Rectangle(x, y, Width, Height), _hueVector);


                // draw slider
                if (MaxValue > MinValue)
                {
                    batcher.Draw(_whiteTexture, new Rectangle(x, y + _sliderPosition, Width, 20), _hueVectorForeground);
                }

                return true; // base.Draw(batcher, x, y);
            }

            protected override int GetScrollableArea() => Height - _rectSlider.Height;

            protected override void OnMouseDown(int x, int y, MouseButtonType button)
            {
                base.OnMouseDown(x, y, button);

                if (_btnSliderClicked && _emptySpace.Contains(x, y))
                {
                    CalculateByPosition(x, y);
                }
            }

            protected override void CalculateByPosition(int x, int y)
            {
                if (y != _clickPosition.Y)
                {
                    y -= _emptySpace.Y + (_rectSlider.Height >> 1);

                    if (y < 0)
                    {
                        y = 0;
                    }

                    int scrollableArea = GetScrollableArea();

                    if (y > scrollableArea)
                    {
                        y = scrollableArea;
                    }

                    _sliderPosition = y;
                    _clickPosition.X = x;
                    _clickPosition.Y = y;

                    if (y == 0 && _clickPosition.Y < (_rectSlider.Height >> 1))
                    {
                        _clickPosition.Y = _rectSlider.Height >> 1;
                    }
                    else if (y == scrollableArea && _clickPosition.Y > Height - (_rectSlider.Height >> 1))
                    {
                        _clickPosition.Y = Height - (_rectSlider.Height >> 1);
                    }

                    _value = (int)Math.Round(y / (float)scrollableArea * (MaxValue - MinValue) + MinValue);
                }
            }

            public override bool Contains(int x, int y) => x >= 0 && x <= Width && y >= 0 && y <= Height;
        }
    }

    protected class ComboBoxWithLabel : Control, SearchableOption
    {
        private TextBox _label;
        private Combobox _comboBox;
        private readonly string[] options;

        public ComboBoxWithLabel(World world, string label, int labelWidth, int comboWidth, string[] options, int selectedIndex, Action<int, string> onOptionSelected = null)
        {
            AcceptMouseInput = true;
            CanMove = true;

            _label = TextBox.GetOne
                (label, ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default(labelWidth > 0 ? labelWidth : null));

            Add(_label);

            Add
            (
                _comboBox = new Combobox(world, comboWidth, options, selectedIndex, onOptionSelected: onOptionSelected)
                {
                    X = _label.MeasuredSize.X + _label.X + 5
                }
            );

            Width = labelWidth + comboWidth + 5;
            Height = Math.Max(_label.MeasuredSize.Y, _comboBox.Height);

            SearchValueChanged += ModernOptionsGump_SearchValueChanged;
            this.options = options;
        }

        public override void Dispose()
        {
            base.Dispose();
            SearchValueChanged -= ModernOptionsGump_SearchValueChanged;
        }

        private void ModernOptionsGump_SearchValueChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(SearchText))
            {
                if (Search(SearchText))
                {
                    OnSearchMatch();
                    SetParentsForMatchingSearch(this, Page);
                }
                else
                {
                    _label.Alpha = ThemeSettings.NO_MATCH_SEARCH;
                }
            }
            else
            {
                _label.Alpha = 1f;
            }
        }

        public bool Search(string text)
        {
            if (_label.Text.ToLower().Contains(text.ToLower()))
            {
                return true;
            }

            foreach (string o in options)
            {
                if (o.ToLower().Contains(text.ToLower()))
                {
                    return true;
                }
            }

            return false;
        }

        public void OnSearchMatch() => _label.Alpha = 1f;

        public int SelectedIndex => _comboBox.SelectedIndex;

        private class Combobox : Control
        {
            private readonly string[] _items;
            private readonly int _maxHeight;
            private TextBox _label;
            private int _selectedIndex = 0;
            private readonly int[] _originalIndices;
            private readonly string[] _sortedItems;

            private World world;

            public Combobox(World world, int width, string[] items, int selected = -1, int maxHeight = 400, Action<int, string> onOptionSelected = null)
            {
                this.world = world;
                Width = width;
                Height = 25;
                _items = items;
                _maxHeight = maxHeight;
                OnOptionSelected = onOptionSelected;
                AcceptMouseInput = true;

                // Create sorted items with original index mapping
                _originalIndices = Enumerable.Range(0, items.Length).ToArray();
                _sortedItems = new string[items.Length];
                Array.Copy(items, _sortedItems, items.Length);

                // Sort both arrays together
                Array.Sort(_sortedItems, _originalIndices);

                // Find the display index for the selected original index
                int displayIndex = selected > -1 ? Array.IndexOf(_originalIndices, selected) : -1;

                string initialText = displayIndex > -1 ? _sortedItems[displayIndex] : _sortedItems[_originalIndices[0]];

                Add(new ColorBox(Width, Height, ThemeSettings.SEARCH_BACKGROUND));

                _label = TextBox.GetOne(initialText, ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default(width));
                _label.X = 2;
                _label.Y = (Height >> 1) - (_label.Height >> 1);
                Add(_label);

                _selectedIndex = displayIndex;
            }

            public int SelectedIndex
            {
                get => _selectedIndex > -1 ? _originalIndices[_selectedIndex] : _originalIndices[0]; // Return original index
                set
                {
                    // Find display index from original index
                    int displayIndex = value > -1 ? Array.IndexOf(_originalIndices, value) : _originalIndices[0];
                    _selectedIndex = displayIndex;

                    if (_sortedItems != null && displayIndex > -1)
                    {
                        _label.Text = _sortedItems[displayIndex];
                        OnOptionSelected?.Invoke(value, _sortedItems[displayIndex]); // Pass original index
                    }
                    else if (_label != null)
                    {
                        _label.Text = string.Empty;
                    }
                }
            }

            public Action<int, string> OnOptionSelected { get; }

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                if (button != MouseButtonType.Left)
                {
                    return;
                }

                int comboY = ScreenCoordinateY + Offset.Y;

                if (comboY < 0)
                {
                    comboY = 0;
                }
                else if (comboY + _maxHeight > Client.Game.Window.ClientBounds.Height)
                {
                    comboY = Client.Game.Window.ClientBounds.Height - _maxHeight;
                }

                UIManager.Add(new ComboboxGump(world, ScreenCoordinateX, comboY, Width, _maxHeight, _sortedItems, _originalIndices, this));

                base.OnMouseUp(x, y, button);
            }

            private class ComboboxGump : Gump
            {
                private readonly Combobox _combobox;

                public ComboboxGump(World world, int x, int y, int width, int maxHeight, string[] items, int[] originalIndices, Combobox combobox) : base(world, 0, 0)
                {
                    CanMove = false;
                    AcceptMouseInput = true;
                    X = x;
                    Y = y;

                    IsModal = true;
                    LayerOrder = UILayer.Over;
                    ModalClickOutsideAreaClosesThisControl = true;

                    _combobox = combobox;

                    ColorBox cb;
                    Add(cb = new ColorBox(width, 0, ThemeSettings.BACKGROUND));

                    var labels = new HoveredLabel[items.Length];

                    for (int i = 0; i < items.Length; i++)
                    {
                        string item = items[i];

                        if (item == null)
                        {
                            item = string.Empty;
                        }

                        var label = new HoveredLabel
                            (item, ThemeSettings.DROPDOWN_OPTION_NORMAL_HUE, ThemeSettings.DROPDOWN_OPTION_HOVER_HUE, ThemeSettings.DROPDOWN_OPTION_SELECTED_HUE, width)
                            {
                                X = 2,
                                Tag = originalIndices[i],
                                IsSelected = combobox.SelectedIndex == originalIndices[i] ? true : false
                            };

                        label.Y = i * label.Height + 5;

                        label.MouseUp += LabelOnMouseUp;

                        labels[i] = label;
                    }

                    int totalHeight = Math.Min(maxHeight, labels.Max(o => o.Y + o.Height));
                    int maxWidth = Math.Max(width, labels.Max(o => o.X + o.Width));

                    var area = new ScrollArea(0, 0, maxWidth + 15, totalHeight)
                    {
                        AcceptMouseInput = true
                    };

                    foreach (HoveredLabel label in labels)
                    {
                        area.Add(label);
                    }

                    Add(area);

                    cb.Width = maxWidth;
                    cb.Height = totalHeight;
                    Width = maxWidth;
                    Height = totalHeight;
                }

                private void LabelOnMouseUp(object sender, MouseEventArgs e)
                {
                    if (e.Button == MouseButtonType.Left)
                    {
                        _combobox.SelectedIndex = (int)((HoveredLabel)sender).Tag;
                        Dispose();
                    }
                }

                private class HoveredLabel : Control
                {
                    private readonly Color _overHue, _normalHue, _selectedHue;

                    private readonly TextBox _label;

                    public HoveredLabel(string text, Color hue, Color overHue, Color selectedHue, int maxwidth = 0)
                    {
                        _overHue = overHue;
                        _normalHue = hue;
                        _selectedHue = selectedHue;
                        AcceptMouseInput = true;

                        _label = TextBox.GetOne
                        (
                            text, ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR,
                            TextBox.RTLOptions.Default(maxwidth > 0 ? maxwidth : null).MouseInput()
                        );

                        Height = _label.MeasuredSize.Y;
                        Width = Math.Max(_label.MeasuredSize.X, maxwidth);

                        IsVisible = !string.IsNullOrEmpty(text);
                    }

                    public bool DrawBackgroundCurrentIndex = true;
                    public bool IsSelected, ForceHover;

                    public Color Hue;

                    public override void Update()
                    {
                        if (IsSelected)
                        {
                            if (Hue != _selectedHue)
                            {
                                Hue = _selectedHue;
                                _label.FontColor = Hue;
                            }
                        }
                        else if (MouseIsOver || ForceHover)
                        {
                            if (Hue != _overHue)
                            {
                                Hue = _overHue;
                                _label.FontColor = Hue;
                            }
                        }
                        else if (Hue != _normalHue)
                        {
                            Hue = _normalHue;
                            _label.FontColor = Hue;
                        }

                        base.Update();
                    }

                    public override bool Draw(UltimaBatcher2D batcher, int x, int y)
                    {
                        if (DrawBackgroundCurrentIndex && MouseIsOver && !string.IsNullOrWhiteSpace(_label.Text))
                        {
                            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

                            batcher.Draw(SolidColorTextureCache.GetTexture(Color.Gray), new Rectangle(x, y + 2, Width - 4, Height - 4), hueVector);
                        }

                        _label.Draw(batcher, x, y);

                        return base.Draw(batcher, x, y);
                    }
                }
            }
        }
    }

    public class InputFieldWithLabel : Control, SearchableOption
    {
        public string Text => _inputField.Text;
        private readonly InputField _inputField;
        private readonly TextBox _label;

        public InputFieldWithLabel(string label, int inputWidth, string inputText, bool numbersonly = false, EventHandler onTextChange = null)
        {
            AcceptMouseInput = true;
            CanMove = true;

            _label = TextBox.GetOne(label, ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default());
            Add(_label);

            Add
            (
                _inputField = new InputField(inputWidth, 40, 0, -1, inputText, numbersonly, onTextChange)
                {
                    X = _label.Width + _label.X + 5
                }
            );

            _label.Y = (_inputField.Height >> 1) - (_label.Height >> 1);

            Width = _label.Width + _inputField.Width + 5;
            Height = Math.Max(_label.Height, _inputField.Height);

            SearchValueChanged += ModernOptionsGump_SearchValueChanged;
        }

        public override void Dispose()
        {
            base.Dispose();
            SearchValueChanged -= ModernOptionsGump_SearchValueChanged;
        }

        private void ModernOptionsGump_SearchValueChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(SearchText))
            {
                if (Search(SearchText))
                {
                    OnSearchMatch();
                    SetParentsForMatchingSearch(this, Page);
                }
                else
                {
                    _label.Alpha = ThemeSettings.NO_MATCH_SEARCH;
                }
            }
            else
            {
                _label.Alpha = 1f;
            }
        }

        public void SetText(string text) => _inputField.SetText(text);

        public bool Search(string text)
        {
            if (_label.Text.ToLower().Contains(text.ToLower()))
            {
                return true;
            }

            return false;
        }

        public void OnSearchMatch() => _label.Alpha = 1f;
    }

    protected class ModernColorPickerWithLabel : Control, SearchableOption
    {
        private TextBox _label;
        private ModernColorPicker.HueDisplay _colorPicker;

        public ModernColorPickerWithLabel(World world, string text, ushort hue, Action<ushort> hueSelected = null, int maxWidth = 0)
        {
            AcceptMouseInput = true;
            CanMove = true;
            WantUpdateSize = false;

            Add(_colorPicker = new ModernColorPicker.HueDisplay(world, hue, hueSelected, true));

            _label = TextBox.GetOne
                (text, ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default(maxWidth > 0 ? maxWidth : null));

            _label.X = _colorPicker.Width + 5;
            Add(_label);

            Width = _label.Width + _colorPicker.Width + 5;
            Height = Math.Max(_colorPicker.Height, _label.MeasuredSize.Y);

            SearchValueChanged += ModernOptionsGump_SearchValueChanged;
        }

        public override void Dispose()
        {
            base.Dispose();
            SearchValueChanged -= ModernOptionsGump_SearchValueChanged;
        }

        public ushort Hue
        {
            get { return _colorPicker.Hue; }
            set { _colorPicker.Hue = value; }
        }

        private void ModernOptionsGump_SearchValueChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(SearchText))
            {
                if (Search(SearchText))
                {
                    OnSearchMatch();
                    SetParentsForMatchingSearch(this, Page);
                }
                else
                {
                    _label.Alpha = ThemeSettings.NO_MATCH_SEARCH;
                }
            }
            else
            {
                _label.Alpha = 1f;
            }
        }

        public bool Search(string text) => _label.Text.ToLower().Contains(text.ToLower());

        public void OnSearchMatch() => _label.Alpha = 1f;
    }

    public class CheckboxWithLabel : Control, SearchableOption
    {
        private bool _isChecked;
        private readonly TextBox _text;

        public TextBox TextLabel => _text;

        private Vector3 hueVector = ShaderHueTranslator.GetHueVector(ThemeSettings.CHECKBOX, false, 0.9f);

        public CheckboxWithLabel(string text = "", int maxWidth = 0, bool isChecked = false, Action<bool> valueChanged = null)
        {
            _isChecked = isChecked;
            ValueChanged = valueChanged;

            _text = TextBox.GetOne
                (text, ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default(maxWidth == 0 ? null : maxWidth));

            _text.X = ThemeSettings.CHECKBOX_SIZE + 5;

            Width = ThemeSettings.CHECKBOX_SIZE + 5 + _text.Width;
            Height = Math.Max(ThemeSettings.CHECKBOX_SIZE, _text.MeasuredSize.Y);

            _text.Y = (Height / 2) - (_text.Height / 2);

            CanMove = true;
            AcceptMouseInput = true;

            SearchValueChanged += ModernOptionsGump_SearchValueChanged;
        }

        private void ModernOptionsGump_SearchValueChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(SearchText))
            {
                if (Search(SearchText))
                {
                    OnSearchMatch();
                    SetParentsForMatchingSearch(this, Page);
                }
                else
                {
                    _text.Alpha = ThemeSettings.NO_MATCH_SEARCH;
                }
            }
            else
            {
                _text.Alpha = 1f;
            }
        }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnCheckedChanged();
                }
            }
        }

        public override ClickPriority Priority => ClickPriority.High;

        public string Text => _text.Text;

        public Action<bool> ValueChanged { get; }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            batcher.Draw(SolidColorTextureCache.GetTexture(Color.White), new Rectangle(x, y, ThemeSettings.CHECKBOX_SIZE, ThemeSettings.CHECKBOX_SIZE), hueVector);

            if (IsChecked)
            {
                batcher.Draw
                (
                    SolidColorTextureCache.GetTexture(Color.Black),
                    new Rectangle
                        (x + (ThemeSettings.CHECKBOX_SIZE / 2) / 2, y + (ThemeSettings.CHECKBOX_SIZE / 2) / 2, ThemeSettings.CHECKBOX_SIZE / 2, ThemeSettings.CHECKBOX_SIZE / 2),
                    hueVector
                );
            }

            _text.Draw(batcher, x + _text.X, y + _text.Y);

            return base.Draw(batcher, x, y);
        }

        protected virtual void OnCheckedChanged() => ValueChanged?.Invoke(IsChecked);

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left && MouseIsOver)
            {
                IsChecked = !IsChecked;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _text?.Dispose();
            SearchValueChanged -= ModernOptionsGump_SearchValueChanged;
        }

        public bool Search(string text) => _text.Text.ToLower().Contains(text.ToLower());

        public void OnSearchMatch() => _text.Alpha = 1f;
    }

    protected class SliderWithLabel : Control, SearchableOption
    {
        private readonly TextBox _label;
        private readonly Slider _slider;
        public int GetValue() => _slider.Value;

        public SliderWithLabel(string label, int textWidth, int barWidth, int min, int max, int value, Action<int> valueChanged = null)
        {
            AcceptMouseInput = true;
            CanMove = true;

            _label = TextBox.GetOne
                (label, ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default(textWidth > 0 ? textWidth : null));

            Add(_label);

            Add
            (
                _slider = new Slider(barWidth, min, max, value, valueChanged)
                {
                    X = _label.X + _label.Width + 5
                }
            );

            Width = textWidth + barWidth + 5;
            Height = Math.Max(_label.Height, _slider.Height);

            _slider.Y = (Height / 2) - (_slider.Height / 2);

            SearchValueChanged += ModernOptionsGump_SearchValueChanged;
        }

        public override void Dispose()
        {
            base.Dispose();
            SearchValueChanged -= ModernOptionsGump_SearchValueChanged;
        }

        private void ModernOptionsGump_SearchValueChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(SearchText))
            {
                if (Search(SearchText))
                {
                    OnSearchMatch();
                    SetParentsForMatchingSearch(this, Page);
                }
                else
                {
                    _label.Alpha = ThemeSettings.NO_MATCH_SEARCH;
                }
            }
            else
            {
                _label.Alpha = 1f;
            }
        }

        public bool Search(string text) => _label.Text.ToLower().Contains(text.ToLower());

        public void OnSearchMatch() => _label.Alpha = 1f;

        private class Slider : Control
        {
            private bool _clicked;
            private int _sliderX;
            private readonly TextBox _text;
            private int _value = -1;

            public Slider(int barWidth, int min, int max, int value, Action<int> valueChanged = null)
            {
                _text = TextBox.GetOne(string.Empty, ThemeSettings.FONT, ThemeSettings.STANDARD_TEXT_SIZE, ThemeSettings.TEXT_FONT_COLOR, TextBox.RTLOptions.Default(barWidth));

                MinValue = min;
                MaxValue = max;
                BarWidth = barWidth;
                AcceptMouseInput = true;
                AcceptKeyboardInput = true;
                Width = barWidth;
                Height = Math.Max(_text.MeasuredSize.Y, 15);

                CalculateOffset();

                Value = value;
                ValueChanged = valueChanged;
            }

            public int MinValue { get; set; }

            public int MaxValue { get; set; }

            public int BarWidth { get; set; }

            public float Percents { get; private set; }

            public int Value
            {
                get => _value;
                set
                {
                    if (_value != value)
                    {
                        int oldValue = _value;

                        _value = /*_newValue =*/
                            value;
                        //if (IsInitialized)
                        //    RecalculateSliderX();

                        if (_value < MinValue)
                        {
                            _value = MinValue;
                        }
                        else if (_value > MaxValue)
                        {
                            _value = MaxValue;
                        }

                        if (_text != null)
                        {
                            _text.Text = Value.ToString();
                        }

                        if (_value != oldValue)
                        {
                            CalculateOffset();
                        }

                        ValueChanged?.Invoke(_value);
                    }
                }
            }

            public Action<int> ValueChanged { get; }

            public override void Update()
            {
                base.Update();

                if (_clicked)
                {
                    int x = Mouse.Position.X - X - ParentX;
                    int y = Mouse.Position.Y - Y - ParentY;

                    CalculateNew(x);
                }
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(ThemeSettings.BACKGROUND);

                int mx = x;

                //Draw background line
                batcher.Draw(SolidColorTextureCache.GetTexture(Color.White), new Rectangle(mx, y + 3, BarWidth, 10), hueVector);

                hueVector = ShaderHueTranslator.GetHueVector(ThemeSettings.SEARCH_BACKGROUND);

                batcher.Draw(SolidColorTextureCache.GetTexture(Color.White), new Rectangle(mx + _sliderX, y, 15, 16), hueVector);

                _text?.Draw(batcher, mx + BarWidth + 2, y + (Height >> 1) - (_text.Height >> 1));

                return base.Draw(batcher, x, y);
            }

            protected override void OnMouseDown(int x, int y, MouseButtonType button)
            {
                if (button != MouseButtonType.Left)
                {
                    return;
                }

                _clicked = true;
            }

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                if (button != MouseButtonType.Left)
                {
                    return;
                }

                _clicked = false;
                CalculateNew(x);
            }

            protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
            {
                base.OnKeyUp(key, mod);

                switch (key)
                {
                    case SDL.SDL_Keycode.SDLK_LEFT: Value--; break;
                    case SDL.SDL_Keycode.SDLK_RIGHT: Value++; break;
                }
            }

            protected override void OnMouseEnter(int x, int y)
            {
                base.OnMouseEnter(x, y);
                UIManager.KeyboardFocusControl = this;
            }

            private void CalculateNew(int x)
            {
                int len = BarWidth;
                int maxValue = MaxValue - MinValue;

                len -= 15;
                float perc = x / (float)len * 100.0f;
                Value = (int)(maxValue * perc / 100.0f) + MinValue;
                CalculateOffset();
            }

            private void CalculateOffset()
            {
                if (Value < MinValue)
                {
                    Value = MinValue;
                }
                else if (Value > MaxValue)
                {
                    Value = MaxValue;
                }

                int value = Value - MinValue;
                int maxValue = MaxValue - MinValue;
                int length = BarWidth;

                length -= 15;

                if (maxValue > 0)
                {
                    Percents = value / (float)maxValue * 100.0f;
                }
                else
                {
                    Percents = 0;
                }

                _sliderX = (int)(length * Percents / 100.0f);

                if (_sliderX < 0)
                {
                    _sliderX = 0;
                }
            }

            public override void Dispose()
            {
                _text?.Dispose();
                base.Dispose();
            }
        }
    }

    #endregion
}
