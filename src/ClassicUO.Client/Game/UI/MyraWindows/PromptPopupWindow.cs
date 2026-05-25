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

    public PromptPopupWindow(World world) : base("服务器提示")
    {
        _world = world;

        var layout = new VerticalStackPanel { Spacing = 8, Padding = new Thickness(8) };

        layout.Widgets.Add(new MyraLabel("服务器请求输入:", MyraLabel.TextStyle.P));

        _inputBox = new MyraInputBox { Width = 300, HintText = "请输入您的回复..." };
        layout.Widgets.Add(_inputBox);

        var disableCheck = MyraCheckButton.CreateWithCallback(
            !ProfileManager.CurrentProfile.UsePromptPopup,
            isChecked => ProfileManager.CurrentProfile.UsePromptPopup = !isChecked,
            "禁用此弹窗（改用聊天输入）",
            "勾选后，服务器提示将仅通过聊天输入处理"
        );
        layout.Widgets.Add(disableCheck);

        var btnRow = new HorizontalStackPanel { Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        btnRow.Widgets.Add(new MyraButton("提交", Submit));
        btnRow.Widgets.Add(new MyraButton("取消", Cancel));
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
