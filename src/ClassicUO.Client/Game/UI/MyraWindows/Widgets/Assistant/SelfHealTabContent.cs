#nullable enable
using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Managers.Hotkeys;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Input;
using Myra.Graphics2D.UI;
using SDL3;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Assistant;

public static class SelfHealTabContent
{
    public static Widget Build()
    {
        Profile profile = ProfileManager.CurrentProfile;

        // The self-heal hotkey now lives in the central registry; this row edits the same entry
        // surfaced in the Hotkeys tab. Mirror writes back to the legacy Profile fields so the
        // import seed stays consistent.
        HotKeyEntry entry = HotKeys.Get(SelfHealManager.SelfHealHotkeyId) ?? SelfHealManager.RegisterHotkey();

        SDL.SDL_Keycode capturedKey = SDL.SDL_Keycode.SDLK_UNKNOWN;
        SDL.SDL_Keymod capturedMod = SDL.SDL_Keymod.SDL_KMOD_NONE;
        Action? unsubscribe = null;

        // The recast + grace sliders are auto-filled from FC/FCR (and stay manually editable).
        LabeledHorizontalSlider recastSlider = null!;
        LabeledHorizontalSlider graceSlider = null!;

        void ApplyFcFcr()
        {
            var (recast, grace) = SelfHealTimings.Compute(
                profile.SelfHeal_UseChivalry, profile.SelfHeal_FC, profile.SelfHeal_FCR);
            profile.SelfHeal_RecastDelayMs = recast;
            profile.SelfHeal_CastStartGraceMs = grace;
            recastSlider.Value = recast;   // programmatic set -> updates the label, not ValueChangedByUser
            graceSlider.Value = grace;
        }

        var root = new VerticalStackPanel { Spacing = 6 };

        root.Widgets.Add(new MyraLabel("Self Heal", MyraLabel.TextStyle.H2));
        root.Widgets.Add(new MyraLabel(
            "Hold the bound key to heal yourself (cure when poisoned). Release to stop.",
            MyraLabel.TextStyle.P));

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.SelfHeal_Enabled,
            b => profile.SelfHeal_Enabled = b,
            "Enable self heal", "Enable or disable the hold-to-heal hotkey"));

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.SelfHeal_UseChivalry,
            b => { profile.SelfHeal_UseChivalry = b; ApplyFcFcr(); },   // school changes recovery base + caps
            "Use Chivalry (Close Wounds / Cleanse by Fire)",
            "Off = Magery (Heal / Cure). On = Chivalry: Close Wounds to heal, Cleanse by Fire to cure poison."));

        string KeyDisplay()
        {
            if (entry.Binding.IsEmpty) return "None";
            string s = entry.Binding.Describe();
            return string.IsNullOrEmpty(s) ? "None" : s;
        }

        var keyLabel = new MyraLabel("Hotkey: " + KeyDisplay(), MyraLabel.TextStyle.P);
        root.Widgets.Add(keyLabel);

        var normalPanel = new HorizontalStackPanel { Spacing = 4 };
        var editPanel = new HorizontalStackPanel { Spacing = 4, Visible = false };

        void StopListening()
        {
            capturedKey = SDL.SDL_Keycode.SDLK_UNKNOWN;
            capturedMod = SDL.SDL_Keymod.SDL_KMOD_NONE;
            unsubscribe?.Invoke();
            unsubscribe = null;
            normalPanel.Visible = true;
            editPanel.Visible = false;
            keyLabel.Text = "Hotkey: " + KeyDisplay();
        }

        normalPanel.Widgets.Add(new MyraButton("Set", () =>
        {
            StopListening();
            capturedKey = SDL.SDL_Keycode.SDLK_UNKNOWN;
            capturedMod = SDL.SDL_Keymod.SDL_KMOD_NONE;
            normalPanel.Visible = false;
            editPanel.Visible = true;
            keyLabel.Text = "Press a key...";

            void Handler(string hotkey)
            {
                (capturedKey, capturedMod) = HotkeyUtil.ParseHotKeyString(hotkey);
                keyLabel.Text = "Press a key... " + KeysTranslator.TryGetKey(capturedKey, capturedMod);
            }

            Keyboard.KeyDownEvent += Handler;
            unsubscribe = () => Keyboard.KeyDownEvent -= Handler;
        }));
        normalPanel.Widgets.Add(new MyraButton("Clear", () =>
        {
            entry.Binding.Clear();
            profile.SelfHeal_Key = 0;
            profile.SelfHeal_Mod = 0;
            keyLabel.Text = "Hotkey: " + KeyDisplay();
        }));

        editPanel.Widgets.Add(new MyraButton("Apply", () =>
        {
            if (capturedKey != SDL.SDL_Keycode.SDLK_UNKNOWN)
            {
                entry.Binding = new HotkeyBinding(capturedKey, capturedMod);
                profile.SelfHeal_Key = (int)capturedKey;
                profile.SelfHeal_Mod = (int)capturedMod;
            }
            StopListening();
        }));
        editPanel.Widgets.Add(new MyraButton("Cancel", StopListening));

        root.Widgets.Add(normalPanel);
        root.Widgets.Add(editPanel);

        // Faster Casting / Recovery -> auto-computes the recast & grace below for your school.
        root.Widgets.Add(new MyraLabel("Your FC / FCR (auto-sets recast & grace):", MyraLabel.TextStyle.P));
        root.Widgets.Add(LabeledHorizontalSlider.SliderWithLabel(
            "Faster Casting (FC) - Magery caps at 2, Chivalry at 4 (2 if you have Magery/Mysticism 70+)",
            out _,
            v => { profile.SelfHeal_FC = (int)v; ApplyFcFcr(); },
            min: 0, max: 4, value: profile.SelfHeal_FC));
        root.Widgets.Add(LabeledHorizontalSlider.SliderWithLabel(
            "Faster Cast Recovery (FCR) - Magery caps at 6, Chivalry at 7",
            out _,
            v => { profile.SelfHeal_FCR = (int)v; ApplyFcFcr(); },
            min: 0, max: 7, value: profile.SelfHeal_FCR));

        // Recast delay ("recuperation"): pad after a successful heal before the next cast. Auto from FCR.
        root.Widgets.Add(new MyraLabel("Recast delay / recuperation (ms):", MyraLabel.TextStyle.P));
        root.Widgets.Add(LabeledHorizontalSlider.SliderWithLabel(
            "ms pad after a heal before recasting (auto from FCR; lower = faster)",
            out recastSlider,
            v => profile.SelfHeal_RecastDelayMs = (int)v,
            min: 0, max: 2000, value: profile.SelfHeal_RecastDelayMs));

        // Cast start grace: how long a cast may take to register before treating it as failed. Auto from FC.
        root.Widgets.Add(new MyraLabel("Cast start grace (ms):", MyraLabel.TextStyle.P));
        root.Widgets.Add(LabeledHorizontalSlider.SliderWithLabel(
            "ms to wait for a cast to register before retrying (auto from FC; bounds the post-interrupt stall)",
            out graceSlider,
            v => profile.SelfHeal_CastStartGraceMs = (int)v,
            min: 200, max: 3000, value: profile.SelfHeal_CastStartGraceMs));

        // Cure recheck delay: how long to wait for poison to clear before recasting Cure.
        root.Widgets.Add(new MyraLabel("Cure recheck delay (ms):", MyraLabel.TextStyle.P));
        root.Widgets.Add(LabeledHorizontalSlider.SliderWithLabel(
            "ms before recasting Cure if still poisoned",
            out _,
            v => profile.SelfHeal_CureVerifyMs = (int)v,
            min: 100, max: 2000, value: profile.SelfHeal_CureVerifyMs));

        // Interrupt retry delay: how fast to recast after a cast is disrupted (e.g. by damage).
        root.Widgets.Add(new MyraLabel("Interrupt retry delay (ms):", MyraLabel.TextStyle.P));
        root.Widgets.Add(LabeledHorizontalSlider.SliderWithLabel(
            "ms before recasting after a cast is interrupted",
            out _,
            v => profile.SelfHeal_InterruptRetryMs = (int)v,
            min: 0, max: 1000, value: profile.SelfHeal_InterruptRetryMs));

        return root;
    }
}
