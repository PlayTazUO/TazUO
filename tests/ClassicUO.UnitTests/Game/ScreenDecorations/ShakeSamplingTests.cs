using System;
using System.Collections.Generic;
using ClassicUO.Game.ScreenDecorations.Shake;
using ClassicUO.UnitTests.Fixtures;
using FluentAssertions;
using Microsoft.Xna.Framework;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

/// <summary>
/// What the shake accumulators feed into the noise table, which has to keep moving for as long as a
/// client is left running - a session is measured in days, and the offset is read every frame of it.
/// <para>
/// These drive the real accumulators, which are static, so they join the main-thread collection to
/// serialize against the tests that drive the manager.
/// </para>
/// </summary>
[Collection(MainThreadCollection.Name)]
public class ShakeSamplingTests : IDisposable
{
    public ShakeSamplingTests()
    {
        ScreenShake.Enabled = true;
        ScreenShake.Viewport.Clear();
    }

    public void Dispose()
    {
        ScreenShake.Viewport.Clear();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// The table position is walked and wrapped per frame rather than multiplied out of a running
    /// clock. A clock only grows, and single precision thins out as it does: past a few days of
    /// uptime one frame's advance fell under one ulp, the position stopped moving, and shake held a
    /// static offset instead of oscillating.
    /// <para>
    /// Exercised through the decaying accumulator, since that is the one that lives as long as the
    /// session - a scheduled shake walks a position of its own that only has to last its duration.
    /// Trauma is re-set every frame so amplitude holds still and the position is the only thing left
    /// that can move the offset.
    /// </para>
    /// </summary>
    [Fact]
    public void ShakeStillOscillatesAfterDaysOfUptime()
    {
        // ~167h, in strides - only the elapsed total matters here, not how it was accrued.
        for (int pump = 0; pump < 600; pump++)
            ScreenShake.Viewport.GetOffset(1000f, 1f);

        var offsets = new HashSet<Point>();

        for (int frame = 0; frame < 120; frame++)
        {
            ScreenShake.Viewport.SetTrauma(1f);
            offsets.Add(ScreenShake.Viewport.GetOffset(1f / 60f, 1f));
        }

        offsets.Count.Should().BeGreaterThan(1);
    }
}
