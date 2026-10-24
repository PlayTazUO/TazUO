#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using ClassicUO.LegionScripting.ApiClasses;
using ClassicUO.LegionScripting.ApiClasses.VisualEffects;
using FluentAssertions;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using Microsoft.Xna.Framework;
using Xunit;

namespace ClassicUO.UnitTests.LegionScripting;

/// <summary>
/// The default text form <see cref="PyApiObject"/> gives every script-visible spec, and the Python
/// behaviour it exists to fix. Scripts build effects by setting these objects' properties, so
/// printing one has to show its values - the state of a half-tuned composition is the only thing a
/// script author has to debug with.
/// </summary>
public class PyApiObjectTests
{
    #region Private members

    /// <summary>Mirrors the base's own cap. A test that hard-codes 4 and a base that changes to 5
    /// would still agree, which is the one thing these must not do.</summary>
    private const int MAX_DEPTH = 4;

    private const int MAX_SEQUENCE_ITEMS = 8;

    #endregion

    #region Public methods

    [Fact]
    public void ToString_NamesTheTypeAndEveryProperty()
    {
        var pair = new Pair { First = 1, Second = 2 };

        pair.ToString().Should().Be("Pair(First=1, Second=2)");
    }

    [Fact]
    public void Repr_IsTheSameTextAsToString()
    {
        var pair = new Pair { First = 7, Second = 8 };

        pair.__repr__().Should().Be(pair.ToString());
    }

    [Fact]
    public void ToString_SpellsValuesTheWayPythonDoes()
    {
        var spread = new Spread
        {
            Flag = true,
            Other = false,
            Text = "hello",
            Missing = null,
            Channel = ApiVisualEffects.NoiseChannel.Blue
        };

        string printed = spread.ToString();

        printed.Should().Contain("Flag=True");
        printed.Should().Contain("Other=False");
        printed.Should().Contain("Text='hello'");
        printed.Should().Contain("Missing=None");
        printed.Should().Contain("Channel=Blue", "an enum is worth more by name than by number");
    }

    [Fact]
    public void ToString_DoesNotLeakFloatWideningNoise()
    {
        // How every knob on these specs arrives: a float default widened to the double a script sets.
        var widened = new Single { Value = 0.3f };

        widened.ToString().Should().Be("Single(Value=0.3)", "0.30000001192092896 is not a number anyone typed");
    }

