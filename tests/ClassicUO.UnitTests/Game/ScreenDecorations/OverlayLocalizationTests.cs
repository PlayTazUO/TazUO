using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ClassicUO.Configuration;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Effects;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;
using ClassicUO.Game.ScreenDecorations.Triggers.Implementations;
using ClassicUO.Renderer.Effects;
using FluentAssertions;
using Myra.Graphics2D.UI.Properties;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

/// <summary>
/// The localized attributes fail quietly: a key with no entry behind it shows its English
/// fallback and looks perfectly correct, so nothing but a check like this catches a typo or a
/// key that was renamed on one side only.
/// </summary>
public class OverlayLocalizationTests
{
    /// <summary>Every type the overlay editors put in front of a property grid.</summary>
    private static readonly Type[] _editedTypes =
    [
        typeof(ChatMessageParameters),
        typeof(ObjectPropertiesParameters),
        typeof(SoundPlayedParameters),
        typeof(ShakeSpec),
        typeof(LayerEffect),
        typeof(TintEffect),
        typeof(BlurEffect),
        typeof(RadialBlurEffect),
        typeof(ChromaticEffect),
        typeof(PulseSpec),
        typeof(NoiseSpec),
        typeof(ShapeSpec),
        typeof(JitterSpec)
    ];

    /// <summary>
    /// Declared members only: the effect subclasses inherit <see cref="LayerEffect"/>'s properties,
    /// so enumerating everything visible on them yields the same key under the same owner once per
    /// subclass, and xUnit drops the repeats as duplicate test cases.
    /// </summary>
    private static IEnumerable<MemberInfo> EditedMembers()
    {
        return _editedTypes
            .SelectMany(type => type.GetMembers(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
            ))
            .Where(member => member is PropertyInfo or FieldInfo);
    }

    /// <summary>Keys paired with the member that declares them, so a failure names the culprit.</summary>
    public static TheoryData<string, string> DeclaredKeys()
    {
        var keys = new TheoryData<string, string>();

        foreach (MemberInfo member in EditedMembers())
        {
            string owner = $"{member.DeclaringType?.Name}.{member.Name}";
            var displayName = member.GetCustomAttribute<LocalizedDisplayNameAttribute>();
            var description = member.GetCustomAttribute<LocalizedDescriptionAttribute>();

            if (displayName != null)
                keys.Add(displayName.Key, owner);

            if (description != null)
                keys.Add(description.Key, owner);
        }

        return keys;
    }

    [Theory]
    [MemberData(nameof(DeclaredKeys))]
    public void EveryLocalizedKeyHasAnEntry(string key, string owner)
    {
        LangIniSerializer.ReadEmbedded().Should().ContainKey(key, "{0} declares it", owner);
    }

    /// <summary>
    /// Keys are namespaced, because the file is flat and shared by the whole client - an
    /// unqualified "duration" or "strength" is a collision waiting to happen.
    /// <para>
    /// The namespace names whatever owns the wording, which is not always the feature the property
    /// lives in: a field a reusable widget labels and explains belongs to that widget, and filing it
    /// under the first feature to use one would misdescribe it for the second.
    /// </para>
    /// </summary>
    [Theory]
    [MemberData(nameof(DeclaredKeys))]
    public void EveryLocalizedKeyIsNamespaced(string key, string owner)
    {
        key.Should().Match(
            candidate => candidate.StartsWith("visualeffects_")
                || candidate.StartsWith("overlaytrigger_")
                || candidate.StartsWith("falloff_"),
            "{0} declares it and the language file is shared by the whole client",
            owner
        );
    }

    /// <summary>
    /// A localized attribute reports its fallback, not its key, as the framework value - so a
    /// grid with no localizer shows readable English rather than a raw identifier.
    /// </summary>
    [Fact]
    public void TheFallbackIsWhatTheFrameworkAttributeReports()
    {
        var displayName = typeof(ShakeSpec)
            .GetProperty(nameof(ShakeSpec.RampUpSeconds))!
            .GetCustomAttribute<LocalizedDisplayNameAttribute>();

        displayName.Should().NotBeNull();
        displayName!.DisplayName.Should().NotBe(displayName.Key);
        displayName.DisplayName.Should().Be("Ramp up (s)");
    }

    /// <summary>
    /// Anything shown in a grid needs both a name and a tooltip. A parameter with a name and no
    /// explanation is the shape most of these were in before.
    /// </summary>
    [Fact]
    public void EveryEditedMemberCarriesBothPiecesOfMetadata()
    {
        IEnumerable<string> incomplete = EditedMembers()
            .Where(member => member.GetCustomAttribute<System.ComponentModel.BrowsableAttribute>()?.Browsable != false)
            .Where(member => member.GetCustomAttribute<LocalizedDisplayNameAttribute>() != null)
            .Where(member => member.GetCustomAttribute<LocalizedDescriptionAttribute>() == null)
            .Select(member => $"{member.DeclaringType?.Name}.{member.Name}");

        incomplete.Should().BeEmpty();
    }
}
