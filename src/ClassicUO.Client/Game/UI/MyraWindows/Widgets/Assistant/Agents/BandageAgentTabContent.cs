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
            return new MyraLabel("未加载配置文件", MyraLabel.TextStyle.P);

        var root = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };

        root.Widgets.Add(new MyraLabel(
            "当生命值低于阈值时自动使用绷带治疗。",
            MyraLabel.TextStyle.H3));

        var enableRow = new HorizontalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
        enableRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.EnableBandageAgent,
            b => profile.EnableBandageAgent = b,
            "启用绷带代理"));
        enableRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentBandageFriends,
            b => profile.BandageAgentBandageFriends = b,
            "治疗好友",
            "治疗好友列表中的目标"));
        enableRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentBandageAllies,
            b => profile.BandageAgentBandageAllies = b,
            "治疗盟友",
            "治疗附近公会/联盟成员（善恶值: 盟友）"));
        root.Widgets.Add(enableRow);

        enableRow.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentDisableSelfHeal,
            b => profile.BandageAgentDisableSelfHeal = b,
            "禁用自我治疗",
            "启用后，绷带代理将只治疗好友而不治疗自己"));

        // Delay
        var delayBox = new MyraInputBox
        {
            Text = profile.BandageAgentDelay.ToString(),
            Tooltip = "绷带尝试之间的延迟（毫秒，50-30000）",
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
        delayRow.Widgets.Add(new MyraLabel("延迟（毫秒）", MyraLabel.TextStyle.P));
        root.Widgets.Add(new MyraSpacer(15, 1));
        root.Widgets.Add(delayRow);

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentUseDexFormula,
            b => profile.BandageAgentUseDexFormula = b,
            "使用敏捷公式",
            "使用敏捷公式代替固定延迟"));

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentCheckForBuff,
            b => profile.BandageAgentCheckForBuff = b,
            "使用绷带增益", "使用绷带增益效果代替延迟"));

        root.Widgets.Add(MyraHSlider.SliderWithLabel(
            "生命值百分比阈值",
            out _,
            v => profile.BandageAgentHPPercentage = (int)v,
            1, 99,
            profile.BandageAgentHPPercentage));

        root.Widgets.Add(new MyraSpacer(15, 1));
        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentUseNewPacket,
            b => profile.BandageAgentUseNewPacket = b,
            "使用新绷带数据包"));

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentCheckPoisoned,
            b => profile.BandageAgentCheckPoisoned = b,
            "中毒时使用绷带"));

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentCheckHidden,
            b => profile.BandageAgentCheckHidden = b,
            "隐身时跳过绷带"));

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.BandageAgentCheckInvul,
            b => profile.BandageAgentCheckInvul = b,
            "黄色伤害时跳过绷带"));

        // Bandage graphic
        var graphicBox = new MyraInputBox
        {
            Text = $"0x{profile.BandageAgentGraphic:X4}",
            Tooltip = "要使用的绷带图形ID（默认: 0x0E21）。接受十六进制（0x0E21）或十进制（3617）",
            Width = 80,
        };
        graphicBox.TextChangedByUser += (_, _) =>
        {
            if (StringHelper.TryParseInt(graphicBox.Text, out int graphic) && graphic >= 0 && graphic <= ushort.MaxValue)
                profile.BandageAgentGraphic = (ushort)graphic;
        };
        var graphicRow = new HorizontalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
        graphicRow.Widgets.Add(new MyraLabel("绷带图形ID:", MyraLabel.TextStyle.P));
        graphicRow.Widgets.Add(graphicBox);
        root.Widgets.Add(graphicRow);

        return root;
    }

}
