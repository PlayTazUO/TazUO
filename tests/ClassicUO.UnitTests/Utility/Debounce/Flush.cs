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

            debounce.Invoke();
            debounce.Flush();

            Volatile.Read(ref calls).Should().Be(1);
        }

        [Fact]
        public void Flush_Should_Prevent_The_Original_Timer_From_Also_Firing()
        {
            int calls = 0;
            var debounce = new DebounceClass(() => Interlocked.Increment(ref calls), 30);

            debounce.Invoke();
            debounce.Flush();

            Thread.Sleep(120);

            Volatile.Read(ref calls).Should().Be(1);
        }

        [Fact]
        public void Flush_With_No_Pending_Window_Should_Not_Invoke()
        {
            int calls = 0;
            var debounce = new DebounceClass(() => Interlocked.Increment(ref calls), 30);

            debounce.Flush();

            Volatile.Read(ref calls).Should().Be(0);
        }
    }
}
