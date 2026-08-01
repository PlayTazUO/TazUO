using System.Threading;
using FluentAssertions;
using Xunit;
using DebounceOfT = ClassicUO.Utility.Debounce.Debounce<string>;

namespace ClassicUO.UnitTests.Utility.Debounce
{
    public class GenericArgument
    {
        [Fact]
        public void Trailing_Invocation_Should_Use_The_Latest_Argument()
        {
            string received = null;
            var debounce = new DebounceOfT(arg => received = arg, 40);

            debounce.Invoke("first");
            Thread.Sleep(10);
            debounce.Invoke("second");
            Thread.Sleep(10);
            debounce.Invoke("third");

            TestWait.Until(() => received == "third").Should().BeTrue();
        }

        [Fact]
        public void Leading_Invocation_Should_Use_The_Triggering_Argument()
        {
            string received = null;
            var debounce = new DebounceOfT(arg => received = arg, 200, leading: true, trailing: false);

            debounce.Invoke("only");

            received.Should().Be("only");
        }

        [Fact]
        public void Null_Action_Should_Throw()
        {
            System.Action act = () => new DebounceOfT(null, 10);

            act.Should().Throw<System.ArgumentNullException>();
        }
    }
}
