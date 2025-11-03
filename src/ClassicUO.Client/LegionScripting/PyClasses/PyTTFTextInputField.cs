using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.LegionScripting.PyClasses;

public class PyTTFTextInputField(TTFTextInputField textInputField) : PyBaseControl(textInputField)
{
    public string Text
    {
        get
        {
            if (!VerifyIntegrity()) return string.Empty;

            return MainThreadQueue.InvokeOnMainThread(() => textInputField.Text);
        }
    }

    public int CaretIndex
    {
        get
        {
            if (!VerifyIntegrity()) return 0;

            return MainThreadQueue.InvokeOnMainThread(() => textInputField.CaretIndex);
        }
    }

    public bool NumbersOnly
    {
        get
        {
            if (!VerifyIntegrity()) return false;

            return MainThreadQueue.InvokeOnMainThread(() => textInputField.NumbersOnly);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.EnqueueAction(() => textInputField.NumbersOnly = value);
        }
    }

    public bool AcceptKeyboardInput
    {
        get
        {
            if (!VerifyIntegrity()) return false;

            return MainThreadQueue.InvokeOnMainThread(() => textInputField.AcceptKeyboardInput);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.EnqueueAction(() => textInputField.AcceptKeyboardInput = value);
        }
    }

    public bool ConvertHtmlColors
    {
        get
        {
            if (!VerifyIntegrity()) return false;

            return MainThreadQueue.InvokeOnMainThread(() => textInputField.ConvertHtmlColors);
        }
        set
        {
            if (!VerifyIntegrity()) return;

            MainThreadQueue.EnqueueAction(() => textInputField.ConvertHtmlColors = value);
        }
    }

    public void SetText(string text)
    {
        if (!VerifyIntegrity() || text == null) return;

        MainThreadQueue.EnqueueAction(() => textInputField.SetText(text));
    }

    public void SetPlaceholder(string text)
    {
        if (!VerifyIntegrity()) return;

        MainThreadQueue.EnqueueAction(() => textInputField.SetPlaceholder(text));
    }

    public void SetFocus()
    {
        if (!VerifyIntegrity()) return;

        MainThreadQueue.EnqueueAction(() => textInputField.SetFocus());
    }

    public void UpdateSize(int width, int height)
    {
        if (!VerifyIntegrity()) return;

        MainThreadQueue.EnqueueAction(() => textInputField.UpdateSize(width, height));
    }
}
