using System;
using ClassicUO.Common;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public class PropertyBinder
{
    public Accessor<bool> BackingProperty { get; init; }

    public string Label { get; init; }
    public string LabelTooltip { get; init; }

    /// <summary>
    /// Invoked instead of applying the change immediately whenever the user tries to flip this
    /// checkbox, with the requested value and a commit callback. Null means the checkbox flips
    /// unconditionally. See <see cref="GatedCheckBox"/>, which this plugs into.
    /// </summary>
    public Action<bool, Action<bool>> Gate { get; init; }

    public PropertyBinder(Accessor<bool> backingProperty, string label, string labelTooltip = null, Action<bool, Action<bool>> gate = null)
    {
        ArgumentNullException.ThrowIfNull(backingProperty);
        BackingProperty = backingProperty;
        Label = label;
        LabelTooltip = labelTooltip;
        Gate = gate;
    }
}

public class CheckBoxGroup : Panel
{
    private readonly PropertyBinder _primaryControlProp;
    private readonly VerticalStackPanel _primaryPanel = new();
    private readonly VerticalStackPanel _dependentsPanel = new()
    {
        Margin = new Thickness(20, 0, 0, 0),
        Spacing = MyraStyle.STANDARD_SPACING,
    };

    public CheckBoxGroup(PropertyBinder controlProp, params Widget[] widgets)
    {
        ArgumentNullException.ThrowIfNull(controlProp);
        _primaryControlProp = controlProp;

        bool isChecked = _primaryControlProp.BackingProperty.Get();

        MyraCheckButton primaryCheckBox = _primaryControlProp.Gate != null
            ? new GatedCheckBox(_primaryControlProp.Label, isChecked, _primaryControlProp.Gate)
            : new MyraCheckButton(_primaryControlProp.Label, isChecked);

        if (_primaryControlProp.LabelTooltip != null)
            primaryCheckBox.Tooltip = _primaryControlProp.LabelTooltip;

        primaryCheckBox.IsCheckedChanged += (_, _) => OnPrimaryCheckBoxChanged(primaryCheckBox.IsChecked);
        primaryCheckBox.Margin = new Thickness(0, 0, 0, 8);

        _primaryPanel.Widgets.Add(primaryCheckBox);
        _primaryPanel.Widgets.Add(_dependentsPanel);

        if (widgets?.Length > 0)
            Add(widgets);

        UpdateChildrenEnablement(_primaryControlProp.BackingProperty.Get());
        EnabledChanged += (_, _) => UpdateChildrenEnablement(Enabled && primaryCheckBox.IsChecked);

        Children.Add(_primaryPanel);
        ChildrenLayout = new StackPanelLayout(Orientation.Vertical);
    }

    public void Add(params Widget[] widgets)
    {
        foreach (Widget widget in widgets)
            _dependentsPanel.Widgets.Add(widget);
    }

    private void OnPrimaryCheckBoxChanged(bool isChecked)
    {
        _primaryControlProp.BackingProperty.Set(isChecked);
        UpdateChildrenEnablement(isChecked);
    }

    /// <summary>
    /// Updates the enablement of all dependent widgets.
    /// </summary>
    /// <remarks>
    ///     When the primary checkbox is checked, enable all dependent widgets.
    ///     Note this will miss changes done to the property outside of this control flow.
    ///     Can be expanded with INotifyPropertyChanged for more robust handling but not necessary for now.
    /// </remarks>
    /// <param name="enabled">Whether to enable or disable the dependents</param>
    private void UpdateChildrenEnablement(bool enabled) => _dependentsPanel.Enabled = enabled;
}
