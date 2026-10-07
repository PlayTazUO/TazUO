using System.IO;
using System.Xml;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game;

/// <summary>
///     Covers how a macro button's two appearances resolve and survive a save, including the fall back
///     from the running state to the resting one that keeps a macro saved before it existed unchanged.
/// </summary>
public class MacroButtonAppearanceTests
{
    private const string MACRO_NAME = "Heal";

    #region Graphic

    [Fact]
    public void GraphicFor_Inactive_ReturnsTheRestingGraphic()
    {
        Macro macro = NewMacro();
        macro.Graphic = 0x1234;

        macro.GraphicFor(false).Should().Be(0x1234);
    }

    [Fact]
    public void GraphicFor_Active_InheritsTheRestingGraphicWhenUnset()
    {
        Macro macro = NewMacro();
        macro.Graphic = 0x1234;

        macro.GraphicFor(true).Should().Be(0x1234);
    }

    [Fact]
    public void GraphicFor_Active_ReturnsItsOwnGraphicWhenSet()
    {
        Macro macro = NewMacro();
        macro.Graphic = 0x1234;
        macro.ActiveGraphic = 0x5678;

        macro.GraphicFor(true).Should().Be(0x5678);
    }

    /// <summary>
    ///     The sentinel is why the running graphic is a wider signed field: null already means "inherit",
    ///     so without it "draw nothing while running" would be unsayable.
    /// </summary>
    [Fact]
    public void GraphicFor_Active_DrawsNothingForTheNoneSentinel()
    {
        Macro macro = NewMacro();
        macro.Graphic = 0x1234;
        macro.ActiveGraphic = Macro.ACTIVE_GRAPHIC_NONE;

        macro.GraphicFor(true).Should().BeNull();
    }

    #endregion

    #region Hue

    [Fact]
    public void HueFor_Active_InheritsTheRestingHueWhenUnset()
    {
        Macro macro = NewMacro();
        macro.Hue = 50;

        macro.HueFor(true).Should().Be(50);
        macro.HueFor(false).Should().Be(50);
    }

    /// <summary>
    ///     Hue 0 is the unhued appearance, a real value rather than "unset" - which is the whole reason
    ///     the running hue is nullable where the resting one is not.
    /// </summary>
    [Fact]
    public void HueFor_Active_TreatsZeroAsAChosenHue()
    {
        Macro macro = NewMacro();
        macro.Hue = 50;
        macro.ActiveHue = 0;

        macro.HueFor(true).Should().Be(0);
        macro.HueFor(false).Should().Be(50);
    }

    #endregion

    #region Label

    [Fact]
    public void LabelFor_FollowsTheMacroNameWhileNoLabelIsSet()
    {
        Macro macro = NewMacro();

        macro.LabelFor(false).Should().Be(MACRO_NAME);
        macro.LabelFor(true).Should().Be(MACRO_NAME);
    }

    [Fact]
    public void LabelFor_Active_InheritsTheRestingLabel()
    {
        Macro macro = NewMacro();
        macro.Label = "Cure";

        macro.LabelFor(true).Should().Be("Cure");
    }

    [Fact]
    public void LabelFor_Active_ReturnsItsOwnLabelWhenSet()
    {
        Macro macro = NewMacro();
        macro.Label = "Cure";
        macro.ActiveLabel = "Curing";

        macro.LabelFor(true).Should().Be("Curing");
        macro.LabelFor(false).Should().Be("Cure");
    }

    /// <summary>Empty is a value, not "unset", and is how a button is told to show no label at all.</summary>
    [Fact]
    public void LabelFor_TreatsEmptyAsAHiddenLabelRatherThanInheritance()
    {
        Macro macro = NewMacro();
        macro.Label = string.Empty;

        macro.LabelFor(false).Should().BeEmpty();
        macro.LabelFor(true).Should().BeEmpty();
    }

    [Fact]
    public void LabelHueAndOpacityFor_Active_InheritTheRestingValuesWhenUnset()
    {
        Macro macro = NewMacro();
        macro.LabelHue = 77;
        macro.LabelOpacity = 40;

        macro.LabelHueFor(true).Should().Be(77);
        macro.LabelOpacityFor(true).Should().Be(40);
    }

    [Fact]
    public void LabelHueAndOpacityFor_Active_ReturnTheirOwnValuesWhenSet()
    {
        Macro macro = NewMacro();
        macro.LabelHue = 77;
        macro.LabelOpacity = 40;
        macro.ActiveLabelHue = 88;
        macro.ActiveLabelOpacity = 100;

        macro.LabelHueFor(true).Should().Be(88);
        macro.LabelOpacityFor(true).Should().Be(100);
        macro.LabelHueFor(false).Should().Be(77);
        macro.LabelOpacityFor(false).Should().Be(40);
    }

    #endregion

    #region Persistence

