using System.Threading;
using FluentAssertions;
using Xunit;
using DebounceClass = ClassicUO.Utility.Debounce.Debounce;

namespace ClassicUO.UnitTests.Utility.Debounce
{
    public class Leading
    {
        [Fact]
        public void Leading_True_Should_Fire_Immediately_On_First_Call()
        {
            int calls = 0;
            var debounce = new DebounceClass(() => Interlocked.Increment(ref calls), 200, leading: true, trailing: false);

            debounce.Invoke();

            // No polling wait: this must already be true synchronously.
            Volatile.Read(ref calls).Should().Be(1);
        }

        [Fact]
        public void Leading_And_Trailing_Single_Call_Should_Fire_Only_Once()
        {
            int calls = 0;
            var debounce = new DebounceClass(() => Interlocked.Increment(ref calls), 40, leading: true, trailing: true);

            debounce.Invoke();
            Volatile.Read(ref calls).Should().Be(1);

            Thread.Sleep(120);

            Volatile.Read(ref calls).Should().Be(1);
        }

        [Fact]
        public void Leading_And_Trailing_Burst_Should_Fire_On_Both_Edges()
        {
            int calls = 0;
            var debounce = new DebounceClass(() => Interlocked.Increment(ref calls), 40, leading: true, trailing: true);

            debounce.Invoke();
            Volatile.Read(ref calls).Should().Be(1);

            Thread.Sleep(10);
            debounce.Invoke();

            TestWait.Until(() => Volatile.Read(ref calls) == 2).Should().BeTrue();
        }

        [Fact]
        public void Leading_Only_Should_Not_Fire_Again_Within_Same_Burst()
        {
            int calls = 0;
            var debounce = new DebounceClass(() => Interlocked.Increment(ref calls), 60, leading: true, trailing: false);

            for (int i = 0; i < 5; i++)
            {
                debounce.Invoke();
                Thread.Sleep(10);
            }

            Volatile.Read(ref calls).Should().Be(1);
        }
    }
}
