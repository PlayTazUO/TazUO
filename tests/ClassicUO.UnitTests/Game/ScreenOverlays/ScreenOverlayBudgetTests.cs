using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.ScreenOverlays;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenOverlays
{
    public class ScreenOverlayBudgetTests
    {
        private static List<ScreenOverlayManager.ActiveOverlay> Overlays(params int[] layerCounts)
        {
            var list = new List<ScreenOverlayManager.ActiveOverlay>();

            foreach (int count in layerCounts)
            {
                var overlay = new ScreenOverlayManager.ActiveOverlay();

                for (int i = 0; i < count; i++)
                    overlay.Layers.Add(default);

                list.Add(overlay);
            }

            return list;
        }

        private static int[] LayerCountsOf(List<ScreenOverlayManager.ActiveOverlay> overlays) =>
            overlays.Select(o => o.Layers.Count).ToArray();

        [Fact]
        public void KeepsEverythingWhenUnderBudget()
        {
            List<ScreenOverlayManager.ActiveOverlay> overlays = Overlays(2, 1, 3);

            ScreenOverlayManager.ApplyBudget(overlays, 12);

            LayerCountsOf(overlays).Should().Equal(2, 1, 3);
        }

        [Fact]
        public void KeepsExactlyBudgetWhenItFitsPrecisely()
        {
            List<ScreenOverlayManager.ActiveOverlay> overlays = Overlays(2, 2, 2);

            ScreenOverlayManager.ApplyBudget(overlays, 6);

            LayerCountsOf(overlays).Should().Equal(2, 2, 2);
        }

        [Fact]
        public void DropsOverflowingOverlaysWholeNotPartially()
        {
            List<ScreenOverlayManager.ActiveOverlay> overlays = Overlays(3, 3, 3);

            ScreenOverlayManager.ApplyBudget(overlays, 7);

            // Two overlays fit in seven layers; the third is dropped entirely rather than drawn
            // with one of its three layers missing.
            LayerCountsOf(overlays).Should().Equal(3, 3);
        }

        [Fact]
        public void StopsAtFirstOverlayThatDoesNotFit()
        {
            // The trailing single-layer overlay would fit in the leftover budget, but taking it
            // would let a lower-priority overlay displace the higher-priority one ahead of it.
            List<ScreenOverlayManager.ActiveOverlay> overlays = Overlays(2, 4, 1);

            ScreenOverlayManager.ApplyBudget(overlays, 3);

            LayerCountsOf(overlays).Should().Equal(2);
        }

        [Fact]
        public void DropsEverythingWhenTheFirstOverlayAlreadyExceedsBudget()
        {
            List<ScreenOverlayManager.ActiveOverlay> overlays = Overlays(4, 1);

            ScreenOverlayManager.ApplyBudget(overlays, 3);

            overlays.Should().BeEmpty();
        }

        [Fact]
        public void HandlesEmptyInput()
        {
            var overlays = new List<ScreenOverlayManager.ActiveOverlay>();

            ScreenOverlayManager.ApplyBudget(overlays, 12);

            overlays.Should().BeEmpty();
        }
    }
}
