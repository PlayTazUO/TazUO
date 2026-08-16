using System;
using System.Text.Json;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Triggers;
using ClassicUO.Game.ScreenDecorations.Triggers;
using ClassicUO.Game.ScreenDecorations.Triggers.Definitions;
using ClassicUO.Game.ScreenDecorations.Triggers.Implementations;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

public class SoundPlayedTriggerTests
{
    private const int VIEW_RANGE = 18;
    private const int PLAYER_X = 1000;
    private const int PLAYER_Y = 1000;
    private const int SOUND = 755;

    private static SoundPlayedParameters Parameters() => new() { SoundIndex = SOUND };

    private static TriggerSignal? EvaluateAt(SoundPlayedParameters parameters, int tilesAway, int soundIndex = SOUND) =>
        SoundPlayedTrigger.Evaluate(
            parameters,
            soundIndex,
            PLAYER_X + tilesAway,
            PLAYER_Y,
            PLAYER_X,
            PLAYER_Y,
            VIEW_RANGE
        );

    [Fact]
    public void AnotherSoundIsIgnored()
    {
        EvaluateAt(Parameters(), 0, SOUND + 1).Should().BeNull();
    }

    [Fact]
    public void TheConfiguredSoundUnderfootIsFullStrength()
    {
        EvaluateAt(Parameters(), 0)!.Value.Intensity.Should().Be(1f);
    }

    [Fact]
    public void SomethingBeyondTheAudibleRangeIsIgnored()
    {
        EvaluateAt(Parameters(), VIEW_RANGE + 1).Should().BeNull();
    }

    [Fact]
    public void NothingRegistersWhileTheViewRangeIsUnset()
    {
        SoundPlayedTrigger.Evaluate(Parameters(), SOUND, PLAYER_X, PLAYER_Y, PLAYER_X, PLAYER_Y, 0)
            .Should()
            .BeNull();
    }

    /// <summary>
    /// The falloff the dedicated earthquake trigger had, pinned to the numbers it produced rather
    /// than to a second implementation of the same formula: quadratic across the client's audible
    /// range, from a quarter strength at the far edge to full underfoot.
    /// </summary>
    [Theory]
    [InlineData(0, 1f)]
    [InlineData(3, 0.781856f)]
    [InlineData(9, 0.457756f)]
    [InlineData(VIEW_RANGE, 0.252078f)]
    public void TheDefaultsKeepTheOriginalEarthquakeFalloff(int tilesAway, float expected)
    {
        EvaluateAt(Parameters(), tilesAway)!.Value.Intensity.Should().BeApproximately(expected, 1e-5f);
    }

    [Fact]
    public void SomethingCloserThanTheMinimumIsIgnored()
    {
        SoundPlayedParameters parameters = Parameters();
        parameters.MinDistance = 5;

        EvaluateAt(parameters, 4).Should().BeNull();
        EvaluateAt(parameters, 5).Should().NotBeNull();
    }

    [Fact]
    public void SomethingBeyondTheMaximumIsIgnored()
    {
        SoundPlayedParameters parameters = Parameters();
        parameters.MaxDistance = 6;

        EvaluateAt(parameters, 6).Should().NotBeNull();
        EvaluateAt(parameters, 7).Should().BeNull();
    }

    /// <summary>
    /// A band wider than the client can hear over would claim sounds that are never played, so the
    /// far edge is held at the audible range however it is configured.
    /// </summary>
    [Fact]
    public void AMaximumBeyondEarshotIsClampedToIt()
    {
        SoundPlayedParameters parameters = Parameters();
        parameters.MaxDistance = VIEW_RANGE * 10;

        EvaluateAt(parameters, VIEW_RANGE + 1).Should().BeNull();
        EvaluateAt(parameters, VIEW_RANGE).Should().NotBeNull();
    }

    /// <summary>The near edge of a band is full strength wherever it is put, so a rule that only
    /// answers to distant sounds is not permanently faint.</summary>
    [Fact]
    public void TheNearEdgeOfTheBandIsFullStrength()
    {
        SoundPlayedParameters parameters = Parameters();
        parameters.MinDistance = 8;
        parameters.MaxDistance = 16;

        EvaluateAt(parameters, 8)!.Value.Intensity.Should().Be(parameters.MaxIntensity);
    }

