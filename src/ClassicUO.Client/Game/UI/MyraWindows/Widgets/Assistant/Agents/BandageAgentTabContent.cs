#nullable enable
using System;
using ClassicUO.Configuration;
using ClassicUO.Utility;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Agents;

public static class BandageAgentTabContent
{
    public static Widget Build()
    {
        Profile? profile = ProfileManager.CurrentProfile;
        BandageAgentLanguage bandLang = Language.Instance.Assistant.Agents.Bandage;

        if (profile == null)
            return new MyraLabel(bandLang.ProfileNotLoaded, MyraLabel.TextStyle.P);

        var root = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };

        root.Widgets.Add(new MyraLabel(
            bandLang.AutoHealWhenHpBelowThreshold,
            MyraLabel.TextStyle.H3
        ));

        var enableRow = new HorizontalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
        enableRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.EnableBandageAgent,
            b => profile.EnableBandageAgent = b,
            bandLang.EnableBandageAgent
        ));

        enableRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentBandageFriends,
            b => profile.BandageAgentBandageFriends = b,
            bandLang.BandageFriendsCheckbox,
            bandLang.BandageFriendsTooltip
        ));

        enableRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentBandageAllies,
            b => profile.BandageAgentBandageAllies = b,
            bandLang.BandageAlliesCheckbox,
            bandLang.BandageAlliesTooltip
        ));

        enableRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentBandagePets,
            b => profile.BandageAgentBandagePets = b,
            bandLang.BandagePetsCheckbox,
            bandLang.BandagePetsTooltip
        ));

        enableRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentDisableSelfHeal,
            b => profile.BandageAgentDisableSelfHeal = b,
            bandLang.DisableSelfHealCheckbox,
            bandLang.DisableSelfHealTooltip
        ));

        root.Widgets.Add(enableRow);

        // Delay
        var delayBox = new MyraInputBox
        {
            Text = profile.BandageAgentDelay.ToString(),
            Tooltip = "Delay between bandage attempts in milliseconds (50-30000)",
            Width = 80,
        };
        delayBox.TextChangedByUser += (_, _) =>
        {
            if (int.TryParse(delayBox.Text, out int delay))
            {
                profile.BandageAgentDelay = Math.Clamp(delay, 50, 30000);
                delayBox.Text = profile.BandageAgentDelay.ToString();
            }
        };
        var delayRow = new HorizontalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
        delayRow.Widgets.Add(delayBox);
        delayRow.Widgets.Add(new MyraLabel(bandLang.BandageDelayMsLabel, MyraLabel.TextStyle.P));
        root.Widgets.Add(new MyraSpacer(15, 1));
        root.Widgets.Add(delayRow);

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentUseDexFormula,
            b => profile.BandageAgentUseDexFormula = b,
            bandLang.UseDexFormulaCheckbox,
            bandLang.UseDexFormulaTooltip
        ));

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentCheckForBuff,
            b => profile.BandageAgentCheckForBuff = b,
            bandLang.UseBandageBuffCheckbox,
            bandLang.UseBandageBuffTooltip
        ));

        root.Widgets.Add(MyraHSlider.SliderWithLabel(
            bandLang.HealthThresholdSliderLabel,
            out _,
            v => profile.BandageAgentHPPercentage = (int)v,
            1, 99,
            profile.BandageAgentHPPercentage
        ));

        root.Widgets.Add(new MyraSpacer(15, 1));
        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentUseNewPacket,
            b => profile.BandageAgentUseNewPacket = b,
            bandLang.UseNewPacketCheckbox
        ));

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentCheckPoisoned,
            b => profile.BandageAgentCheckPoisoned = b,
            bandLang.BandageIfPoisonedCheckbox
        ));

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentCheckHidden,
            b => profile.BandageAgentCheckHidden = b,
            bandLang.SkipIfHidden
        ));

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentCheckInvul,
            b => profile.BandageAgentCheckInvul = b,
            bandLang.SkipIfYellowHits
        ));

        // Bandage graphic
        var graphicBox = new MyraInputBox
        {
            Text = $"0x{profile.BandageAgentGraphic:X4}",
            Tooltip = bandLang.BandageGraphicIdTooltip,
            Width = 80,
        };
        graphicBox.TextChangedByUser += (_, _) =>
        {
            if (StringHelper.TryParseInt(graphicBox.Text, out int graphic) && graphic >= 0 && graphic <= ushort.MaxValue)
                profile.BandageAgentGraphic = (ushort)graphic;
        };
        var graphicRow = new HorizontalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
        graphicRow.Widgets.Add(new MyraLabel(bandLang.BandageGraphicIdLabel, MyraLabel.TextStyle.P));
        graphicRow.Widgets.Add(graphicBox);
        root.Widgets.Add(graphicRow);

        return root;
    }

}
