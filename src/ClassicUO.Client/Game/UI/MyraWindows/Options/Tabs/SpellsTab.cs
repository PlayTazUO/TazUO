using System;
using System.Net.Http;
using System.Threading.Tasks;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Managers.SpellVisualRange;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class SpellsTab
{
    internal static OptionItem GetContent()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        return new OptionItem(lang.ButtonCombatSpells, GetSection);
    }

    private static WrapPanel GetSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.SpellsTabLang lang = Language.Instance.GetModernOptionsGumpLanguage.SpellsTab;

        return OptionTabCommons.StyledVerticalWrapPanel(
            new CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.EnabledSpellFormat), lang.EnableOverheadSpellFormat),
                OptionsFactory.CreateInputField(lang.SpellOverheadFormat, profile.SpellDisplayFormat, s => profile.SpellDisplayFormat = s)
            ),
            OptionsFactory.CreateCheckboxOption(lang.EnableOverheadSpellHue, new Accessor<bool>(() => profile.EnabledSpellHue)),
            OptionsFactory.CreateCheckboxOption(lang.SingleClickForSpellIcons, new Accessor<bool>(() => profile.CastSpellsByOneClick)),
            OptionsFactory.CreateCheckboxOption(lang.EnableFastSpellHotkeyAssigning, new Accessor<bool>(() => profile.FastSpellsAssign)),
            OptionsFactory.PropBoundSliderOption(lang.SpellIconScale, new Accessor<int>(() => profile.SpellIconScale), 50, 300),
            new CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.SpellIcon_DisplayHotkey), lang.DisplayMatchingHotkeysOnSpellIcons),
                OptionsFactory.PropBoundHuePicker(lang.HotkeyTextHue, new Accessor<ushort>(() => profile.SpellIcon_HotkeyHue))
            ),
            new VisualContainer(
                new VisualContainerProps { LabelText = lang.SpellIndicators },
                OptionsFactory.CreateCheckboxOption(lang.EnableSpellIndicators, new Accessor<bool>(() => profile.EnableSpellIndicators)),
                new MyraButton(lang.ImportIndicatorsFromUrl, OpenConfigDownloadModal)
            )
        );
    }

    private static void OpenConfigDownloadModal()
    {
        UiCommonsLanguage uiLang = Language.Instance.UiCommons;
        ModernOptionsGumpLanguage.SpellsTabLang lang = Language.Instance.GetModernOptionsGumpLanguage.SpellsTab;
        UIManager.Add
        (
            new PromptPopupWindow
            (
                lang.ImportIndicatorsFromUrl,
                lang.SpellIndicatorsDownloadPrompt,
                url => _ = OnDownloadConfirmed(url),
                uiLang.Download,
                uiLang.Cancel,
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
