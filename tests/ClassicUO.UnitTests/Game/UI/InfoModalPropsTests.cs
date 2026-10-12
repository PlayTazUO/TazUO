using ClassicUO.Game.UI.MyraWindows;
using FluentAssertions;
using Microsoft.Xna.Framework;
using Xunit;

namespace ClassicUO.UnitTests.Game.UI;

/// <summary>
///     The dialog feeds these limits to <see cref="System.Math.Clamp(int,int,int)" /> on every mouse-move
///     of a resize, which throws on crossed bounds - so the ordering here is what keeps a consumer's
///     choice of limits from turning a drag into an exception.
/// </summary>
public class InfoModalPropsTests
{
    [Fact]
    public void LimitsSetTheNormalWayAreLeftAlone()
    {
        var props = new InfoModalProps { MinWidth = 100, MinHeight = 50, MaxWidth = 400, MaxHeight = 300 };
        props.GetOrderedSizeLimits().Should().Be((new Point(100, 50), new Point(400, 300)));
    }

    /// <summary>A consumer raising only the minimum crosses it past the default maximum.</summary>
    [Fact]
    public void AMinimumAboveTheMaximumLiftsTheMaximumToMeetIt()
    {
        var props = new InfoModalProps { MinWidth = 900, MinHeight = 800, MaxWidth = 400, MaxHeight = 300 };
        props.GetOrderedSizeLimits().Should().Be((new Point(900, 800), new Point(900, 800)));
    }

    /// <summary>Each axis is ordered on its own, so one crossed axis does not disturb the other.</summary>
    [Fact]
    public void OnlyTheCrossedAxisIsLifted()
    {
        var props = new InfoModalProps { MinWidth = 900, MinHeight = 50, MaxWidth = 400, MaxHeight = 300 };
        props.GetOrderedSizeLimits().Should().Be((new Point(900, 50), new Point(900, 300)));
    }

    [Fact]
    public void NegativeMinimumsAreFlooredAtZero()
    {
        var props = new InfoModalProps { MinWidth = -10, MinHeight = -1, MaxWidth = 400, MaxHeight = 300 };
        props.GetOrderedSizeLimits().Min.Should().Be(Point.Zero);
    }

    /// <summary>A negative maximum would otherwise come back below the floored minimum.</summary>
    [Fact]
    public void ANegativeMaximumStillLandsOnTheMinimum()
    {
        var props = new InfoModalProps { MinWidth = 0, MinHeight = 0, MaxWidth = -400, MaxHeight = -300 };
        props.GetOrderedSizeLimits().Should().Be((Point.Zero, Point.Zero));
    }

    /// <summary>
    ///     The shipped defaults have to satisfy the same invariant, or every unconfigured
    ///     dialog is one drag away from throwing.
    /// </summary>
    [Fact]
    public void TheDefaultsAreAlreadyOrdered()
    {
        var props = new InfoModalProps();

        props.MaxWidth.Should().BeGreaterThan(props.MinWidth);
        props.MaxHeight.Should().BeGreaterThan(props.MinHeight);
        props.GetOrderedSizeLimits().Should().Be((
            new Point(props.MinWidth, props.MinHeight),
            new Point(props.MaxWidth, props.MaxHeight)
        ));
    }

    /// <summary>
    ///     <see cref="InfoModal" /> reconfigures off these notifications; a property that does
    ///     not raise one is a setting that silently fails to apply to an open dialog.
    /// </summary>
    [Theory]
    [InlineData(nameof(InfoModalProps.Closable))]
    [InlineData(nameof(InfoModalProps.Minimizable))]
    [InlineData(nameof(InfoModalProps.MaxHeight))]
    public void ChangingASettingNotifies(string propertyName)
    {
        var props = new InfoModalProps();
        string changed = null;
        props.PropertyChanged += (_, e) => changed = e.PropertyName;

        switch (propertyName)
        {
            case nameof(InfoModalProps.Closable): props.Closable = !props.Closable; break;
            case nameof(InfoModalProps.Minimizable): props.Minimizable = !props.Minimizable; break;
            case nameof(InfoModalProps.MaxHeight): props.MaxHeight = props.MaxHeight + 1; break;
        }

        changed.Should().Be(propertyName);
    }

    /// <summary>
    ///     The close and minimize buttons are on by default; a helpButton dialog with no way to
    ///     dismiss it but a right-click is the state this guards against.
    /// </summary>
    [Fact]
    public void TheDialogOffersItsChromeByDefault()
    {
        var props = new InfoModalProps();

        props.Closable.Should().BeTrue();
        props.Minimizable.Should().BeTrue();
    }
}
