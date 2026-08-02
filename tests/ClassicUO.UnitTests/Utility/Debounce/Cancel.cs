using System.Threading;
using FluentAssertions;
using Xunit;
using DebounceClass = ClassicUO.Utility.Debounce.Debounce;

namespace ClassicUO.UnitTests.Utility.Debounce
{
    public class Cancel
    {
        [Fact]
        public void Cancel_Should_Prevent_Pending_Trailing_Invocation()
        {
            int calls = 0;
            var debounce = new DebounceClass(() => Interlocked.Increment(ref calls), 40);

            try
            {
                debounce.Invoke();
                debounce.Cancel();

                // The canceled timer must never fire, so the count must stay at zero.
                TestWait.Until(() => Volatile.Read(ref calls) > 0).Should().BeFalse();
                Volatile.Read(ref calls).Should().Be(0);
            }
            finally
            {
                debounce.Dispose();
            }
        }

        [Fact]
        public void Call_After_Cancel_Should_Start_A_Fresh_Window()
        {
            int calls = 0;
            var debounce = new DebounceClass(() => Interlocked.Increment(ref calls), 30);

            try
            {
                debounce.Invoke();
                debounce.Cancel();

                debounce.Invoke();

                TestWait.Until(() => Volatile.Read(ref calls) == 1).Should().BeTrue();
            }
            finally
            {
                debounce.Dispose();
            }
        }

        [Fact]
        public void Cancel_With_Nothing_Pending_Should_Not_Throw()
        {
            var debounce = new DebounceClass(() => { }, 30);

            try
            {
                System.Action act = () => debounce.Cancel();

                act.Should().NotThrow();
            }
            finally
            {
                debounce.Dispose();
            }
        }

        [Fact]
        public void Cancel_After_Dispose_Should_Not_Throw()
        {
            var debounce = new DebounceClass(() => { }, 30);
            debounce.Invoke();
            debounce.Dispose();

            System.Action act = () => debounce.Cancel();

            act.Should().NotThrow();
        }
    }
}
