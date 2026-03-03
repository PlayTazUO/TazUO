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

public static class GeneralTabContent
{
    public static Widget Build()
    {
        Profile profile = ProfileManager.CurrentProfile;
        AssistantLanguage lang = Language.Instance.Assistant;
        float gameScale = Client.Game.RenderScale;

        var grid = new MyraGrid();
        grid.AddColumn(new Proportion(ProportionType.Auto));
        grid.AddColumn(new Proportion(ProportionType.Pixels, 10)); //Spacing
        grid.AddColumn(new Proportion(ProportionType.Auto), 2);

        int row = 0;

        grid.AddWidget(new MyraLabel(lang.VisualConfig, MyraLabel.Style.H1), row, Col.LeftColumn.ToInt());
        grid.AddWidget(new MyraLabel(lang.DelayConfig, MyraLabel.Style.H1), row, Col.RightColumn.ToInt());

        row++;

        grid.AddWidget(MyraHSlider.SliderWithLabel(lang.CameraSmoothing, out _, v => profile.CameraSmoothingFactor = v, 0, 1, profile.CameraSmoothingFactor), row, Col.LeftColumn.ToInt());

        row++;

        grid.AddWidget(MyraCheckButton.CreateWithCallback(profile.HighlightGameObjects, (b) => profile.HighlightGameObjects = b, lang.HighlightGameObjects), row, Col.LeftColumn.ToInt());

        row++;

        grid.AddWidget(MyraCheckButton.CreateWithCallback(profile.NameOverheadToggled, (b) => profile.NameOverheadToggled = b, lang.ShowNameplates), row, Col.LeftColumn.ToInt());

        row++;

        grid.AddWidget(MyraCheckButton.CreateWithCallback(profile.EnablePetScaling, b =>
        {
            profile.EnablePetScaling = b;

            Dictionary<uint, Mobile>.ValueCollection mobs = World.Instance.Mobiles.Values;
            foreach (Mobile mob in mobs)
                if (mob != null && mob.IsRenamable)
                    mob.Scale = b ? 0.6f : 1f;
        }, lang.PetScaling, lang.PetScalingTooltip), row, Col.LeftColumn.ToInt());

        row++;

        grid.AddWidget(MyraCheckButton.CreateWithCallback(profile.OutlineMobilesNotoriety, (b) => profile.OutlineMobilesNotoriety = b, lang.OutlineMobiles), row, Col.LeftColumn.ToInt());

        row++;

        grid.AddWidget(MyraHSlider.SliderWithLabel(lang.MinGumpDragDist, out _, v => profile.MinGumpMoveDistance = (int)v, 0, 20, profile.MinGumpMoveDistance), row, Col.LeftColumn.ToInt());

        row++;


        grid.AddWidget(MyraHSlider.SliderWithLabel(lang.GameScale, out MyraHSlider gsSlider, v =>
        {
            gameScale = Math.Clamp(v / 100, Constants.MIN_GAME_SCALE, Constants.MAX_GAME_SCALE);
        }, Constants.MIN_GAME_SCALE * 100, Constants.MAX_GAME_SCALE * 100, Client.Game.RenderScale * 100), row, Col.LeftColumn.ToInt());
        gsSlider.Tooltip = lang.GameScaleTooltip;

        row++;
        grid.AddWidget(new MyraButton("Apply scale", () =>
        {
            Client.Game.SetScale(gameScale);
            _ = Client.Settings.SetAsync(SettingsScope.Global, Constants.SqlSettings.GAME_SCALE, gameScale);
        }), row, Col.LeftColumn.ToInt());

        // Right side
        int rightRow = 1;

        grid.AddWidget(MyraHSlider.SliderWithLabel(lang.TurnDelay, out _, v => profile.TurnDelay = (ushort)v, 0, 150, profile.TurnDelay), rightRow, Col.RightColumn.ToInt());
        rightRow++;

        grid.AddWidget(MyraHSlider.SliderWithLabel(lang.ObjectDelay, out var obDelaySlider, v => profile.MoveMultiObjectDelay = (int)v, 0, 3000, profile.MoveMultiObjectDelay), rightRow, Col.RightColumn.ToInt());
        rightRow++;

        grid.AddWidget(new MyraButton(lang.AutoDelayChecker, () => AutomatedObjectDelay.Begin(() =>
        {
            obDelaySlider?.Value = profile.MoveMultiObjectDelay;
        })) { Tooltip = lang.AutoDelayCheckerTooltip }, rightRow, Col.RightColumn.ToInt());
        rightRow++;

        // Right side: Misc
        grid.AddWidget(new MyraLabel(lang.Misc, MyraLabel.Style.H1), rightRow, Col.RightColumn.ToInt());
        rightRow++;

        grid.AddWidget(MyraCheckButton.CreateWithCallback(profile.QueueManualItemMoves, b => profile.QueueManualItemMoves = b, lang.QueueItemMoves, lang.QueueItemMovesTooltip), rightRow, Col.RightColumn.ToInt());
        rightRow++;

        grid.AddWidget(MyraCheckButton.CreateWithCallback(profile.QueueManualItemUses, b => profile.QueueManualItemUses = b, lang.QueueObjectUses, lang.QueueObjectUsesTooltip), rightRow, Col.RightColumn.ToInt());
        rightRow++;

        grid.AddWidget(MyraCheckButton.CreateWithCallback(profile.AutoOpenOwnCorpse, b => profile.AutoOpenOwnCorpse = b, lang.AutoOpenOwnCorpse, lang.AutoOpenOwnCorpseTooltip), rightRow, Col.RightColumn.ToInt());
        rightRow++;

        grid.AddWidget(MyraCheckButton.CreateWithCallback(profile.AutoUnequipForActions, b => profile.AutoUnequipForActions = b, lang.AutoUnequipForActions, lang.AutoUnequipForActionsTooltip), rightRow, Col.RightColumn.ToInt());
        rightRow++;

        grid.AddWidget(MyraCheckButton.CreateWithCallback(profile.DisableWeather, b =>
        {
            profile.DisableWeather = b;
            if (b) World.Instance?.Weather.Reset();
        }, lang.DisableWeather, lang.DisableWeatherTooltip), rightRow, Col.RightColumn.ToInt());
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
        }), rightRow, Col.RightColumn.ToInt());
        grid.AddWidget(healLabel, rightRow, Col.RightNotesCol.ToInt());
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
        }), rightRow, Col.RightColumn.ToInt());
        grid.AddWidget(cureLabel, rightRow, Col.RightNotesCol.ToInt());

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
        LeftColumn,
        SpacingColumn,
        RightColumn,
        RightNotesCol,
    }
}
