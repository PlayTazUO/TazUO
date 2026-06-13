using System;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant.Agents;

public static class AutoStatLockAgentTabContent
{
    public static Widget Build()
    {
        if (World.Instance?.Player == null)
            return new MyraLabel(TazLang.Get("autostatlock_profilenotloaded"), MyraLabel.TextStyle.P);

        AutoStatLockManager manager = AutoStatLockManager.Instance;
        AutoStatLockState state = manager.State;

        var root = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };

        root.Widgets.Add(new MyraLabel(TazLang.Get("autostatlock_desc"), MyraLabel.TextStyle.P));

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            state.Enabled,
            b => { state.Enabled = b; manager.Save(); },
            TazLang.Get("autostatlock_enable")
        ));

        root.Widgets.Add(new MyraSpacer(10, 10));

        root.Widgets.Add(BuildStatRow(
            TazLang.Get("autostatlock_strength"),
            state.StrEnabled,
            state.DesiredStr,
            b => { state.StrEnabled = b; manager.Save(); },
            v => { state.DesiredStr = v; manager.Save(); }
        ));

        root.Widgets.Add(BuildStatRow(
            TazLang.Get("autostatlock_dexterity"),
            state.DexEnabled,
            state.DesiredDex,
            b => { state.DexEnabled = b; manager.Save(); },
            v => { state.DesiredDex = v; manager.Save(); }
        ));

        root.Widgets.Add(BuildStatRow(
            TazLang.Get("autostatlock_intelligence"),
            state.IntEnabled,
            state.DesiredInt,
            b => { state.IntEnabled = b; manager.Save(); },
            v => { state.DesiredInt = v; manager.Save(); }
        ));

        return root;
    }

    private static Widget BuildStatRow(
        string statName,
        bool enabled,
        ushort desired,
        Action<bool> onEnabledChanged,
        Action<ushort> onDesiredChanged)
    {
        var row = new HorizontalStackPanel { Spacing = MyraStyle.STANDARD_SPACING, VerticalAlignment = Myra.Graphics2D.UI.VerticalAlignment.Center };

        row.Widgets.Add(new MyraLabel(statName, MyraLabel.TextStyle.P) { Width = 90 });

        row.Widgets.Add(MyraCheckButton.CreateWithCallback(enabled, onEnabledChanged, TazLang.Get("autostatlock_autolock")));

        row.Widgets.Add(new MyraLabel(TazLang.Get("autostatlock_desired"), MyraLabel.TextStyle.P));

        var input = new MyraInputBox
        {
            Text = desired.ToString(),
            Width = 60,
        };
        input.TextChangedByUser += (_, _) =>
        {
            if (ushort.TryParse(input.Text, out ushort v))
                onDesiredChanged(v);
        };
        row.Widgets.Add(input);

        return row;
    }
}
