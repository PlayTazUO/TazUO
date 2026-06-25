using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Managers
{
    public class SpellBarScriptSlotTest
    {
        [Fact]
        public void ShouldPlay_WhenNotRunning_True()
        {
            SpellBarSlot.ShouldPlay(isRunning: false).Should().BeTrue();
        }

        [Fact]
        public void ShouldPlay_WhenRunning_False()
        {
            SpellBarSlot.ShouldPlay(isRunning: true).Should().BeFalse();
        }

        [Fact]
        public void FromScript_Null_ReturnsEmpty()
        {
            SpellBarSlot.FromScript(null).IsEmpty.Should().BeTrue();
        }

        [Fact]
        public void ScriptSlotType_HasValue4()
        {
            ((int)SpellBarSlotType.Script).Should().Be(4);
        }
    }
}
