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
        float gameScale = Client.Game.RenderScale;

        var mainContent = new HorizontalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
        var leftSide = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
        var rightSide = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };

        mainContent.Widgets.Add(leftSide);
        mainContent.Widgets.Add(rightSide);


        leftSide.Widgets.Add(new MyraLabel(TazLang.Get("assistant_visualconfig"), MyraLabel.TextStyle.H2));
        rightSide.Widgets.Add(new MyraLabel(TazLang.Get("assistant_delayconfig"), MyraLabel.TextStyle.H2));

        leftSide.Widgets.Add(MyraCheckButton.CreateWithCallback(profile.HighlightGameObjects, (b) => profile.HighlightGameObjects = b, TazLang.Get("assistant_highlightgameobjects")));

        leftSide.Widgets.Add(MyraCheckButton.CreateWithCallback(profile.NameOverheadToggled, (b) => profile.NameOverheadToggled = b, TazLang.Get("assistant_shownameplates")));

        leftSide.Widgets.Add(MyraCheckButton.CreateWithCallback(profile.EnablePetScaling, b =>
        {
            profile.EnablePetScaling = b;

            Dictionary<uint, Mobile>.ValueCollection mobs = World.Instance.Mobiles.Values;
            foreach (Mobile mob in mobs)
                if (mob != null && mob.IsRenamable)
                    mob.Scale = b ? 0.6f : 1f;
        }, TazLang.Get("assistant_petscaling"), TazLang.Get("assistant_petscaling_tooltip")));

        leftSide.Widgets.Add(MyraCheckButton.CreateWithCallback(profile.OutlineMobilesNotoriety, (b) => profile.OutlineMobilesNotoriety = b, TazLang.Get("assistant_outlinemobiles")));

        leftSide.Widgets.Add(LabeledHorizontalSlider.SliderWithLabel(TazLang.Get("assistant_mingumpdragdist"), out _, v => profile.MinGumpMoveDistance = (int)v, 0, 20, profile.MinGumpMoveDistance));

        leftSide.Widgets.Add(LabeledHorizontalSlider.SliderWithLabel(TazLang.Get("assistant_gamescale"), out LabeledHorizontalSlider gsSlider, v =>
        {
            gameScale = Math.Clamp(v / 100, Constants.MIN_GAME_SCALE, Constants.MAX_GAME_SCALE);
        }, Constants.MIN_GAME_SCALE * 100, Constants.MAX_GAME_SCALE * 100, Client.Game.RenderScale * 100));
        gsSlider.Tooltip = TazLang.Get("assistant_gamescale_tooltip");

        leftSide.Widgets.Add(new MyraButton("Apply scale", () =>
        {
            Client.Game.SetScale(gameScale);
            _ = Client.Settings.SetAsync(SettingsScope.Global, Constants.SqlSettings.GAME_SCALE, gameScale);
        }));


        //Right side
        rightSide.Widgets.Add(LabeledHorizontalSlider.SliderWithLabel(TazLang.Get("assistant_turndelay"), out _, v => profile.TurnDelay = (ushort)v, 0, 150, profile.TurnDelay));

        rightSide.Widgets.Add(LabeledHorizontalSlider.SliderWithLabel(TazLang.Get("assistant_objectdelay"), out LabeledHorizontalSlider obDelaySlider,
            v => profile.MoveMultiObjectDelay = (int)v, 0, 3000, profile.MoveMultiObjectDelay));

        rightSide.Widgets.Add(new MyraButton(TazLang.Get("assistant_autodelaychecker"), () => AutomatedObjectDelay.Begin(() =>
        {
            obDelaySlider?.Value = profile.MoveMultiObjectDelay;
        })) { Tooltip = TazLang.Get("assistant_autodelaychecker_tooltip") });

        // Right side: Misc
        rightSide.Widgets.Add(new MyraSpacer(20, 15));

        rightSide.Widgets.Add(new MyraLabel(TazLang.Get("assistant_misc"), MyraLabel.TextStyle.H2));

        rightSide.Widgets.Add(MyraCheckButton.CreateWithCallback(profile.QueueManualItemMoves,
            b => profile.QueueManualItemMoves = b, TazLang.Get("assistant_queueitemmoves"), TazLang.Get("assistant_queueitemmoves_tooltip")));

        rightSide.Widgets.Add(MyraCheckButton.CreateWithCallback(profile.QueueManualItemUses,
            b => profile.QueueManualItemUses = b, TazLang.Get("assistant_queueobjectuses"), TazLang.Get("assistant_queueobjectuses_tooltip")));

        rightSide.Widgets.Add(MyraCheckButton.CreateWithCallback(profile.AutoOpenOwnCorpse,
            b => profile.AutoOpenOwnCorpse = b, TazLang.Get("assistant_autoopenowncorpse"), TazLang.Get("assistant_autoopenowncorpse_tooltip")));

        rightSide.Widgets.Add(MyraCheckButton.CreateWithCallback(profile.AutoUnequipForActions,
            b => profile.AutoUnequipForActions = b, TazLang.Get("assistant_autounequipforactions"), TazLang.Get("assistant_autounequipforactions_tooltip")));

        rightSide.Widgets.Add(MyraCheckButton.CreateWithCallback(profile.DisableWeather,
            b => {
                profile.DisableWeather = b;
                if (b) World.Instance?.Weather.Reset();
            }, TazLang.Get("assistant_disableweather"), TazLang.Get("assistant_disableweather_tooltip")));

        var healLabel = new MyraLabel(SpellDefinition.FullIndexGetSpell(profile.QuickHealSpell)?.Name ??
                                      profile.QuickHealSpell.ToString(), MyraLabel.TextStyle.P) { Tooltip = TazLang.Get("assistant_quickspelltooltip") };

        rightSide.Widgets.Add(new MyraButton(TazLang.Get("assistant_setquickhealspell"), () =>
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
                                      profile.QuickCureSpell.ToString(), MyraLabel.TextStyle.P) { Tooltip = TazLang.Get("assistant_quickspelltooltip") };
        rightSide.Widgets.Add(new MyraButton(TazLang.Get("assistant_setquickcurespell"), () =>
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

        rightSide.Widgets.Add(MyraCheckButton.CreateWithCallback(profile.SingleClickMobileSetsLastTarget,
            b => {
                profile.SingleClickMobileSetsLastTarget = b;
            }, TazLang.Get("assistant_singleclicklasttarg")));

        return mainContent;
    }
}
