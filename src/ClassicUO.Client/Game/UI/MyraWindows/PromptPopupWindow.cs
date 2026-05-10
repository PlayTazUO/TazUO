using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Network;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

public class PromptPopupWindow : MyraControl
{
    private readonly World _world;
    private readonly MyraInputBox _inputBox;

    public PromptPopupWindow(World world) : base("Server Prompt")
    {
        _world = world;

        var layout = new VerticalStackPanel { Spacing = 8, Padding = new Thickness(8) };

        layout.Widgets.Add(new MyraLabel("The server is requesting input:", MyraLabel.TextStyle.P));

        _inputBox = new MyraInputBox { Width = 300, HintText = "Enter your response..." };
        layout.Widgets.Add(_inputBox);

        var disableCheck = MyraCheckButton.CreateWithCallback(
            !ProfileManager.CurrentProfile.UsePromptPopup,
            isChecked => ProfileManager.CurrentProfile.UsePromptPopup = !isChecked,
            "Disable this popup (use chat instead)",
            "When checked, server prompts will only be handled through the chat input"
        );
        layout.Widgets.Add(disableCheck);

        var btnRow = new HorizontalStackPanel { Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        btnRow.Widgets.Add(new MyraButton("Submit", Submit));
        btnRow.Widgets.Add(new MyraButton("Cancel", Cancel));
        layout.Widgets.Add(btnRow);

        SetRootContent(layout);
        CenterInViewPort();
        UIManager.Add(this);
        BringOnTop();
    }

    private void Submit()
    {
        string text = _inputBox.Text ?? string.Empty;
        SendResponse(text, text.Length < 1);
        _disposeRequested = true;
    }

    private void Cancel()
    {
        SendResponse(string.Empty, true);
        _disposeRequested = true;
    }

    private void SendResponse(string text, bool cancel)
    {
        PromptData promptData = _world.MessageManager.PromptData;
        if (promptData.Prompt == ConsolePrompt.ASCII)
        {
            AsyncNetClient.Socket.Send_ASCIIPromptResponse(_world, text, cancel);
        }
        else if (promptData.Prompt == ConsolePrompt.Unicode)
        {
            AsyncNetClient.Socket.Send_UnicodePromptResponse(_world, text, Settings.GlobalSettings.Language, cancel);
        }
        _world.MessageManager.PromptData = default;
    }
}
