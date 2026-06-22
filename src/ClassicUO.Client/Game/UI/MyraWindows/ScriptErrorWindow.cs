#nullable enable
using ClassicUO.Configuration;
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

    public ScriptErrorWindow(ScriptErrorDetails errorDetails) : base(TazLang.Get("myra_scripterror_title_fmt", new[] { _id++.ToString() }))
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

        root.Widgets.Add(new MyraLabel(TazLang.Get("myra_scripterror_intro", "Your script encountered an error, here's what we know:"), MyraLabel.TextStyle.P));

        // Clickable red error message
        var errorLabel = new MyraLabel(errorDetails.ErrorMsg, MyraLabel.TextStyle.P)
        {
            TextColor = Color.Red,
            Tooltip = TazLang.Get("myra_scripterror_tooltip_copy", "Click to copy to clipboard")
        };
        errorLabel.TouchDown += (_, _) =>
        {
            SDL3.SDL.SDL_SetClipboardText(errorDetails.ErrorMsg);
            GameActions.Print(TazLang.Get("shared_copied", "Copied to clipboard!"), Constants.HUE_SUCCESS);
        };
        root.Widgets.Add(errorLabel);

        // Locations in reverse order (innermost first)
        for (int i = errorDetails.Locations.Count - 1; i >= 0; i--)
        {
            ScriptErrorLocation loc = errorDetails.Locations[i];

            root.Widgets.Add(new MyraLabel(TazLang.Get("myra_scripterror_file_line_fmt", new[] { loc.FileName, loc.LineNumber.ToString() }), MyraLabel.TextStyle.P));

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
        btnRow.Widgets.Add(new MyraButton(TazLang.Get("shared_edit", "Edit"), () => new ScriptEditorWindow(errorDetails.Script)));
        btnRow.Widgets.Add(new MyraButton(TazLang.Get("myra_scripterror_btn_edit_externally", "Edit Externally"), () =>
            ClassicUO.Utility.FileSystemHelper.OpenFileWithDefaultApp(errorDetails.Script.FullPath)));
        root.Widgets.Add(btnRow);

        SetRootContent(root);
    }
}
