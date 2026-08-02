using System;
using FluentAssertions;
using Xunit;
using DebounceClass = ClassicUO.Utility.Debounce.Debounce;

namespace ClassicUO.UnitTests.Utility.Debounce
{
    public class Constructor
    {
        [Fact]
        public void Null_Action_Should_Throw()
        {
            Action act = () => new DebounceClass(null, 10);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Negative_WaitMs_Should_Throw()
        {
            Action act = () => new DebounceClass(() => { }, -1);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Zero_WaitMs_Should_Not_Throw()
        {
            using var debounce = new DebounceClass(() => { }, 0);
        }
    }
}
