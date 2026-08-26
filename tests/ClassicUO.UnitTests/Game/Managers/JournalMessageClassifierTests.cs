using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using Xunit;

namespace ClassicUO.UnitTests.Game.Managers;

public class JournalMessageClassifierTests
{
    /// <summary>Verifies that a complete timestamped system chat line becomes Global Chat.</summary>
    [Fact]
    public void Classify_MatchingSystemMessage_ReturnsChatSystem()
    {
        MessageType result = JournalMessageClassifier.Classify(
            MessageType.System,
            "[17:00] Maxwe: Hello world",
            true,
            Profile.DefaultSystemMessageGlobalChatRegex
        );

        Assert.Equal(MessageType.ChatSystem, result);
    }

    /// <summary>Verifies that ordinary or malformed system messages remain System messages.</summary>
    /// <param name="text">The nonmatching system message to classify.</param>
    [Theory]
    [InlineData("World save complete. The entire process took 6.34 seconds.")]
    [InlineData("You have hidden yourself well.")]
    [InlineData("[24:00] Maxwe: Invalid time")]
    [InlineData("[17:00] : Missing name")]
    [InlineData("[17:00] Maxwe:")]
    public void Classify_NonMatchingSystemMessage_RemainsSystem(string text)
    {
        MessageType result = JournalMessageClassifier.Classify(
            MessageType.System,
            text,
            true,
            Profile.DefaultSystemMessageGlobalChatRegex
        );

        Assert.Equal(MessageType.System, result);
    }

    /// <summary>Verifies that matching remains disabled unless the profile option is enabled.</summary>
    [Fact]
    public void Classify_MatchingSystemMessageWhenDisabled_RemainsSystem()
    {
        MessageType result = JournalMessageClassifier.Classify(
            MessageType.System,
            "[17:00] Maxwe: Hello world",
            false,
            Profile.DefaultSystemMessageGlobalChatRegex
        );

        Assert.Equal(MessageType.System, result);
    }

    /// <summary>Verifies that the classifier never changes non-System message types.</summary>
    [Fact]
    public void Classify_MatchingNonSystemMessage_PreservesOriginalType()
    {
        MessageType result = JournalMessageClassifier.Classify(
            MessageType.Guild,
            "[17:00] Maxwe: Hello world",
            true,
            Profile.DefaultSystemMessageGlobalChatRegex
        );

        Assert.Equal(MessageType.Guild, result);
    }

    /// <summary>Verifies that shards can supply their own global chat format.</summary>
    [Fact]
    public void Classify_CustomPattern_UsesConfiguredFormat()
    {
        MessageType result = JournalMessageClassifier.Classify(
            MessageType.System,
            "[Global] Maxwe > Hello world",
            true,
            @"^\[Global\] [^>]+ > .+$"
        );

        Assert.Equal(MessageType.ChatSystem, result);
    }

    /// <summary>Verifies that a malformed configured pattern cannot interrupt journal processing.</summary>
    [Fact]
    public void Classify_InvalidPattern_RemainsSystem()
    {
        MessageType result = JournalMessageClassifier.Classify(
            MessageType.System,
            "[Global] Maxwe > Hello world",
            true,
            "["
        );

        Assert.Equal(MessageType.System, result);
    }

    /// <summary>Verifies that classified system text appears only in the Global Chat filter.</summary>
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
