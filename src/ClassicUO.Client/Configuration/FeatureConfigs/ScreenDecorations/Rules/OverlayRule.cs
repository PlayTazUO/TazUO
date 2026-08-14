#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Triggers;
using ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;

namespace ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Rules;

/// <summary>Binds a trigger definition to the values it runs with. Config, not code.</summary>
public sealed class TriggerBinding
{
    /// <summary>The persisted <c>ITriggerDefinition.Id</c>, not its display name.</summary>
    public string DefinitionId { get; set; } = string.Empty;

    /// <summary>Null for a definition that takes no parameters.</summary>
    public TriggerParameters? Parameters { get; set; }

    /// <summary>Copy, so editing one rule's binding cannot write into another's.</summary>
    /// <returns>An independent copy.</returns>
    public TriggerBinding Clone() => new() { DefinitionId = DefinitionId, Parameters = Parameters?.Clone() };
}

/// <summary>
/// One row of the rulebase: this trigger raises this look.
/// <para>
/// Holds no tuning of its own - the profile owns appearance, the trigger owns strength and duration
/// - so a rule is pure composition and can be copied and re-pointed without carrying settings along.
/// </para>
/// <para>
/// The rule's <see cref="Id" /> is the compositor's slot key: per rule rather than per profile, so
/// two rules on one profile are two occurrences and both draw. One rule firing twice replaces rather
/// than stacks.
/// </para>
/// </summary>
public sealed class OverlayRule : IRule, INotifyPropertyChanged
{
    #region Public events

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    #endregion

    #region Public accessors

    /// <summary>Stable identity, and the compositor slot this rule occupies.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>What the rulebase shows in its name column.</summary>
    public string Name { get; set => SetField(ref field, value); } = string.Empty;

    /// <summary>Which look, by id - so renaming the profile does not orphan this.</summary>
    public Guid ProfileId { get; set => SetField(ref field, value); }

    /// <summary>What raises it. One for now; a list if several ever need to share a slot.</summary>
    public TriggerBinding Trigger { get; set => SetField(ref field, value ?? new TriggerBinding()); } = new();

    /// <summary>
    /// Table position, and composite precedence with it: dragging a rule up makes it win. Inverted
    /// against the compositor, which takes higher as stronger.
    /// </summary>
    public uint Order { get; set => SetField(ref field, value); }

    /// <summary>
    /// Whether the rule runs. Opt-in: a rule that has just been authored is switched on by the act
    /// of authoring it, but nothing the client ships turns itself on.
    /// </summary>
    public bool Enabled { get; set => SetField(ref field, value); } = true;

    /// <summary>
    /// A shipped rule, resolved from code rather than stored. Only its enabled state and position
    /// are the user's; everything else stays whatever the client ships, which is what makes a
    /// built-in dependable. Copy one to customise it.
    /// </summary>
    [JsonIgnore]
    public bool IsBuiltIn { get; init; }

    /// <inheritdoc />
    [JsonIgnore]
    public bool CanEdit { get; set => SetField(ref field, value); } = true;

    /// <inheritdoc />
    [JsonIgnore]
    public bool CanDelete { get; set => SetField(ref field, value); } = true;

    /// <summary>
    /// Precedence as the compositor reads it: higher composites on top and survives the concurrency
    /// cap. Inverted from <see cref="Order" />, which is lowest-first like every other rulebase.
    /// </summary>
    [JsonIgnore]
    public int Priority => -(int)Order;

    #endregion

    #region Public methods

    /// <summary>
    /// Copy with a fresh identity, editable and deletable however the original was - copying a
    /// built-in is exactly how one gets customised.
    /// </summary>
    /// <param name="name">Name for the copy; the original's when null.</param>
    /// <returns>The copy.</returns>
    public OverlayRule Clone(string? name = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name ?? Name,
            ProfileId = ProfileId,
            Trigger = Trigger.Clone(),
            Order = Order,
            Enabled = Enabled
        };

    /// <summary>
    /// Takes on everything about <paramref name="other"/> that the user may edit, leaving identity,
    /// position and the built-in flags alone. For an editor working on a draft copy, which is what
    /// lets a cancelled edit leave no trace.
    /// </summary>
    /// <param name="other">The draft to adopt.</param>
    public void ApplyFrom(OverlayRule other)
    {
        Name = other.Name;
        ProfileId = other.ProfileId;
        Trigger = other.Trigger.Clone();
        Enabled = other.Enabled;
    }

    #endregion

    #region Private methods

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        OnPropertyChanged(propertyName);
    }

    #endregion
}

/// <summary>
/// The parts of a shipped rule that belong to the user. Everything else about a built-in comes from
/// code, so this is all that needs storing - and a built-in that has never been touched stores
/// nothing at all.
/// </summary>
public sealed class OverlayRuleOverride
{
    /// <summary>Which shipped rule this applies to.</summary>
    public Guid RuleId { get; set; }

    /// <summary>Whether the user has switched it on. Shipped rules are opt-in, so this starts
    /// false and an untouched built-in stores nothing at all.</summary>
    public bool Enabled { get; set; }

    /// <summary>Where the user dragged it in the table.</summary>
    public uint Order { get; set; }
}
