using System.Collections.Generic;
using System.IO;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI.MyraWindows.Widgets;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for profile management utilities (import settings from other characters,
/// set defaults, and copy macros)</summary>
public static class ProfileTab
{
    // Char-scope keys ({server}_{username}_{serial}) offered in the import dropdown, plus the selected index.
    private static List<string> _importScopes = new();
    private static int _selectedImportIndex;

    /// <summary>Returns the option fragment with profile transfer helpers</summary>
    internal static IOptionSource GetContent() => GetSection();

    private static OptionFragment GetSection()
    {
        ModernOptionsGumpLanguage.TazUO lang = Language.Instance.GetModernOptionsGumpLanguage.GetTazUO;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

        List<string> allLocations = GetProfileLocations();

        _importScopes = Client.Settings?.GetCharProfileScopeKeys() ?? new List<string>();
        _selectedImportIndex = 0;

        var children = new List<OptionContent>();

        // Import another character's settings (pulls from the SQLite settings store).
        if (_importScopes.Count > 0)
        {
            children.Add(Option.ComboBox(
                lang.ImportFromProfile,
                0,
                _importScopes.ToArray(),
                i => _selectedImportIndex = i,
                search: new SearchMetadata(lang.ImportFromProfile, Keywords: [kw.Profile])
            ));
            children.Add(Option.Button(
                lang.ImportFromButton,
                ImportFromSelected,
                new SearchMetadata(lang.ImportFromButton, Keywords: [kw.Profile])
            ));
        }
        else
        {
            children.Add(Option.Custom(() => new MyraLabel(lang.NoProfilesToImport, MyraLabel.TextStyle.P)));
        }

        // Copy this profile's macros to other characters / set defaults for new characters.
        children.Add(Option.Button(
            string.Format(lang.OverrideAllMacros, allLocations.Count - 1),
            () => OverrideAllMacros(allLocations),
            new SearchMetadata(lang.OverrideAllMacros, Keywords: [kw.Override])
        ));
        children.Add(Option.Button(
            lang.SetAsDefault,
            SetProfileAsDefault,
            new SearchMetadata(lang.SetAsDefault, Keywords: [kw.Profile])
        ));
        children.Add(Option.Button(
            lang.SetMacrosAsDefault,
            SetMacrosAsDefault,
            new SearchMetadata(lang.SetMacrosAsDefault)
        ));

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = lang.SettingsTransfers },
            children.ToArray()
        ).WithSearch(new SearchMetadata(lang.SettingsTransfers, [kw.Profile]));
    }

    private static List<string> GetProfileLocations()
    {
        var all = new List<string>();

        foreach (string account in Directory.GetDirectories(ProfileManager.RootPath))
        foreach (string server in Directory.GetDirectories(account))
        foreach (string character in Directory.GetDirectories(server))
            all.Add(character);

        return all;
    }

    private static void ImportFromSelected()
    {
        if (_selectedImportIndex < 0 || _selectedImportIndex >= _importScopes.Count)
            return;

        string source = _importScopes[_selectedImportIndex];

        Client.Settings?.ImportSettingsFromScope(source);
        ProfileManager.CurrentProfile?.ReloadCharScopedSettingsFromDatabase();

        GameActions.Print(
            World.Instance,
            string.Format(Language.Instance.GetModernOptionsGumpLanguage.GetTazUO.ImportFromSuccess, source),
            Constants.HUE_SUCCESS,
            MessageType.System
        );
    }

    private static void OverrideAllMacros(List<string> locations)
    {
        foreach (string location in locations)
            World.Instance.Macros.Save(Path.Combine(location, "macros.xml"));

        PrintOverrideSuccess(locations.Count - 1);
    }

    private static void SetProfileAsDefault()
    {
        ProfileManager.SetProfileAsDefault(ProfileManager.CurrentProfile);
        GameActions.Print(
            World.Instance,
            Language.Instance.GetModernOptionsGumpLanguage.GetTazUO.SetAsDefaultSuccess,
            Constants.HUE_SUCCESS,
            MessageType.System
        );
    }

    private static void SetMacrosAsDefault()
    {
        World.Instance.Macros.Save(Path.Combine(ProfileManager.RootPath, "macros.xml"));
        GameActions.Print(
            World.Instance,
            Language.Instance.GetModernOptionsGumpLanguage.GetTazUO.SetMacrosAsDefaultSuccess,
            Constants.HUE_SUCCESS,
            MessageType.System
        );
    }

    private static void PrintOverrideSuccess(int count) =>
        GameActions.Print(
            World.Instance,
            string.Format(Language.Instance.GetModernOptionsGumpLanguage.GetTazUO.OverrideSuccess, count),
            Constants.HUE_SUCCESS,
            MessageType.System
            );
}
