using System.Text.RegularExpressions;
using ClassicUO.Game.ScreenDecorations.Triggers.Implementations;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

/// <summary>
/// Chat matching. Case is a property of the comparison rather than of any one way of making one,
/// so the flag has to reach every mode - the regex included, where it is baked into the compiled
/// pattern rather than passed at match time.
/// </summary>
public class ChatMessageTriggerTests
{
    private static bool Matches(ChatMessageParameters parameters, string text)
    {
        return ChatMessageTrigger.MatchesText(parameters, ChatMessageTrigger.CompilePattern(parameters), text);
    }

    private static ChatMessageParameters Params(ChatMatchMode mode, string pattern, bool caseSensitive)
    {
        return new ChatMessageParameters { Mode = mode, Pattern = pattern, CaseSensitive = caseSensitive };
    }

    [Theory]
    [InlineData(ChatMatchMode.Contains, "you feel")]
    [InlineData(ChatMatchMode.Exact, "You feel poisoned")]
    [InlineData(ChatMatchMode.StartsWith, "you feel")]
    [InlineData(ChatMatchMode.Regex, "you feel .*")]
    public void EveryModeIgnoresCaseByDefault(ChatMatchMode mode, string pattern)
    {
        Matches(Params(mode, pattern, caseSensitive: false), "You feel poisoned").Should().BeTrue();
    }

    /// <summary>The flag is worth nothing if a mode quietly ignores it.</summary>
    [Theory]
    [InlineData(ChatMatchMode.Contains, "you feel")]
    [InlineData(ChatMatchMode.Exact, "you feel poisoned")]
    [InlineData(ChatMatchMode.StartsWith, "you feel")]
    [InlineData(ChatMatchMode.Regex, "you feel .*")]
    public void EveryModeRespectsTheFlagWhenSet(ChatMatchMode mode, string pattern)
    {
        Matches(Params(mode, pattern, caseSensitive: true), "You feel poisoned").Should().BeFalse();
    }

    [Theory]
    [InlineData(ChatMatchMode.Contains, "You feel")]
    [InlineData(ChatMatchMode.Exact, "You feel poisoned")]
    [InlineData(ChatMatchMode.StartsWith, "You feel")]
    [InlineData(ChatMatchMode.Regex, "You feel .*")]
    public void ACaseSensitiveMatchStillMatchesTheRightCase(ChatMatchMode mode, string pattern)
    {
        Matches(Params(mode, pattern, caseSensitive: true), "You feel poisoned").Should().BeTrue();
    }

    /// <summary>
    /// The regex has no case argument at match time, so the flag has to be resolved when the
    /// pattern is compiled - which is once per rule, not once per line.
    /// </summary>
    [Fact]
    public void TheRegexCarriesItsCaseHandling()
    {
        Regex insensitive = ChatMessageTrigger.CompilePattern(Params(ChatMatchMode.Regex, "abc", false));
        Regex sensitive = ChatMessageTrigger.CompilePattern(Params(ChatMatchMode.Regex, "abc", true));

        insensitive.Options.Should().HaveFlag(RegexOptions.IgnoreCase);
        sensitive.Options.Should().NotHaveFlag(RegexOptions.IgnoreCase);
    }

    /// <summary>A pattern is only compiled where one is used; the plain modes must not pay for
    /// it.</summary>
    [Fact]
    public void OnlyRegexModeCompilesAPattern()
    {
        ChatMessageTrigger.CompilePattern(Params(ChatMatchMode.Contains, "abc", false)).Should().BeNull();
        ChatMessageTrigger.CompilePattern(Params(ChatMatchMode.Regex, string.Empty, false)).Should().BeNull();
    }

    /// <summary>
    /// A user-authored pattern is untrusted input. A broken one has to disable the rule rather
    /// than throw on every line the client displays.
    /// </summary>
    [Fact]
    public void ABrokenPatternNeverMatchesAndNeverThrows()
    {
        ChatMessageParameters parameters = Params(ChatMatchMode.Regex, "you feel (", false);

        ChatMessageTrigger.CompilePattern(parameters).Should().BeNull();
        Matches(parameters, "you feel (").Should().BeFalse();
    }

    [Fact]
    public void AnEmptyPatternOrLineNeverMatches()
    {
        Matches(Params(ChatMatchMode.Contains, string.Empty, false), "anything").Should().BeFalse();
        Matches(Params(ChatMatchMode.Contains, "anything", false), string.Empty).Should().BeFalse();
        Matches(Params(ChatMatchMode.Contains, "anything", false), null).Should().BeFalse();
    }

    [Fact]
    public void CloneCarriesTheCaseFlag()
    {
        var original = Params(ChatMatchMode.Exact, "abc", caseSensitive: true);

        var copy = (ChatMessageParameters)original.Clone();

        copy.CaseSensitive.Should().BeTrue();
        copy.Should().BeEquivalentTo(original);
    }
}
