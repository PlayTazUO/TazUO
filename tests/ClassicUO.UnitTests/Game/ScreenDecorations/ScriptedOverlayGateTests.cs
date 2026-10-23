using System;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;
using ClassicUO.Game.ScreenDecorations.Manager;
using ClassicUO.UnitTests.Fixtures;
using FluentAssertions;
using Xunit;
using DecorationSettings = ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.ScreenDecorations;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

/// <summary>
/// What a script gets back when it asks for an effect it cannot have. Zero means the client declined
/// - the system is off, the player is out of the world, or the cap is spoken for - so a script can
/// tell "declined" from "failed" without catching anything, and the exceptions are reserved for a
/// script asking for something impossible.
/// <para>
/// The manager is a main-thread-confined singleton, so these run through the fixture's thread and
/// hand back everything they took, the world session included.
/// </para>
/// </summary>
[Collection(MainThreadCollection.Name)]
public class ScriptedOverlayGateTests : IDisposable
{
    private readonly MainThreadFixture _mainThread;
    private readonly object _owner = new();

    /// <summary>
    /// The manager is a process-wide singleton, so a world session another test left open would
    /// decide what these see. Reset drops every script demand whoever raised it, which is the only
    /// teardown strong enough - StopScriptedByOwner reaches this class's owner and nothing else.
    /// </summary>
    /// <param name="mainThread">The fixture's main thread.</param>
    public ScriptedOverlayGateTests(MainThreadFixture mainThread)
    {
        this._mainThread = mainThread;
        mainThread.Invoke(ScreenOverlayManager.Instance.Reset);
    }

    public void Dispose()
    {
        _mainThread.Invoke(ScreenOverlayManager.Instance.Reset);
    }

    private static EffectProfile Profile()
    {
        return new EffectProfile { Name = "scripted" };
    }

    /// <summary>Fresh defaults, which ship with every decoration switched off.</summary>
    private static void SetDecorations(bool enabled)
    {
        DecorationSettings settings = DecorationSettings.LoadForProfile(null);
        settings.Enabled = enabled;
        settings.Overlays.Enabled = enabled;
    }

    /// <summary>
    /// Settings, then a world session - the order the client does it in, and the order
    /// <see cref="ScreenOverlayManager.StartScripted"/> needs, since it declines out of world.
    /// </summary>
    /// <param name="enabled">Whether overlays are switched on.</param>
    private static void EnterWorld(bool enabled)
    {
        SetDecorations(enabled);
        ScreenOverlayManager.Instance.Start();
    }

    [Fact]
    public void AskingWhileOverlaysAreSwitchedOffAnswersZero()
    {
        _mainThread.Invoke(() =>
            {
                EnterWorld(false);

                ScreenOverlayManager.Instance
                    .StartScripted(Profile(), Guid.NewGuid(), 1f, null, _owner)
                    .Should()
                    .Be(0u, "the user said no, which is not an error");
            }
        );
    }

    [Fact]
    public void AskingWhileOverlaysAreOnAnswersAHandle()
    {
        _mainThread.Invoke(() =>
            {
                EnterWorld(true);

                try
                {
                    ScreenOverlayManager.Instance
                        .StartScripted(Profile(), Guid.NewGuid(), 1f, null, _owner)
                        .Should()
                        .BeGreaterThan(0u);
                }
                finally
                {
                    ScreenOverlayManager.Instance.StopScriptedByOwner(_owner);
                }
            }
        );
    }

    /// <summary>
    /// Exhausting the cap answers 0 rather than faulting. The cap is shared between every script on
    /// the client, so one script holding effects it has not stopped is a state another script finds
    /// itself in through no fault of its own - and <see cref="ScreenOverlayManager.ScriptSlotsFree"/>
    /// is what tells the two cases apart.
    /// </summary>
    [Fact]
    public void ExhaustingTheScriptCapAnswersZero()
    {
        _mainThread.Invoke(() =>
            {
                EnterWorld(true);

                try
                {
                    for (int i = 0; i < ScreenOverlayManager.ScriptCap; i++)
                        ScreenOverlayManager.Instance.StartScripted(Profile(), Guid.NewGuid(), 1f, null, _owner);

                    ScreenOverlayManager.Instance.ScriptSlotsFree().Should().Be(0);

                    ScreenOverlayManager.Instance
                        .StartScripted(Profile(), Guid.NewGuid(), 1f, null, _owner)
                        .Should()
                        .Be(0u);

                    ScreenOverlayManager.Instance.StopScriptedByOwner(_owner);

                    ScreenOverlayManager.Instance.ScriptSlotsFree().Should().Be(ScreenOverlayManager.ScriptCap);
                }
                finally
                {
                    ScreenOverlayManager.Instance.StopScriptedByOwner(_owner);
                }
            }
        );
    }

