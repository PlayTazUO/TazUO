using ClassicUO.Game.Managers;
using ClassicUO.Game.ScreenDecorations.Triggers;
using ClassicUO.Game.ScreenDecorations.Triggers.Implementations;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

public class ObjectUsedTriggerTests
{
    private const uint FIRST_SERIAL = 0x40000001;
    private const uint SECOND_SERIAL = 0x40000002;
    private const uint UNWATCHED_SERIAL = 0x40000003;

    [Fact]
    public void AnyOfSeveralConfiguredSerialsMatches()
    {
        var parameters = new ObjectUsedParameters { Serials = [FIRST_SERIAL, SECOND_SERIAL] };

        using var trigger = new ObjectUsedTrigger(parameters);
        trigger.Attach();

        TriggerFiredArgs? fired = null;
        trigger.Fired += (_, args) => fired = args;

        EventSink.InvokeOnObjectUsed(SECOND_SERIAL);

        fired.Should().NotBeNull();
        fired!.Signal.Duration.Should().Be(parameters.Duration);
    }

    [Fact]
    public void AnUnconfiguredSerialIsIgnored()
    {
        var parameters = new ObjectUsedParameters { Serials = [FIRST_SERIAL] };

        using var trigger = new ObjectUsedTrigger(parameters);
        trigger.Attach();

        TriggerFiredArgs? fired = null;
        trigger.Fired += (_, args) => fired = args;

        EventSink.InvokeOnObjectUsed(UNWATCHED_SERIAL);

        fired.Should().BeNull();
    }

    [Fact]
    public void ADetachedTriggerHearsNothing()
    {
        var parameters = new ObjectUsedParameters { Serials = [FIRST_SERIAL] };

        using var trigger = new ObjectUsedTrigger(parameters);
        trigger.Attach();
        trigger.Detach();

        TriggerFiredArgs? fired = null;
        trigger.Fired += (_, args) => fired = args;

        EventSink.InvokeOnObjectUsed(FIRST_SERIAL);

        fired.Should().BeNull();
    }
}
