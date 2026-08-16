#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using ClassicUO.Configuration;
using ClassicUO.Game.ScreenDecorations.Triggers;
using ClassicUO.Game.UI.MyraWindows.Widgets.Search;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
/// Marks a <see cref="FalloffCurve" /> property as the one <see cref="FalloffPicker" /> should edit,
/// and names the siblings that only mean something alongside it.
/// <para>
/// Named rather than found by convention: the picker writes to all of them, and a property found by
/// guessing at its name would fail silently the day one was renamed.
/// </para>
/// </summary>
/// <param name="powerProperty">Sibling <see cref="float" /> holding the custom curve's power.</param>
/// <param name="nearStrengthProperty">Sibling <see cref="float" /> holding the strength at the near
/// edge of the range.</param>
/// <param name="farStrengthProperty">Sibling <see cref="float" /> holding the strength at the far
/// edge. Meaningless without a fade, and hidden when there is none.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class FalloffEditorAttribute(
    string powerProperty,
    string nearStrengthProperty,
    string farStrengthProperty
) : Attribute
{
    /// <summary>The sibling the custom curve's power is stored in.</summary>
    public string PowerProperty { get; } = powerProperty;

    /// <summary>The sibling holding the strength at the near edge of the range.</summary>
    public string NearStrengthProperty { get; } = nearStrengthProperty;

    /// <summary>The sibling holding the strength at the far edge of the range.</summary>
    public string FarStrengthProperty { get; } = farStrengthProperty;
}

/// <summary>The properties one <see cref="FalloffPicker" /> edits, resolved off its attribute.</summary>
/// <param name="Curve">Which curve is in force.</param>
/// <param name="Power">The custom curve's power.</param>
/// <param name="NearStrength">Strength at the near edge of the range.</param>
/// <param name="FarStrength">Strength at the far edge.</param>
public readonly record struct FalloffProperties(
    PropertyInfo Curve,
    PropertyInfo? Power,
    PropertyInfo? NearStrength,
    PropertyInfo? FarStrength
);

/// <summary>
/// Edits how an effect answers to distance: which curve, and the values only that curve gives
/// meaning to.
/// <para>
/// One editor rather than four grid rows, because three of the four are conditional. A power means
/// nothing except under <see cref="FalloffCurve.Custom" />, and a separate strength for the far edge
/// of the range means nothing under <see cref="FalloffCurve.Flat" />, where every distance is drawn
/// alike. Shown always, they read as knobs that do nothing.
/// </para>
/// <para>
/// The chosen curve is explained by a line of text under the list rather than by a tooltip. Myra
/// shows a tooltip for the widget the pointer is over, and the rows inside a dropdown are wrapped in
/// list buttons that take the pointer themselves - so a per-row tooltip never surfaces. A visible
/// description is better regardless: it needs no hover, and it answers "what did I just pick".
/// </para>
/// </summary>
public sealed class FalloffPicker : VerticalStackPanel
{
    #region Private members

    private const int SPACING = 4;

    /// <summary>Smallest power worth offering. Zero would flatten the curve and a negative one would
    /// invert it, both of which <see cref="ProximityMath.Shape" /> refuses anyway.</summary>
    private const float MIN_POWER = 0.05f;

    /// <summary>
    /// Every curve in the order the list offers them: gentlest first, so reading the list is also
    /// reading the scale, with the two that ignore distance at either end. Internal so a test can
    /// hold it against the enum - a curve added and not offered here is unreachable.
    /// </summary>
    internal static readonly FalloffCurve[] Offered =
    [
        FalloffCurve.Flat,
        FalloffCurve.SquareRoot,
        FalloffCurve.Linear,
        FalloffCurve.Quadratic,
        FalloffCurve.Cubic,
        FalloffCurve.Custom
    ];

    private readonly Dictionary<string, FalloffCurve> _byLabel = [];

    private readonly object _owner;

    private readonly FalloffProperties _properties;

    private readonly MyraLabel _description;

    private readonly HorizontalStackPanel? _powerRow;

    private readonly HorizontalStackPanel? _farStrengthRow;

    #endregion

    #region Ctor

    /// <param name="owner">The object holding the edited properties.</param>
    /// <param name="properties">The properties to edit.</param>
    /// <param name="listWidth">Width for the curve list.</param>
    /// <param name="numberWidth">Width for the numeric fields.</param>
    public FalloffPicker(object owner, FalloffProperties properties, int listWidth, int numberWidth)
    {
        ArgumentNullException.ThrowIfNull(owner);

        _owner = owner;
        _properties = properties;

        Spacing = SPACING;

        foreach (FalloffCurve offered in Offered)
            _byLabel[DisplayName(offered)] = offered;

        FalloffCurve curve = CurrentCurve();

        var curves = new ContainsLevenshteinComboBox(
            DisplayName(curve),
            _byLabel.Keys,
            OnCurveChosen,
            addSelectedItemIfMissing: false
        )
        {
            VerticalAlignment = VerticalAlignment.Center,
            Width = listWidth
        };

        MyraStyle.ApplySearchComboBoxPopupBorder(curves);

        // A notch smaller than the fields around it: it explains the control above rather than being
        // one, and at the same size it competes with them for the eye.
        _description = new MyraLabel(Description(curve), MyraLabel.TextStyle.H6) { Wrap = true };

        Widgets.Add(curves);
        Widgets.Add(_description);

        _powerRow = NumberRow(properties.Power, numberWidth, MIN_POWER);
        _farStrengthRow = NumberRow(properties.FarStrength, numberWidth, 0f, 1f);

        // Near strength is unconditional - every curve has a value at the near edge, even the one
        // that draws every distance alike.
        HorizontalStackPanel? near = NumberRow(properties.NearStrength, numberWidth, 0f, 1f);

        AddIfPresent(near);
        AddIfPresent(_farStrengthRow);
        AddIfPresent(_powerRow);

        ApplyCurveVisibility(curve);
    }

