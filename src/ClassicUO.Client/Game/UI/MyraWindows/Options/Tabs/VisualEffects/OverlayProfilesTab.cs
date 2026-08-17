#nullable enable

using System.Collections.Generic;
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
    #region Internal methods

    /// <summary>Returns the profile library and composer as an option source.</summary>
    /// <returns>The option source.</returns>
    internal static IOptionSource GetContent()
    {
        OptionFragment panel = OptionsUi.Vertical(Option.Custom(BuildEditor));

        // The composer doesn't render meaningfully in the search results page.
        panel.InheritsSearch = false;

        return panel;
    }

    #endregion

    #region Private methods

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
