using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Effects;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;
using ClassicUO.Game.ScreenDecorations.Overlays;
using ClassicUO.Game.ScreenDecorations.Shake;
using ClassicUO.LegionScripting;
using ClassicUO.LegionScripting.ApiClasses.VisualEffects;
using ClassicUO.Renderer.Effects;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

/// <summary>
/// The scripting DTOs are a third mirror of the layer parameters, after the authoring specs and the
/// renderer structs. A field added to one and forgotten in another fails silently - the knob simply
/// stops reaching the shader - so the mirroring is asserted here rather than reviewed.
/// <para>
/// Deliberate omissions are listed per pair. Adding to those lists is the way to exempt something;
/// anything not listed has to exist on both sides.
/// </para>
/// </summary>
public class ApiEffectDtoParityTests
{
    /// <summary>
    /// One DTO and the type it mirrors, plus what is knowingly not mirrored.
    /// </summary>
    /// <param name="Dto">The script-facing type.</param>
    /// <param name="Mirrored">The internal type it stands in for.</param>
    /// <param name="Renames">DTO member name keyed by the internal name, where the two differ.</param>
    /// <param name="DtoOnly">DTO members with no internal counterpart.</param>
    /// <param name="MirroredOnly">Internal members deliberately not exposed to scripts.</param>
    private sealed record Pairing(
        Type Dto,
        Type Mirrored,
        Dictionary<string, string> Renames = null,
        string[] DtoOnly = null,
        string[] MirroredOnly = null
    )
    {
        public override string ToString() => Dto.Name;
    }

    /// <summary>
    /// Every technique and spec a script can reach. <see cref="ApiEffectProfileInfo"/> is absent on
    /// purpose: it is a listing row this tier invented, not a mirror of anything.
    /// </summary>
    private static readonly Pairing[] _pairings =
    [
        new(typeof(ApiJitterSpec), typeof(JitterSpec)),
        new(typeof(ApiShapeSpec), typeof(ShapeSpec)),
        new(typeof(ApiNoiseSpec), typeof(NoiseSpec)),
        new(typeof(ApiPulseSpec), typeof(PulseSpec)),
        new(typeof(ApiFadeSpec), typeof(FadeSpec)),
        new(typeof(ApiShakeSpec), typeof(ShakeSpec)),

        // TechniqueName is the composer's label for a row in the layer list; a script that already
        // knows which factory it called does not need to be told what it built.
        new(typeof(ApiLayerEffect), typeof(LayerEffect), MirroredOnly: ["TechniqueName"]),

        // The colour arrives as four channels, because scripts cannot name the XNA type.
        new(
            typeof(ApiTintLayer),
            typeof(TintEffect),
            DtoOnly: ["R", "G", "B", "A"],
            MirroredOnly: ["Tint", "TechniqueName"]
        ),
        new(typeof(ApiBlurLayer), typeof(BlurEffect), MirroredOnly: ["TechniqueName"]),
        new(typeof(ApiRadialBlurLayer), typeof(RadialBlurEffect), MirroredOnly: ["TechniqueName"]),
        new(typeof(ApiChromaticLayer), typeof(ChromaticEffect), MirroredOnly: ["TechniqueName"]),

        // Name is what the composer calls a row. A script-built composition is never listed anywhere.
        new(typeof(ApiProfileLayer), typeof(ProfileLayer), MirroredOnly: ["Name"]),

        // Identity and built-in marking belong to the library, which nothing a script passes enters.
        new(
            typeof(ApiEffectProfile),
            typeof(EffectProfile),
            MirroredOnly: ["Id", "IsBuiltIn", "Deletable"]
        ),

        // The spans are seconds here: scripts cannot name TimeSpan. FullScreen has no counterpart
        // because a composition's shake takes its scope from the composition.
        new(
            typeof(ApiShakeConfig),
            typeof(ShakeRequest),
            Renames: new Dictionary<string, string>
            {
                ["Duration"] = "Seconds",
                ["RampUp"] = "RampUpSeconds",
                ["RampDown"] = "RampDownSeconds"
            },
            DtoOnly: ["FullScreen"]
        )
    ];

    public static TheoryData<string> PairNames()
    {
        var names = new TheoryData<string>();

        foreach (Pairing pairing in _pairings)
            names.Add(pairing.Dto.Name);

        return names;
    }

