using System;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.LegionScripting.ApiClasses;

/// <summary>
/// Inherits from ApiUiBaseControl
/// Wraps a <see cref="HSliderBar"/> so scripts can read and change its value and react to changes.
/// </summary>
/// <param name="slider">The slider control to wrap.</param>
/// <param name="api">The owning API instance, used to dispatch value-changed callbacks to the script.</param>
public class ApiUiSlider(HSliderBar slider, LegionAPI api) : ApiUiBaseControl(slider)
{
    /// <summary>
    /// Gets or sets the slider's current value. Values outside MinValue/MaxValue are clamped.
    /// </summary>
    public int Value
    {
        get => GetProp(() => slider.Value);
        set => SetProp(() => slider.Value = value);
    }

    /// <summary>
    /// Gets or sets the slider's minimum value.
    /// </summary>
    public int MinValue
    {
        get => GetProp(() => slider.MinValue);
        set => SetProp(() => slider.MinValue = value);
    }

    /// <summary>
    /// Gets or sets the slider's maximum value.
    /// </summary>
    public int MaxValue
    {
        get => GetProp(() => slider.MaxValue);
        set => SetProp(() => slider.MaxValue = value);
    }

    /// <summary>
    /// Gets the slider's current value.
    /// Used in python API
    /// </summary>
    /// <returns>The current value</returns>
    public int GetValue() => GetProp(() => slider.Value);

    /// <summary>
    /// Sets the slider's current value. Values outside MinValue/MaxValue are clamped.
    /// Used in python API
    /// </summary>
    /// <param name="value">The new value</param>
    /// <returns>Returns this control so methods can be chained.</returns>
    public ApiUiSlider SetValue(int value)
    {
        SetProp(() => slider.Value = value);
        return this;
    }

    /// <summary>
    /// Add an onValueChanged callback to this slider, invoked whenever the value changes.
    /// The callback function receives the new value as its only argument.
    /// Example:
    /// ```py
    /// def on_change(value):
    ///   API.SysMsg(f"Slider is now {value}")
    ///
    /// slider = API.Gumps.CreateGumpSlider(200, 0, 100, 50)
    /// slider.OnValueChanged(on_change)
    ///
    /// while True:
    ///   API.ProcessCallbacks()
    /// ```
    /// </summary>
    /// <param name="onValueChanged">The callback function that receives the new value</param>
    /// <returns>Returns this control so methods can be chained.</returns>
    public ApiUiSlider OnValueChanged(object onValueChanged)
    {
        if (!VerifyIntegrity() || onValueChanged == null || api == null || !api.CallbackChannel.CanInvoke(onValueChanged))
            return this;

        slider.ValueChanged += (_, _) =>
        {
            api?.ScheduleCallback(() =>
            {
                try
                {
                    api.CallbackChannel.Invoke(onValueChanged, slider.Value);
                }
                catch (Exception ex)
                {
                    Game.GameActions.Print($"Script callback error: {ex}", Constants.HUE_ERROR);
                }
            });
        };

        return this;
    }
}
