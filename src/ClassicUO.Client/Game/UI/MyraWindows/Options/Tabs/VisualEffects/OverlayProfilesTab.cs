#nullable enable

using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Effects;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;
using ClassicUO.Game.ScreenDecorations.Manager;
using ClassicUO.Game.UI.MyraWindows.Options.Editors.Overlays;
using ClassicUO.Game.UI.MyraWindows.Options.Editors.Profile;
using Myra.Graphics2D.UI;
using DecorationSettings = ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.ScreenDecorations;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs.VisualEffects;

/// <summary>
/// The library of looks. One flat pool shared by every rule: a profile is not owned by the effect it
/// happens to be named after, so editing one changes every rule pointing at it.
/// </summary>
internal static class OverlayProfilesTab
{
    #region Private members

    private static string ProfileKeywords => TazLang.Get("visualeffects_kw_profile");

    #endregion

    #region Internal methods

    /// <summary>Returns the profile library and composer as an option source.</summary>
    /// <returns>The option source.</returns>
    internal static IOptionSource GetContent()
    {
        string label = TazLang.Get("visualeffects_profiles", "Profiles");

        // One navigate-only entry for the whole library: the composer doesn't render meaningfully in
        // a result row, and an unnamed group contributes nothing to the index at all - which left
        // the library unreachable by search.
        return OptionsUi.Vertical(Option.Custom(BuildEditor))
            .AsSearchGroup()
            .AsLinkOnly()
            .WithSearch(new SearchMetadata(BuildMatchText(label), Keywords: [ProfileKeywords]) { Label = label });
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Match fuel for the single entry the library contributes: the name of every look it holds,
    /// shipped and authored alike, so searching for an effect by name reaches the page that edits
    /// it. Never displayed, which is what lets it be a blob.
    /// </summary>
    /// <param name="label">The tab's own name, so the entry answers to it too.</param>
    /// <returns>The match text.</returns>
    private static string BuildMatchText(string label)
    {
        IEnumerable<string> names = DecorationSettings.Current.Overlays.AllProfiles().Select(profile => profile.Name);
        return string.Join(' ', names.Prepend(label));
    }

    private static Widget BuildEditor()
    {
        OverlaySystemSettings overlays = DecorationSettings.Current.Overlays;
        List<EffectProfile> library = [.. overlays.AllProfiles()];

        return new ProfileEditor<EffectProfile>(
            profile => new OverlayProfileEditor(profile, Commit),
            (name, source) => Create(overlays, name, source),
            profile => Delete(overlays, profile),
            library,
            _ => Commit(),
            newestFirst: true
        );
    }

    /// <summary>
    /// Adds a look to the library, either fresh or as a duplicate of an existing one.
    /// <para>
    /// A new one starts from a single flat tint rather than from nothing: an empty stack draws
    /// nothing at all, which reads as the editor being broken rather than as a blank canvas. A copy
    /// is the user's outright even when taken from a shipped look - <see cref="EffectProfile.Clone"/>
    /// mints a fresh id and drops the built-in marking - so rules pointing at the original go on
    /// pointing at it.
    /// </para>
    /// </summary>
    /// <param name="overlays">The pool to add to.</param>
    /// <param name="name">Name offered by the profile editor.</param>
    /// <param name="source">The look to duplicate, or null for a fresh one.</param>
    /// <returns>The stored profile.</returns>
    private static EffectProfile Create(OverlaySystemSettings overlays, string name, EffectProfile? source)
    {
        EffectProfile created = source?.Clone(name)
                                ?? new EffectProfile
                                {
                                    Name = name,
                                    Layers = [new ProfileLayer { Effect = new TintEffect() }]
                                };

        overlays.AddProfile(created);
        Commit();

        return created;
    }

    /// <summary>
    /// Removes a look. Rules still pointing at it are left alone rather than repaired: they report
    /// the missing profile and stop running, which is visible, where silently re-pointing them at
    /// something else would not be.
    /// </summary>
    /// <param name="overlays">The pool to remove from.</param>
    /// <param name="profile">The profile being deleted.</param>
    private static void Delete(OverlaySystemSettings overlays, EffectProfile profile)
    {
        // A deleted profile that's still previewing would otherwise keep drawing: the toggle that
        // would normally turn it off no longer has a widget to live on.
        ScreenOverlayManager.Instance.SetPreview(profile.Id, false);

        overlays.Profiles.Remove(profile);
        ScreenOverlayManager.Instance.RulesChanged();
        Commit();
    }

    /// <summary>Persists the library and re-applies whatever is on screen.</summary>
    private static void Commit()
    {
        DecorationSettings.Current.Save();
        ScreenOverlayManager.Instance.ProfilesChanged();
    }

    #endregion
}
