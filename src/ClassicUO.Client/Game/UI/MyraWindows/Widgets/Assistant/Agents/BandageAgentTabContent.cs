#nullable enable
using System;
using System.Globalization;
using ClassicUO.Configuration;
using Myra.Graphics2D.UI;
using TextBox = Myra.Graphics2D.UI.TextBox;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Agents;

public static class BandageAgentTabContent
{
    public static Widget Build()
    {
        Profile? profile = ProfileManager.CurrentProfile;
        if (profile == null)
            return new MyraLabel("Profile not loaded", MyraLabel.Style.P);

        var root = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };

        root.Widgets.Add(new MyraLabel(
            "Automatically use bandages to heal when HP drops below threshold.",
            MyraLabel.Style.P));

        var enableRow = new HorizontalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
        enableRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.EnableBandageAgent,
            b => profile.EnableBandageAgent = b,
            "Enable bandage agent"));
        enableRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentBandageFriends,
            b => profile.BandageAgentBandageFriends = b,
            "Bandage friends"));
        enableRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentBandageAllies,
            b => profile.BandageAgentBandageAllies = b,
            "Bandage allies",
            "Bandage nearby guild/alliance members (notoriety: ally)"));
        root.Widgets.Add(enableRow);

        enableRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentDisableSelfHeal,
            b => profile.BandageAgentDisableSelfHeal = b,
            "Disable self heal",
            "When enabled, bandage agent will only heal friends and not yourself"));

        // Delay
        var delayBox = new TextBox
        {
            Text = profile.BandageAgentDelay.ToString(),
            Width = 80,
            Tooltip = "Delay between bandage attempts in milliseconds (50-30000)"
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
        delayRow.Widgets.Add(new MyraLabel("Delay (ms)", MyraLabel.Style.P));
        root.Widgets.Add(new MyraSpacer(15, 1));
        root.Widgets.Add(delayRow);

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentUseDexFormula,
            b => profile.BandageAgentUseDexFormula = b,
            "Use dex formula",
            "Use the dex formula instead of a set delay"));

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentCheckForBuff,
            b => profile.BandageAgentCheckForBuff = b,
            "Use bandaging buff", "Use bandaging buff instead of delay"));

        root.Widgets.Add(MyraHSlider.SliderWithLabel(
            "HP percentage threshold",
            out _,
            v => profile.BandageAgentHPPercentage = (int)v,
            1, 99,
            profile.BandageAgentHPPercentage));

        root.Widgets.Add(new MyraSpacer(15, 1));
        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentUseNewPacket,
            b => profile.BandageAgentUseNewPacket = b,
            "Use new bandage packet"));

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentCheckPoisoned,
            b => profile.BandageAgentCheckPoisoned = b,
            "Bandage if poisoned"));

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentCheckHidden,
            b => profile.BandageAgentCheckHidden = b,
            "Skip bandage if hidden"));

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentCheckInvul,
            b => profile.BandageAgentCheckInvul = b,
            "Skip bandage if yellow hits"));

        // Bandage graphic
        var graphicBox = new TextBox
        {
            Text = $"0x{profile.BandageAgentGraphic:X4}",
            Width = 80,
            Tooltip = "Graphic ID of bandages to use (default: 0x0E21). Accepts hex (0x0E21) or decimal (3617)"
        };
        graphicBox.TextChangedByUser += (_, _) =>
        {
            if (TryParseBandageGraphic(graphicBox.Text, out ushort graphic))
                profile.BandageAgentGraphic = graphic;
        };
        var graphicRow = new HorizontalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
        graphicRow.Widgets.Add(new MyraLabel("Bandage graphic ID:", MyraLabel.Style.P));
        graphicRow.Widgets.Add(graphicBox);
        root.Widgets.Add(graphicRow);

        return root;
    }

    private static bool TryParseBandageGraphic(string text, out ushort graphic)
    {
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return ushort.TryParse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out graphic);
        return ushort.TryParse(text, out graphic);
    }
}
