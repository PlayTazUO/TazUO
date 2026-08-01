using System.Threading;
using FluentAssertions;
using Xunit;
using DebounceClass = ClassicUO.Utility.Debounce.Debounce;

namespace ClassicUO.UnitTests.Utility.Debounce
{
    public class MaxWait
    {
        [Fact]
        public void Continuous_Calls_Should_Still_Fire_Within_MaxWait()
        {
            int calls = 0;
            var debounce = new DebounceClass(() => Interlocked.Increment(ref calls), waitMs: 50, maxWaitMs: 120);

            // Keep the window alive continuously (each call resets the 50ms wait) for well past maxWaitMs.
            var sw = System.Diagnostics.Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < 400)
            {
                debounce.Invoke();
                Thread.Sleep(15);
            }

            Volatile.Read(ref calls).Should().BeGreaterThan(1);
        }

        [Fact]
        public void Without_MaxWait_Continuous_Calls_Never_Fire()
        {
            int calls = 0;
            var debounce = new DebounceClass(() => Interlocked.Increment(ref calls), 50);

            var sw = System.Diagnostics.Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < 200)
            {
                debounce.Invoke();
                Thread.Sleep(15);
            }

            Volatile.Read(ref calls).Should().Be(0);
        }
    }
}
