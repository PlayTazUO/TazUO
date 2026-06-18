#nullable enable
using System;
using ClassicUO.Configuration;
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

        SDL.SDL_Keycode capturedKey = SDL.SDL_Keycode.SDLK_UNKNOWN;
        SDL.SDL_Keymod capturedMod = SDL.SDL_Keymod.SDL_KMOD_NONE;
        Action? unsubscribe = null;

        var root = new VerticalStackPanel { Spacing = 6 };

        root.Widgets.Add(new MyraLabel("Self Heal", MyraLabel.TextStyle.H2));
        root.Widgets.Add(new MyraLabel(
            "Hold the bound key to cast Heal on yourself (Cure when poisoned). Release to stop.",
            MyraLabel.TextStyle.P));

        root.Widgets.Add(MyraCheckButton.CreateWithCallback(
            profile.SelfHeal_Enabled,
            b => profile.SelfHeal_Enabled = b,
            "Enable self heal", "Enable or disable the hold-to-heal hotkey"));

        string KeyDisplay()
        {
            if (profile.SelfHeal_Key == 0) return "None";
            string s = KeysTranslator.TryGetKey((SDL.SDL_Keycode)profile.SelfHeal_Key, (SDL.SDL_Keymod)profile.SelfHeal_Mod);
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
                (capturedKey, capturedMod) = ParseHotKeyString(hotkey);
                keyLabel.Text = "Press a key... " + KeysTranslator.TryGetKey(capturedKey, capturedMod);
            }

            Keyboard.KeyDownEvent += Handler;
            unsubscribe = () => Keyboard.KeyDownEvent -= Handler;
        }));
        normalPanel.Widgets.Add(new MyraButton("Clear", () =>
        {
            profile.SelfHeal_Key = 0;
            profile.SelfHeal_Mod = 0;
            keyLabel.Text = "Hotkey: " + KeyDisplay();
        }));

        editPanel.Widgets.Add(new MyraButton("Apply", () =>
        {
            if (capturedKey != SDL.SDL_Keycode.SDLK_UNKNOWN)
            {
                profile.SelfHeal_Key = (int)capturedKey;
                profile.SelfHeal_Mod = (int)capturedMod;
            }
            StopListening();
        }));
        editPanel.Widgets.Add(new MyraButton("Cancel", StopListening));

        root.Widgets.Add(normalPanel);
        root.Widgets.Add(editPanel);

        // Cure recheck delay: how long to wait for poison to clear before recasting Cure.
        root.Widgets.Add(new MyraLabel("Cure recheck delay (ms):", MyraLabel.TextStyle.P));
        root.Widgets.Add(MyraHSlider.SliderWithLabel(
            "ms before recasting Cure if still poisoned",
            out _,
            v => profile.SelfHeal_CureVerifyMs = (int)v,
            min: 100, max: 2000, value: profile.SelfHeal_CureVerifyMs));

        // Interrupt retry delay: how fast to recast after a cast is disrupted (e.g. by damage).
        root.Widgets.Add(new MyraLabel("Interrupt retry delay (ms):", MyraLabel.TextStyle.P));
        root.Widgets.Add(MyraHSlider.SliderWithLabel(
            "ms before recasting after a cast is interrupted",
            out _,
            v => profile.SelfHeal_InterruptRetryMs = (int)v,
            min: 0, max: 1000, value: profile.SelfHeal_InterruptRetryMs));

        return root;
    }

    private static (SDL.SDL_Keycode key, SDL.SDL_Keymod mod) ParseHotKeyString(string hotkey)
    {
        SDL.SDL_Keycode key = SDL.SDL_Keycode.SDLK_UNKNOWN;
        SDL.SDL_Keymod mod = SDL.SDL_Keymod.SDL_KMOD_NONE;

        if (string.IsNullOrEmpty(hotkey))
            return (key, mod);

        foreach (string part in hotkey.Split('+'))
        {
            switch (part.ToUpperInvariant())
            {
                case "CTRL":  mod |= SDL.SDL_Keymod.SDL_KMOD_CTRL;  break;
                case "SHIFT": mod |= SDL.SDL_Keymod.SDL_KMOD_SHIFT; break;
                case "ALT":   mod |= SDL.SDL_Keymod.SDL_KMOD_ALT;   break;
                default:
                    if (Enum.TryParse<SDL.SDL_Keycode>(part, true, out SDL.SDL_Keycode parsed))
                        key = parsed;
                    break;
            }
        }

        return (key, mod);
    }
}
