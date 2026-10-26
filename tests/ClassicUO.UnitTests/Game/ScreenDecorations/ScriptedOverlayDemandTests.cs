using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;
using ClassicUO.Game.ScreenDecorations.Manager;
using ClassicUO.Game.ScreenDecorations.Rules;
using ClassicUO.Game.ScreenDecorations.Triggers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.ScreenDecorations;

/// <summary>
/// A script raising an effect joins the same first-match resolution the rules go through, between
/// the preview and the rulebase. The claim is what stops a script and a rule asking for one look
/// from drawing it twice at doubled alpha; the per-source caps are what stop either starving the
/// other off the screen.
/// </summary>
public class ScriptedOverlayDemandTests
{
    private static readonly object _owner = new();

    private static EffectProfile Profile(string name) => new() { Name = name };

    private static ScriptedOverlayRegistry Registry(params ScriptedDemand[] demands)
    {
        var registry = new ScriptedOverlayRegistry();

        foreach (ScriptedDemand demand in demands)
            registry.Add(demand.Profile, demand.SourceProfileId, demand.Intensity, demand.ExpiresAt, demand.Owner);

        return registry;
    }

    /// <summary>A demand as the registry would hold it, for feeding the resolution policy directly.</summary>
    private static ScriptedDemand Demand(
        EffectProfile profile,
        Guid? source = null,
        float intensity = 1f,
        DateTime? expiresAt = null,
        object owner = null
    ) =>
        new(0, Guid.NewGuid(), profile, source ?? profile.Id, intensity, expiresAt, owner ?? _owner);

    private static Dictionary<Guid, RuleDemand> Resolve(HashSet<Guid> claimed, params ScriptedDemand[] scripted)
    {
        var desired = new Dictionary<Guid, RuleDemand>();

        ScreenOverlayManager.AddScripted(scripted, claimed, desired);

        return desired;
    }

    [Fact]
    public void AScriptRaisedLookClaimsItAgainstTheRules()
    {
        EffectProfile poison = Profile("Poison");
        var claimed = new HashSet<Guid>();

        Resolve(claimed, Demand(poison));

        claimed.Should().Contain(poison.Id);
    }

    /// <summary>
    /// The preview claims first, so previewing a look a script is raising shows the one being tuned.
    /// </summary>
    [Fact]
    public void APreviewAlreadyHoldingTheLookSuppressesTheScript()
    {
        EffectProfile poison = Profile("Poison");

        Resolve([poison.Id], Demand(poison)).Should().BeEmpty();
    }

    /// <summary>
    /// Insertion order decides which of two demands on one look draws, so the answer does not change
    /// between passes. The other stays in the registry and takes over once the first lapses.
    /// </summary>
    [Fact]
    public void TheOlderOfTwoDemandsOnOneLookDrawsAndTheOtherIsInert()
    {
        EffectProfile poison = Profile("Poison");

        ScriptedDemand first = Demand(poison);
        ScriptedDemand second = Demand(poison);

        Dictionary<Guid, RuleDemand> desired = Resolve([], first, second);

        desired.Should().ContainSingle();
        desired.Should().ContainKey(first.Slot);
    }

    /// <summary>
    /// A composition the script built has no library look behind it, so there is nothing for it to
    /// claim and nothing for it to lose. Two identical hand-built ones therefore do composite.
    /// </summary>
    [Fact]
    public void ScriptBuiltCompositionsNeitherClaimNorSuppressEachOther()
    {
        var claimed = new HashSet<Guid>();

        Dictionary<Guid, RuleDemand> desired = Resolve(
            claimed,
            Demand(Profile("built"), Guid.Empty),
            Demand(Profile("built"), Guid.Empty)
        );

        desired.Should().HaveCount(2);
        claimed.Should().BeEmpty();
    }

    [Fact]
    public void ScriptDemandsSortAboveRulesAndBelowThePreview()
    {
        Dictionary<Guid, RuleDemand> desired = Resolve([], Demand(Profile("Poison")));

        RuleDemand raised = desired.Values.Single();

        raised.Source.Should().Be(DemandSource.Script);
        raised.Priority.Should().BeGreaterThan(0, "every rule's priority is zero or below");
        raised.Priority.Should().BeLessThan(100, "a preview has to win against a script");
    }

    [Fact]
    public void TheIntensityAScriptAskedForReachesTheCompositor()
    {
        Dictionary<Guid, RuleDemand> desired = Resolve([], Demand(Profile("Poison"), intensity: 0.25f));

        desired.Values.Single().Signal.Intensity.Should().Be(0.25f);
    }

