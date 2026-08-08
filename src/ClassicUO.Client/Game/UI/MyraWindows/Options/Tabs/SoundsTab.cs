using ClassicUO.Common;
using ClassicUO.Common.Enums;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Input;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for audio settings (master volume, music, ambient, and footstep sounds)</summary>
public static class SoundsTab
{
    /// <summary>Returns the option fragment for sound enable/disable and volume controls</summary>
    internal static IOptionSource GetContent() => GetSection();

    private static OptionFragment GetSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        string playRainSound = TazLang.Get("sound_play_rain", "Play rain sound");

        return OptionsUi.Vertical(
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => ProfileManager.GlobalSettings.EnableSound), TazLang.Get("mog_sound_enablesound")),
                Option.Slider(
                    TazLang.Get("mog_sound_sharedvolume"),
                    0,
                    100,
                    new Accessor<int>(() => ProfileManager.GlobalSettings.SoundVolume),
                    search: new SearchMetadata(TazLang.Get("mog_sound_sharedvolume"), Keywords: [TazLang.Get("mog_kw_volume")])
                )
            ).WithSearch(new SearchMetadata(TazLang.Get("mog_sound_enablesound"), Keywords: [TazLang.Get("mog_kw_sound")])),
            Option.Spacer(),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => ProfileManager.GlobalSettings.EnableMusic), TazLang.Get("mog_sound_enablemusic")),
                Option.Slider(
                    TazLang.Get("mog_sound_sharedvolume"),
                    0,
                    100,
                    new Accessor<int>(() => ProfileManager.GlobalSettings.MusicVolume),
                    search: new SearchMetadata(TazLang.Get("mog_sound_sharedvolume"), Keywords: [TazLang.Get("mog_kw_music"), TazLang.Get("mog_kw_volume")])
                )
            ).WithSearch(new SearchMetadata(TazLang.Get("mog_sound_enablemusic"), Keywords: [TazLang.Get("mog_kw_music")])),
            Option.Spacer(),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => Settings.GlobalSettings.LoginMusic), TazLang.Get("mog_sound_loginmusic")),
                Option.Slider(
                    TazLang.Get("mog_sound_sharedvolume"),
                    0,
                    100,
                    new Accessor<int>(() => Settings.GlobalSettings.LoginMusicVolume),
                    search: new SearchMetadata(TazLang.Get("mog_sound_sharedvolume"), Keywords: [TazLang.Get("mog_kw_login"), TazLang.Get("mog_kw_volume")])
                )
            ).WithSearch(new SearchMetadata(TazLang.Get("mog_sound_loginmusic"), Keywords: [TazLang.Get("mog_kw_login"), TazLang.Get("mog_kw_music")])),
            Option.Spacer(),
            Option.Checkbox(
                TazLang.Get("mog_sound_playfootsteps"),
                new Accessor<bool>(() => ProfileManager.GlobalSettings.EnableFootstepsSound),
                search: new SearchMetadata(TazLang.Get("mog_sound_playfootsteps"), Keywords: [TazLang.Get("mog_kw_footstep")])
            ),
            Option.Checkbox(
                playRainSound,
                new Accessor<bool>(() => ProfileManager.GlobalSettings.EnableRainSound),
                search: new SearchMetadata(playRainSound, Keywords: [TazLang.Get("mog_kw_rain")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_sound_combatmusic"),
                new Accessor<bool>(() => ProfileManager.GlobalSettings.EnableCombatMusic),
                search: new SearchMetadata(TazLang.Get("mog_sound_combatmusic"), Keywords: [TazLang.Get("mog_kw_combat"), TazLang.Get("mog_kw_music")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_sound_backgroundmusic"),
                new Accessor<bool>(() => ProfileManager.GlobalSettings.ReproduceSoundsInBackground),
                search: new SearchMetadata(TazLang.Get("mog_sound_backgroundmusic"), Keywords: [TazLang.Get("mog_kw_background"), TazLang.Get("mog_kw_music")])
            ),
            Option.Spacer(),
            OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = TazLang.Get("mog_soundtab_voicetotext") },
                Option.Button(
                    TazLang.Get("mog_soundtab_createvoicebutton"),
                    OnCreateVoiceButtonClick,
                    new SearchMetadata(TazLang.Get("mog_soundtab_createvoicebutton"), Keywords: [TazLang.Get("mog_kw_voice")])
                ),
                Option.InputField(
                    TazLang.Get("mog_tazuo_voicemodelpath"),
                    new Accessor<string>(() => profile.VoiceModelPath, s => profile.VoiceModelPath = s),
                    TazLang.Get("mog_tazuo_voicemodelpathtooltip"),
                    new SearchMetadata(TazLang.Get("mog_tazuo_voicemodelpath"), Keywords: [TazLang.Get("mog_kw_voice"), TazLang.Get("mog_kw_model")])
                )
            )
        ).WithSearch(new SearchMetadata(
            TazLang.Get("mog_soundtab_label"),
            [TazLang.Get("mog_kw_sound"), TazLang.Get("mog_kw_audio")],
            [TazLang.Get("mog_kw_sound"), TazLang.Get("mog_kw_audio"), TazLang.Get("mog_kw_music"), TazLang.Get("mog_kw_volume")])
        );
    }

    private static void OnCreateVoiceButtonClick()
    {

        var macroManager = MacroManager.TryGetMacroManager(World.Instance);
        if (macroManager == null)
            return;

        var macro = Macro.CreateFastMacro(TazLang.Get("mog_tazuo_voicetoggle"), MacroType.ToggleVoiceRecognition, MacroSubType.MSC_NONE);
        macroManager.PushToBack(macro);
        UIManager.Add(new MacroButtonGump(World.Instance, macro, Mouse.Position.X, Mouse.Position.Y));
    }
}
