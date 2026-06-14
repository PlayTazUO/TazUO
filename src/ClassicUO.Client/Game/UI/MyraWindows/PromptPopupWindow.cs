using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Network;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

/// <summary>
/// A reusable popup that collects a single line of text from the user.
/// Use the general constructor for arbitrary prompts, or the <see cref="PromptPopupWindow(World)"/>
/// constructor for responding to server prompts (which adds a checkbox to disable the popup).
/// </summary>
public class PromptPopupWindow : MyraControl
{
    private readonly MyraInputBox _inputBox;
    private readonly Action<string> _onSubmit;
    private readonly Action _onCancel;

    /// <summary>
    /// Creates a reusable text prompt popup.
    /// </summary>
    /// <param name="title">The window title.</param>
    /// <param name="message">The message shown above the input box.</param>
    /// <param name="onSubmit">Invoked with the entered text when the submit button (or Enter) is used.</param>
    /// <param name="submitText">Text for the submit button.</param>
    /// <param name="cancelText">Text for the cancel button.</param>
    /// <param name="onCancel">Invoked when the cancel button is used. Optional.</param>
    /// <param name="defaultValue">The initial value of the input box.</param>
    /// <param name="hintText">Placeholder text shown while the input box is empty.</param>
    public PromptPopupWindow(
        string title,
        string message,
        Action<string> onSubmit,
        string submitText = "Submit",
        string cancelText = "Cancel",
        Action onCancel = null,
        string defaultValue = "",
        string hintText = "Enter your response..."
    ) : this(title, message, onSubmit, submitText, cancelText, onCancel, defaultValue, hintText, null)
    {
    }

    /// <summary>
    /// Creates a prompt popup for responding to a server prompt. Includes a checkbox that lets the
    /// user disable the popup for future system prompts (handling them through chat instead).
    /// </summary>
    public PromptPopupWindow(World world) : this(
        "Server Prompt",
        "The server is requesting input:",
        text => SendResponse(world, text, text.Length < 1),
        "Submit",
        "Cancel",
        () => SendResponse(world, string.Empty, true),
        string.Empty,
        "Enter your response...",
        BuildDisablePopupCheckbox()
    )
    {
    }

    private PromptPopupWindow(
        string title,
        string message,
        Action<string> onSubmit,
        string submitText,
        string cancelText,
        Action onCancel,
        string defaultValue,
        string hintText,
        Widget extraWidget
    ) : base(title)
    {
        _onSubmit = onSubmit;
        _onCancel = onCancel;

        var layout = new VerticalStackPanel { Spacing = 8, Padding = new Thickness(8) };

        layout.Widgets.Add(new MyraLabel(message, MyraLabel.TextStyle.P));

        _inputBox = new MyraInputBox { Width = 300, HintText = hintText, Text = defaultValue ?? string.Empty };
        _inputBox.KeyDown += (s, e) =>
        {
            if (e.Data == Microsoft.Xna.Framework.Input.Keys.Enter)
            {
                Submit();
            }
        };
        layout.Widgets.Add(_inputBox);

        if (extraWidget != null)
            layout.Widgets.Add(extraWidget);

        var btnRow = new HorizontalStackPanel { Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        btnRow.Widgets.Add(new MyraButton(submitText, Submit));
        btnRow.Widgets.Add(new MyraButton(cancelText, Cancel));
        layout.Widgets.Add(btnRow);

        SetRootContent(layout);
        CenterInViewPort();
        UIManager.Add(this);
        BringOnTop();
        UIManager.KeyboardFocusControl = this;
        _inputBox.SetKeyboardFocus();
    }

    private void Submit()
    {
        _onSubmit?.Invoke(_inputBox.Text ?? string.Empty);
        _disposeRequested = true;
    }

    private void Cancel()
    {
        _onCancel?.Invoke();
        _disposeRequested = true;
    }

    private static MyraCheckButton BuildDisablePopupCheckbox() =>
        MyraCheckButton.CreateWithCallback(
            !ProfileManager.CurrentProfile.UsePromptPopup,
            isChecked => ProfileManager.CurrentProfile.UsePromptPopup = !isChecked,
            "Disable this popup (use chat instead)",
            "When checked, server prompts will only be handled through the chat input"
        );

    private static void SendResponse(World world, string text, bool cancel)
    {
        PromptData promptData = world.MessageManager.PromptData;
        if (promptData.Prompt == ConsolePrompt.ASCII)
        {
            AsyncNetClient.Socket.Send_ASCIIPromptResponse(world, text, cancel);
        }
        else if (promptData.Prompt == ConsolePrompt.Unicode)
        {
            AsyncNetClient.Socket.Send_UnicodePromptResponse(world, text, Settings.GlobalSettings.Language, cancel);
        }
        world.MessageManager.PromptData = default;
    }
}
