using System;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;
using ClassicUO.Game.ScreenDecorations.Manager;
using ClassicUO.UnitTests.Fixtures;
using FluentAssertions;
using Xunit;
using DecorationSettings = ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.ScreenDecorations;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

/// <summary>
/// What a script gets back when it asks for an effect it cannot have. Zero means exactly one thing -
/// the user has the system switched off - so a script can tell "declined" from "failed" without
/// catching anything, and the exceptions are reserved for a script asking for something impossible.
/// <para>
/// The manager is a main-thread-confined singleton, so these run through the fixture's thread and
/// hand back everything they took.
/// </para>
/// </summary>
[Collection(MainThreadCollection.Name)]
public class ScriptedOverlayGateTests(MainThreadFixture mainThread)
{
    private readonly object _owner = new();

    private static EffectProfile Profile() => new() { Name = "scripted" };

    /// <summary>Fresh defaults, which ship with every decoration switched off.</summary>
    private static void SetDecorations(bool enabled)
    {
        DecorationSettings settings = DecorationSettings.LoadForProfile(null);
        settings.Enabled = enabled;
        settings.Overlays.Enabled = enabled;
    }

    [Fact]
    public void AskingWhileOverlaysAreSwitchedOffAnswersZero()
    {
        mainThread.Invoke(() =>
            {
                SetDecorations(false);

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
        mainThread.Invoke(() =>
            {
                SetDecorations(true);

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
    /// Exhausting the cap is a script bug - something is starting effects it never stops - so it
    /// faults rather than answering 0, which would read as the user having switched the system off.
    /// </summary>
    [Fact]
    public void ExhaustingTheScriptCapFaults()
    {
        mainThread.Invoke(() =>
            {
                SetDecorations(true);

                try
                {
                    for (int i = 0; i < ScreenOverlayManager.ScriptCap; i++)
                        ScreenOverlayManager.Instance.StartScripted(Profile(), Guid.NewGuid(), 1f, null, _owner);

                    ScreenOverlayManager.Instance
                        .Invoking(manager => manager.StartScripted(Profile(), Guid.NewGuid(), 1f, null, _owner))
                        .Should()
                        .Throw<InvalidOperationException>();
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
        mainThread.Invoke(() =>
            {
                SetDecorations(true);

                try
                {
                    uint first = ScreenOverlayManager.Instance.StartScripted(Profile(), Guid.NewGuid(), 1f, null, _owner);

                    for (int i = 1; i < ScreenOverlayManager.ScriptCap; i++)
                        ScreenOverlayManager.Instance.StartScripted(Profile(), Guid.NewGuid(), 1f, null, _owner);

                    ScreenOverlayManager.Instance.StopScripted(first);

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
    /// the next world load, which is why the cap is free again afterwards.
    /// </summary>
    [Fact]
    public void LeavingTheWorldDropsEveryScriptEffect()
    {
        mainThread.Invoke(() =>
            {
                SetDecorations(true);

                ScreenOverlayManager.Instance.StartScripted(Profile(), Guid.NewGuid(), 1f, null, _owner);
                ScreenOverlayManager.Instance.Reset();

                // The cap would still be spoken for had Reset left the demands behind.
                for (int i = 0; i < ScreenOverlayManager.ScriptCap; i++)
                {
                    ScreenOverlayManager.Instance
                        .StartScripted(Profile(), Guid.NewGuid(), 1f, null, _owner)
                        .Should()
                        .BeGreaterThan(0u);
                }

                ScreenOverlayManager.Instance.StopScriptedByOwner(_owner);
            }
        );
    }

    [Fact]
    public void StoppingAHandleThatIsNotLiveDoesNothing()
    {
        mainThread.Invoke(() =>
            {
                SetDecorations(true);

                uint live = ScreenOverlayManager.Instance.StartScripted(Profile(), Guid.NewGuid(), 1f, null, _owner);

                try
                {
                    ScreenOverlayManager.Instance.StopScripted(0);
                    ScreenOverlayManager.Instance.StopScripted(uint.MaxValue);
                    ScreenOverlayManager.Instance.StopScripted(live);

                    // Stopping the same handle twice must not reach whatever was started since.
                    ScreenOverlayManager.Instance.StopScripted(live);

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