    /// <summary>
    /// Declared members only. The layer DTOs inherit <see cref="ApiLayerEffect"/>'s four properties
    /// exactly as the internal ones inherit <see cref="LayerEffect"/>'s, so comparing everything
    /// visible would check that base pair once per technique and report its omissions four times.
    /// </summary>
    private static string[] MirroredNames(Type type)
    {
        return type
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(member => member is PropertyInfo or FieldInfo)
            .Select(member => member.Name)
            .ToArray();
    }

    private static Pairing Find(string dtoName) => _pairings.Single(pairing => pairing.Dto.Name == dtoName);

    [Theory]
    [MemberData(nameof(PairNames))]
    public void EveryMirroredFieldIsReachableFromAScript(string dtoName)
    {
        Pairing pairing = Find(dtoName);
        Dictionary<string, string> renames = pairing.Renames ?? [];
        string[] exempt = pairing.MirroredOnly ?? [];
        string[] reachable = MirroredNames(pairing.Dto);

        IEnumerable<string> missing = MirroredNames(pairing.Mirrored)
            .Where(name => !exempt.Contains(name))
            .Select(name => renames.GetValueOrDefault(name, name))
            .Where(name => !reachable.Contains(name));

        missing.Should()
            .BeEmpty(
                $"{pairing.Mirrored.Name} members must be mirrored on {pairing.Dto.Name} or listed as "
                + "deliberately unexposed - a field missing here is a knob that silently stops working"
            );
    }

    /// <summary>
    /// The other direction. A DTO member nothing maps to is one that reads back whatever a script
    /// set and never reaches the shader, which is worse than not offering it.
    /// </summary>
    [Theory]
    [MemberData(nameof(PairNames))]
    public void EveryScriptFacingFieldMapsOntoSomething(string dtoName)
    {
        Pairing pairing = Find(dtoName);
        string[] exempt = pairing.DtoOnly ?? [];

        IEnumerable<string> mapped = MirroredNames(pairing.Mirrored)
            .Select(name => (pairing.Renames ?? []).GetValueOrDefault(name, name));

        MirroredNames(pairing.Dto)
            .Where(name => !exempt.Contains(name))
            .Except(mapped)
            .Should()
            .BeEmpty($"every {pairing.Dto.Name} member must map onto {pairing.Mirrored.Name}");
    }

    /// <summary>
    /// The API enums are retypes rather than re-exports, so the conversions are plain casts. Names
    /// and values therefore have to agree exactly: a reordered internal enum would otherwise turn
    /// every script's noise channel into a different one.
    /// </summary>
    public static TheoryData<Type, Type> EnumPairs() =>
        new()
        {
            { typeof(ApiVisualEffects.OverlayBlend), typeof(OverlayBlend) },
            { typeof(ApiVisualEffects.NoiseChannel), typeof(NoiseChannel) },
            { typeof(ApiVisualEffects.OverlaySampleTaps), typeof(OverlaySampleTaps) },
            { typeof(ApiVisualEffects.ShakeGradient), typeof(ShakeGradient) },
            { typeof(ApiVisualEffects.ShakeCurve), typeof(ShakeCurve) }
        };

    [Theory]
    [MemberData(nameof(EnumPairs))]
    public void ApiEnumsCarryTheSameNamesAndValues(Type api, Type mirrored)
    {
        static Dictionary<string, long> Members(Type type) =>
            Enum.GetValues(type)
                .Cast<object>()
                .ToDictionary(value => Enum.GetName(type, value)!, Convert.ToInt64);

        Members(api).Should().BeEquivalentTo(Members(mirrored));
    }

    /// <summary>
    /// The shipped compositions are named through the language file, so a script has no way to reach
    /// one but by id. These constants are that way in, and a drifted one points at nothing.
    /// </summary>
    [Fact]
    public void TheShippedProfileIdConstantsMatchTheProfilesTheyName()
    {
        Dictionary<string, Guid> declared = typeof(BuiltInProfiles.Ids)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .ToDictionary(field => field.Name, field => (Guid)field.GetValue(null)!);

        Dictionary<string, Guid> exposed = typeof(ApiVisualEffects.Ids)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .ToDictionary(field => field.Name, field => Guid.Parse((string)field.GetRawConstantValue()!));

        exposed.Should().BeEquivalentTo(declared);
    }
}
