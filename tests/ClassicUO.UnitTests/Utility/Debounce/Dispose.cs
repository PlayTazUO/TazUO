using System.Threading;
using FluentAssertions;
using Xunit;
using DebounceClass = ClassicUO.Utility.Debounce.Debounce;

namespace ClassicUO.UnitTests.Utility.Debounce
{
    public class Dispose
    {
        [Fact]
        public void Dispose_Should_Cancel_Pending_Invocation()
        {
            int calls = 0;
            var debounce = new DebounceClass(() => Interlocked.Increment(ref calls), 30);

            debounce.Invoke();
            debounce.Dispose();

            Thread.Sleep(120);

            Volatile.Read(ref calls).Should().Be(0);
        }

        [Fact]
        public void Invoke_After_Dispose_Should_Be_A_NoOp()
        {
            int calls = 0;
            var debounce = new DebounceClass(() => Interlocked.Increment(ref calls), 20);
            debounce.Dispose();

            debounce.Invoke();

            Thread.Sleep(80);

            Volatile.Read(ref calls).Should().Be(0);
        }

        [Fact]
        public void Dispose_Should_Not_Throw_When_Called_Twice()
        {
            var debounce = new DebounceClass(() => { }, 20);
            debounce.Dispose();

            System.Action act = () => debounce.Dispose();

            act.Should().NotThrow();
        }
    }
}
