using System;
using System.Net.Http;
using System.Threading.Tasks;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Managers.SpellVisualRange;
using ClassicUO.Game.UI.MyraWindows.Widgets;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for spell overhead format and visual-range display settings</summary>
public static class SpellsTab
{
    /// <summary>Returns the option fragment for spell-format and visual-range configuration</summary>
    internal static IOptionSource GetContent() => GetSection();

    private static OptionFragment GetSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.Vertical(
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.EnabledSpellFormat), TazLang.Get("mog_combattab_spells_enableoverheadspellformat")),
                Option.InputField(
                    TazLang.Get("mog_combattab_spells_spelloverheadformat"),
                    new Accessor<string>(() => profile.SpellDisplayFormat, s => profile.SpellDisplayFormat = s),
                    search: new SearchMetadata(TazLang.Get("mog_combattab_spells_spelloverheadformat"), Keywords: [TazLang.Get("mog_kw_format")])
                )
            ).WithSearch(new SearchMetadata(Keywords: [TazLang.Get("mog_kw_format")])),
            Option.Checkbox(
                TazLang.Get("mog_combattab_spells_enableoverheadspellhue"),
                new Accessor<bool>(() => profile.EnabledSpellHue),
                search: new SearchMetadata(TazLang.Get("mog_combattab_spells_enableoverheadspellhue"), Keywords: [TazLang.Get("mog_kw_hue"), TazLang.Get("mog_kw_color")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_combattab_spells_singleclickforspellicons"),
                new Accessor<bool>(() => profile.CastSpellsByOneClick),
                search: new SearchMetadata(TazLang.Get("mog_combattab_spells_singleclickforspellicons"), Keywords: [TazLang.Get("mog_kw_click"), TazLang.Get("mog_kw_cast")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_combattab_spells_enablefastspellhotkeyassigning"),
                new Accessor<bool>(() => profile.FastSpellsAssign),
                search: new SearchMetadata(TazLang.Get("mog_combattab_spells_enablefastspellhotkeyassigning"), Keywords: [TazLang.Get("mog_kw_hotkey"), TazLang.Get("mog_kw_assign")])
            ),
            Option.Slider(
                TazLang.Get("mog_combattab_spells_spelliconscale"), 50, 300, new Accessor<float>(() => profile.SpellIconScale, f => profile.SpellIconScale = (int)f),
                search: new SearchMetadata(TazLang.Get("mog_combattab_spells_spelliconscale"), Keywords: [TazLang.Get("mog_kw_scale"), TazLang.Get("mog_kw_size")])
            ),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.SpellIcon_DisplayHotkey), TazLang.Get("mog_combattab_spells_displaymatchinghotkeysonspellicons")),
                Option.HuePicker(
                    TazLang.Get("mog_combattab_spells_hotkeytexthue"),
                    new Accessor<ushort>(() => profile.SpellIcon_HotkeyHue, h => profile.SpellIcon_HotkeyHue = h),
                    search: new SearchMetadata(TazLang.Get("mog_combattab_spells_hotkeytexthue"), Keywords: [TazLang.Get("mog_kw_color"), TazLang.Get("mog_kw_hue")])
                )
            ).WithSearch(new SearchMetadata(Keywords: [TazLang.Get("mog_kw_hotkey")])),
            OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = TazLang.Get("mog_combattab_spells_spellindicators") },
                Option.Checkbox(
                    TazLang.Get("mog_combattab_spells_enablespellindicators"),
                    new Accessor<bool>(() => profile.EnableSpellIndicators),
                    search: new SearchMetadata(TazLang.Get("mog_combattab_spells_enablespellindicators"))
                ),
                Option.Button(
                    TazLang.Get("mog_combattab_spells_importindicatorsfromurl"),
                    OpenConfigDownloadModal,
                    search: new SearchMetadata(TazLang.Get("mog_combattab_spells_importindicatorsfromurl"), Keywords: [TazLang.Get("mog_kw_import"), TazLang.Get("mog_kw_download")])
                )
            )
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_combattab_spells_spelllabel"), Tags: [TazLang.Get("mog_kw_spell"), TazLang.Get("mog_kw_magic")]));
    }

    private static void OpenConfigDownloadModal()
    {
        UIManager.Add
        (
            new PromptPopupWindow
            (
                TazLang.Get("mog_spellstab_importindicatorsfromurl"),
                TazLang.Get("mog_spellstab_spellindicatorsdownloadprompt"),
                url => _ = OnDownloadConfirmed(url),
                TazLang.Get("uicommons_download"),
                TazLang.Get("uicommons_cancel"),
                null,
                "https://github.com/PlayTazUO/TazUO/raw/refs/heads/dev/src/ClassicUO.Client/Game/Managers/DefaultSpellIndicatorConfig.json"
            ) { X = (Client.Game.Window.ClientBounds.Width >> 1) - 50, Y = (Client.Game.Window.ClientBounds.Height >> 1) - 50 }
        );
    }

    private static async Task OnDownloadConfirmed(string url)
    {

        if (string.IsNullOrWhiteSpace(url))
            return;

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            return;

        GameActions.Print(World.Instance, TazLang.Get("mog_tazuo_attemptingtodownloadspellconfig"));

        try
        {
            // ReSharper disable once ShortLivedHttpClient
            using var httpClient = new HttpClient();
            string fetchResult = await httpClient.GetStringAsync(uri);

            if (SpellVisualRangeManager.Instance.LoadFromString(fetchResult))
                GameActions.Print(World.Instance, TazLang.Get("mog_tazuo_succesfullydownloadednewspellconfig"));
            else
            {
                string message = TazLang.Get("mog_tazuo_failedtodownloadthespellconfigexmessage", [TazLang.Get("mog_tazuo_failedtoloadspellconfigmessage")]);
                GameActions.Print(World.Instance, message, Constants.HUE_WARN);
            }
        }
        catch (Exception ex)
        {
            GameActions.Print(World.Instance, TazLang.Get("mog_tazuo_failedtodownloadthespellconfigexmessage", [ex.Message]));
        }
    }
}
