using System;
using ClassicUO.Game.ScreenDecorations.Shake;
using ClassicUO.UnitTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

/// <summary>
/// Shakes a script raises are capped apart from the ones the player's own state raises, and a source
/// never evicts another's. Without that a script looping on StartScreenShake would displace exactly
/// the shakes that carry information.
/// <para>
/// These run against the real accumulators, which are static, and <see cref="ScreenShake.Enabled"/>
/// is written by the manager on every world transition. Joining the main-thread collection is what
/// serializes them against the tests that drive the manager, which would otherwise switch shake off
/// underneath these.
/// </para>
/// </summary>
[Collection(MainThreadCollection.Name)]
public class ScriptedShakeTests : IDisposable
{
    private readonly object _owner = new();

    public ScriptedShakeTests()
    {
        ScreenShake.Enabled = true;
        ScreenShake.Viewport.Clear();
        ScreenShake.Window.Clear();
    }

    public void Dispose()
    {
        ScreenShake.Viewport.Clear();
        ScreenShake.Window.Clear();
        GC.SuppressFinalize(this);
    }

    private static ShakeRequest Request(float intensity = 1f, double seconds = 5) =>
        new()
        {
            Duration = TimeSpan.FromSeconds(seconds),
            Intensity = intensity,
            Gradient = ShakeGradient.Constant
        };

    /// <summary>
    /// The cap partitioning. Four scripted shakes must not consume the eight slots the rules share,
    /// and a fifth must displace one of the script's own rather than a rule's.
    /// </summary>
    [Fact]
    public void ScriptedShakesDoNotEvictRuleShakes()
    {
        uint ruleShake = ScreenShake.Viewport.Trauma(Request(0.2f));

        for (int i = 0; i < 8; i++)
            ScreenShake.Viewport.Trauma(Request(1f), ShakeSource.Script, _owner);

        // The rule's is still there to be cancelled, which it would not be had a script taken it.
        ScreenShake.Viewport.Cancel(ruleShake).Should().BeTrue();
    }

    /// <summary>
    /// With the list already full of rule shakes a scripted one is dropped rather than taking one:
    /// 0 back means nothing was scheduled, which is the same answer as the gate being off.
    /// </summary>
    [Fact]
    public void AScriptedShakeIsDroppedRatherThanEvictingAnotherSource()
    {
        for (int i = 0; i < 8; i++)
            ScreenShake.Viewport.Trauma(Request());

        ScreenShake.Viewport.Trauma(Request(), ShakeSource.Script, _owner).Should().Be(0u);
    }

    [Fact]
    public void NothingIsScheduledWhileShakeIsSwitchedOff()
    {
        ScreenShake.Enabled = false;

        try
        {
            ScreenShake.Viewport.Trauma(Request(), ShakeSource.Script, _owner).Should().Be(0u);
            ScreenShake.Viewport.IsShaking.Should().BeFalse();
        }
        finally
        {
            ScreenShake.Enabled = true;
        }
    }

    [Fact]
    public void HandlesAreUniqueAcrossBothScopes()
    {
        uint viewport = ScreenShake.Viewport.Trauma(Request(), ShakeSource.Script, _owner);
        uint window = ScreenShake.Window.Trauma(Request(), ShakeSource.Script, _owner);

        viewport.Should().NotBe(0u);
        window.Should().NotBe(viewport);
    }

    [Fact]
    public void CancellingAHandleEndsThatShakeAndNothingElse()
    {
        uint first = ScreenShake.Viewport.Trauma(Request(), ShakeSource.Script, _owner);
        uint second = ScreenShake.Viewport.Trauma(Request(), ShakeSource.Script, _owner);

        ScreenShake.Viewport.Cancel(first).Should().BeTrue();
        ScreenShake.Viewport.IsShaking.Should().BeTrue();

        ScreenShake.Viewport.Cancel(second).Should().BeTrue();
        ScreenShake.Viewport.IsShaking.Should().BeFalse();
    }

    [Fact]
    public void CancellingAnUnknownOrZeroHandleDoesNothing()
    {
        ScreenShake.Viewport.Trauma(Request(), ShakeSource.Script, _owner);

        ScreenShake.Viewport.Cancel(0).Should().BeFalse();
        ScreenShake.Viewport.Cancel(uint.MaxValue).Should().BeFalse();
        ScreenShake.Viewport.IsShaking.Should().BeTrue();
    }

    /// <summary>
    /// Teardown when a script stops. A four-second quake outliving its script has nothing left that
    /// could stop it.
    /// </summary>
    [Fact]
    public void TearingDownOneScriptLeavesEveryOtherShakeAlone()
    {
        var theirs = new object();

        ScreenShake.Viewport.Trauma(Request(), ShakeSource.Script, _owner);
        ScreenShake.Viewport.Trauma(Request(), ShakeSource.Script, _owner);
        uint survivor = ScreenShake.Viewport.Trauma(Request(), ShakeSource.Script, theirs);
        uint ruleShake = ScreenShake.Viewport.Trauma(Request());

        ScreenShake.Viewport.CancelByOwner(_owner);

        ScreenShake.Viewport.Cancel(survivor).Should().BeTrue();
        ScreenShake.Viewport.Cancel(ruleShake).Should().BeTrue();
        ScreenShake.Viewport.IsShaking.Should().BeFalse();
    }
}
