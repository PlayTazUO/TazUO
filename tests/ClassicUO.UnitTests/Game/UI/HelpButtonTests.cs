using System;
using ClassicUO.Game.UI.Controls;
using FluentAssertions;
using Myra.Events;
using Myra.Graphics2D.UI;
using Xunit;

namespace ClassicUO.UnitTests.Game.UI;

/// <summary>
///     Only the overload taking a caller-supplied button is exercised: the others build the standard
///     glyph button, which loads a font and so needs a graphics device the test host has no reason to
///     stand up.
/// </summary>
public class HelpButtonTests
{
    /// <summary>
    ///     The button is seated through <c>ChildrenLayout</c> rather than by adding it to a container, so
    ///     losing that assignment would leave the helpButton button measured and rendered as nothing - visibly
    ///     an empty gap, with no error anywhere.
    /// </summary>
    [Fact]
    public void TheSuppliedButtonBecomesTheOnlyChild()
    {
        var button = new Widget();
        var help = new HelpButton(button, new Widget(), "Title");
        help.GetChildren().Should().ContainSingle().Which.Should().BeSameAs(button);
    }

    [Fact]
    public void NullContentIsRejected()
    {
        Action build = () => new HelpButton(new Widget(), null, "Title");
        build.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void NullButtonIsRejected()
    {
        Action build = () => new HelpButton(null, new Widget(), "Title");
        build.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    ///     Every dialog setting is reachable from the helpButton button, so a consumer that only holds
    ///     the <see cref="HelpButton" /> can still shape what it opens.
    /// </summary>
    [Fact]
    public void TheModalConfigurationIsExposed()
    {
        var help = new HelpButton(new Widget(), new Widget());
        help.ModalProps.Should().NotBeNull();
    }

    /// <summary>
    ///     Myra raises a touch-down for every mouse button, and the client closes gumps on right-click -
    ///     so a right press must not also reach the modal path, which here would fault on the font load
    ///     that opening a dialog needs.
    /// </summary>
    [Fact]
    public void ANonLeftPressDoesNotOpenTheHelp()
    {
        var help = new HelpButton(new Widget(), new Widget());

        foreach (var button in new[] { TouchButton.Right, TouchButton.Middle })
        {
            var press = () => help.OnTouchDown(new TouchEventArgs(default, button));
            press.Should().NotThrow();
        }
    }
}
