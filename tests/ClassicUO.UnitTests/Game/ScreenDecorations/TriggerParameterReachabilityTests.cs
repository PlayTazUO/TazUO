using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Triggers;
using ClassicUO.Game.Logic;
using ClassicUO.Game.ScreenDecorations.Triggers;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using FluentAssertions;
using Myra.Graphics2D.UI.Properties;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

/// <summary>
/// A trigger parameter reaches its editor one of two ways: as a property grid row, or through a
/// hand-built editor that claims it. Hiding one from the grid without wiring the second is silent -
/// the parameter still persists, still drives the trigger, and simply cannot be set by anyone.
/// </summary>
public class TriggerParameterReachabilityTests
{
    /// <summary>Every parameter type the catalogue can hand the rule editor.</summary>
    private static IEnumerable<Type> ParameterTypes() =>
        TriggerCatalog.Instance.All
            .Select(definition => definition.ParameterType)
            .Where(type => type != null)
            .Distinct()!;

    /// <summary>
    /// The properties a hand-built editor takes responsibility for: the one carrying the attribute,
    /// any sibling it names, and everything an editor claims through an interface rather than an
    /// attribute.
    /// </summary>
    private static HashSet<string> ClaimedBy(Type parameters)
    {
        var claimed = new HashSet<string>(StringComparer.Ordinal);

        // The rule configurator builds a LogicBuilder for anything implementing this, so every
        // member of the contract is spoken for. Taken off the interface rather than named here, so
        // a property added to it does not have to be added again below.
        if (typeof(ILogicFilterParameters).IsAssignableFrom(parameters))
        {
            foreach (PropertyInfo member in typeof(ILogicFilterParameters).GetProperties())
                claimed.Add(member.Name);
        }

        foreach (PropertyInfo property in parameters.GetProperties())
        {
            if (property.GetCustomAttribute<SoundIndexEditorAttribute>() != null)
                claimed.Add(property.Name);

            if (property.GetCustomAttribute<BuffTriggerEditorAttribute>() is { } buffTrigger)
            {
                claimed.Add(property.Name);
                claimed.Add(buffTrigger.BuffTypeProperty);
                claimed.Add(buffTrigger.DurationSecondsProperty);
            }

            if (property.GetCustomAttribute<FalloffEditorAttribute>() is not { } falloff)
                continue;

            claimed.Add(property.Name);
            claimed.Add(falloff.PowerProperty);
            claimed.Add(falloff.NearStrengthProperty);
            claimed.Add(falloff.FarStrengthProperty);
        }

        return claimed;
    }

    public static TheoryData<Type> AllParameterTypes()
    {
        var types = new TheoryData<Type>();

        foreach (Type type in ParameterTypes())
            types.Add(type);

        return types;
    }

    /// <summary>
    /// Everything persisted is either in the grid or claimed by an editor. Read-only properties are
    /// exempt: they are readings of a stored field rather than settings of their own, which is why
    /// they carry <see cref="JsonIgnoreAttribute"/> as well.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllParameterTypes))]
    public void EveryHiddenParameterHasAnEditor(Type parameters)
    {
        HashSet<string> claimed = ClaimedBy(parameters);

        IEnumerable<string> unreachable = parameters.GetProperties()
            .Where(property => property.CanWrite)
            .Where(property => property.GetCustomAttribute<JsonIgnoreAttribute>() == null)
            .Where(property => property.GetCustomAttribute<BrowsableAttribute>() is { Browsable: false })
            .Select(property => property.Name)
            .Where(name => !claimed.Contains(name));

        unreachable.Should().BeEmpty();
    }

    /// <summary>
    /// The siblings are named as strings, so a rename that misses one compiles and then silently
    /// drops that field out of the editor.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllParameterTypes))]
    public void EveryNamedSiblingExists(Type parameters)
    {
        foreach (PropertyInfo property in parameters.GetProperties())
        {
            if (property.GetCustomAttribute<FalloffEditorAttribute>() is not { } falloff)
                continue;

            string[] siblings =
            [
                falloff.PowerProperty,
                falloff.NearStrengthProperty,
                falloff.FarStrengthProperty
            ];

            foreach (string sibling in siblings)
                parameters.GetProperty(sibling).Should().NotBeNull("{0} names it", property.Name);
        }
    }
}