    [Fact]
    public void FlatFalloffMakesTheWholeBandEqual()
    {
        SoundPlayedParameters parameters = Parameters();
        parameters.Curve = FalloffCurve.Flat;

        EvaluateAt(parameters, 0)!.Value.Intensity
            .Should()
            .Be(EvaluateAt(parameters, VIEW_RANGE)!.Value.Intensity);
    }

    /// <summary>
    /// The two ends are taken as authored rather than sorted, so a look that should be faint nearby
    /// and strong at a distance is expressible.
    /// </summary>
    [Fact]
    public void AnInvertedIntensityRangeIsHonoured()
    {
        SoundPlayedParameters parameters = Parameters();
        parameters.MinIntensity = 1f;
        parameters.MaxIntensity = 0.2f;

        EvaluateAt(parameters, 0)!.Value.Intensity
            .Should()
            .BeLessThan(EvaluateAt(parameters, VIEW_RANGE)!.Value.Intensity);
    }

    /// <summary>Parameters are hand-editable JSON and are not trusted; an out-of-range strength must
    /// not reach the compositor as one.</summary>
    [Fact]
    public void AnOutOfRangeIntensityIsClamped()
    {
        SoundPlayedParameters parameters = Parameters();
        parameters.MaxIntensity = 40f;
        parameters.MinIntensity = -12f;

        EvaluateAt(parameters, 0)!.Value.Intensity.Should().BeInRange(0f, 1f);
        EvaluateAt(parameters, VIEW_RANGE)!.Value.Intensity.Should().BeInRange(0f, 1f);
    }

    [Fact]
    public void TheConfiguredDurationIsWhatRetiresTheOccurrence()
    {
        SoundPlayedParameters parameters = Parameters();
        parameters.DurationSeconds = 7.5f;

        EvaluateAt(parameters, 0)!.Value.Duration.Should().Be(TimeSpan.FromSeconds(7.5));
    }

    [Fact]
    public void ANegativeDurationIsFlooredRatherThanRunningBackwards()
    {
        SoundPlayedParameters parameters = Parameters();
        parameters.DurationSeconds = -4f;

        parameters.Duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void CloningCopiesEveryParameter()
    {
        var parameters = new SoundPlayedParameters
        {
            SoundIndex = 42,
            MinDistance = 2,
            MaxDistance = 11,
            Curve = FalloffCurve.Custom,
            CurveExponent = 0.5f,
            MinIntensity = 0.1f,
            MaxIntensity = 0.9f,
            DurationSeconds = 6f
        };

        parameters.Clone().Should().BeEquivalentTo(parameters);
    }

    [Fact]
    public void TheCatalogueOffersTheTrigger()
    {
        TriggerCatalog.Instance.Find("sound_played").Should().BeOfType<SoundPlayedTriggerDefinition>();
    }

    [Fact]
    public void TheDefinitionRefusesParametersItCannotRead()
    {
        var definition = new SoundPlayedTriggerDefinition();

        definition.Invoking(target => target.Create(new ChatMessageParameters()))
            .Should()
            .Throw<ArgumentException>();
    }

    /// <summary>
    /// The discriminator is what a persisted rule names its parameters by, so it has to survive a
    /// round trip through the generated context - a subtype missing its [JsonDerivedType] fails here
    /// rather than by silently losing every rule using it.
    /// </summary>
    [Fact]
    public void ParametersRoundTripThroughThePolymorphicSerializer()
    {
        var parameters = new SoundPlayedParameters { SoundIndex = 755, MaxDistance = 9 };

        string json = JsonSerializer.Serialize<TriggerParameters>(
            parameters,
            ScreenDecorationsJsonContext.DefaultToUse.Options
        );

        var restored = JsonSerializer.Deserialize<TriggerParameters>(
            json,
            ScreenDecorationsJsonContext.DefaultToUse.Options
        );

        restored.Should().BeOfType<SoundPlayedParameters>();
        restored.Should().BeEquivalentTo(parameters);
    }
}
