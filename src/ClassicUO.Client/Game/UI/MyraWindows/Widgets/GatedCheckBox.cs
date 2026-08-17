#nullable enable

using System;
using Myra.Events;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>
/// A checkbox whose value can only change through a gate. A normal checkbox flips on click and
/// leaves the caller to undo it if the click turns out to be unwanted; this one cancels the click
/// outright and hands the attempted value to <c>gate</c>, which decides whether - and when - to
/// actually commit it. That means a confirmation dialog opened from the gate never has to show the
/// checkbox in the wrong state while it waits for an answer.
/// </summary>
public class GatedCheckBox : MyraCheckButton
{
    private Action<bool, Action<bool>> _gate = null!;

    /// <param name="text">Label text.</param>
    /// <param name="isChecked">Initial checked state.</param>
    /// <param name="gate">
    /// Invoked instead of flipping the value whenever the user tries to change it, with the value
    /// they asked for and a commit callback. Not called when the value is set programmatically
    /// (e.g. via <see cref="MyraCheckButton.CreatePropBoundCheckButton"/>-style binding), only on
    /// user interaction.
    /// </param>
    public GatedCheckBox(string text, bool isChecked, Action<bool, Action<bool>> gate) : base(text, isChecked)
    {
        Init(gate);
    }

    /// <param name="isChecked">Initial checked state.</param>
    /// <param name="gate">See the other constructor overload.</param>
    public GatedCheckBox(bool isChecked, Action<bool, Action<bool>> gate) : base(isChecked)
    {
        Init(gate);
    }

    private void Init(Action<bool, Action<bool>> gate)
    {
        ArgumentNullException.ThrowIfNull(gate);
        _gate = gate;
        PressedChangingByUser += OnPressedChangingByUser;
    }

    /// <summary>
    /// Cancels the click unconditionally and defers to the gate. Cancelling first means the
    /// checkbox's own visual state never moves until the gate itself calls the commit callback.
    /// </summary>
    private void OnPressedChangingByUser(object? sender, ValueChangingEventArgs<bool> args)
    {
        args.Cancel = true;
        _gate(args.NewValue, value => IsChecked = value);
    }
}
