#nullable enable
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant;

public static class TitleBarTabContent
{
    public static Widget Build()
    {
        Profile profile = ProfileManager.CurrentProfile;

        var outer = new VerticalStackPanel { Spacing = 6 };

        outer.Widgets.Add(new MyraLabel(
            "配置窗口标题栏以显示生命值、法力和耐力信息。",
            MyraLabel.TextStyle.H3));

        // Enable
        outer.Widgets.Add(MyraCheckButton.CreateWithCallback(profile.EnableTitleBarStats,
            b =>
            {
                profile.EnableTitleBarStats = b;
                if (b)
                    TitleBarStatsManager.ForceUpdate();
                else
                    Client.Game.SetWindowTitle(
                        string.IsNullOrEmpty(World.Instance.Player?.Name)
                            ? string.Empty
                            : World.Instance.Player.Name);
            }, "启用标题栏状态"));

        // Display mode
        outer.Widgets.Add(new MyraSpacer(15, 5));
        outer.Widgets.Add(new MyraLabel("显示模式", MyraLabel.TextStyle.H2));

        var previewLabel = new MyraLabel(TitleBarStatsManager.GetPreviewText(), MyraLabel.TextStyle.P);

        void SetMode(TitleBarStatsMode mode)
        {
            profile.TitleBarStatsMode = mode;
            TitleBarStatsManager.ForceUpdate();
            previewLabel.Text = TitleBarStatsManager.GetPreviewText();
        }

        // All three radio buttons must be direct children of the same parent
        // so Myra's RadioButton auto-exclusivity works correctly.
        var radioGroup = new VerticalStackPanel { Spacing = 4 };

        var rbText = new RadioButton
        {
            Content = new MyraLabel("文本  (HP 85/100, MP 42/50, SP 95/100)", MyraLabel.TextStyle.P),
            IsPressed = profile.TitleBarStatsMode == TitleBarStatsMode.Text
        };
        rbText.PressedChanged += (_, _) => { if (rbText.IsPressed) SetMode(TitleBarStatsMode.Text); };

        var rbPercent = new RadioButton
        {
            Content = new MyraLabel("百分比  (HP 85%, MP 84%, SP 95%)", MyraLabel.TextStyle.P),
            IsPressed = profile.TitleBarStatsMode == TitleBarStatsMode.Percent
        };
        rbPercent.PressedChanged += (_, _) => { if (rbPercent.IsPressed) SetMode(TitleBarStatsMode.Percent); };

        var rbBar = new RadioButton
        {
            Content = new MyraLabel("进度条  (HP [||||||    ] MP [||||||    ] SP [||||||    ])", MyraLabel.TextStyle.P),
            IsPressed = profile.TitleBarStatsMode == TitleBarStatsMode.ProgressBar
        };
        rbBar.PressedChanged += (_, _) => { if (rbBar.IsPressed) SetMode(TitleBarStatsMode.ProgressBar); };

        radioGroup.Widgets.Add(rbText);
        radioGroup.Widgets.Add(rbPercent);
        radioGroup.Widgets.Add(rbBar);
        outer.Widgets.Add(radioGroup);

        // Preview
        outer.Widgets.Add(new MyraSpacer(15, 5));
        outer.Widgets.Add(new MyraLabel("预览", MyraLabel.TextStyle.H2));
        outer.Widgets.Add(previewLabel);

        return outer;
    }
}