    /// <summary>Releasing one makes room, so a script that cleans up after itself keeps working.</summary>
    [Fact]
    public void StoppingAnEffectReturnsItsPlaceInTheCap()
    {
        _mainThread.Invoke(() =>
            {
                EnterWorld(true);

                try
                {
                    uint first = ScreenOverlayManager.Instance.StartScripted(Profile(), Guid.NewGuid(), 1f, null, _owner);

                    for (int i = 1; i < ScreenOverlayManager.ScriptCap; i++)
                        ScreenOverlayManager.Instance.StartScripted(Profile(), Guid.NewGuid(), 1f, null, _owner);

                    ScreenOverlayManager.Instance.StopScripted(first, _owner);

                    ScreenOverlayManager.Instance
                        .StartScripted(Profile(), Guid.NewGuid(), 1f, null, _owner)
                        .Should()
                        .BeGreaterThan(0u);
                }
                finally
                {
                    ScreenOverlayManager.Instance.StopScriptedByOwner(_owner);
                }
            }
        );
    }

    /// <summary>
    /// Leaving the world drops every script effect. Handles go stale and nothing is re-asserted on
    /// the next world load, which is why the cap is free again on the next one.
    /// </summary>
    [Fact]
    public void LeavingTheWorldDropsEveryScriptEffect()
    {
        _mainThread.Invoke(() =>
            {
                EnterWorld(true);

                try
                {
                    ScreenOverlayManager.Instance.StartScripted(Profile(), Guid.NewGuid(), 1f, null, _owner);
                    ScreenOverlayManager.Instance.Reset();

                    ScreenOverlayManager.Instance.Start();

                    // The cap would still be spoken for had Reset left the demands behind.
                    for (int i = 0; i < ScreenOverlayManager.ScriptCap; i++)
                    {
                        ScreenOverlayManager.Instance
                            .StartScripted(Profile(), Guid.NewGuid(), 1f, null, _owner)
                            .Should()
                            .BeGreaterThan(0u);
                    }
                }
                finally
                {
                    ScreenOverlayManager.Instance.StopScriptedByOwner(_owner);
                }
            }
        );
    }

    /// <summary>
    /// Out of world nothing reconciles, so a demand taken there would hold a slot, draw nothing, and
    /// then arrive all at once on the next world load. Declined instead, and
    /// <see cref="ScreenOverlayManager.ScriptSlotsFree"/> says so rather than offering a slot that
    /// cannot be had.
    /// </summary>
    [Fact]
    public void AskingWhileOutOfWorldAnswersZero()
    {
        _mainThread.Invoke(() =>
            {
                SetDecorations(true);
                ScreenOverlayManager.Instance.Reset();

                ScreenOverlayManager.Instance
                    .StartScripted(Profile(), Guid.NewGuid(), 1f, null, _owner)
                    .Should()
                    .Be(0u, "there is nothing to draw over and nothing reconciling");

                ScreenOverlayManager.Instance.ScriptSlotsFree().Should().Be(0);
            }
        );
    }

    [Fact]
    public void StoppingAHandleThatIsNotLiveDoesNothing()
    {
        _mainThread.Invoke(() =>
            {
                EnterWorld(true);

                uint live = ScreenOverlayManager.Instance.StartScripted(Profile(), Guid.NewGuid(), 1f, null, _owner);

                try
                {
                    ScreenOverlayManager.Instance.StopScripted(0, _owner);
                    ScreenOverlayManager.Instance.StopScripted(uint.MaxValue, _owner);
                    ScreenOverlayManager.Instance.StopScripted(live, _owner);

                    // Stopping the same handle twice must not reach whatever was started since.
                    ScreenOverlayManager.Instance.StopScripted(live, _owner);

                    ScreenOverlayManager.Instance
                        .StartScripted(Profile(), Guid.NewGuid(), 1f, null, _owner)
                        .Should()
                        .BeGreaterThan(live);
                }
                finally
                {
                    ScreenOverlayManager.Instance.StopScriptedByOwner(_owner);
                }
            }
        );
    }
}
