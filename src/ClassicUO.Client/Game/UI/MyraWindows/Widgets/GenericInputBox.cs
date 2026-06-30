#nullable enable
using System;
using Myra.Events;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

public abstract class GenericInputBox<T> : TextBox where T : struct
{
    private T _value;
    private readonly Action<T>? _valueChangedCallback;

    public event EventHandler<ValueChangedEventArgs<T>>? ValueChanged;

    public T Value
    {
        get => _value;
        set
        {
            Validate(value, out T validatedValue);
            if (_value.Equals(validatedValue))
            {
                // Ensure UI text is in sync even if value didn't change (e.g. if text was "005" and value is 5)
                string formatted = FormatValue(validatedValue);
                if (Text != formatted)
                    Text = formatted;
                return;
            }

            T oldValue = _value;
            _value = validatedValue;
            Text = FormatValue(_value);
            OnValueChanged(oldValue, _value);
        }
    }

    protected GenericInputBox(): this(null) { }

    protected GenericInputBox(Action<T>? valueChangedCallback)
    {
        _valueChangedCallback = valueChangedCallback;
        ValueChanging += OnValueChangingInternal;
        TextChanged += OnTextChangedInternal;
    }

    private void OnValueChangingInternal(object? sender, ValueChangingEventArgs<string> e)
    {
        if (string.IsNullOrEmpty(e.NewValue))
        {
            if (AllowEmpty || IsIntermediate(e.NewValue))
                return;
            e.Cancel = true;
            return;
        }

        if (!TryParse(e.NewValue, out T result))
        {
            if (IsIntermediate(e.NewValue))
                return;
            e.Cancel = true;
            return;
        }

        // We don't call Validate here to avoid blocking Backspace/editing
        // when the intermediate value is out of range.
    }

    private void OnTextChangedInternal(object? sender, ValueChangedEventArgs<string> e)
    {
        if (!TryParse(e.NewValue, out T newValue))
            return;

        // Ensure the value is valid/clamped before updating the internal _value.
        // But we DON'T update the Text here to avoid interrupting the user's typing.
        Validate(newValue, out T validatedValue);

        if (_value.Equals(validatedValue))
            return;

        T oldValue = _value;
        _value = validatedValue;
        OnValueChanged(oldValue, _value);
    }

    public override void OnLostKeyboardFocus()
    {
        base.OnLostKeyboardFocus();
        // Sync the Text with the internal _value (which is guaranteed to be validated/clamped).
        Text = FormatValue(_value);
    }

    protected virtual void OnValueChanged(T oldValue, T newValue)
    {
        ValueChanged?.Invoke(this, new ValueChangedEventArgs<T>(oldValue, newValue));
        _valueChangedCallback?.Invoke(newValue);
    }

    protected abstract bool TryParse(string text, out T value);
    protected virtual string FormatValue(T value) => value.ToString() ?? string.Empty;

    protected virtual bool Validate(T value, out T validatedValue)
    {
        validatedValue = value;
        return true;
    }

    protected virtual bool IsIntermediate(string text) => false;
    protected virtual bool AllowEmpty => false;

    public override void OnTouchDoubleClick()
    {
        // Myra is buggy and does not actually consider enablement for double clicks here
        if (!Enabled)
            return;

        base.OnTouchDoubleClick();
    }
}
