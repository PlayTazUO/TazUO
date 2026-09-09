// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.IO;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

internal sealed class SaveNamePlatePresetWindow : MyraControl
{
    private readonly Profile _profile;
    private readonly SavedNamePlatePreset _snapshot;
    private readonly Action _onSaved;
    private readonly MyraInputBox _name = new() { Width = 320 };
    private readonly MyraLabel _error = new(string.Empty, MyraLabel.TextStyle.P)
    {
        TextColor = Color.OrangeRed,
        Wrap = true,
        Width = 320,
        Visible = false
    };

    public static void Show(Profile profile, Action onSaved)
    {
        SaveNamePlatePresetWindow existing = UIManager.GetGump<SaveNamePlatePresetWindow>();
        if (existing != null)
        {
            existing.BringOnTop();
            return;
        }

        new SaveNamePlatePresetWindow(profile, onSaved);
    }

    private SaveNamePlatePresetWindow(Profile profile, Action onSaved) : base(TazLang.Get("nameplate_savepreset"))
    {
        _profile = profile;
        _snapshot = SavedNamePlatePreset.Capture(profile);
        _onSaved = onSaved;
        IsModal = true;
        ModalClickOutsideAreaClosesThisControl = false;
        LayerOrder = UILayer.Over;
        _rootWindow.CloseKey = Keys.Escape;

        var layout = new VerticalStackPanel { Spacing = 8, Padding = new Thickness(8) };
        layout.Widgets.Add(new MyraLabel(TazLang.Get("nameplate_savepreset_prompt"), MyraLabel.TextStyle.P) { Width = 320, Wrap = true });
        _name.HintText = TazLang.Get("nameplate_savepreset_name");
        _name.KeyDown += (_, e) =>
        {
            if (e.Data == Keys.Enter)
                Save();
        };
        layout.Widgets.Add(_name);
        layout.Widgets.Add(_error);

        var buttons = new HorizontalStackPanel { Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Widgets.Add(new MyraButton(TazLang.Get("nameplate_savepreset_save"), Save));
        buttons.Widgets.Add(new MyraButton(TazLang.Get("uicommons_cancel"), () => _disposeRequested = true));
        layout.Widgets.Add(buttons);
        SetRootContent(layout);
        CenterInViewPort();
        UIManager.Add(this);
        BringOnTop();
        UIManager.KeyboardFocusControl = this;
        _name.SetKeyboardFocus();
    }

    private void Save()
    {
        if (_disposeRequested || IsDisposed)
            return;

        try
        {
            SaveNamePlatePresetResult result = NamePlatePresetStore.Shared.Save(_snapshot, _name.Text,
                NamePlatePresets.GetOptions(), out SavedNamePlatePreset saved);
            if (result != SaveNamePlatePresetResult.Saved)
            {
                ShowError(TazLang.Get(result == SaveNamePlatePresetResult.InvalidName
                    ? "nameplate_savepreset_invalidname" : "nameplate_savepreset_duplicate"));
                return;
            }

            NamePlatePresets.SelectSaved(_profile, saved);
            _profile.Save();
            _disposeRequested = true;
            MainThreadQueue.EnqueueAction(_onSaved);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log.Error($"Unable to save nameplate preset: {e}");
            ShowError(TazLang.Get("nameplate_savepreset_failed"));
        }
    }

    private void ShowError(string message)
    {
        _error.Text = message;
        _error.Visible = true;
        _name.SetKeyboardFocus();
    }
}