    private static Dictionary<Guid, RuleDemand> Capped(
        int ruleCap,
        int scriptCap,
        params (DemandSource Source, int Priority)[] demands
    )
    {
        var desired = new Dictionary<Guid, RuleDemand>();

        foreach ((DemandSource source, int priority) in demands)
        {
            Guid slot = Guid.NewGuid();
            desired[slot] = new RuleDemand(slot, Profile("look"), priority, TriggerSignal.Default, source);
        }

        ScreenOverlayManager.ApplyConcurrencyCap(desired, [], ruleCap, DemandSource.Rule);
        ScreenOverlayManager.ApplyConcurrencyCap(desired, [], scriptCap, DemandSource.Script);

        return desired;
    }

    /// <summary>
    /// The core of the partitioning. Scripts sort above rules, so a shared cap would let four
    /// scripted effects push the player's own state off the screen entirely.
    /// </summary>
    [Fact]
    public void RuleEffectsSurviveAScriptHoldingItsWholeAllowance()
    {
        Dictionary<Guid, RuleDemand> kept = Capped(
            ruleCap: 2,
            scriptCap: 2,
            (DemandSource.Script, 50),
            (DemandSource.Script, 50),
            (DemandSource.Rule, 0),
            (DemandSource.Rule, 0)
        );

        kept.Values.Count(demand => demand.Source == DemandSource.Rule).Should().Be(2);
        kept.Values.Count(demand => demand.Source == DemandSource.Script).Should().Be(2);
    }

    [Fact]
    public void EachPartitionIsTrimmedToItsOwnCap()
    {
        Dictionary<Guid, RuleDemand> kept = Capped(
            ruleCap: 1,
            scriptCap: 2,
            (DemandSource.Script, 50),
            (DemandSource.Script, 50),
            (DemandSource.Script, 50),
            (DemandSource.Rule, 0),
            (DemandSource.Rule, -1)
        );

        kept.Values.Count(demand => demand.Source == DemandSource.Script).Should().Be(2);
        kept.Values.Count(demand => demand.Source == DemandSource.Rule).Should().Be(1);
    }

    /// <summary>The preview is singular by construction, so no partition may evict it.</summary>
    [Fact]
    public void ThePreviewIsNeverTrimmed()
    {
        Dictionary<Guid, RuleDemand> kept = Capped(
            ruleCap: 1,
            scriptCap: 1,
            (DemandSource.Preview, 100),
            (DemandSource.Script, 50),
            (DemandSource.Rule, 0)
        );

        kept.Values.Should().ContainSingle(demand => demand.Source == DemandSource.Preview);
    }

    [Fact]
    public void HandlesAreMonotonicAndNeverZero()
    {
        var registry = new ScriptedOverlayRegistry();

        uint first = registry.Add(Profile("a"), Guid.NewGuid(), 1f, null, _owner);
        uint second = registry.Add(Profile("b"), Guid.NewGuid(), 1f, null, _owner);

        first.Should().BeGreaterThan(0);
        second.Should().BeGreaterThan(first);
    }

    /// <summary>
    /// Handles are not reused, so a script stopping one it already stopped cannot take down whatever
    /// came after it.
    /// </summary>
    [Fact]
    public void AReleasedHandleIsNotHandedOutAgain()
    {
        var registry = new ScriptedOverlayRegistry();

        uint released = registry.Add(Profile("a"), Guid.NewGuid(), 1f, null, _owner);
        registry.Remove(released, _owner).Should().BeTrue();

        registry.Add(Profile("b"), Guid.NewGuid(), 1f, null, _owner).Should().NotBe(released);
        registry.Remove(released, _owner).Should().BeFalse();
    }

    [Fact]
    public void StoppingAnUnknownOrZeroHandleDoesNothing()
    {
        var registry = new ScriptedOverlayRegistry();
        registry.Add(Profile("a"), Guid.NewGuid(), 1f, null, _owner);

        registry.Remove(0, _owner).Should().BeFalse();
        registry.Remove(9999, _owner).Should().BeFalse();
        registry.Count.Should().Be(1);
    }

