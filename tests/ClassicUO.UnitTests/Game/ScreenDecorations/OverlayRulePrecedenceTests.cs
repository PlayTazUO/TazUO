using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Rules;
using ClassicUO.Game.ScreenDecorations.Manager;
using ClassicUO.Game.ScreenDecorations.Rules;
using ClassicUO.Game.ScreenDecorations.Triggers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

/// <summary>
/// Rules are evaluated firewall-style: top to bottom, first match claims the effect. Anything
/// below that raises the same look is skipped, so a look that decides something singular - which
/// rectangle its shake displaces - has exactly one rule answering for it.
/// </summary>
public class OverlayRulePrecedenceTests
{
    /// <summary>A trigger that fires, or does not, on command.</summary>
    private sealed class StubTrigger(bool firing, float intensity = 1f) : IPollingTrigger
    {
        public int SampleCount { get; private set; }

        public void Attach()
        {
        }

        public void Detach()
        {
        }

        public void Dispose()
        {
        }

        public TriggerSignal? Sample()
        {
            SampleCount++;

            return firing ? new TriggerSignal { Intensity = intensity } : null;
        }
    }

    private static EffectProfile Profile(string name)
    {
        return new EffectProfile { Name = name };
    }

    private static WatchedRule Watched(string name, uint order, EffectProfile profile, StubTrigger trigger)
    {
        return new WatchedRule(
            new OverlayRule { Name = name, Order = order, ProfileId = profile.Id },
            profile,
            trigger,
            TriggerKind.Poll
        );
    }

    private static Dictionary<Guid, RuleDemand> Select(params WatchedRule[] ordered)
    {
        var desired = new Dictionary<Guid, RuleDemand>();

        ScreenOverlayManager.SelectFirstMatches([.. ordered], [], desired);

        return desired;
    }

    [Fact]
    public void TheTopmostFiringRuleClaimsTheEffect()
    {
        EffectProfile poison = Profile("Poison");

        WatchedRule first = Watched("first", 0, poison, new StubTrigger(true));
        WatchedRule second = Watched("second", 1, poison, new StubTrigger(true));

        Dictionary<Guid, RuleDemand> desired = Select(first, second);

        desired.Should().ContainSingle();
        desired.Should().ContainKey(first.Rule.Id);
        desired.Should().NotContainKey(second.Rule.Id);
    }

    /// <summary>
    /// Losing the effect must cost nothing. A skipped rule's condition reaches into live game
    /// state, and evaluating it for a result that is thrown away is the one cost this ordering
    /// could have introduced.
    /// </summary>
    [Fact]
    public void ARuleThatLostTheEffectIsNotEvenSampled()
    {
        EffectProfile poison = Profile("Poison");

        var loser = new StubTrigger(true);

        Select(Watched("first", 0, poison, new StubTrigger(true)), Watched("second", 1, poison, loser));

        loser.SampleCount.Should().Be(0);
    }

    [Fact]
    public void ALowerRuleWinsTheEffectWhenTheOneAboveIsNotFiring()
    {
        EffectProfile poison = Profile("Poison");

        WatchedRule first = Watched("first", 0, poison, new StubTrigger(false));
        WatchedRule second = Watched("second", 1, poison, new StubTrigger(true, 0.5f));

        Dictionary<Guid, RuleDemand> desired = Select(first, second);

        desired.Should().ContainSingle();
        desired[second.Rule.Id].Signal.Intensity.Should().Be(0.5f);
    }

    /// <summary>
    /// Only the same effect conflicts. Two rules raising genuinely different looks both draw -
    /// that is what composition is for, and it is the case the concurrency cap exists to bound.
    /// </summary>
    [Fact]
    public void DifferentEffectsBothDraw()
    {
        WatchedRule poison = Watched("poison", 0, Profile("Poison"), new StubTrigger(true));
        WatchedRule fog = Watched("fog", 1, Profile("Fog"), new StubTrigger(true));

        Dictionary<Guid, RuleDemand> desired = Select(poison, fog);

        desired.Should().HaveCount(2);
        desired.Keys.Should().BeEquivalentTo([poison.Rule.Id, fog.Rule.Id]);
    }

    /// <summary>
    /// Precedence is the table's, not the dictionary's. Table position is inverted into the
    /// compositor's priority, so the winner also composites on top of anything below it.
    /// </summary>
    [Fact]
    public void TheWinnerCarriesTheStrongerPrecedence()
    {
        WatchedRule top = Watched("top", 0, Profile("Poison"), new StubTrigger(true));
        WatchedRule bottom = Watched("bottom", 3, Profile("Fog"), new StubTrigger(true));

        Dictionary<Guid, RuleDemand> desired = Select(top, bottom);

        desired[top.Rule.Id].Priority.Should().BeGreaterThan(desired[bottom.Rule.Id].Priority);
    }

    [Fact]
    public void NothingIsDemandedWhenNoRuleFires()
    {
        EffectProfile poison = Profile("Poison");

        Select(Watched("a", 0, poison, new StubTrigger(false)), Watched("b", 1, poison, new StubTrigger(false)))
            .Should()
            .BeEmpty();
    }

    private static Dictionary<Guid, RuleDemand> Capped(int cap, params (Guid Id, int Priority)[] demands)
    {
        var desired = demands.ToDictionary(
            demand => demand.Id,
            demand => new RuleDemand(demand.Id, Profile("look"), demand.Priority, TriggerSignal.Default)
        );

        ScreenOverlayManager.ApplyConcurrencyCap(desired, [], cap);

        return desired;
    }

    [Fact]
    public void TheCapKeepsTheStrongestDemands()
    {
        (Guid Id, int Priority) weak = (Guid.NewGuid(), 0);
        (Guid Id, int Priority) middle = (Guid.NewGuid(), 5);
        (Guid Id, int Priority) strong = (Guid.NewGuid(), 10);

        Dictionary<Guid, RuleDemand> kept = Capped(2, weak, strong, middle);

        kept.Keys.Should().BeEquivalentTo([strong.Id, middle.Id]);
    }

    [Fact]
    public void NothingIsDroppedWhileTheDemandsFitTheCap()
    {
        (Guid Id, int Priority) first = (Guid.NewGuid(), 0);
        (Guid Id, int Priority) second = (Guid.NewGuid(), 1);

        Capped(4, first, second).Should().HaveCount(2);
    }

    /// <summary>
    /// Survivors must not depend on hash order - equal-priority demands swapping between passes
    /// would cross-fade every poll.
    /// </summary>
    [Fact]
    public void TiesAreBrokenTheSameWayEveryPass()
    {
        (Guid Id, int Priority) first = (Guid.NewGuid(), 3);
        (Guid Id, int Priority) second = (Guid.NewGuid(), 3);
        (Guid Id, int Priority) third = (Guid.NewGuid(), 3);

        Dictionary<Guid, RuleDemand> kept = Capped(2, first, second, third);
        Dictionary<Guid, RuleDemand> again = Capped(2, third, first, second);

        kept.Keys.Should().BeEquivalentTo(again.Keys);
    }
}
