#nullable enable
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.LegionScripting;
using Myra.Graphics2D.UI;
using TextBox = Myra.Graphics2D.UI.TextBox;

namespace ClassicUO.Game.UI.MyraWindows;

public class ScriptEditorWindow : MyraControl
{
    private readonly ScriptFile _script;
    private bool _hasChanges;
    private MyraButton _saveButton = null!;

    private const int MAX_LENGTH = 1024 * 1024;

    public ScriptEditorWindow(ScriptFile script) : base(script.FileName)
    {
        _script = script;
        string content = string.Join("\n", script.ReadFromFile());

        if (content.Length > MAX_LENGTH)
        {
            GameActions.Print("File too large to edit!", Constants.HUE_ERROR);
            _disposeRequested = true;
            IsVisible = false; //Need to still add to uimanager to properly dispose later.
        }
        else
        {
            Build(content);
        }

        CenterInViewPort();
        UIManager.Add(this);
        BringOnTop();
    }

    private void Build(string content)
    {
        var editor = new TextBox
        {
            Text = content,
            Multiline = true,
            Width = 700,
            Height = 500,
        };
        editor.TextChangedByUser += (_, _) =>
        {
            _hasChanges = true;
            _saveButton.Enabled = true;
        };

        _saveButton = new MyraButton("Save Changes", () =>
        {
            _script.OverrideFileContents(editor.Text ?? "");
            _hasChanges = false;
            _saveButton.Enabled = false;
        }) { Enabled = false };

        var root = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
        root.Widgets.Add(editor);
        root.Widgets.Add(_saveButton);
        SetRootContent(root);
    }
}
