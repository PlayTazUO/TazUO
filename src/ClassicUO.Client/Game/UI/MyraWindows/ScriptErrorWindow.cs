#nullable enable
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.LegionScripting;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows;

public class ScriptErrorWindow : MyraControl
{
    private static int _id = 1;

    public ScriptErrorWindow(ScriptErrorDetails errorDetails) : base("脚本错误 " + _id++)
    {
        Build(errorDetails);
        _rootWindow.UpdateArrange();
        CenterInViewPort();
        UIManager.Add(this);
        BringOnTop();
    }

    private void Build(ScriptErrorDetails errorDetails)
    {
        var root = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };

        root.Widgets.Add(new MyraLabel("您的脚本遇到了错误，以下是已知信息：", MyraLabel.TextStyle.P));

        // Clickable red error message
        var errorLabel = new MyraLabel(errorDetails.ErrorMsg, MyraLabel.TextStyle.P)
        {
            TextColor = Color.Red,
            Tooltip = "点击复制到剪贴板"
        };
        errorLabel.TouchDown += (_, _) =>
        {
            SDL3.SDL.SDL_SetClipboardText(errorDetails.ErrorMsg);
            GameActions.Print($"已将错误复制到剪贴板。", Constants.HUE_SUCCESS);
        };
        root.Widgets.Add(errorLabel);

        // Locations in reverse order (innermost first)
        for (int i = errorDetails.Locations.Count - 1; i >= 0; i--)
        {
            ScriptErrorLocation loc = errorDetails.Locations[i];

            root.Widgets.Add(new MyraLabel($"File: {loc.FileName}  |  Line: {loc.LineNumber}", MyraLabel.TextStyle.P));

            if (!string.IsNullOrEmpty(loc.LineContent))
            {
                root.Widgets.Add(new MyraInputBox
                {
                    Text = loc.LineContent,
                    Multiline = true,
                    Width = 480,
                    Height = 80,
                    Enabled = false
                });
            }
        }

        var btnRow = new HorizontalStackPanel { Spacing = 4 };
        btnRow.Widgets.Add(new MyraButton("编辑", () => new ScriptEditorWindow(errorDetails.Script)));
        btnRow.Widgets.Add(new MyraButton("外部编辑", () =>
            ClassicUO.Utility.FileSystemHelper.OpenFileWithDefaultApp(errorDetails.Script.FullPath)));
        root.Widgets.Add(btnRow);

        SetRootContent(root);
    }
}
