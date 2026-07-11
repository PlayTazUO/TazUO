using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.LegionScripting.ApiClasses;

namespace ClassicUO.LegionScripting.ApiClasses;

public class ApiUiTtfTextInputField(TTFTextInputField textInputField) : ApiUiBaseControl(textInputField)
{
    public string Text => GetProp(() => textInputField.Text, string.Empty);

    public int CaretIndex => GetProp(() => textInputField.CaretIndex);

    public bool NumbersOnly
    {
        get => GetProp(() => textInputField.NumbersOnly);
        set => SetProp(() => textInputField.NumbersOnly = value);
    }

    public bool AcceptKeyboardInput
    {
        get => GetProp(() => textInputField.AcceptKeyboardInput);
        set => SetProp(() => textInputField.AcceptKeyboardInput = value);
    }

    public bool ConvertHtmlColors
    {
        get => GetProp(() => textInputField.ConvertHtmlColors);
        set => SetProp(() => textInputField.ConvertHtmlColors = value);
    }

    public void SetText(string text)
    {
        if (text != null) SetProp(() => textInputField.SetText(text));
    }

    public void SetPlaceholder(string text) => SetProp(() => textInputField.SetPlaceholder(text));

    public void SetFocus() => SetProp(() => textInputField.SetFocus());

    public void UpdateSize(int width, int height) => SetProp(() => textInputField.UpdateSize(width, height));
}
