using ClassicUO.Common;
using ClassicUO.Common.Enums;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Input;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class SoundsTab
{
    internal static IOptionSource GetContent() => GetSection();

    private static OptionFragment GetSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.SoundTabLang soundLang = lang.SoundTab;
        ModernOptionsGumpLanguage.Sound soundSubLang = lang.GetSound;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return OptionsUi.Vertical(
            Option.Checkbox(
                soundSubLang.EnableSound,
                new Accessor<bool>(() => profile.EnableSound),
                search: new SearchMetadata(soundSubLang.EnableSound, Keywords: [kw.Sound])
            ),
            Option.Slider(
                soundSubLang.SharedVolume,
                0,
                100,
                new Accessor<int>(() => profile.SoundVolume),
                search: new SearchMetadata(soundSubLang.SharedVolume, Keywords: [kw.Volume])
            ),
            Option.Spacer(),
            Option.Checkbox(
                soundSubLang.EnableMusic,
                new Accessor<bool>(() => profile.EnableMusic),
                search: new SearchMetadata(soundSubLang.EnableMusic, Keywords: [kw.Music])
            ),
            Option.Slider(
                soundSubLang.SharedVolume,
                0,
                100,
                new Accessor<int>(() => profile.MusicVolume),
                search: new SearchMetadata(soundSubLang.SharedVolume, Keywords: [kw.Music, kw.Volume])
            ),
            Option.Spacer(),
            Option.Checkbox(
                soundSubLang.LoginMusic,
                new Accessor<bool>(() => Settings.GlobalSettings.LoginMusic),
                search: new SearchMetadata(soundSubLang.LoginMusic, Keywords: [kw.Login, kw.Music])
            ),
            Option.Slider(
                soundSubLang.SharedVolume,
                0,
                100,
                new Accessor<int>(() => Settings.GlobalSettings.LoginMusicVolume),
                search: new SearchMetadata(soundSubLang.SharedVolume, Keywords: [kw.Login, kw.Volume])
            ),
            Option.Spacer(),
            Option.Checkbox(
                soundSubLang.PlayFootsteps,
                new Accessor<bool>(() => profile.EnableFootstepsSound),
                search: new SearchMetadata(soundSubLang.PlayFootsteps, Keywords: [kw.Footstep])
            ),
            Option.Checkbox(
                soundSubLang.CombatMusic,
                new Accessor<bool>(() => profile.EnableCombatMusic),
                search: new SearchMetadata(soundSubLang.CombatMusic, Keywords: [kw.Combat, kw.Music])
            ),
            Option.Checkbox(
                soundSubLang.BackgroundMusic,
                new Accessor<bool>(() => profile.ReproduceSoundsInBackground),
                search: new SearchMetadata(soundSubLang.BackgroundMusic, Keywords: [kw.Background, kw.Music])
            ),
            Option.Spacer(),
            OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = soundLang.VoiceToText },
                Option.Button(
                    soundLang.CreateVoiceButton,
                    OnCreateVoiceButtonClick,
                    new SearchMetadata(soundLang.CreateVoiceButton, Keywords: [kw.Voice])
                ),
                Option.InputField(
                    lang.GetTazUO.VoiceModelPath,
                    new Accessor<string>(() => profile.VoiceModelPath, s => profile.VoiceModelPath = s),
                    lang.GetTazUO.VoiceModelPathTooltip,
                    new SearchMetadata(lang.GetTazUO.VoiceModelPath, Keywords: [kw.Voice, kw.Model])
                )
            )
        ).WithSearch(new SearchMetadata(soundLang.Label, [soundLang.Tags], [soundLang.Keywords]));
    }

    private static void OnCreateVoiceButtonClick()
    {
        ModernOptionsGumpLanguage.TazUO tuoLang = Language.Instance.GetModernOptionsGumpLanguage.GetTazUO;

        var macroManager = MacroManager.TryGetMacroManager(World.Instance);
        if (macroManager == null)
            return;

        var macro = Macro.CreateFastMacro(tuoLang.VoiceToggle, MacroType.ToggleVoiceRecognition, MacroSubType.MSC_NONE);
        macroManager.PushToBack(macro);
        UIManager.Add(new MacroButtonGump(World.Instance, macro, Mouse.Position.X, Mouse.Position.Y));
    }
}
