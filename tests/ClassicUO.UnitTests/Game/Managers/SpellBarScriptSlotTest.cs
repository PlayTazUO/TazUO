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

        [Fact]
        public void AbbreviateName_Capitals_UsesCapitalLetters()
        {
            SpellBarSlot.AbbreviateName("Last Object Macro").Should().Be("LOM");
        }

        [Fact]
        public void AbbreviateName_NoCapitals_UsesWordInitials()
        {
            SpellBarSlot.AbbreviateName("loot all corpses").Should().Be("LAC");
        }

        [Fact]
        public void AbbreviateName_NullOrEmpty_ReturnsEmpty()
        {
            SpellBarSlot.AbbreviateName(null).Should().BeEmpty();
            SpellBarSlot.AbbreviateName(string.Empty).Should().BeEmpty();
        }

        [Fact]
        public void SlotLabel_Macro_ReturnsMacroName()
        {
            var slot = new SpellBarSlot { Type = SpellBarSlotType.Macro, MacroName = "MyMacro" };
            slot.SlotLabel.Should().Be("MyMacro");
        }

        [Fact]
        public void SlotLabel_Spell_ReturnsNull()
        {
            new SpellBarSlot { Type = SpellBarSlotType.Spell, SpellId = 1 }.SlotLabel.Should().BeNull();
        }

        [Fact]
        public void SlotLabel_Empty_ReturnsNull()
        {
            SpellBarSlot.Empty().SlotLabel.Should().BeNull();
        }
    }
}
