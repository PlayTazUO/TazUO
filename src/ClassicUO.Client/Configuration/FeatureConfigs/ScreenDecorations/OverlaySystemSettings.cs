#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Rules;
using ClassicUO.Game.ScreenDecorations.Overlays;
using ClassicUO.Game.ScreenDecorations.Rules;

namespace ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;

/// <summary>
/// The overlay half of <see cref="ScreenDecorations"/>: whether full-screen effects run at all, how
/// strongly, how many at once, and the flat pools of rules and profiles underneath.
/// <para>
/// Both pools are flat and shared. A profile is addressed by id and may be pointed at by any number
/// of rules; a rule names a profile and a trigger and carries nothing else. Neither is keyed by a
/// fixed set of effect names, which is what lets one look serve several triggers and one trigger
/// raise something other than the effect it is named after.
/// </para>
/// </summary>
public class OverlaySystemSettings : ObservableSettings
{
    #region Public accessors

    public bool Enabled { get; set => SetField(ref field, value); }

    /// <summary>Scales every overlay's intensity. Clamped to [0, 1] where it is consumed.</summary>
    public float Intensity { get; set => SetField(ref field, value); } = 1f;

    /// <summary>
    /// How many overlays may composite at once, lowest priority evicted past it. About legibility
    /// first and cost second: more than a few tinted fields on screen together is unreadable
    /// whatever it costs, and each one is at least one extra draw call per frame.
    /// </summary>
    public int MaxConcurrent
    {
        get;
        set => SetField(ref field, Math.Clamp(value, MinConcurrent, MaxAllowedConcurrent));
    } = DefaultConcurrent;

    /// <summary>User-authored profiles. The shipped ones live in code and are never stored here.</summary>
    public List<EffectProfile> Profiles { get; set => SetField(ref field, value); } = [];

    /// <summary>User-authored rules. The shipped ones live in code; see <see cref="BuiltInRuleStates"/>.</summary>
    public List<OverlayRule> Rules { get; set => SetField(ref field, value); } = [];

    /// <summary>
    /// What the user has changed about the shipped rules. Absent means untouched, which is why a
    /// fresh config carries none of them.
    /// </summary>
    public List<OverlayRuleOverride> BuiltInRuleStates { get; set => SetField(ref field, value); } = [];

    #endregion

    #region Public constants

    /// <summary>Below this nothing could composite at all.</summary>
    public const int MinConcurrent = 1;

    /// <summary>
    /// Ceiling on <see cref="MaxConcurrent"/>. Not a performance cliff so much as the point past
    /// which the screen is unreadable; the draw budget scales with it either way.
    /// </summary>
    public const int MaxAllowedConcurrent = 8;

    public const int DefaultConcurrent = 4;

    #endregion

    #region Public methods

    /// <summary>
    /// Every profile a rule may point at: the shipped ones first, then the user's.
    /// </summary>
    /// <returns>The pool, in library order.</returns>
    public IEnumerable<EffectProfile> AllProfiles() => BuiltInProfiles.All.Concat(Profiles);

    /// <summary>
    /// The profile with this id, shipped or authored.
    /// </summary>
    /// <param name="id">The profile to look up.</param>
    /// <returns>The profile, or null where a rule points at one that has since been deleted.</returns>
    public EffectProfile? FindProfile(Guid id) =>
        BuiltInProfiles.Find(id) ?? Profiles.FirstOrDefault(profile => profile.Id == id);

    /// <summary>
    /// The rules in force: shipped rules with the user's overrides stamped on, followed by the
    /// user's own, in table order.
    /// </summary>
    /// <returns>Fresh rule objects for the shipped ones; the stored instances for the rest.</returns>
    public List<OverlayRule> ResolveRules()
    {
        PruneDuplicateRules();

        var resolved = new List<OverlayRule>();

        foreach (OverlayRule rule in BuiltInRules.Create())
        {
            OverlayRuleOverride? state = BuiltInRuleStates.FirstOrDefault(entry => entry.RuleId == rule.Id);

            if (state != null)
            {
                rule.Enabled = state.Enabled;
                rule.Order = state.Order;
            }

            resolved.Add(rule);
        }

        resolved.AddRange(Rules);
        resolved.Sort(static (left, right) => left.Order.CompareTo(right.Order));

        return resolved;
    }

    /// <summary>
    /// Records whatever the user changed about <paramref name="rule"/>. A shipped rule keeps only
    /// its enabled state and position; a user rule is already stored by reference and needs nothing
    /// doing.
    /// </summary>
    /// <param name="rule">The rule as the table now shows it.</param>
    public void TrackRuleState(OverlayRule rule)
    {
        if (!rule.IsBuiltIn)
            return;

        OverlayRuleOverride? state = BuiltInRuleStates.FirstOrDefault(entry => entry.RuleId == rule.Id);

        if (state == null)
        {
            state = new OverlayRuleOverride { RuleId = rule.Id };
            BuiltInRuleStates.Add(state);
        }

        state.Enabled = rule.Enabled;
        state.Order = rule.Order;
    }

    /// <summary>
    /// Adds <paramref name="profile"/>, renaming it to stay distinguishable in the library. Ids are
    /// what identify a profile, so a duplicate name is only ever a usability problem.
    /// </summary>
    /// <param name="profile">The profile to store.</param>
    /// <returns>The name it was stored under.</returns>
    public string AddProfile(EffectProfile profile)
    {
        string baseName = string.IsNullOrWhiteSpace(profile.Name) ? "Overlay" : profile.Name;
        string name = baseName;

        for (int suffix = 2; AllProfiles().Any(other => NameMatches(other, name)); suffix++)
            name = $"{baseName} ({suffix})";

        profile.Name = name;
        Profiles.Add(profile);
        OnPropertyChanged(nameof(Profiles));

        return name;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Drops any rule sharing an ID with an earlier one, keeping the first. An ID is the rule's
    /// identity, so two entries carrying one are a rule stored twice.
    /// <para>
    /// Silent by design: the manager rebuilds its wiring from inside <see cref="ResolveRules"/>, so
    /// announcing a change here re-enters that rebuild part-way through.
    /// </para>
    /// <code>
    /// SyncRules ─┬─ TearDownWatching
    ///            ├─ ResolveRules ── Prune ── PropertyChanged(Rules)
    ///            │                                  └─ SyncRules (nested: builds + attaches)
    ///            └─ resumes: overwrites _watching without detaching → leaked subscriptions
    /// </code>
    /// </summary>
    private void PruneDuplicateRules()
    {
        var seen = new HashSet<Guid>(Rules.Count);

        Rules.RemoveAll(rule => !seen.Add(rule.Id));
    }

    private static bool NameMatches(EffectProfile profile, string name) =>
        string.Equals(profile.Name, name, StringComparison.OrdinalIgnoreCase);

    #endregion
}
