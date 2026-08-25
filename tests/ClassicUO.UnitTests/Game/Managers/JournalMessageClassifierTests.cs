using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using Xunit;

namespace ClassicUO.UnitTests.Game.Managers;

public class JournalMessageClassifierTests
{
    [Fact]
    public void Classify_MatchingSystemMessage_ReturnsChatSystem()
    {
        MessageType result = JournalMessageClassifier.Classify(
            MessageType.System,
            "[17:00] Maxwe: Hello world",
            true
        );

        Assert.Equal(MessageType.ChatSystem, result);
    }

    [Theory]
    [InlineData("World save complete. The entire process took 6.34 seconds.")]
    [InlineData("You have hidden yourself well.")]
    [InlineData("[24:00] Maxwe: Invalid time")]
    [InlineData("[17:00] : Missing name")]
    [InlineData("[17:00] Maxwe:")]
    public void Classify_NonMatchingSystemMessage_RemainsSystem(string text)
    {
        MessageType result = JournalMessageClassifier.Classify(MessageType.System, text, true);

        Assert.Equal(MessageType.System, result);
    }

    [Fact]
    public void Classify_MatchingSystemMessageWhenDisabled_RemainsSystem()
    {
        MessageType result = JournalMessageClassifier.Classify(
            MessageType.System,
            "[17:00] Maxwe: Hello world",
            false
        );

        Assert.Equal(MessageType.System, result);
    }

    [Fact]
    public void Classify_MatchingNonSystemMessage_PreservesOriginalType()
    {
        MessageType result = JournalMessageClassifier.Classify(
            MessageType.Guild,
            "[17:00] Maxwe: Hello world",
            true
        );

        Assert.Equal(MessageType.Guild, result);
    }

    [Fact]
    public void MatchesMessageTypeFilter_ClassifiedSystemText_MatchesOnlyGlobalChat()
    {
        bool matchesGlobalChat = ResizableJournal.MatchesMessageTypeFilter(
            TextType.SYSTEM,
            MessageType.ChatSystem,
            MessageType.ChatSystem
        );
        bool matchesSystem = ResizableJournal.MatchesMessageTypeFilter(
            TextType.SYSTEM,
            MessageType.ChatSystem,
            MessageType.System
        );

        Assert.True(matchesGlobalChat);
        Assert.False(matchesSystem);
    }
}
