using System;
using ClassicUO.LegionScripting;
using ClassicUO.UnitTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.LegionScript;

/// <summary>
/// Covers the stop signals exposed by <see cref="LegionAPI" />: the public flag script code polls,
/// the token that gates main-thread work during teardown, and the token that aborts blocking work
/// once the graceful stop window is over.
/// </summary>
[Collection(MainThreadCollection.Name)]
public class LegionApiStopTests : IDisposable
{
    private readonly LegionAPI _api;

    public LegionApiStopTests()
    {
        Client.UnitTestingActive = true;
        _api = new LegionAPI(new CSharpCallbackChannel(), null);
    }

    public void Dispose() => _api.Dispose();

    [Fact]
    public void Pause_WorksDuringGracefulStop_WhileOnlyStopRequestedIsSet()
    {
        // StopRequested flips as soon as OnStop starts; the stop token only cancels at the end of
        // the grace period, so cleanup can still wait on the main thread during that window.
        _api.StopRequested = true;

        Action pause = () => _api.Pause(0.02);

        pause.Should().NotThrow();
    }

    [Fact]
    public void Pause_Throws_WhenStopTokenCancelled()
    {
        _api.StopTokenSource.Cancel();

        Action pause = () => _api.Pause(10);

        pause.Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public void Pause_Throws_WhenMainTokenCancelled()
    {
        // The stop token is linked to the main token, so teardown also aborts a pending pause.
        _api.CancellationToken.Cancel();

        Action pause = () => _api.Pause(10);

        pause.Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public void StopPending_DoesNotDeriveFromScriptFacingStopRequested()
    {
        _api.StopRequested = true;
        _api.IsStopPending.Should().BeFalse();

        _api.BeginStop();

        _api.IsStopPending.Should().BeTrue();
        _api.StopRequested.Should().BeTrue();
    }

    [Fact]
    public void OnStopCallback_RunsWithStopRequestedSet()
    {
        bool? stopRequestedInCallback = null;

        _api.OnStop(new Action(() => stopRequestedInCallback = _api.StopRequested));
        _api.BeginStopCallback().Should().BeTrue();

        _api.ProcessCallbacks();

        stopRequestedInCallback.Should().BeTrue();
        _api.OnStopCompleted.Should().BeTrue();
    }

    [Fact]
    public void OnStopCallback_CanPauseDuringGrace()
    {
        _api.OnStop(new Action(() => _api.Pause(0.02)));
        _api.BeginStopCallback().Should().BeTrue();

        Action process = () => _api.ProcessCallbacks();

        process.Should().NotThrow();
        _api.OnStopCompleted.Should().BeTrue();
    }

    [Fact]
    public void ProcessCallbacks_RethrowsCallbackException()
    {
        _api.ScheduleCallback(new Action(() => throw new InvalidOperationException("boom")));

        Action process = () => _api.ProcessCallbacks();

        process.Should().Throw<InvalidOperationException>().WithMessage("boom");
    }
}
