#nullable enable
using ClassicUO.Assets;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.LegionScripting;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.Brushes;
using System.Linq;

namespace ClassicUO.Game.UI.MyraWindows;

public class ScriptEditorWindow : MyraControl
{
    private readonly ScriptFile _script;
    private bool _hasChanges;
    private MyraButton _saveButton = null!;
    private MyraInputBox _editor = null!;
    private MyraInputBox _lines = null!;
    private ScrollViewer _scrollViewer = null!;

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
        var monoFont = TrueTypeLoader.Instance.GetFont(TrueTypeLoader.MONO_FONT, 18);

        _lines = new MyraInputBox
        {
            Text = "",
            Multiline = true,
            Enabled = false,
            Font = monoFont,
            // HorizontalAlignment = HorizontalAlignment.Right,
            Background = new SolidBrush(new Color(0, 0, 0, 75)),
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        _editor = new MyraInputBox
        {
            Text = content,
            Width = 700,
            Multiline = true,
            Font = monoFont,
            VerticalAlignment = VerticalAlignment.Stretch,
        };


        var codeRow = new HorizontalStackPanel { Spacing = 0 };
        codeRow.Widgets.Add(_lines);
        codeRow.Widgets.Add(_editor);

        _scrollViewer = new ScrollViewer { Content = codeRow, Height = 500 };

        UpdateLineNumbers();

        _editor.TextChangedByUser += (_, _) =>
        {
            _hasChanges = true;
            _saveButton.Enabled = true;
            UpdateLineNumbers();
        };

        _editor.CursorPositionChanged += (_, _) => EnsureCursorVisible();

        _saveButton = new MyraButton("Save Changes", () =>
        {
            _script.OverrideFileContents(_editor.Text ?? "");
            _hasChanges = false;
            _saveButton.Enabled = false;
        }) { Enabled = false };

        var root = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
        root.Widgets.Add(_scrollViewer);
        root.Widgets.Add(_saveButton);
        SetRootContent(root);
    }

    private void UpdateLineNumbers()
    {
        int lineCount = (_editor.Text ?? "").Split('\n').Length;
        var maxDigits = lineCount.ToString().Length;

        var numbers = Enumerable.Range(1, lineCount)
            .Select(n => n.ToString().PadLeft(maxDigits + 1));

        _lines.Text = string.Join("\n", numbers);
    }

    private void EnsureCursorVisible()
    {
        int cursorPos = _editor.CursorPosition;
        string text = _editor.Text ?? "";

        int lineIndex = text.Substring(0, cursorPos).Count(c => c == '\n');
        int lineHeight = 18;
        int cursorY = lineIndex * lineHeight;

        int visibleTop = _scrollViewer.ScrollPosition.Y;
        int visibleHeight = _scrollViewer.Height ?? 500;
        int visibleBottom = visibleTop + visibleHeight;

        if (cursorY < visibleTop)
            _scrollViewer.ScrollPosition = new Point(0, cursorY);
        else if (cursorY > visibleBottom - lineHeight * 2)
            _scrollViewer.ScrollPosition = new Point(0, cursorY - visibleHeight + lineHeight * 3);
    }
}
