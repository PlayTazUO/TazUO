using ClassicUO.Common.Enums;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game;

/// <summary>
///     UseSkill macros store the <see cref="MacroSubType" /> value, so the enum order is part of the
///     save format and must stay fixed. Only the displayed skill name comes from the client data files.
/// </summary>
public class MacroUseSkillMappingTests
{
    [Theory]
    [InlineData(MacroSubType.Anatomy, 1)]
    [InlineData(MacroSubType.AnimalTaming, 35)]
    [InlineData(MacroSubType.ItemIdentification, 3)]
    [InlineData(MacroSubType.Imbuing, 56)]
    [InlineData(MacroSubType.Tracking, 38)]
    public void GetUseSkillIndex_ReturnsTheDataFileSkillIndex(MacroSubType subType, int expected)
    {
        Macro.GetUseSkillIndex(subType).Should().Be(expected);
    }

    [Fact]
    public void GetUseSkillIndex_MapsEverySkillGroupSubType()
    {
        for (int i = (int)MacroSubType.Anatomy; i < (int)MacroSubType.LeftHand; i++)
        {
            Macro.GetUseSkillIndex((MacroSubType)i).Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    public void GetUseSkillIndex_ReturnsMinusOneOutsideTheSkillGroup()
    {
        Macro.GetUseSkillIndex(MacroSubType.NW).Should().Be(-1);
        Macro.GetUseSkillIndex(MacroSubType.LeftHand).Should().Be(-1);
    }

    [Fact]
    public void GetSubTypeName_FallsBackToTheEnumNameForNonSkillActions()
    {
        Macro.GetSubTypeName(MacroType.Walk, MacroSubType.NW).Should().Be(nameof(MacroSubType.NW));
    }
}