    /// <summary>
    /// Slots are pooled, not minted per demand.
    /// <para>
    /// A slot the compositor has been told to hide stays in its active set until the look's fade-out
    /// finishes, and nothing counts those - so a fresh slot per start would let a start-stop loop
    /// grow that set without bound, and the compositor walks and sorts all of it every frame.
    /// Reusing a vacated slot makes the next start replace the retiring overlay instead.
    /// </para>
    /// </summary>
    [Fact]
    public void SlotsAreReusedRatherThanMintedPerStart()
    {
        var registry = new ScriptedOverlayRegistry();
        var seen = new HashSet<Guid>();

        // Far more cycles than the cap, which is exactly the shape of the loop that grew the
        // compositor's active set.
        for (int i = 0; i < ScreenOverlayManager.ScriptCap * 20; i++)
        {
            uint handle = registry.Add(Profile("a"), Guid.NewGuid(), 1f, null, _owner);

            seen.Add(registry.Entries[^1].Slot);
            registry.Remove(handle, _owner).Should().BeTrue();
        }

        seen.Should().HaveCount(ScreenOverlayManager.ScriptCap);
    }

    /// <summary>Slots are distinct while held, or two demands would fight for one compositor entry.</summary>
    [Fact]
    public void EveryLiveDemandHoldsItsOwnSlot()
    {
        var registry = new ScriptedOverlayRegistry();

        for (int i = 0; i < ScreenOverlayManager.ScriptCap; i++)
            registry.Add(Profile($"p{i}"), Guid.NewGuid(), 1f, null, _owner);

        registry.Entries.Select(entry => entry.Slot).Should().OnlyHaveUniqueItems();
    }

    /// <summary>
    /// Handles are monotonic small integers, so without an ownership check one script holding a
    /// single handle could walk the range and stop every other script's effects.
    /// </summary>
    [Fact]
    public void AHandleOnlyStopsForTheScriptThatRaisedIt()
    {
        var mine = new object();
        var theirs = new object();

        var registry = new ScriptedOverlayRegistry();
        uint handle = registry.Add(Profile("a"), Guid.NewGuid(), 1f, null, theirs);

        registry.Remove(handle, mine).Should().BeFalse();
        registry.Count.Should().Be(1);

        registry.Remove(handle, theirs).Should().BeTrue();
        registry.Count.Should().Be(0);
    }

    /// <summary>
    /// The leak guard. An effect asked for with no duration has nothing left that could stop it once
    /// its script is gone.
    /// </summary>
    [Fact]
    public void TearingDownOneScriptLeavesAnothersEffectsAlone()
    {
        var mine = new object();
        var theirs = new object();

        var registry = new ScriptedOverlayRegistry();
        registry.Add(Profile("a"), Guid.NewGuid(), 1f, null, mine);
        registry.Add(Profile("b"), Guid.NewGuid(), 1f, null, mine);
        uint survivor = registry.Add(Profile("c"), Guid.NewGuid(), 1f, null, theirs);

        registry.RemoveByOwner(mine).Should().BeTrue();

        registry.Entries.Should().ContainSingle();
        registry.Entries[0].Handle.Should().Be(survivor);
    }

    [Fact]
    public void ClearingDropsEverythingHeld()
    {
        ScriptedOverlayRegistry registry = Registry(
            Demand(Profile("a")),
            Demand(Profile("b"), expiresAt: DateTime.UtcNow.AddMinutes(5))
        );

        registry.Clear();

        registry.Count.Should().Be(0);
        registry.NextExpiry().Should().BeNull();
    }

    [Fact]
    public void LapsedDemandsAreDroppedAndOpenEndedOnesAreNot()
    {
        DateTime now = DateTime.UtcNow;

        ScriptedOverlayRegistry registry = Registry(
            Demand(Profile("lapsed"), expiresAt: now.AddSeconds(-1)),
            Demand(Profile("running"), expiresAt: now.AddSeconds(10)),
            Demand(Profile("open ended"))
        );

        registry.RemoveLapsed(now).Should().BeTrue();

        registry.Entries.Select(entry => entry.Profile.Name).Should().BeEquivalentTo(["running", "open ended"]);
    }

    /// <summary>
    /// The scheduler wakes on the soonest deadline, so a scripted timeout the pass cannot see is one
    /// that expires whenever something else happens to run a pass.
    /// </summary>
    [Fact]
    public void TheSoonestScriptedTimeoutIsReported()
    {
        DateTime now = DateTime.UtcNow;

        ScriptedOverlayRegistry registry = Registry(
            Demand(Profile("late"), expiresAt: now.AddSeconds(30)),
            Demand(Profile("soon"), expiresAt: now.AddSeconds(2)),
            Demand(Profile("open ended"))
        );

        registry.NextExpiry().Should().Be(now.AddSeconds(2));
    }

    [Fact]
    public void NothingIsScheduledWhenEveryDemandIsOpenEnded()
    {
        Registry(Demand(Profile("a")), Demand(Profile("b"))).NextExpiry().Should().BeNull();
    }
}
