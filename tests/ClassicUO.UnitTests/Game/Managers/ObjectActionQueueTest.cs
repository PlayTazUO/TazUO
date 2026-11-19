using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Managers
{
    public class ObjectActionQueueTest
    {
        [Fact]
        public void CancelMovesFor_RunsCompletionCallbacksOfRemovedItems()
        {
            const uint serial = 0x0A000001;
            int completions = 0;

            ObjectActionQueue.Instance.Enqueue(
                new ObjectActionQueueItem(() => { }, _ => completions++) { Serial = serial },
                ActionPriority.LootItemMedium
            );

            ObjectActionQueue.Instance.Enqueue(
                new ObjectActionQueueItem(() => { }, _ => completions++) { Serial = serial },
                ActionPriority.LootItemMedium
            );

            ObjectActionQueue.Instance.CancelMovesFor(serial);

            completions.Should().Be(2, "removed items must still report completion so callers can clear in-flight state");
        }

        [Fact]
        public void CancelMovesFor_LeavesOtherItemsQueued()
        {
            const uint canceledSerial = 0x0A000002;
            const uint keptSerial = 0x0A000003;
            int canceledCompletions = 0;
            int keptCompletions = 0;

            ObjectActionQueue.Instance.Enqueue(
                new ObjectActionQueueItem(() => { }, _ => canceledCompletions++) { Serial = canceledSerial },
                ActionPriority.LootItemMedium
            );

            ObjectActionQueue.Instance.Enqueue(
                new ObjectActionQueueItem(() => { }, _ => keptCompletions++) { Serial = keptSerial },
                ActionPriority.LootItemHigh
            );

            ObjectActionQueue.Instance.CancelMovesFor(canceledSerial);

            canceledCompletions.Should().Be(1);
            keptCompletions.Should().Be(0);
            ObjectActionQueue.Instance.IsEmpty.Should().BeFalse();

            // Drain the remaining action so the shared queue singleton is left empty for other tests.
            ObjectActionQueue.Instance.CancelMovesFor(keptSerial);
            keptCompletions.Should().Be(1);
            ObjectActionQueue.Instance.IsEmpty.Should().BeTrue();
        }
    }
}
