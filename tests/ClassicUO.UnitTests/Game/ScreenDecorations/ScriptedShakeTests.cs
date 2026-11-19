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
    /// The cap partitioning. However many shakes a script asks for, it must not displace the rule's:
    /// past its own allowance it starts overwriting its own.
    /// </summary>
    [Fact]
    public void ScriptedShakesDoNotEvictRuleShakes()
    {
        ScreenShake.Viewport.Trauma(Request(0.2f), ShakeOrigin.Rule);

        for (int i = 0; i < 16; i++)
            ScreenShake.Viewport.Trauma(Request(1f), ShakeOrigin.Script(_owner));

        // Taking every scripted shake away leaves the rule's still running, which it would not be
        // had a script taken its slot.
        ScreenShake.Viewport.CancelByOwner(_owner);

        ScreenShake.Viewport.IsShaking.Should().BeTrue();
    }

    /// <summary>
    /// The caps are independent, not shares of one number: rules filling their own allowance must
    /// still leave a script able to raise its.
    /// </summary>
    [Fact]
    public void RuleShakesDoNotConsumeTheScriptAllowance()
    {
        for (int i = 0; i < 8; i++)
            ScreenShake.Viewport.Trauma(Request(), ShakeOrigin.Rule);

        ScreenShake.Viewport.Trauma(Request(), ShakeOrigin.Script(_owner)).Should().NotBe(0u);
    }

    /// <summary>
    /// Neither cancel path can reach an unowned scripted shake, so one would hold a slot of the
    /// script allowance, unstoppable, until its duration ran out. The overload demands an owner, so
    /// only a deliberate null gets this far.
    /// </summary>
    [Fact]
    public void AScriptedShakeMustCarryAnOwner()
    {
        FluentActions.Invoking(() => ShakeOrigin.Script(null!)).Should().Throw<ArgumentNullException>();

        ScreenShake.Viewport.IsShaking.Should().BeFalse();
    }

    [Fact]
    public void NothingIsScheduledWhileShakeIsSwitchedOff()
    {
        ScreenShake.Enabled = false;

        try
        {
            ScreenShake.Viewport.Trauma(Request(), ShakeOrigin.Script(_owner)).Should().Be(0u);
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
        uint viewport = ScreenShake.Viewport.Trauma(Request(), ShakeOrigin.Script(_owner));
        uint window = ScreenShake.Window.Trauma(Request(), ShakeOrigin.Script(_owner));

        viewport.Should().NotBe(0u);
        window.Should().NotBe(viewport);
    }

    [Fact]
    public void CancellingAHandleEndsThatShakeAndNothingElse()
    {
        uint first = ScreenShake.Viewport.Trauma(Request(), ShakeOrigin.Script(_owner));
        uint second = ScreenShake.Viewport.Trauma(Request(), ShakeOrigin.Script(_owner));

        ScreenShake.Viewport.Cancel(first, _owner).Should().BeTrue();
        ScreenShake.Viewport.IsShaking.Should().BeTrue();

        ScreenShake.Viewport.Cancel(second, _owner).Should().BeTrue();
        ScreenShake.Viewport.IsShaking.Should().BeFalse();
    }

    [Fact]
    public void CancellingAnUnknownOrZeroHandleDoesNothing()
    {
        ScreenShake.Viewport.Trauma(Request(), ShakeOrigin.Script(_owner));

        ScreenShake.Viewport.Cancel(0, _owner).Should().BeFalse();
        ScreenShake.Viewport.Cancel(uint.MaxValue, _owner).Should().BeFalse();
        ScreenShake.Viewport.IsShaking.Should().BeTrue();
    }

    /// <summary>
    /// Handles are monotonic small integers, so a caller holding one could otherwise walk the range
    /// and cancel whatever it found - which is what the per-source caps exist to prevent.
    /// </summary>
    [Fact]
    public void AHandleOnlyCancelsForTheOwnerThatRaisedIt()
    {
        var theirs = new object();

        uint ruleShake = ScreenShake.Viewport.Trauma(Request(), ShakeOrigin.Rule);
        uint scripted = ScreenShake.Viewport.Trauma(Request(), ShakeOrigin.Script(theirs));

        ScreenShake.Viewport.Cancel(ruleShake, _owner).Should().BeFalse();
        ScreenShake.Viewport.Cancel(scripted, _owner).Should().BeFalse();
        ScreenShake.Viewport.Cancel(scripted, theirs).Should().BeTrue();
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

        ScreenShake.Viewport.Trauma(Request(), ShakeOrigin.Script(_owner));
        ScreenShake.Viewport.Trauma(Request(), ShakeOrigin.Script(_owner));
        uint survivor = ScreenShake.Viewport.Trauma(Request(), ShakeOrigin.Script(theirs));

        ScreenShake.Viewport.Trauma(Request(), ShakeOrigin.Rule);

        ScreenShake.Viewport.CancelByOwner(_owner);

        // The other script's is still cancellable, so tearing one script down left it alone.
        ScreenShake.Viewport.Cancel(survivor, theirs).Should().BeTrue();

        // And the rule's, which no handle can reach, is still running.
        ScreenShake.Viewport.IsShaking.Should().BeTrue();
    }
}
