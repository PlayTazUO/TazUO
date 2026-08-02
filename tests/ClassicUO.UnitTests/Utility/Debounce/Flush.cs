using System.Threading;
using FluentAssertions;
using Xunit;
using DebounceClass = ClassicUO.Utility.Debounce.Debounce;

namespace ClassicUO.UnitTests.Utility.Debounce
{
    public class Flush
    {
        [Fact]
        public void Flush_Should_Invoke_Pending_Call_Immediately()
        {
            int calls = 0;
            var debounce = new DebounceClass(() => Interlocked.Increment(ref calls), 5000);

            try
            {
                debounce.Invoke();
                debounce.Flush();

                Volatile.Read(ref calls).Should().Be(1);
            }
            finally
            {
                debounce.Dispose();
            }
        }

        [Fact]
        public void Flush_Should_Prevent_The_Original_Timer_From_Also_Firing()
        {
            int calls = 0;
            var debounce = new DebounceClass(() => Interlocked.Increment(ref calls), 30);

            try
            {
                debounce.Invoke();
                debounce.Flush();

                Volatile.Read(ref calls).Should().Be(1);

                // The canceled timer must never fire again, so the count must stay at one.
                TestWait.Until(() => Volatile.Read(ref calls) != 1).Should().BeFalse();
                Volatile.Read(ref calls).Should().Be(1);
            }
            finally
            {
                debounce.Dispose();
            }
        }

        [Fact]
        public void Flush_With_No_Pending_Window_Should_Not_Invoke()
        {
            int calls = 0;
            var debounce = new DebounceClass(() => Interlocked.Increment(ref calls), 30);

            try
            {
                debounce.Flush();

                Volatile.Read(ref calls).Should().Be(0);
            }
            finally
            {
                debounce.Dispose();
            }
        }
    }
}
