using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.ScreenDecorations.Overlays;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

public class ScreenOverlayCompositorBudgetTests
{
    private static List<ScreenOverlayCompositor.ActiveOverlay> Overlays(params int[] layerCounts)
    {
        var list = new List<ScreenOverlayCompositor.ActiveOverlay>();

        foreach (int count in layerCounts)
        {
            var overlay = new ScreenOverlayCompositor.ActiveOverlay();

            for (int i = 0; i < count; i++)
                overlay.Layers.Add(default);

            list.Add(overlay);
        }

        return list;
    }

    private static int[] LayerCountsOf(List<ScreenOverlayCompositor.ActiveOverlay> overlays)
    {
        return overlays.Select(o => o.Layers.Count).ToArray();
    }

    [Fact]
    public void KeepsEverythingWhenUnderBudget()
    {
        List<ScreenOverlayCompositor.ActiveOverlay> overlays = Overlays(2, 1, 3);

        ScreenOverlayCompositor.ApplyBudget(overlays, 12);

        LayerCountsOf(overlays).Should().Equal(2, 1, 3);
    }

    [Fact]
    public void KeepsExactlyBudgetWhenItFitsPrecisely()
    {
        List<ScreenOverlayCompositor.ActiveOverlay> overlays = Overlays(2, 2, 2);

        ScreenOverlayCompositor.ApplyBudget(overlays, 6);

        LayerCountsOf(overlays).Should().Equal(2, 2, 2);
    }

    [Fact]
    public void DropsOverflowingOverlaysWholeNotPartially()
    {
        List<ScreenOverlayCompositor.ActiveOverlay> overlays = Overlays(3, 3, 3);

        ScreenOverlayCompositor.ApplyBudget(overlays, 7);

        // Two overlays fit in seven layers; the third is dropped entirely rather than drawn
        // with one of its three layers missing.
        LayerCountsOf(overlays).Should().Equal(3, 3);
    }

    [Fact]
    public void StopsAtFirstOverlayThatDoesNotFit()
    {
        // The trailing single-layer overlay would fit in the leftover budget, but taking it
        // would let a lower-priority overlay displace the higher-priority one ahead of it.
        List<ScreenOverlayCompositor.ActiveOverlay> overlays = Overlays(2, 4, 1);

        ScreenOverlayCompositor.ApplyBudget(overlays, 3);

        LayerCountsOf(overlays).Should().Equal(2);
    }

    [Fact]
    public void DropsEverythingWhenTheFirstOverlayAlreadyExceedsBudget()
    {
        List<ScreenOverlayCompositor.ActiveOverlay> overlays = Overlays(4, 1);

        ScreenOverlayCompositor.ApplyBudget(overlays, 3);

        overlays.Should().BeEmpty();
    }

    [Fact]
    public void HandlesEmptyInput()
    {
        var overlays = new List<ScreenOverlayCompositor.ActiveOverlay>();

        ScreenOverlayCompositor.ApplyBudget(overlays, 12);

        overlays.Should().BeEmpty();
    }
}
