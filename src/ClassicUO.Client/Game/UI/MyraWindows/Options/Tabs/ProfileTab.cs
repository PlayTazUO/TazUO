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

        (List<string> allLocations, List<string> sameServerLocations) = GetProfileLocations();

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = TazLang.Get("mog_tazuo_settingstransfers") },
            Option.Custom(() => new MyraLabel(TazLang.Get("mog_tazuo_settingswarning", [allLocations.Count.ToString()]), MyraLabel.TextStyle.P)),
            Option.Button(
                TazLang.Get("mog_tazuo_overrideall", [(allLocations.Count - 1).ToString()]),
                () => OverrideAllProfiles(allLocations),
                new SearchMetadata(TazLang.Get("mog_tazuo_overrideall"), Keywords: [TazLang.Get("mog_kw_profile"), TazLang.Get("mog_kw_override")])
            ),
            Option.Button(
                TazLang.Get("mog_tazuo_overridesame", [(sameServerLocations.Count - 1).ToString()]),
                () => OverrideAllProfiles(sameServerLocations),
                new SearchMetadata(TazLang.Get("mog_tazuo_overridesame"), Keywords: [TazLang.Get("mog_kw_profile"), TazLang.Get("mog_kw_override")])
            ),
            Option.Button(
                TazLang.Get("mog_tazuo_overrideallmacros", [(allLocations.Count - 1).ToString()]),
                () => OverrideAllMacros(allLocations),
                new SearchMetadata(TazLang.Get("mog_tazuo_overrideallmacros"), Keywords: [TazLang.Get("mog_kw_override")])
            ),
            Option.Button(
                TazLang.Get("mog_tazuo_setasdefault"),
                SetProfileAsDefault,
                new SearchMetadata(TazLang.Get("mog_tazuo_setasdefault"), Keywords: [TazLang.Get("mog_kw_profile")])
            ),
            Option.Button(
                TazLang.Get("mog_tazuo_setmacrosasdefault"),
                SetMacrosAsDefault,
                new SearchMetadata(TazLang.Get("mog_tazuo_setmacrosasdefault"))
            )
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_tazuo_settingstransfers"), [TazLang.Get("mog_kw_profile")]));
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
            TazLang.Get("mog_tazuo_setasdefaultsuccess"),
            Constants.HUE_SUCCESS,
            MessageType.System
        );
    }

    private static void SetMacrosAsDefault()
    {
        World.Instance.Macros.Save(Path.Combine(ProfileManager.RootPath, "macros.xml"));
        GameActions.Print(
            World.Instance,
            TazLang.Get("mog_tazuo_setmacrosasdefaultsuccess"),
            Constants.HUE_SUCCESS,
            MessageType.System
        );
    }

    private static void PrintOverrideSuccess(int count) =>
        GameActions.Print(
            World.Instance,
            TazLang.Get("mog_tazuo_overridesuccess", [count.ToString()]),
            Constants.HUE_SUCCESS,
            MessageType.System
            );
}