    #endregion

    #region Internal methods

    /// <summary>
    /// What one curve is called in the list. Kept to a word or two - the explanation is the line
    /// underneath, and a name long enough to need it is a name that overflows the list.
    /// </summary>
    /// <param name="curve">The curve to name.</param>
    /// <returns>Its display name.</returns>
    internal static string DisplayName(FalloffCurve curve) =>
        curve switch
        {
            FalloffCurve.Flat => TazLang.Get("falloff_flat", "None"),
            FalloffCurve.SquareRoot => TazLang.Get("falloff_sqrt", "Gentle"),
            FalloffCurve.Linear => TazLang.Get("falloff_linear", "Even"),
            FalloffCurve.Quadratic => TazLang.Get("falloff_quadratic", "Steep"),
            FalloffCurve.Cubic => TazLang.Get("falloff_cubic", "Very steep"),
            FalloffCurve.Custom => TazLang.Get("falloff_custom", "Custom"),
            _ => curve.ToString()
        };

    /// <summary>
    /// What one curve does, in terms of what the player will see. Shown under the list, so the
    /// current choice always explains itself.
    /// </summary>
    /// <param name="curve">The curve to describe.</param>
    /// <returns>Its description.</returns>
    internal static string Description(FalloffCurve curve) =>
        curve switch
        {
            FalloffCurve.Flat => TazLang.Get(
                "falloff_flat_tooltip",
                "Distance changes nothing - everything in range is full strength"
            ),
            FalloffCurve.SquareRoot => TazLang.Get(
                "falloff_sqrt_tooltip",
                "Starts high and fades slowly, staying strong to the edge"
            ),
            FalloffCurve.Linear => TazLang.Get(
                "falloff_linear_tooltip",
                "Fades evenly - halfway out is half strength"
            ),
            FalloffCurve.Quadratic => TazLang.Get(
                "falloff_quadratic_tooltip",
                "Starts high and fades quickly"
            ),
            FalloffCurve.Cubic => TazLang.Get(
                "falloff_cubic_tooltip",
                "Fades almost at once - only the closest tiles register"
            ),
            FalloffCurve.Custom => TazLang.Get(
                "falloff_custom_tooltip",
                "strength = nearness ^ power, where nearness runs 1 at the near edge to 0 at the far one.\n"
                + "A power of 1 is Even, 2 is Steep, 3 is Very steep"
            ),
            _ => string.Empty
        };

    #endregion

    #region Private methods

    private FalloffCurve CurrentCurve() =>
        _properties.Curve.GetValue(_owner) is FalloffCurve stored ? stored : FalloffCurve.Quadratic;

    private void AddIfPresent(Widget? row)
    {
        if (row != null)
            Widgets.Add(row);
    }

    /// <summary>
    /// One labelled numeric field, writing straight through to its property.
    /// </summary>
    /// <param name="property">The property to edit, or null to build nothing.</param>
    /// <param name="width">Width for the field.</param>
    /// <param name="minimum">Smallest accepted value.</param>
    /// <param name="maximum">Largest accepted value, or null for none.</param>
    /// <returns>The row, or null where there is no property behind it.</returns>
    private HorizontalStackPanel? NumberRow(PropertyInfo? property, int width, float minimum, float? maximum = null)
    {
        if (property == null)
            return null;

        var input = new FloatInputBox
        {
            MinValue = minimum,
            MaxValue = maximum,
            Width = width,
            VerticalAlignment = VerticalAlignment.Center,
            Value = property.GetValue(_owner) is float stored ? stored : minimum,

            // Its own, rather than inheriting the row's: the row above explains the curve, which says
            // nothing about what a number typed here does.
            Tooltip = ParameterMetadata.TooltipFor(property)
        };

        input.ValueChanged += (_, args) => property.SetValue(_owner, args.NewValue);

        return new HorizontalStackPanel
        {
            Spacing = SPACING,
            Widgets =
            {
                new MyraLabel(ParameterMetadata.LabelFor(property), MyraLabel.TextStyle.P)
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Tooltip = ParameterMetadata.TooltipFor(property)
                },
                input
            }
        };
    }

    private void OnCurveChosen(string? label)
    {
        if (label == null || !_byLabel.TryGetValue(label, out FalloffCurve curve))
            return;

        _properties.Curve.SetValue(_owner, curve);
        _description.Text = Description(curve);

        ApplyCurveVisibility(curve);
    }

    /// <summary>
    /// Shows only the fields the chosen curve gives meaning to. Hidden rather than disabled: a greyed
    /// field still reads as something that ought to apply.
    /// </summary>
    /// <param name="curve">The curve now chosen.</param>
    private void ApplyCurveVisibility(FalloffCurve curve)
    {
        _powerRow?.Visible = curve == FalloffCurve.Custom;

        // Without a fade there is only one strength, so a second one for the far edge would be a
        // field the effect never reads.
        _farStrengthRow?.Visible = curve != FalloffCurve.Flat;
    }

    #endregion
}
