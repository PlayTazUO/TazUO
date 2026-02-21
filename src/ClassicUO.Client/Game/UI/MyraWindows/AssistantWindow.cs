using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

public class AssistantWindow : MyraControl
{
    public const int WIDTH = 550;
    private TabItem _selectedTab;

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
        var cameraSmoothing = new MyraHSlider { Minimum = 0, Maximum = 1, Value = profile.CameraSmoothingFactor };
        cameraSmoothing.ValueChangedByUser += (_, _) => profile.CameraSmoothingFactor = Math.Clamp(cameraSmoothing.Value, 0f, 1f);
        grid.AddWidget(cameraSmoothing, row, Col.LeftContent.ToInt());

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

        return grid;
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