    [Fact]
    public void RoundTrip_KeepsEveryAppearanceValue()
    {
        Macro macro = NewMacro();
        macro.Label = "Cure";
        macro.ActiveLabel = "Curing";
        macro.LabelHue = 77;
        macro.ActiveLabelHue = 88;
        macro.LabelOpacity = 40;
        macro.ActiveLabelOpacity = 90;
        macro.Hue = 50;
        macro.ActiveHue = 60;
        macro.Graphic = 0x1234;
        macro.ActiveGraphic = 0x5678;
        macro.Scale = 150;

        Macro loaded = RoundTrip(macro);

        loaded.Label.Should().Be("Cure");
        loaded.ActiveLabel.Should().Be("Curing");
        loaded.LabelHue.Should().Be(77);
        loaded.ActiveLabelHue.Should().Be(88);
        loaded.LabelOpacity.Should().Be(40);
        loaded.ActiveLabelOpacity.Should().Be(90);
        loaded.Hue.Should().Be(50);
        loaded.ActiveHue.Should().Be(60);
        loaded.Graphic.Should().Be(0x1234);
        loaded.ActiveGraphic.Should().Be(0x5678);
        loaded.Scale.Should().Be(150);
    }

    /// <summary>
    ///     Unset has to survive as unset: an absent or empty attribute is what tells "inherit the resting
    ///     value" apart from a value deliberately equal to it.
    /// </summary>
    [Fact]
    public void RoundTrip_KeepsUnsetRunningValuesUnset()
    {
        Macro macro = NewMacro();
        macro.Hue = 50;
        macro.Graphic = 0x1234;

        Macro loaded = RoundTrip(macro);

        loaded.ActiveLabel.Should().BeNull();
        loaded.ActiveLabelHue.Should().BeNull();
        loaded.ActiveLabelOpacity.Should().BeNull();
        loaded.ActiveHue.Should().BeNull();
        loaded.ActiveGraphic.Should().BeNull();
    }

    /// <summary>An empty label is a value, so it has to come back empty rather than as "follow the name".</summary>
    [Fact]
    public void RoundTrip_KeepsAHiddenLabelHidden()
    {
        Macro macro = NewMacro();
        macro.Label = string.Empty;

        Macro loaded = RoundTrip(macro);

        loaded.Label.Should().BeEmpty();
        loaded.LabelFor(false).Should().BeEmpty();
    }

    /// <summary>
    ///     A macro written before these attributes existed keeps the defaults. <c>TryParse</c> zeroes its
    ///     out parameter on failure, so reading an absent attribute straight into a field would leave a
    ///     black label at zero opacity.
    /// </summary>
    [Fact]
    public void Load_LeavesDefaultsAloneWhenTheAttributesAreAbsent()
    {
        Macro loaded = Load("<macro alt=\"False\" ctrl=\"False\" shift=\"False\" />");

        loaded.LabelHue.Should().Be(Macro.DEFAULT_LABEL_HUE);
        loaded.LabelOpacity.Should().Be(Macro.FULL_OPACITY);
        loaded.ActiveLabelHue.Should().BeNull();
        loaded.ActiveLabelOpacity.Should().BeNull();
        loaded.ActiveHue.Should().BeNull();
        loaded.ActiveGraphic.Should().BeNull();
    }

    /// <summary>
    ///     The flag these fields replaced only said whether to draw the macro's name, so it maps exactly:
    ///     hidden becomes an empty label, shown becomes null and still follows a later rename.
    /// </summary>
    [Theory]
    [InlineData("True", "")]
    [InlineData("False", null)]
    public void Load_MigratesTheOldHideLabelFlag(string hidden, string expectedLabel)
    {
        Macro loaded = Load($"<macro alt=\"False\" ctrl=\"False\" shift=\"False\" hidelabel=\"{hidden}\" />");

        loaded.Label.Should().Be(expectedLabel);
    }

    [Fact]
    public void Load_PrefersAStoredLabelOverTheOldHideLabelFlag()
    {
        Macro loaded = Load("<macro alt=\"False\" ctrl=\"False\" shift=\"False\" hidelabel=\"True\" label=\"Cure\" />");

        loaded.Label.Should().Be("Cure");
    }

    #endregion

    #region Helpers

    /// <summary>A macro with nothing but a name, which is every appearance field at its default.</summary>
    private static Macro NewMacro() => new(MACRO_NAME);

    /// <summary>Writes a macro out and reads it back, as saving and restarting the client would.</summary>
    /// <param name="macro">The macro to persist.</param>
    /// <returns>The macro as it comes back off disk.</returns>
    private static Macro RoundTrip(Macro macro)
    {
        var text = new StringWriter();

        using (var writer = new XmlTextWriter(text))
        {
            macro.Save(writer);
        }

        return Load(text.ToString());
    }

    /// <summary>Reads a macro from one <c>&lt;macro&gt;</c> element's markup.</summary>
    /// <param name="xml">The element, as text.</param>
    /// <returns>A macro carrying what the markup held.</returns>
    private static Macro Load(string xml)
    {
        var document = new XmlDocument();
        document.LoadXml(xml);

        var macro = new Macro(MACRO_NAME);
        macro.Load(document.DocumentElement);

        return macro;
    }

    #endregion
}
