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

        if (profile == null)
            return new MyraLabel(TazLang.Get("bandageagent_profilenotloaded"), MyraLabel.TextStyle.P);

        var root = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };

        root.Widgets.Add(new MyraLabel(
            TazLang.Get("bandageagent_autohealwhenhpbelowthreshold"),
            MyraLabel.TextStyle.H3
        ));

        var enableRow = new HorizontalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
        enableRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.EnableBandageAgent,
            b => profile.EnableBandageAgent = b,
            TazLang.Get("bandageagent_enable")
        ));

        enableRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentBandageFriends,
            b => profile.BandageAgentBandageFriends = b,
            TazLang.Get("bandageagent_bandagefriends"),
            TazLang.Get("bandageagent_bandagefriends_tooltip")
        ));

        enableRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentBandageAllies,
            b => profile.BandageAgentBandageAllies = b,
            TazLang.Get("bandageagent_bandageallies"),
            TazLang.Get("bandageagent_bandageallies_tooltip")
        ));

        enableRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentBandagePets,
            b => profile.BandageAgentBandagePets = b,
            TazLang.Get("bandageagent_bandagepets"),
            TazLang.Get("bandageagent_bandagepets_tooltip")
        ));

        enableRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentDisableSelfHeal,
            b => profile.BandageAgentDisableSelfHeal = b,
            TazLang.Get("bandageagent_disableselfheal"),
            TazLang.Get("bandageagent_disableselfheal_tooltip")
        ));

        root.Widgets.Add(enableRow);

        // Delay + HP threshold on the same row
        var delayBox = new MyraInputBox
        {
            Text = profile.BandageAgentDelay.ToString(),
            Tooltip = TazLang.Get("bandageagent_delay_tooltip"),
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
        delayRow.Widgets.Add(new MyraLabel(TazLang.Get("bandageagent_delay_label"), MyraLabel.TextStyle.P));
        delayRow.Widgets.Add(new MyraSpacer(15, 1));
        delayRow.Widgets.Add(MyraHSlider.SliderWithLabel(
            TazLang.Get("bandageagent_hpthreshold"),
            out _,
            v => profile.BandageAgentHPPercentage = (int)v,
            1, 99,
            profile.BandageAgentHPPercentage
        ));
        root.Widgets.Add(new MyraSpacer(15, 1));
        root.Widgets.Add(delayRow);

        // Journal messages below delay/HP
        root.Widgets.Add(new MyraLabel(TazLang.Get("bandageagent_journalmessages_label"), MyraLabel.TextStyle.P));
        var journalMessageBox = new MyraInputBox
        {
            Text = profile.BandageAgentJournalMessages,
            Tooltip = TazLang.Get("bandageagent_journalmessages_tooltip"),
            Width = 300,
        };
        journalMessageBox.TextChangedByUser += (_, _) =>
        {
            profile.BandageAgentJournalMessages = journalMessageBox.Text ?? "";
        };
        root.Widgets.Add(journalMessageBox);

        // Timing mode checkboxes in one row
        var timingRow = new HorizontalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
        timingRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentUseDexFormula,
            b => profile.BandageAgentUseDexFormula = b,
            TazLang.Get("bandageagent_usedexformula"),
            TazLang.Get("bandageagent_usedexformula_tooltip")
        ));
        timingRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentCheckForBuff,
            b => profile.BandageAgentCheckForBuff = b,
            TazLang.Get("bandageagent_usebandagebuff"),
            TazLang.Get("bandageagent_usebandagebuff_tooltip")
        ));
        timingRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentUseJournalTrigger,
            b => profile.BandageAgentUseJournalTrigger = b,
            TazLang.Get("bandageagent_journaltrigger"),
            TazLang.Get("bandageagent_journaltrigger_tooltip")
        ));
        root.Widgets.Add(timingRow);

        root.Widgets.Add(new MyraSpacer(15, 1));
        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentUseNewPacket,
            b => profile.BandageAgentUseNewPacket = b,
            TazLang.Get("bandageagent_usenewpacket")
        ));

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentCheckPoisoned,
            b => profile.BandageAgentCheckPoisoned = b,
            TazLang.Get("bandageagent_bandageifpoisoned")
        ));

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentCheckHidden,
            b => profile.BandageAgentCheckHidden = b,
            TazLang.Get("bandageagent_skipifhidden")
        ));

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentCheckInvul,
            b => profile.BandageAgentCheckInvul = b,
            TazLang.Get("bandageagent_skipifyellowhits")
        ));

        // Bandage graphic
        var graphicBox = new MyraInputBox
        {
            Text = $"0x{profile.BandageAgentGraphic:X4}",
            Tooltip = TazLang.Get("bandageagent_graphicid_tooltip"),
            Width = 80,
        };
        graphicBox.TextChangedByUser += (_, _) =>
        {
            if (StringHelper.TryParseInt(graphicBox.Text, out int graphic) && graphic >= 0 && graphic <= ushort.MaxValue)
                profile.BandageAgentGraphic = (ushort)graphic;
        };
        var graphicRow = new HorizontalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
        graphicRow.Widgets.Add(new MyraLabel(TazLang.Get("bandageagent_graphicid_label"), MyraLabel.TextStyle.P));
        graphicRow.Widgets.Add(graphicBox);
        root.Widgets.Add(graphicRow);

        return root;
    }

}
