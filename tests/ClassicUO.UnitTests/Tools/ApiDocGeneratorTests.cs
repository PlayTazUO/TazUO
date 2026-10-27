#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Tools;

/// <summary>
/// Covers the markdown the scripting API documentation is generated from. These assert on rendering
/// decisions that are invisible until the site builds - an unquoted YAML scalar or an unclosed fence
/// fails the website build, not this one, so they are worth pinning here.
/// </summary>
public sealed class ApiDocGeneratorTests : IDisposable
{
    #region Private members

    private readonly List<string> _tempFiles = [];

    #endregion

    #region Public methods

    public void Dispose()
    {
        foreach (string path in _tempFiles)
            File.Delete(path);
    }

    [Fact]
    public void ANestedConstantHolderIsRenderedIntoItsParentRatherThanGivenAPage()
    {
        string page = Render(
            """
            /// <summary>Holder parent.</summary>
            public class ApiThing
            {
                /// <summary>The shipped ids.</summary>
                public static class Ids
                {
                    /// <summary>The first one.</summary>
                    public const string First = "abc";
                }
            }
            """,
            "ApiThing"
        );

        page.Should().Contain("## Constants").And.Contain("### Ids").And.Contain("| `First` | `abc` |");
    }

    [Fact]
    public void ATopLevelConstantHolderStillGetsItsOwnPage()
    {
        Dictionary<string, Tuple<StringBuilder, StringBuilder>> generated = Generate(
            """
            /// <summary>Nothing but constants.</summary>
            public static class ApiLimits
            {
                /// <summary>How many.</summary>
                public const int MaxThings = 4;
            }
            """
        );

        generated.Should().ContainKey("ApiLimits");
    }

    [Fact]
    public void AColonInASummaryIsQuotedRatherThanBreakingTheFrontmatter()
    {
        string page = Render(
            """
            /// <summary>Where it lives: the shape, and how far it extends.</summary>
            public class ApiThing
            {
            }
            """,
            "ApiThing"
        );

        page.Should().Contain("description: \"Where it lives: the shape, and how far it extends.\"");
    }

    [Fact]
    public void AQuoteInASummaryIsEscapedRatherThanClosingTheScalar()
    {
        string page = Render(
            """
            /// <summary>Named the "built-in" one.</summary>
            public class ApiThing
            {
            }
            """,
            "ApiThing"
        );

        page.Should().Contain("""description: "Named the \"built-in\" one." """.TrimEnd());
    }

    [Fact]
    public void OnlyTheFirstParagraphOfASummaryReachesTheFrontmatter()
    {
        string page = Render(
            """
            /// <summary>
            /// The short of it.
            /// <para>Every caveat, which has no business in a meta description.</para>
            /// </summary>
            public class ApiThing
            {
            }
            """,
            "ApiThing"
        );

        page.Should().Contain("description: \"The short of it.\"").And.NotContain("description: \"The short of it. Every");
    }

    [Fact]
    public void AnAsideIsRenderedAsADirectiveAndKeptOutOfTheFrontmatter()
    {
        string page = Render(
            """
            /// <summary>
            /// The short of it.
            /// <warn title="Careful">Do not do that.</warn>
            /// </summary>
            public class ApiThing
            {
            }
            """,
            "ApiThing"
        );

        page.Should().Contain(":::caution[Careful]").And.Contain("description: \"The short of it.\"");
    }

    [Fact]
    public void ACrossReferenceBecomesTheNameItPointsAt()
    {
        string page = Render(
            """
            /// <summary>Inherits from <see cref="ApiGameObject"/>.</summary>
            public class ApiThing
            {
            }
            """,
            "ApiThing"
        );

        // The tag is its own syntax node, so without the punctuation pass this reads "`ApiGameObject` ."
        page.Should().Contain("Inherits from `ApiGameObject`.").And.NotContain("`ApiGameObject` .");
    }

    [Fact]
    public void AFencedSampleKeepsItsFencesOnTheirOwnLines()
    {
        string page = Render(
            """
            /// <summary>
            /// Does a thing.
            /// Example:
            /// ```py
            /// API.Thing(1)
            /// ```
            /// </summary>
            public class ApiThing
            {
            }
            """,
            "ApiThing"
        );

        // The blank line belongs above the opening fence only. Above the closing one it lands
        // inside the block and renders as a trailing empty line in the sample.
        page.Should().Contain("\n\n```py\nAPI.Thing(1)\n```");
    }

    [Fact]
    public void ABlankLineInsideASampleDoesNotFlattenTheHalfBelowIt()
    {
        string page = Render(
            """
            /// <summary>
            /// Does a thing.
            /// ```py
            /// if API.Thing(1):
            ///     API.SysMsg("one")
            ///
            /// if API.Thing(2):
            ///     API.SysMsg("two")
            /// ```
            /// </summary>
            public class ApiThing
            {
            }
            """,
            "ApiThing"
        );

        // Both halves are one sample, so both keep their indentation. Dedenting the lower half on
        // its own would strip the body's indent and leave the author an IndentationError.
        page.Should()
            .Contain("if API.Thing(1):\n    API.SysMsg(\"one\")")
            .And.Contain("if API.Thing(2):\n    API.SysMsg(\"two\")");
    }

    [Fact]
    public void WhatAMethodThrowsIsRenderedForTheCaller()
    {
        string page = Render(
            """
            /// <summary>Holder.</summary>
            public class ApiThing
            {
                /// <summary>Does a thing.</summary>
                /// <returns>The thing, or 0 when there is none.</returns>
                /// <exception cref="ArgumentException">The id is malformed.</exception>
                public uint Do(string id) => 0;
            }
            """,
            "ApiThing"
        );

        page.Should()
            .Contain("**Raises:**")
            .And.Contain("- `ArgumentException` - The id is malformed.")
            .And.Contain("**Return Type:** `uint` - The thing, or 0 when there is none.");
    }

    #endregion

    #region Private methods

    /// <summary>Renders one source snippet and returns the page generated for a named class.</summary>
    /// <param name="source">C# to document.</param>
    /// <param name="className">Class whose page is wanted.</param>
    /// <returns>The markdown.</returns>
    private string Render(string source, string className) => Generate(source)[className].Item1.ToString();

    /// <summary>Runs the generator over a snippet written to a temporary file.</summary>
    /// <param name="source">C# to document.</param>
    /// <returns>The generated pages and Python stubs, by class name.</returns>
    private Dictionary<string, Tuple<StringBuilder, StringBuilder>> Generate(string source)
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.cs");

        File.WriteAllText(path, source);
        _tempFiles.Add(path);

        return GenDoc.GenerateMarkdown(path);
    }

    #endregion
}
