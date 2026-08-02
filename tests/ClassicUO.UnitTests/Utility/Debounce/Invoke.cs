using System.Threading;
using FluentAssertions;
using Xunit;
using DebounceClass = ClassicUO.Utility.Debounce.Debounce;

namespace ClassicUO.UnitTests.Utility.Debounce
{
    public class Invoke
    {
        [Fact]
        public void Single_Call_Should_Fire_Once_After_Wait()
        {
            int calls = 0;
            var debounce = new DebounceClass(() => Interlocked.Increment(ref calls), 30);

            try
            {
                debounce.Invoke();

                TestWait.Until(() => Volatile.Read(ref calls) == 1).Should().BeTrue();
            }
            finally
            {
                debounce.Dispose();
            }
        }

        [Fact]
        public void Rapid_Calls_Should_Collapse_Into_One_Trailing_Invocation()
        {
            int calls = 0;
            var debounce = new DebounceClass(() => Interlocked.Increment(ref calls), 60);

            try
            {
                for (int i = 0; i < 10; i++)
                {
                    debounce.Invoke();
                    Thread.Sleep(10);
                }

                TestWait.Until(() => Volatile.Read(ref calls) == 1).Should().BeTrue();

                // Make sure no further invocation sneaks in.
                TestWait.Until(() => Volatile.Read(ref calls) != 1).Should().BeFalse();
                Volatile.Read(ref calls).Should().Be(1);
            }
            finally
            {
                debounce.Dispose();
            }
        }

        [Fact]
        public void Calls_Spaced_Beyond_Wait_Should_Each_Fire_Separately()
        {
            int calls = 0;
            var debounce = new DebounceClass(() => Interlocked.Increment(ref calls), 30);

            try
            {
                debounce.Invoke();
                TestWait.Until(() => Volatile.Read(ref calls) == 1).Should().BeTrue();

                debounce.Invoke();
                TestWait.Until(() => Volatile.Read(ref calls) == 2).Should().BeTrue();
            }
            finally
            {
                debounce.Dispose();
            }
        }
    }
}
