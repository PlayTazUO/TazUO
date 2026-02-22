using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;
using Button = Myra.Graphics2D.UI.Button;
using Label = Myra.Graphics2D.UI.Label;

namespace ClassicUO.Game.UI.MyraWindows;

public class AssistantWindow : MyraControl
{
    public const int WIDTH = 550;
    private TabItem _selectedTab;
    private float _gameScale = Client.Game.RenderScale;


    public AssistantWindow() : base("Legion Assistant")
    {
        Build();

        CenterInViewPort();
    }

    private void Build()
    {
        var tabs = new TabControl()
        {
            Width = WIDTH
        };

        tabs.SelectedIndexChanged += (s, e) =>
        {
            _selectedTab = tabs.SelectedItem;

            if (_selectedTab.Content != null) return;

            if(_selectedTab.Tag is int tPage)
                switch ((TabPage)tPage)
                {
                    default:
                    case TabPage.General:
                        _selectedTab.Content = BuildGeneral();
                        break;
                }
        };

        tabs.Items.Add(new TabItem("General") { Tag = TabPage.General.ToInt() });

        tabs.SelectedIndex = 0;

        SetRootContent(tabs);
    }

    private Widget BuildGeneral()
    {
        Profile profile = ProfileManager.CurrentProfile;
        AssistantLanguage lang = Language.Instance.Assistant;

        var grid = new MyraGrid();
        grid.AddColumn(new Proportion(ProportionType.Auto), 4);

        int row = 0;

        grid.AddWidget(new MyraLabel(lang.VisualConfig, MyraLabel.Style.H1), row, Col.LeftLabel.ToInt());
        grid.AddWidget(new MyraLabel(lang.DelayConfig, MyraLabel.Style.H1), row, Col.RightLabel.ToInt());

        row++;

        grid.AddWidget(new MyraLabel(lang.CameraSmoothing, MyraLabel.Style.P) { Tooltip = lang.CameraSmoothingTooltip }, row, Col.LeftLabel.ToInt());
        grid.AddWidget(CreateSlider(0, 1, profile.CameraSmoothingFactor, v => profile.CameraSmoothingFactor = v), row, Col.LeftContent.ToInt());

        row++;

        grid.AddWidget(new MyraLabel(lang.HighlightGameObjects, MyraLabel.Style.P), row, Col.LeftLabel.ToInt());
        grid.AddWidget(CreateCheckBox(profile.HighlightGameObjects, (b) => profile.HighlightGameObjects = b), row, Col.LeftContent.ToInt());

        row++;

        grid.AddWidget(new MyraLabel(lang.ShowNameplates, MyraLabel.Style.P), row, Col.LeftLabel.ToInt());
        grid.AddWidget(CreateCheckBox(profile.NameOverheadToggled, (b) => profile.NameOverheadToggled = b), row, Col.LeftContent.ToInt());

        row++;

        grid.AddWidget(new MyraLabel(lang.PetScaling, MyraLabel.Style.P) { Tooltip = lang.PetScalingTooltip }, row, Col.LeftLabel.ToInt());
        grid.AddWidget(CreateCheckBox(profile.EnablePetScaling, b =>
        {
            profile.EnablePetScaling = b;

            Dictionary<uint, Mobile>.ValueCollection mobs = World.Instance.Mobiles.Values;
            foreach (Mobile mob in mobs)
                if (mob != null && mob.IsRenamable)
                    mob.Scale = b ? 0.6f : 1f;
        }), row, Col.LeftContent.ToInt());

        row++;

        grid.AddWidget(new MyraLabel(lang.OutlineMobiles, MyraLabel.Style.P), row, Col.LeftLabel.ToInt());
        grid.AddWidget(CreateCheckBox(profile.OutlineMobilesNotoriety, (b) => profile.OutlineMobilesNotoriety = b), row, Col.LeftContent.ToInt());

        row++;

        grid.AddWidget(new MyraLabel(lang.MinGumpDragDist, MyraLabel.Style.P) { Tooltip = lang.MinGumpDragDistTooltip }, row, Col.LeftLabel.ToInt());
        grid.AddWidget(CreateSlider(0, 20, profile.MinGumpMoveDistance, v => profile.MinGumpMoveDistance = (int)v), row, Col.LeftContent.ToInt());

        row++;

        grid.AddWidget(new MyraLabel(lang.GameScale, MyraLabel.Style.P) { Tooltip = lang.GameScaleTooltip }, row, Col.LeftLabel.ToInt());
        grid.AddWidget(CreateSlider(Constants.MIN_GAME_SCALE, Constants.MAX_GAME_SCALE, Client.Game.RenderScale, v =>
        {
            _gameScale = Math.Clamp(v, Constants.MIN_GAME_SCALE, Constants.MAX_GAME_SCALE);
        }), row, Col.LeftContent.ToInt());

        row++;
        grid.AddWidget(new MyraButton("Apply scale", () =>
        {
            Client.Game.SetScale(_gameScale);
            _ = Client.Settings.SetAsync(SettingsScope.Global, Constants.SqlSettings.GAME_SCALE, _gameScale);
        }) , row, Col.LeftContent.ToInt());

        row++;
        grid.AddWidget(new Myra.Graphics2D.UI.Button
        {
            Content = new Label
            {
                Text = "Show"
            }
        }, row, Col.LeftContent.ToInt());

        return grid;
    }

    private MyraHSlider CreateSlider(float min, float max, float value, Action<float> onChanged)
    {
        var slider = new MyraHSlider { Minimum = min, Maximum = max, Value = value };
        slider.ValueChangedByUser += (_, _) => onChanged(Math.Clamp(slider.Value, min, max));
        return slider;
    }

    private CheckButton CreateCheckBox(bool isChecked, Action<bool> onClick)
    {
        var button = new CheckButton { IsChecked = isChecked };
        button.IsCheckedChanged += (_, _) => onClick(button.IsChecked);
        return button;
    }

    private enum Col
    {
        LeftLabel,
        LeftContent,
        RightLabel,
        RightContent,
    }

    private enum TabPage
    {
        General
    }
}
