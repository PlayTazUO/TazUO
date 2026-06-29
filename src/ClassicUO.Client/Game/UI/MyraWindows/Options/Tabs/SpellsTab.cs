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
        ModernOptionsGumpLanguage.SpellsTabLang lang = Language.Instance.GetModernOptionsGumpLanguage.CombatTab.Spells;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

        return OptionsUi.Vertical(
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.EnabledSpellFormat), lang.EnableOverheadSpellFormat),
                Option.InputField(
                    lang.SpellOverheadFormat,
                    new Accessor<string>(() => profile.SpellDisplayFormat, s => profile.SpellDisplayFormat = s),
                    search: new SearchMetadata(lang.SpellOverheadFormat, Keywords: [kw.Format])
                )
            ).WithSearch(new SearchMetadata(Keywords: [kw.Format])),
            Option.Checkbox(
                lang.EnableOverheadSpellHue,
                new Accessor<bool>(() => profile.EnabledSpellHue),
                search: new SearchMetadata(lang.EnableOverheadSpellHue, Keywords: [kw.Hue, kw.Color])
            ),
            Option.Checkbox(
                lang.SingleClickForSpellIcons,
                new Accessor<bool>(() => profile.CastSpellsByOneClick),
                search: new SearchMetadata(lang.SingleClickForSpellIcons, Keywords: [kw.Click, kw.Cast])
            ),
            Option.Checkbox(
                lang.EnableFastSpellHotkeyAssigning,
                new Accessor<bool>(() => profile.FastSpellsAssign),
                search: new SearchMetadata(lang.EnableFastSpellHotkeyAssigning, Keywords: [kw.Hotkey, kw.Assign])
            ),
            Option.Slider(
                lang.SpellIconScale, 50, 300, new Accessor<float>(() => profile.SpellIconScale, f => profile.SpellIconScale = (int)f),
                search: new SearchMetadata(lang.SpellIconScale, Keywords: [kw.Scale, kw.Size])
            ),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.SpellIcon_DisplayHotkey), lang.DisplayMatchingHotkeysOnSpellIcons),
                Option.HuePicker(
                    lang.HotkeyTextHue,
                    new Accessor<ushort>(() => profile.SpellIcon_HotkeyHue, h => profile.SpellIcon_HotkeyHue = h),
                    search: new SearchMetadata(lang.HotkeyTextHue, Keywords: [kw.Color, kw.Hue])
                )
            ).WithSearch(new SearchMetadata(Keywords: [kw.Hotkey])),
            OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = lang.SpellIndicators },
                Option.Checkbox(
                    lang.EnableSpellIndicators,
                    new Accessor<bool>(() => profile.EnableSpellIndicators),
                    search: new SearchMetadata(lang.EnableSpellIndicators)
                ),
                Option.Button(
                    lang.ImportIndicatorsFromUrl,
                    OpenConfigDownloadModal,
                    search: new SearchMetadata(lang.ImportIndicatorsFromUrl, Keywords: [kw.Import, kw.Download])
                )
            )
        ).WithSearch(new SearchMetadata(lang.SpellLabel, Tags: [kw.Spell, kw.Magic]));
    }

    private static void OpenConfigDownloadModal()
    {
        ModernOptionsGumpLanguage.SpellsTabLang lang = Language.Instance.GetModernOptionsGumpLanguage.SpellsTab;
        UIManager.Add
        (
            new PromptPopupWindow
            (
                lang.ImportIndicatorsFromUrl,
                lang.SpellIndicatorsDownloadPrompt,
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
        ModernOptionsGumpLanguage.TazUO tuoLang = Language.Instance.GetModernOptionsGumpLanguage.GetTazUO;

        if (string.IsNullOrWhiteSpace(url))
            return;

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            return;

        GameActions.Print(World.Instance, tuoLang.AttemptingToDownloadSpellConfig);

        try
        {
            // ReSharper disable once ShortLivedHttpClient
            using var httpClient = new HttpClient();
            string fetchResult = await httpClient.GetStringAsync(uri);

            if (SpellVisualRangeManager.Instance.LoadFromString(fetchResult))
                GameActions.Print(World.Instance, tuoLang.SuccesfullyDownloadedNewSpellConfig);
            else
            {
                string message = string.Format(tuoLang.FailedToDownloadTheSpellConfigExMessage, tuoLang.FailedToLoadSpellConfigMessage);
                GameActions.Print(World.Instance, message, Constants.HUE_WARN);
            }
        }
        catch (Exception ex)
        {
            GameActions.Print(World.Instance, string.Format(tuoLang.FailedToDownloadTheSpellConfigExMessage, ex.Message));
        }
    }
}
