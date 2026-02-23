using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Processes;
using ClassicUO.Game.UI.Gumps.SpellBar;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant;

public static class GeneralTab
{
    public static Widget Build()
    {
        Profile profile = ProfileManager.CurrentProfile;
        AssistantLanguage lang = Language.Instance.Assistant;
        float gameScale = Client.Game.RenderScale;

        var grid = new MyraGrid();
        grid.AddColumn(new Proportion(ProportionType.Auto), 2);
        grid.AddColumn(new Proportion(ProportionType.Pixels, 10)); //Spacing
        grid.AddColumn(new Proportion(ProportionType.Auto), 2);

        int row = 0;

        grid.AddWidget(new MyraLabel(lang.VisualConfig, MyraLabel.Style.H1), row, Col.LeftLabel.ToInt());
        grid.AddWidget(new MyraLabel(lang.DelayConfig, MyraLabel.Style.H1), row, Col.RightLabel.ToInt());

        row++;

        grid.AddWidget(new MyraLabel(lang.CameraSmoothing, MyraLabel.Style.P) { Tooltip = lang.CameraSmoothingTooltip }, row, Col.LeftLabel.ToInt());
        grid.AddWidget(MyraHSlider.CreateSliderWithCallback(0, 1, profile.CameraSmoothingFactor, v => profile.CameraSmoothingFactor = v), row, Col.LeftContent.ToInt());

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
        grid.AddWidget(MyraHSlider.CreateSliderWithCallback(0, 20, profile.MinGumpMoveDistance, v => profile.MinGumpMoveDistance = (int)v), row, Col.LeftContent.ToInt());

        row++;

        grid.AddWidget(new MyraLabel(lang.GameScale, MyraLabel.Style.P) { Tooltip = lang.GameScaleTooltip }, row, Col.LeftLabel.ToInt());
        grid.AddWidget(MyraHSlider.CreateSliderWithCallback(Constants.MIN_GAME_SCALE, Constants.MAX_GAME_SCALE, Client.Game.RenderScale, v =>
        {
            gameScale = Math.Clamp(v, Constants.MIN_GAME_SCALE, Constants.MAX_GAME_SCALE);
        }), row, Col.LeftContent.ToInt());

        row++;
        grid.AddWidget(new MyraButton("Apply scale", () =>
        {
            Client.Game.SetScale(gameScale);
            _ = Client.Settings.SetAsync(SettingsScope.Global, Constants.SqlSettings.GAME_SCALE, gameScale);
        }), row, Col.LeftContent.ToInt());

        // === Right side: Delay Config ===
        int rightRow = 1;

        grid.AddWidget(new MyraLabel(lang.TurnDelay, MyraLabel.Style.P), rightRow, Col.RightLabel.ToInt());
        grid.AddWidget(MyraHSlider.CreateSliderWithCallback(0, 150, profile.TurnDelay, v => profile.TurnDelay = (ushort)v), rightRow, Col.RightContent.ToInt());
        rightRow++;

        grid.AddWidget(new MyraLabel(lang.ObjectDelay, MyraLabel.Style.P), rightRow, Col.RightLabel.ToInt());
        grid.AddWidget(MyraHSlider.CreateSliderWithCallback(0, 3000, profile.MoveMultiObjectDelay, v => profile.MoveMultiObjectDelay = (int)v), rightRow, Col.RightContent.ToInt());
        rightRow++;

        grid.AddWidget(new MyraButton(lang.AutoDelayChecker, () => AutomatedObjectDelay.Begin()) { Tooltip = lang.AutoDelayCheckerTooltip }, rightRow, Col.RightContent.ToInt());
        rightRow++;

        // === Right side: Misc ===
        grid.AddWidget(new MyraLabel(lang.Misc, MyraLabel.Style.H1), rightRow, Col.RightLabel.ToInt());
        rightRow++;

        grid.AddWidget(new MyraLabel(lang.QueueItemMoves, MyraLabel.Style.P) { Tooltip = lang.QueueItemMovesTooltip }, rightRow, Col.RightLabel.ToInt());
        grid.AddWidget(CreateCheckBox(profile.QueueManualItemMoves, b => profile.QueueManualItemMoves = b), rightRow, Col.RightContent.ToInt());
        rightRow++;

        grid.AddWidget(new MyraLabel(lang.QueueObjectUses, MyraLabel.Style.P) { Tooltip = lang.QueueObjectUsesTooltip }, rightRow, Col.RightLabel.ToInt());
        grid.AddWidget(CreateCheckBox(profile.QueueManualItemUses, b => profile.QueueManualItemUses = b), rightRow, Col.RightContent.ToInt());
        rightRow++;

        grid.AddWidget(new MyraLabel(lang.AutoOpenOwnCorpse, MyraLabel.Style.P) { Tooltip = lang.AutoOpenOwnCorpseTooltip }, rightRow, Col.RightLabel.ToInt());
        grid.AddWidget(CreateCheckBox(profile.AutoOpenOwnCorpse, b => profile.AutoOpenOwnCorpse = b), rightRow, Col.RightContent.ToInt());
        rightRow++;

        grid.AddWidget(new MyraLabel(lang.AutoUnequipForActions, MyraLabel.Style.P) { Tooltip = lang.AutoUnequipForActionsTooltip }, rightRow, Col.RightLabel.ToInt());
        grid.AddWidget(CreateCheckBox(profile.AutoUnequipForActions, b => profile.AutoUnequipForActions = b), rightRow, Col.RightContent.ToInt());
        rightRow++;

        grid.AddWidget(new MyraLabel(lang.DisableWeather, MyraLabel.Style.P) { Tooltip = lang.DisableWeatherTooltip }, rightRow, Col.RightLabel.ToInt());
        grid.AddWidget(CreateCheckBox(profile.DisableWeather, b =>
        {
            profile.DisableWeather = b;
            if (b) World.Instance?.Weather.Reset();
        }), rightRow, Col.RightContent.ToInt());
        rightRow++;

        var healSpell = SpellDefinition.FullIndexGetSpell(profile.QuickHealSpell);
        var healLabel = new MyraLabel(healSpell?.Name ?? profile.QuickHealSpell.ToString(), MyraLabel.Style.P) { Tooltip = lang.QuickSpellTooltip };
        grid.AddWidget(new MyraButton(lang.SetQuickHealSpell, () =>
        {
            UIManager.Add(new SpellQuickSearch(World.Instance, 0, 0, s =>
            {
                if (s != null)
                {
                    healLabel.Text = s.Name;
                    profile.QuickHealSpell = s.ID;
                }
            }, true).CenterInViewPort());
        }), rightRow, Col.RightLabel.ToInt());
        grid.AddWidget(healLabel, rightRow, Col.RightContent.ToInt());
        rightRow++;

        var cureSpell = SpellDefinition.FullIndexGetSpell(profile.QuickCureSpell);
        var cureLabel = new MyraLabel(cureSpell?.Name ?? profile.QuickCureSpell.ToString(), MyraLabel.Style.P) { Tooltip = lang.QuickSpellTooltip };
        grid.AddWidget(new MyraButton(lang.SetQuickCureSpell, () =>
        {
            UIManager.Add(new SpellQuickSearch(World.Instance, 0, 0, s =>
            {
                if (s != null)
                {
                    cureLabel.Text = s.Name;
                    profile.QuickCureSpell = s.ID;
                }
            }, true).CenterInViewPort());
        }), rightRow, Col.RightLabel.ToInt());
        grid.AddWidget(cureLabel, rightRow, Col.RightContent.ToInt());

        return grid;
    }

    private static CheckButton CreateCheckBox(bool isChecked, Action<bool> onClick)
    {
        var button = new CheckButton { IsChecked = isChecked };
        button.IsCheckedChanged += (_, _) => onClick(button.IsChecked);
        return button;
    }

    private enum Col
    {
        LeftLabel,
        LeftContent,
        SpacingColumn,
        RightLabel,
        RightContent,
    }
}
