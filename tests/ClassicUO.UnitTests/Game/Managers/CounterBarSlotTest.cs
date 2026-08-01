using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Managers
{
    public class CounterBarSlotTest
    {
        [Fact]
        public void ShouldPlay_WhenNotRunning_True()
        {
            CounterBarSlot.ShouldPlay(isRunning: false).Should().BeTrue();
        }

        [Fact]
        public void ShouldPlay_WhenRunning_False()
        {
            CounterBarSlot.ShouldPlay(isRunning: true).Should().BeFalse();
        }

        [Fact]
        public void FromScript_Null_ReturnsEmpty()
        {
            CounterBarSlot.FromScript(null).IsEmpty.Should().BeTrue();
        }

        [Fact]
        public void ScriptSlotType_HasValue4()
        {
            ((int)CounterBarSlotType.Script).Should().Be(4);
        }

        [Fact]
        public void SkillSlotType_HasValue5()
        {
            ((int)CounterBarSlotType.Skill).Should().Be(5);
        }

        [Fact]
        public void DressAgentSlotType_HasValue6()
        {
            ((int)CounterBarSlotType.DressAgent).Should().Be(6);
        }

        [Fact]
        public void FromSkill_Negative_ReturnsEmpty()
        {
            CounterBarSlot.FromSkill(-1).IsEmpty.Should().BeTrue();
        }

        [Fact]
        public void FromSkill_ValidIndex_CreatesSkillSlot()
        {
            CounterBarSlot slot = CounterBarSlot.FromSkill(21); // Hiding

            slot.IsEmpty.Should().BeFalse();
            slot.Type.Should().Be(CounterBarSlotType.Skill);
            slot.SkillIndex.Should().Be(21);
        }

        [Fact]
        public void FromDressAgent_Null_ReturnsEmpty()
        {
            CounterBarSlot.FromDressAgent(null, false).IsEmpty.Should().BeTrue();
        }

        [Theory]
        [InlineData(false, "Dress: PvP")]
        [InlineData(true, "Undress: PvP")]
        public void FromDressAgent_ValidConfig_CreatesActionSlot(bool undress, string expectedLabel)
        {
            CounterBarSlot slot = CounterBarSlot.FromDressAgent(
                new DressConfig { Name = "PvP", CharacterName = "Character Name" }, undress);

            slot.IsEmpty.Should().BeFalse();
            slot.Type.Should().Be(CounterBarSlotType.DressAgent);
            slot.DressConfigName.Should().Be("PvP");
            slot.DressAgentUndress.Should().Be(undress);
            slot.SlotLabel.Should().Be(expectedLabel);
            slot.TryGetTooltip(null, out string tooltip).Should().BeTrue();
            tooltip.Should().Be(expectedLabel);
        }

        [Fact]
        public void SlotLabel_Macro_ReturnsMacroName()
        {
            var slot = new CounterBarSlot { Type = CounterBarSlotType.Macro, MacroName = "MyMacro" };
            slot.SlotLabel.Should().Be("MyMacro");
        }

        [Fact]
        public void SlotLabel_Spell_ReturnsNull()
        {
            new CounterBarSlot { Type = CounterBarSlotType.Spell, SpellId = 1 }.SlotLabel.Should().BeNull();
        }

        [Fact]
        public void SlotLabel_Empty_ReturnsNull()
        {
            CounterBarSlot.Empty().SlotLabel.Should().BeNull();
        }

        [Fact]
        public void ActiveHue_MatchesSpellBarValue()
        {
            CounterBarSlot.ActiveHue.Should().Be(38);
        }

        [Fact]
        public void GetActiveHue_NullWorld_ReturnsZero()
        {
            CounterBarSlot.FromAbility(true).GetActiveHue(null).Should().Be(0);
            new CounterBarSlot { Type = CounterBarSlotType.Spell, SpellId = 1 }.GetActiveHue(null).Should().Be(0);
            CounterBarSlot.Empty().GetActiveHue(null).Should().Be(0);
        }
    }
}
