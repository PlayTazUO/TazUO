using System;
using System.Collections.Generic;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using SDL3;

namespace ClassicUO.Game.UI.Gumps;

public class SelectableItemListGump : Gump
{
    private readonly List<string> _items;
    private readonly Action<string> _onItemSelected;
    private readonly Action<string> _onSelectionChanged;
    private readonly List<Label> _itemLabels;
    private int _selectedIndex;
    private VBoxContainer _vbox;
    private AlphaBlendControl _background;
    private const int PADDING = 5;

    public SelectableItemListGump(List<string> items, Action<string> onItemSelected, Action<string> onSelectionChanged, int width = 200) : base(World.Instance, 0, 0)
    {
        CanMove = false;
        CanCloseWithRightClick = true;
        AcceptKeyboardInput = true;
        AcceptMouseInput = true;
        CanCloseWithEsc = true;
        LayerOrder = UILayer.Over;
        IsModal = true;
        IsFocused = true;

        _items = items ?? new List<string>();
        _onItemSelected = onItemSelected;
        _onSelectionChanged = onSelectionChanged;
        _itemLabels = new List<Label>();

        CreateItemLabels();
        UpdateVisibleItems();

        ForceSizeUpdate();
        _background.Width = Width;
        _background.Height = Height;
    }

    private void CreateItemLabels()
    {
        Add(_background = new AlphaBlendControl());
        Add(_vbox = new(Width, 0, PADDING));

        int newW = 0;
        for (int i = 0; i < _items.Count; i++)
        {
            var label = new Label(_items[i], true, 0xFFFF)
            {
                X = PADDING,
                AcceptMouseInput = true
            };

            if(label.Width > newW) newW = label.Width;

            label.MouseUp += OnItemClicked;
            _itemLabels.Add(label);
            _vbox.Add(label);
        }

        _vbox.Width = newW + (PADDING * 2);
    }

    private void UpdateVisibleItems()
    {
        for (int i = 0; i < _itemLabels.Count; i++)
        {
            int itemIndex = i;

            if (itemIndex < _items.Count)
            {
                // Highlight selected item
                if (itemIndex == _selectedIndex)
                {
                    _itemLabels[i].Hue = 0x0035; // Highlight color
                }
                else
                {
                    _itemLabels[i].Hue = 0xFFFF; // Normal color
                }
            }
            else
            {
                _itemLabels[i].IsVisible = false;
            }
        }
    }

    private void OnItemClicked(object sender, MouseEventArgs e)
    {
        if(e.Button != MouseButtonType.Left) return;

        if (sender is Label label)
        {
            int labelIndex = _itemLabels.IndexOf(label);
            if (labelIndex >= 0)
            {
                int itemIndex = labelIndex;
                if (itemIndex < _items.Count)
                {
                    _selectedIndex = itemIndex;
                    UpdateVisibleItems();
                    _onItemSelected?.Invoke(_items[itemIndex]);
                    Dispose();
                    UIManager.SystemChat?.SetFocus();
                }
            }
        }
    }

    protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
    {
        switch (key)
        {
            case SDL.SDL_Keycode.SDLK_UP:
                MoveSelection(-1);
                return;

            case SDL.SDL_Keycode.SDLK_DOWN:
                MoveSelection(1);
                return;

            case SDL.SDL_Keycode.SDLK_RETURN:
            case SDL.SDL_Keycode.SDLK_KP_ENTER:
                if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
                {
                    _onItemSelected?.Invoke(_items[_selectedIndex]);
                    Dispose();
                    UIManager.SystemChat?.SetFocus();
                }
                return;
        }

        base.OnKeyDown(key, mod);
    }

    private void MoveSelection(int direction)
    {
        if (_items.Count == 0) return;

        _selectedIndex = Math.Max(0, Math.Min(_items.Count - 1, _selectedIndex + direction));
        _onSelectionChanged?.Invoke(_items[_selectedIndex]);

        UpdateVisibleItems();
    }

    public string GetSelectedItem()
    {
        if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
        {
            return _items[_selectedIndex];
        }
        return null;
    }

    public void SetSelectedItem(string item)
    {
        int index = _items.IndexOf(item);
        if (index >= 0)
        {
            _selectedIndex = index;
            UpdateVisibleItems();
        }
    }
}
