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

        [Fact]
        public void SkillSlotType_HasValue5()
        {
            ((int)SpellBarSlotType.Skill).Should().Be(5);
        }

        [Fact]
        public void FromSkill_Negative_ReturnsEmpty()
        {
            SpellBarSlot.FromSkill(-1).IsEmpty.Should().BeTrue();
        }

        [Fact]
        public void FromSkill_ValidIndex_CreatesSkillSlot()
        {
            SpellBarSlot slot = SpellBarSlot.FromSkill(21); // Hiding

            slot.IsEmpty.Should().BeFalse();
            slot.Type.Should().Be(SpellBarSlotType.Skill);
            slot.SkillIndex.Should().Be(21);
        }
    }
}
