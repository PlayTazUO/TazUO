using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Processes;
using ClassicUO.Game.UI.Gumps.SpellBar;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant;

public static class GeneralTabContent
{
    public static Widget Build()
    {
        Profile profile = ProfileManager.CurrentProfile;
        AssistantLanguage lang = Language.Instance.Assistant;
        float gameScale = Client.Game.RenderScale;

        var mainContent = new HorizontalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
        var leftSide = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
        var rightSide = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };

        mainContent.Widgets.Add(leftSide);
        mainContent.Widgets.Add(rightSide);


        leftSide.Widgets.Add(new MyraLabel(lang.VisualConfig, MyraLabel.TextStyle.H1));
        rightSide.Widgets.Add(new MyraLabel(lang.DelayConfig, MyraLabel.TextStyle.H1));

        leftSide.Widgets.Add(MyraHSlider.SliderWithLabel(lang.CameraSmoothing, out MyraHSlider _cSmoothSlider, v => profile.CameraSmoothingFactor = v, 0, 1, profile.CameraSmoothingFactor));
        _cSmoothSlider.RoundValues = false;
        _cSmoothSlider.WheelStep = 0.1f;

        leftSide.Widgets.Add(MyraCheckButton.CreateWithCallback(profile.HighlightGameObjects, (b) => profile.HighlightGameObjects = b, lang.HighlightGameObjects));

        leftSide.Widgets.Add(MyraCheckButton.CreateWithCallback(profile.NameOverheadToggled, (b) => profile.NameOverheadToggled = b, lang.ShowNameplates));

        leftSide.Widgets.Add(MyraCheckButton.CreateWithCallback(profile.EnablePetScaling, b =>
        {
            profile.EnablePetScaling = b;

            Dictionary<uint, Mobile>.ValueCollection mobs = World.Instance.Mobiles.Values;
            foreach (Mobile mob in mobs)
                if (mob != null && mob.IsRenamable)
                    mob.Scale = b ? 0.6f : 1f;
        }, lang.PetScaling, lang.PetScalingTooltip));

        leftSide.Widgets.Add(MyraCheckButton.CreateWithCallback(profile.OutlineMobilesNotoriety, (b) => profile.OutlineMobilesNotoriety = b, lang.OutlineMobiles));

        leftSide.Widgets.Add(MyraHSlider.SliderWithLabel(lang.MinGumpDragDist, out _, v => profile.MinGumpMoveDistance = (int)v, 0, 20, profile.MinGumpMoveDistance));

        leftSide.Widgets.Add(MyraHSlider.SliderWithLabel(lang.GameScale, out MyraHSlider gsSlider, v =>
        {
            gameScale = Math.Clamp(v / 100, Constants.MIN_GAME_SCALE, Constants.MAX_GAME_SCALE);
        }, Constants.MIN_GAME_SCALE * 100, Constants.MAX_GAME_SCALE * 100, Client.Game.RenderScale * 100));
        gsSlider.Tooltip = lang.GameScaleTooltip;

        leftSide.Widgets.Add(new MyraButton("Apply scale", () =>
        {
            Client.Game.SetScale(gameScale);
            _ = Client.Settings.SetAsync(SettingsScope.Global, Constants.SqlSettings.GAME_SCALE, gameScale);
        }));


        //Right side
        rightSide.Widgets.Add(MyraHSlider.SliderWithLabel(lang.TurnDelay, out _, v => profile.TurnDelay = (ushort)v, 0, 150, profile.TurnDelay));

        rightSide.Widgets.Add(MyraHSlider.SliderWithLabel(lang.ObjectDelay, out MyraHSlider obDelaySlider,
            v => profile.MoveMultiObjectDelay = (int)v, 0, 3000, profile.MoveMultiObjectDelay));

        rightSide.Widgets.Add(new MyraButton(lang.AutoDelayChecker, () => AutomatedObjectDelay.Begin(() =>
        {
            obDelaySlider?.Value = profile.MoveMultiObjectDelay;
        })) { Tooltip = lang.AutoDelayCheckerTooltip });

        // Right side: Misc
        rightSide.Widgets.Add(new MyraSpacer(20, 15));

        rightSide.Widgets.Add(new MyraLabel(lang.Misc, MyraLabel.TextStyle.H1));

        rightSide.Widgets.Add(MyraCheckButton.CreateWithCallback(profile.QueueManualItemMoves,
            b => profile.QueueManualItemMoves = b, lang.QueueItemMoves, lang.QueueItemMovesTooltip));

        rightSide.Widgets.Add(MyraCheckButton.CreateWithCallback(profile.QueueManualItemUses,
            b => profile.QueueManualItemUses = b, lang.QueueObjectUses, lang.QueueObjectUsesTooltip));

        rightSide.Widgets.Add(MyraCheckButton.CreateWithCallback(profile.AutoOpenOwnCorpse,
            b => profile.AutoOpenOwnCorpse = b, lang.AutoOpenOwnCorpse, lang.AutoOpenOwnCorpseTooltip));

        rightSide.Widgets.Add(MyraCheckButton.CreateWithCallback(profile.AutoUnequipForActions,
            b => profile.AutoUnequipForActions = b, lang.AutoUnequipForActions, lang.AutoUnequipForActionsTooltip));

        rightSide.Widgets.Add(MyraCheckButton.CreateWithCallback(profile.DisableWeather,
            b => {
                profile.DisableWeather = b;
                if (b) World.Instance?.Weather.Reset();
            }, lang.DisableWeather, lang.DisableWeatherTooltip));

        var healLabel = new MyraLabel(SpellDefinition.FullIndexGetSpell(profile.QuickHealSpell)?.Name ??
                                      profile.QuickHealSpell.ToString(), MyraLabel.TextStyle.P) { Tooltip = lang.QuickSpellTooltip };

        rightSide.Widgets.Add(new MyraButton(lang.SetQuickHealSpell, () =>
        {
            UIManager.Add(new SpellQuickSearch(World.Instance, 0, 0, s =>
            {
                if (s != null)
                {
                    healLabel.Text = s.Name;
                    profile.QuickHealSpell = s.ID;
                }
            }, true).CenterInViewPort());
        }).PlaceBefore(healLabel));

        var cureLabel = new MyraLabel(SpellDefinition.FullIndexGetSpell(profile.QuickCureSpell)?.Name ??
                                      profile.QuickCureSpell.ToString(), MyraLabel.TextStyle.P) { Tooltip = lang.QuickSpellTooltip };
        rightSide.Widgets.Add(new MyraButton(lang.SetQuickCureSpell, () =>
        {
            UIManager.Add(new SpellQuickSearch(World.Instance, 0, 0, s =>
            {
                if (s != null)
                {
                    cureLabel.Text = s.Name;
                    profile.QuickCureSpell = s.ID;
                }
            }, true).CenterInViewPort());
        }).PlaceBefore(cureLabel));

        return mainContent;
    }
}