    [Fact]
    public void ToString_IsCultureInvariant()
    {
        CultureInfo original = Thread.CurrentThread.CurrentCulture;

        try
        {
            // Comma-decimal locale: without an invariant format the separators land inside what
            // otherwise reads as Python output, and a reader cannot tell them from field separators.
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

            new Single { Value = 0.5f }.ToString().Should().Be("Single(Value=0.5)");
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }

    [Fact]
    public void ToString_ExpandsNestedApiObjects()
    {
        var outer = new Nest { Depth = 0, Inner = new Nest { Depth = 1 } };

        outer.ToString().Should().Be("Nest(Inner=Nest(Inner=None, Depth=1), Depth=0)");
    }

    [Fact]
    public void ToString_StopsExpandingPastItsDepthBudget()
    {
        Nest chain = BuildChain(MAX_DEPTH + 2);

        string printed = chain.ToString();

        printed.Should().Contain("Nest(...)", "an unbounded tree buries whatever the author was looking at");
        printed.Split("Depth=").Length.Should().Be(MAX_DEPTH + 1, "one level per unit of budget, and no more");
    }

    [Fact]
    public void ToString_ListsSequenceElements()
    {
        var holder = new Sequence { Items = { 1, 2, 3 } };

        holder.ToString().Should().Be("Sequence(Items=[1, 2, 3])");
    }

    [Fact]
    public void ToString_ElidesLongSequences()
    {
        var holder = new Sequence();

        holder.Items.AddRange(Enumerable.Range(0, MAX_SEQUENCE_ITEMS + 4));

        string printed = holder.ToString();

        printed.Should().EndWith(", ...])");
        printed.Split(", ").Length.Should().Be(MAX_SEQUENCE_ITEMS + 1, "the elision replaces the tail, it does not join it");
    }

    [Fact]
    public void ToString_SurvivesAThrowingGetter()
    {
        var broken = new Broken();

        // The whole point of this path is debugging a half-built object, which is exactly when a
        // getter is most likely to fault.
        broken.ToString().Should().Be("Broken(Fine=1, Faulting=<unreadable>)");
    }

    [Fact]
    public void ToString_PrefersATypesOwnOverride()
    {
        new ApiVector2(1.5, -2.5).ToString().Should().Be("(1.5, -2.5)");

        new ApiEffectProfileInfo { Id = "7195989d", Name = "Fog", IsBuiltIn = true }
            .ToString()
            .Should()
            .Be("Fog [7195989d]");
    }

    /// <summary>
    /// An override still owes the caller what the shared path gives them: a pair read back off a
    /// layer holds widened floats, so the exact form prints digits no script ever set.
    /// </summary>
    [Fact]
    public void ToString_RoundsAnOverridesNumbersLikeTheSharedPath()
    {
        ApiVector2.From(new Vector2(0.3f, -1.7f)).ToString().Should().Be("(0.3, -1.7)");
    }

    [Fact]
    public void ToString_PrintsARealCompositionsLayers()
    {
        var profile = new ApiEffectProfile { Name = "test" };
        profile.AddLayer(new ApiTintLayer { R = 220, G = 40, B = 40 }, ApiVisualEffects.OverlayBlend.Additive);

        string printed = profile.ToString();

        printed.Should().StartWith("ApiEffectProfile(Name='test'");
        printed.Should().Contain("Layers=[ApiProfileLayer(Effect=ApiTintLayer(R=220, G=40, B=40");
        printed.Should().Contain("Blend=Additive");
    }

    #region IronPython behaviour

    [Fact]
    public void Python_SynthesizesStrFromToString_WithNoHookAtAll()
    {
        // Why no __str__ is declared anywhere: IronPython already provides it for every CLR type,
        // so one of our own would be dead weight.
        Evaluate("str(subject)", new Plain { Value = 3 }).Should().Be("Plain(3)");
        Evaluate("hasattr(subject, '__str__')", new Plain { Value = 3 }).Should().Be("True");
        Evaluate("subject.__str__()", new Plain { Value = 3 }).Should().Be("Plain(3)");
    }

    [Fact]
    public void Python_FallsBackToAnObjectAddressForRepr_WithoutTheHook()
    {
        // Why __repr__ is declared: this is what a script sees without it.
        string printed = Evaluate("repr(subject)", new Plain { Value = 3 });

        printed.Should().StartWith("<");
        printed.Should().Contain("object at 0x");
    }

    [Fact]
    public void Python_UsesTheHookForRepr()
    {
        Evaluate("repr(subject)", new Pair { First = 1, Second = 2 }).Should().Be("Pair(First=1, Second=2)");
    }

    [Fact]
    public void Python_FormatsContainerElementsWithRepr()
    {
        // The case that makes the hook load-bearing rather than cosmetic: GetProfiles answers a
        // list, and printing it to find an id is the first thing a script author does.
        var listed = new List<ApiEffectProfileInfo>
        {
            new() { Id = "7195989d", Name = "Fog" },
            new() { Id = "1c419034", Name = "Poison" }
        };

        string printed = Evaluate("str(subject)", listed);

        printed.Should().Contain("Fog [7195989d]");
        printed.Should().Contain("Poison [1c419034]");
        printed.Should().NotContain("object at 0x");
    }

    #endregion

    #endregion

    #region Private methods

    /// <summary>Runs one expression against a single CLR object bound as <c>subject</c>.</summary>
    /// <param name="expression">The Python expression to evaluate.</param>
    /// <param name="subject">The object to bind.</param>
    /// <returns>The expression's result, as Python would print it.</returns>
    private static string Evaluate(string expression, object subject)
    {
        // An engine per call: these are a handful of tests, and a shared engine would make them
        // order-dependent through its module state for no measurable gain.
        ScriptEngine engine = Python.CreateEngine();
        ScriptScope scope = engine.CreateScope();

        scope.SetVariable("subject", subject);

        return engine.Execute<object>(expression, scope).ToString() ?? string.Empty;
    }

    /// <summary>Builds a chain of nested objects, each one level deeper than the last.</summary>
    /// <param name="levels">How many objects to link.</param>
    /// <returns>The outermost link.</returns>
    private static Nest BuildChain(int levels)
    {
        Nest? built = null;

        for (int depth = levels - 1; depth >= 0; depth--)
            built = new Nest { Depth = depth, Inner = built };

        return built!;
    }

    #endregion

    #region Test types

    /// <summary>Two properties, for asserting the exact shape of the default form.</summary>
    public sealed class Pair : PyApiObject
    {
        public int First { get; set; }

        public int Second { get; set; }
    }

    /// <summary>One property per value spelling the default form special-cases.</summary>
    public sealed class Spread : PyApiObject
    {
        public bool Flag { get; set; }

        public bool Other { get; set; }

        public string? Text { get; set; }

        public string? Missing { get; set; }

        public ApiVisualEffects.NoiseChannel Channel { get; set; }
    }

    /// <summary>A single float-widened double, for the numeric format.</summary>
    public sealed class Single : PyApiObject
    {
        public double Value { get; set; }
    }

    /// <summary>Self-nesting, for the depth budget.</summary>
    public sealed class Nest : PyApiObject
    {
        public Nest? Inner { get; set; }

        public int Depth { get; set; }
    }

    /// <summary>Carries a sequence, for element listing and elision.</summary>
    public sealed class Sequence : PyApiObject
    {
        public List<int> Items { get; } = [];
    }

    /// <summary>A faulting getter, to prove one cannot take the whole text form down.</summary>
    public sealed class Broken : PyApiObject
    {
        public int Fine => 1;

        public int Faulting => throw new InvalidOperationException("half-built");
    }

    /// <summary>Deliberately not a <see cref="PyApiObject"/> - the baseline the hooks are measured
    /// against.</summary>
    public sealed class Plain
    {
        public int Value { get; set; }

        public override string ToString() => $"Plain({Value})";
    }

    #endregion
}
