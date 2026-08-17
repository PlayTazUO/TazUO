#nullable enable
using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
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

        // ===== Widget construction =====

        // Enable and targeting options
        var enableRow = new VisualContainer(new VisualContainerProps() { Orientation = Orientation.Horizontal },
            MyraCheckButton.CreateWithCallback(
                profile.EnableBandageAgent,
                b => profile.EnableBandageAgent = b,
                TazLang.Get("bandageagent_enable")
            ),
            MyraCheckButton.CreateWithCallback(
                profile.BandageAgentBandageFriends,
                b => profile.BandageAgentBandageFriends = b,
                TazLang.Get("bandageagent_bandagefriends"),
                TazLang.Get("bandageagent_bandagefriends_tooltip")
            ),
            MyraCheckButton.CreateWithCallback(
                profile.BandageAgentBandageAllies,
                b => profile.BandageAgentBandageAllies = b,
                TazLang.Get("bandageagent_bandageallies"),
                TazLang.Get("bandageagent_bandageallies_tooltip")
            ),
            MyraCheckButton.CreateWithCallback(
                profile.BandageAgentBandagePets,
                b => profile.BandageAgentBandagePets = b,
                TazLang.Get("bandageagent_bandagepets"),
                TazLang.Get("bandageagent_bandagepets_tooltip")
            ),
            MyraCheckButton.CreateWithCallback(
                profile.BandageAgentDisableSelfHeal,
                b => profile.BandageAgentDisableSelfHeal = b,
                TazLang.Get("bandageagent_disableselfheal"),
                TazLang.Get("bandageagent_disableselfheal_tooltip")
            )
        );

        // Self-heal via server command instead of double-clicking bandages
        var selfCommandBox = new MyraInputBox
        {
            Text = profile.BandageAgentSelfCommand,
            Tooltip = TazLang.Get("bandageagent_selfcommand_tooltip"),
            Width = 120,
        };
        selfCommandBox.TextChangedByUser += (_, _) =>
        {
            profile.BandageAgentSelfCommand = selfCommandBox.Text ?? "";
        };

        var selfCommandRow = new VisualContainer(new VisualContainerProps() { Orientation = Orientation.Horizontal },
            MyraCheckButton.CreateWithCallback(
                profile.BandageAgentUseSelfCommand,
                b => profile.BandageAgentUseSelfCommand = b,
                TazLang.Get("bandageagent_useselfcommand"),
                TazLang.Get("bandageagent_useselfcommand_tooltip")
            ),
            new MyraLabel(TazLang.Get("bandageagent_selfcommand_label"), MyraLabel.TextStyle.P),
            selfCommandBox,
            MyraCheckButton.CreateWithCallback(
                profile.BandageAgentSelfCommandExpectTarget,
                b => profile.BandageAgentSelfCommandExpectTarget = b,
                TazLang.Get("bandageagent_selfcommand_expecttarget"),
                TazLang.Get("bandageagent_selfcommand_expecttarget_tooltip")
            )
        );

        // Delay + HP threshold
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

        HorizontalStackPanel hpSlider = LabeledHorizontalSlider.SliderWithLabel(
            TazLang.Get("bandageagent_hpthreshold"),
            out _,
            v => profile.BandageAgentHPPercentage = (int)v,
            1, 99,
            profile.BandageAgentHPPercentage
        );

        // Journal messages
        var journalLabel = new MyraLabel(TazLang.Get("bandageagent_journalmessages_label"), MyraLabel.TextStyle.P);
        var journalMessageBox = new MyraInputBox
        {
            Text = profile.BandageAgentJournalMessages,
            Tooltip = TazLang.Get("bandageagent_journalmessages_tooltip"),
            MaxWidth = 650
        };
        journalMessageBox.TextChangedByUser += (_, _) =>
        {
            profile.BandageAgentJournalMessages = journalMessageBox.Text ?? "";
        };

        // Timing mode checkboxes
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

        var timingContainer = new VisualContainer(new VisualContainerProps() { Orientation = Orientation.Vertical },
            delayRow,
            journalLabel,
            journalMessageBox,
            timingRow
        );

        // Poison, hidden and yellow hit settings
        var conditionRow = new VisualContainer(new VisualContainerProps() { Orientation = Orientation.Horizontal },
            MyraCheckButton.CreateWithCallback(
                profile.BandageAgentCheckPoisoned,
                b => profile.BandageAgentCheckPoisoned = b,
                TazLang.Get("bandageagent_bandageifpoisoned")
            ),
            MyraCheckButton.CreateWithCallback(
                profile.BandageAgentCheckHidden,
                b => profile.BandageAgentCheckHidden = b,
                TazLang.Get("bandageagent_skipifhidden")
            ),
            MyraCheckButton.CreateWithCallback(
                profile.BandageAgentCheckInvul,
                b => profile.BandageAgentCheckInvul = b,
                TazLang.Get("bandageagent_skipifyellowhits")
            )
        );

        var useNewPacketCheck = MyraCheckButton.CreateWithCallback(
            profile.BandageAgentUseNewPacket,
            b => profile.BandageAgentUseNewPacket = b,
            TazLang.Get("bandageagent_usenewpacket")
        );

        // Target type to expect when auto-targeting the bandage
        var targetTypeCombo = new ComboView { MinWidth = 150, VerticalAlignment = VerticalAlignment.Center };
        targetTypeCombo.ListView.Widgets.Add(new Label { Text = TazLang.Get("bandageagent_targettype_neutral", "Neutral") });
        targetTypeCombo.ListView.Widgets.Add(new Label { Text = TazLang.Get("bandageagent_targettype_harmful", "Harmful") });
        targetTypeCombo.ListView.Widgets.Add(new Label { Text = TazLang.Get("bandageagent_targettype_beneficial", "Beneficial") });
        targetTypeCombo.ListView.SelectedIndex = (int)Math.Min((int)profile.BandageAgentTargetType, (int)TargetType.Beneficial);
        targetTypeCombo.ListView.SelectedIndexChanged += (_, _) =>
        {
            if (targetTypeCombo.ListView.SelectedIndex.HasValue)
                profile.BandageAgentTargetType = (TargetType)targetTypeCombo.ListView.SelectedIndex.Value;
        };
        var targetTypeRow = new HorizontalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
        targetTypeRow.Widgets.Add(new MyraLabel(
            TazLang.Get("bandageagent_targettype_label", "Auto-target type"),
            MyraLabel.TextStyle.P
        ));
        targetTypeRow.Widgets.Add(targetTypeCombo);

        // Bandage distance for friends/allies
        HorizontalStackPanel distanceSlider = LabeledHorizontalSlider.SliderWithLabel(
            TazLang.Get("bandageagent_distance"),
            out _,
            v => { if (ProfileManager.AccountSettings != null) ProfileManager.AccountSettings.BandageAgentDistance = (int)v; },
            1, 15,
            ProfileManager.AccountSettings?.BandageAgentDistance ?? 3
        );

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

        // ===== Layout =====
        root.Widgets.Add(enableRow);
        root.Widgets.Add(selfCommandRow);
        root.Widgets.Add(timingContainer);
        root.Widgets.Add(conditionRow);
        root.Widgets.Add(new MyraSpacer(15, 1));
        root.Widgets.Add(hpSlider);
        root.Widgets.Add(distanceSlider);
        root.Widgets.Add(useNewPacketCheck);
        root.Widgets.Add(targetTypeRow);
        root.Widgets.Add(graphicRow);


        return root;
    }

}
