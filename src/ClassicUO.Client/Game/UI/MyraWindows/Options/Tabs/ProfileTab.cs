using System.Collections.Generic;
using System.IO;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for profile management utilities (copy settings to other character profiles)</summary>
public static class ProfileTab
{
    /// <summary>Returns the option fragment with profile-override buttons and transfer helpers</summary>
    internal static IOptionSource GetContent() => GetSection();

    private static OptionFragment GetSection()
    {
        ModernOptionsGumpLanguage.TazUO lang = Language.Instance.GetModernOptionsGumpLanguage.GetTazUO;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

        (List<string> allLocations, List<string> sameServerLocations) = GetProfileLocations();

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = lang.SettingsTransfers },
            Option.Custom(() => new MyraLabel(string.Format(lang.SettingsWarning, allLocations.Count), MyraLabel.TextStyle.P)),
            Option.Button(
                string.Format(lang.OverrideAll, allLocations.Count - 1),
                () => OverrideAllProfiles(allLocations),
                new SearchMetadata(lang.OverrideAll, Keywords: [kw.Profile, kw.Override])
            ),
            Option.Button(
                string.Format(lang.OverrideSame, sameServerLocations.Count - 1),
                () => OverrideAllProfiles(sameServerLocations),
                new SearchMetadata(lang.OverrideSame, Keywords: [kw.Profile, kw.Override])
            ),
            Option.Button(
                string.Format(lang.OverrideAllMacros, allLocations.Count - 1),
                () => OverrideAllMacros(allLocations),
                new SearchMetadata(lang.OverrideAllMacros, Keywords: [kw.Override])
            ),
            Option.Button(
                lang.SetAsDefault,
                SetProfileAsDefault,
                new SearchMetadata(lang.SetAsDefault, Keywords: [kw.Profile])
            ),
            Option.Button(
                lang.SetMacrosAsDefault,
                SetMacrosAsDefault,
                new SearchMetadata(lang.SetMacrosAsDefault)
            )
        ).WithSearch(new SearchMetadata(lang.SettingsTransfers, [kw.Profile]));
    }

    private static (List<string> All, List<string> SameServer) GetProfileLocations()
    {
        Profile profile = ProfileManager.CurrentProfile;
        var all = new List<string>();
        var sameServer = new List<string>();

        foreach (string account in Directory.GetDirectories(ProfileManager.RootPath))
        foreach (string server in Directory.GetDirectories(account))
        foreach (string character in Directory.GetDirectories(server))
        {
            all.Add(character);

            if (FileSystemHelper.RemoveInvalidChars(profile.ServerName) == FileSystemHelper.RemoveInvalidChars(Path.GetFileName(server)))
                sameServer.Add(character);
        }

        return (all, sameServer);
    }

    private static void OverrideAllProfiles(List<string> locations)
    {
        foreach (string location in locations)
            ProfileManager.CurrentProfile.Save(World.Instance, location, false);

        PrintOverrideSuccess(locations.Count - 1);
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
