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
            "Configure window title bar to show HP, Mana, and Stamina information.",
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
            }, "Enable title bar stats"));

        // Display mode
        outer.Widgets.Add(new MyraSpacer(15, 5));
        outer.Widgets.Add(new MyraLabel("Display Mode", MyraLabel.TextStyle.H2));

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
            Content = new MyraLabel("Text  (HP 85/100, MP 42/50, SP 95/100)", MyraLabel.TextStyle.P),
            IsPressed = profile.TitleBarStatsMode == TitleBarStatsMode.Text
        };
        rbText.PressedChanged += (_, _) => { if (rbText.IsPressed) SetMode(TitleBarStatsMode.Text); };

        var rbPercent = new RadioButton
        {
            Content = new MyraLabel("Percent  (HP 85%, MP 84%, SP 95%)", MyraLabel.TextStyle.P),
            IsPressed = profile.TitleBarStatsMode == TitleBarStatsMode.Percent
        };
        rbPercent.PressedChanged += (_, _) => { if (rbPercent.IsPressed) SetMode(TitleBarStatsMode.Percent); };

        var rbBar = new RadioButton
        {
            Content = new MyraLabel("Progress Bar  (HP [||||||    ] MP [||||||    ] SP [||||||    ])", MyraLabel.TextStyle.P),
            IsPressed = profile.TitleBarStatsMode == TitleBarStatsMode.ProgressBar
        };
        rbBar.PressedChanged += (_, _) => { if (rbBar.IsPressed) SetMode(TitleBarStatsMode.ProgressBar); };

        radioGroup.Widgets.Add(rbText);
        radioGroup.Widgets.Add(rbPercent);
        radioGroup.Widgets.Add(rbBar);
        outer.Widgets.Add(radioGroup);

        // Preview
        outer.Widgets.Add(new MyraSpacer(15, 5));
        outer.Widgets.Add(new MyraLabel("Preview", MyraLabel.TextStyle.H2));
        outer.Widgets.Add(previewLabel);

        return outer;
    }
}
