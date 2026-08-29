using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.ScreenDecorations.Triggers;
using ClassicUO.Game.ScreenDecorations.Triggers.Implementations;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

public class BuffChangedTriggerTests
{
    private static BuffIcon Icon(BuffIconType type) => new(type, 0, 0, string.Empty);

    [Fact]
    public void AnyOfSeveralConfiguredBuffsMatches()
    {
        var parameters = new BuffChangedParameters
        {
            Mode = BuffTriggerMode.Added,
            BuffTypes = [(short)BuffIconType.NightSight, (short)BuffIconType.DeathStrike]
        };

        using var trigger = new BuffChangedTrigger(parameters);
        trigger.Attach();

        TriggerFiredArgs? fired = null;
        trigger.Fired += (_, args) => fired = args;

        EventSink.InvokeOnBuffAdded(null, new BuffEventArgs(Icon(BuffIconType.DeathStrike)));

        fired.Should().NotBeNull();
    }

    [Fact]
    public void AnUnconfiguredBuffIsIgnored()
    {
        var parameters = new BuffChangedParameters
        {
            Mode = BuffTriggerMode.Added,
            BuffTypes = [(short)BuffIconType.NightSight]
        };

        using var trigger = new BuffChangedTrigger(parameters);
        trigger.Attach();

        TriggerFiredArgs? fired = null;
        trigger.Fired += (_, args) => fired = args;

        EventSink.InvokeOnBuffAdded(null, new BuffEventArgs(Icon(BuffIconType.DeathStrike)));

        fired.Should().BeNull();
    }

    [Fact]
    public void RemovalOfAnyConfiguredBuffMatchesUnderTheRemovedMode()
    {
        var parameters = new BuffChangedParameters
        {
            Mode = BuffTriggerMode.Removed,
            BuffTypes = [(short)BuffIconType.NightSight, (short)BuffIconType.DeathStrike]
        };

        using var trigger = new BuffChangedTrigger(parameters);
        trigger.Attach();

        TriggerFiredArgs? fired = null;
        trigger.Fired += (_, args) => fired = args;

        EventSink.InvokeOnBuffRemoved(null, new BuffEventArgs(Icon(BuffIconType.NightSight)));

        fired.Should().NotBeNull();
    }
}
