#nullable enable
// Myra's ComboBox/ListBox/ListItem are deprecated in favour of ComboView/ListView, but every other
// window in this client still uses them; matching that rather than introducing a second convention.
#pragma warning disable CS0612, CS0618
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI;

/// <summary>
/// The in-game visual editor for XML gumps: a Myra window with the element list, a live canvas the
/// elements can be dragged around, and a property panel for the selected element.
/// </summary>
/// <remarks>
/// Edits the document in memory and only writes it to disk on Save, so a session can be abandoned.
/// Open through <see cref="Show"/>, which keeps a single editor across call sites.
/// </remarks>
public sealed class XmlGumpEditorWindow : MyraControl
{
    #region Private members

    private const int LIST_MIN_WIDTH = 190;
    private const int PROPERTIES_MIN_WIDTH = 380;
    private const int EDITOR_WIDTH = 110;
    private const int SMALL_EDITOR_WIDTH = 70;

    /// <summary>Wider than the numeric editors: text holds font names, placeholders and macro names.</summary>
    private const int TEXT_EDITOR_WIDTH = 240;

    private static readonly XmlGumpAttributeDefinition[] RootSchema =
    [
        new("x", XmlGumpAttributeKind.Integer, "X", "0"),
        new("y", XmlGumpAttributeKind.Integer, "Y", "0"),
        new("locked", XmlGumpAttributeKind.Boolean, "Locked", "false"),
        new("saveposition", XmlGumpAttributeKind.Boolean, "Save position", "false"),
        new("acceptmouseinput", XmlGumpAttributeKind.Boolean, "Accept mouse input", "true")
    ];

    private readonly XmlGumpEditorCanvas _canvas;
    private readonly ListBox _elementList = new();
    private readonly VerticalStackPanel _properties = new();
    private readonly ComboBox _openCombo = new();
    private readonly ComboBox _addCombo = new();
    private readonly MyraLabel _status = new(string.Empty, MyraLabel.TextStyle.P);

    private XmlGumpDocument _document;
    private XmlGumpPreviewWindow? _preview;
    private bool _suppressList;
    private bool _dirty;
    private bool _closing;

    #endregion

    #region Ctor

    private XmlGumpEditorWindow(World world, XmlGumpDocument document)
        : base(TazLang.Get("xmlgumpeditor_title", "XML Gump Editor"))
    {
        _document = document;
        _canvas = new XmlGumpEditorCanvas(world);

        _canvas.SelectionChanged += OnCanvasSelectionChanged;
        _canvas.ElementMoved += OnCanvasElementMoved;

        Build();
        _canvas.Rebuild(_document.Elements);
        RebuildElementList();
        BuildProperties();
        UpdateTitle();
        CenterInViewPort();

        _rootWindow.Help = new HelpButton(
            BuildTextPlaceholderHelp(),
            TazLang.Get("xmlgumpeditor_help_title", "Text replacement tags")
        );

        _preview = new XmlGumpPreviewWindow(_canvas, OnPreviewClosed);
        UIManager.Add(_preview);
        _preview.SetPosition(X + Width + 8, Y);
        _preview.SetInScreen();
    }

    #endregion

    #region Public methods

    /// <summary>Opens the editor, replacing any editor already open.</summary>
    /// <param name="world">The game world, used for player-bound previews.</param>
    /// <param name="filePath">A gump file to load, or null to start a new empty gump.</param>
    public static void Show(World world, string? filePath = null)
    {
        foreach (IGui gump in UIManager.Gumps.ToList())
            if (gump is XmlGumpEditorWindow { IsDisposed: false } existing)
                existing.Dispose();

        XmlGumpDocument document = !string.IsNullOrEmpty(filePath) && File.Exists(filePath)
            ? XmlGumpDocument.Load(filePath)
            : new XmlGumpDocument();

        UIManager.Add(new XmlGumpEditorWindow(world, document));
    }

    public override void Dispose()
    {
        if (!_closing)
        {
            _closing = true;
            _preview?.Dispose();
        }

        _canvas.Release();
        base.Dispose();
    }

    private void OnPreviewClosed()
    {
        if (_closing)
            return;

        Dispose();
    }

    #endregion

    #region Layout

    private void Build()
    {
        var root = new VerticalStackPanel { Spacing = 6, Padding = new Thickness(8) };

        root.Widgets.Add(BuildToolbar());
        root.Widgets.Add(BuildBody());
        root.Widgets.Add(_status);

        SetRootContent(root);
    }

    private Widget BuildToolbar()
    {
        var bar = new HorizontalStackPanel { Spacing = 6 };

        bar.Widgets.Add(new MyraButton(TazLang.Get("xmlgumpeditor_new", "New"), NewDocument));
        bar.Widgets.Add(BuildOpenControl());
        bar.Widgets.Add(new MyraButton(TazLang.Get("xmlgumpeditor_save", "Save"), Save));
        bar.Widgets.Add(new MyraButton(TazLang.Get("xmlgumpeditor_saveas", "Save As"), SaveAs));
        bar.Widgets.Add(new MyraButton(TazLang.Get("xmlgumpeditor_export", "Export"), ExportToClipboard));
        bar.Widgets.Add(new MyraButton(TazLang.Get("xmlgumpeditor_import", "Import"), ImportFromClipboard));

        return bar;
    }

    private Widget BuildOpenControl()
    {
        _openCombo.Width = 160;
        RefreshFileList();
        _openCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_openCombo.SelectedItem?.Tag is string name)
                OpenNamed(name);
        };

        return _openCombo;
    }

    private Widget BuildBody()
    {
        // The canvas lives in its own window (see XmlGumpPreviewWindow); this holds the element list
        // and the property panel only.
        var body = new HorizontalStackPanel { Spacing = 8 };

        body.Widgets.Add(BuildElementPanel());
        body.Widgets.Add(BuildPropertiesPanel());

        return body;
    }

    private Widget BuildElementPanel()
    {
        var panel = new VerticalStackPanel { Spacing = 4, MinWidth = LIST_MIN_WIDTH };

        panel.Widgets.Add(new MyraLabel(TazLang.Get("xmlgumpeditor_elements", "Elements"), MyraLabel.TextStyle.H5));

        var addRow = new HorizontalStackPanel { Spacing = 4 };
        _addCombo.Width = 120;

        foreach (XmlGumpTagDefinition tag in XmlGumpSchema.Tags)
            _addCombo.Items.Add(new ListItem(TazLang.Get(XmlGumpSchema.LabelKey(tag.Tag), tag.Display), null, tag.Tag));

        addRow.Widgets.Add(_addCombo);
        addRow.Widgets.Add(new MyraButton(TazLang.Get("xmlgumpeditor_add", "Add"), AddSelectedElement));
        panel.Widgets.Add(addRow);

        var actions = new HorizontalStackPanel { Spacing = 4 };
        actions.Widgets.Add(new MyraButton(TazLang.Get("xmlgumpeditor_duplicate", "Copy"), DuplicateSelected));
        actions.Widgets.Add(new MyraButton(TazLang.Get("xmlgumpeditor_delete", "Delete"), DeleteSelected));
        actions.Widgets.Add(new MyraButton("^", () => MoveSelected(-1)));
        actions.Widgets.Add(new MyraButton("v", () => MoveSelected(1)));
        panel.Widgets.Add(actions);

        _elementList.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressList)
                return;

            _canvas.Selected = _elementList.SelectedItem?.Tag as XmlGumpElement;
        };

        panel.Widgets.Add(new ScrollViewer { MaxHeight = XmlGumpEditorCanvas.CANVAS_HEIGHT, MinWidth = LIST_MIN_WIDTH, Content = _elementList });

        return panel;
    }

    /// <summary>
    /// Builds the body of the window's help dialog: every text-replacement tag the runtime resolves,
    /// read straight off <see cref="XmlGumpHandler.TextPlaceholders" /> so it stays in step.
    /// </summary>
    private static Widget BuildTextPlaceholderHelp()
    {
        var list = new VerticalStackPanel { Spacing = 2 };

        list.Widgets.Add(new MyraLabel(
            TazLang.Get("xmlgumpeditor_help_intro",
                "These tags may be used in a text element, and in a progress bar's value and max_value. They update live when the element has updates enabled."),
            MyraLabel.TextStyle.P
        ));
        list.Widgets.Add(new MyraSpacer(0, 6));

        foreach (XmlGumpHandler.TextPlaceholder placeholder in XmlGumpHandler.TextPlaceholders)
        {
            var row = new HorizontalStackPanel { Spacing = 8 };
            row.Widgets.Add(new MyraLabel(placeholder.Token, MyraLabel.TextStyle.P) { MinWidth = 130 });
            row.Widgets.Add(new MyraLabel(TazLang.Get($"xmlgumpeditor_ph_{placeholder.Token.Trim('{', '}')}", placeholder.Description), MyraLabel.TextStyle.P));
            list.Widgets.Add(row);
        }

        return new ScrollViewer { MaxHeight = 420, Content = list };
    }

    private Widget BuildPropertiesPanel()
    {
        _properties.Spacing = 4;

        return new ScrollViewer
        {
            MaxHeight = XmlGumpEditorCanvas.CANVAS_HEIGHT + 24,
            MinWidth = PROPERTIES_MIN_WIDTH,
            Content = _properties
        };
    }

    #endregion

    #region File operations

    private void RefreshFileList()
    {
        _openCombo.Items.Clear();
        _openCombo.Items.Add(new ListItem(TazLang.Get("xmlgumpeditor_open", "Open...")));

        foreach (string name in XmlGumpHandler.GetAllXmlGumps())
            _openCombo.Items.Add(new ListItem(name, null, name));
    }

    private void NewDocument()
    {
        ConfirmDiscard(() =>
        {
            _document = new XmlGumpDocument();
            _canvas.Selected = null;
            _canvas.Rebuild(_document.Elements);
            RebuildElementList();
            BuildProperties();
            _dirty = false;
            UpdateTitle();
            SetStatus(TazLang.Get("xmlgumpeditor_newstatus", "New gump started."));
        });
    }

    private void OpenNamed(string name)
    {
        string path = Path.Combine(XmlGumpHandler.XmlGumpPath, name + ".xml");

        if (!File.Exists(path))
            return;

        ConfirmDiscard(() =>
        {
            LoadDocument(path);
            _openCombo.SelectedIndex = 0;
        });
    }

    private void LoadDocument(string path)
    {
        try
        {
            _document = XmlGumpDocument.Load(path);
        }
        catch (Exception e)
        {
            SetStatus(e.Message);

            return;
        }

        _canvas.Selected = null;
        _canvas.Rebuild(_document.Elements);
        RebuildElementList();
        BuildProperties();
        _dirty = false;
        UpdateTitle();
        SetStatus(TazLang.Get("xmlgumpeditor_loaded", "Loaded {0}").Replace("{0}", path));
    }

    private void Save()
    {
        if (string.IsNullOrEmpty(_document.FilePath))
        {
            SaveAs();

            return;
        }

        WriteDocument(_document.FilePath);
    }

    private void SaveAs()
    {
        new PromptPopupWindow(
            TazLang.Get("xmlgumpeditor_saveas", "Save As"),
            TazLang.Get("xmlgumpeditor_saveas_prompt", "File name (without extension):"),
            name =>
            {
                if (string.IsNullOrWhiteSpace(name))
                    return;

                string path = Path.Combine(XmlGumpHandler.XmlGumpPath, name.Trim() + ".xml");
                WriteDocument(path);
            },
            TazLang.Get("xmlgumpeditor_save", "Save"),
            TazLang.Get("uicommons_cancel", "Cancel"),
            null,
            _document.FilePath != null ? Path.GetFileNameWithoutExtension(_document.FilePath) : "mygump"
        );
    }

    private void WriteDocument(string path)
    {
        try
        {
            _document.Save(path);
            _dirty = false;
            UpdateTitle();
            RefreshFileList();
            SetStatus(TazLang.Get("xmlgumpeditor_saved", "Saved {0}").Replace("{0}", path));
        }
        catch (Exception e)
        {
            SetStatus(e.Message);
        }
    }

    private void ExportToClipboard()
    {
        try
        {
            _document.ToXmlString().CopyToClipboard();
            SetStatus(TazLang.Get("xmlgumpeditor_exported", "XML copied to clipboard."));
        }
        catch (Exception e)
        {
            SetStatus(e.Message);
        }
    }

    private void ImportFromClipboard()
    {
        string? xml = Clipboard.GetClipboardText();

        if (string.IsNullOrWhiteSpace(xml))
        {
            SetStatus(TazLang.Get("xmlgumpeditor_importempty", "Clipboard is empty."));

            return;
        }

        XmlGumpDocument imported;

        try
        {
            imported = XmlGumpDocument.FromString(xml);
        }
        catch (Exception e)
        {
            SetStatus(TazLang.Get("xmlgumpeditor_importfailed", "Import failed: {0}").Replace("{0}", e.Message));

            return;
        }

        ConfirmDiscard(() =>
        {
            _document = imported;
            _document.FilePath = null;
            _canvas.Selected = null;
            _canvas.Rebuild(_document.Elements);
            RebuildElementList();
            BuildProperties();
            _dirty = true;
            UpdateTitle();
            SetStatus(TazLang.Get("xmlgumpeditor_imported", "Imported from clipboard."));
        });
    }

    private void ConfirmDiscard(Action action)
    {
        if (!_dirty)
        {
            action();

            return;
        }

        new MyraDialog(
            TazLang.Get("xmlgumpeditor_discard_title", "Unsaved changes"),
            new MyraLabel(TazLang.Get("xmlgumpeditor_discard", "Discard unsaved changes?"), MyraLabel.TextStyle.P),
            ok =>
            {
                if (ok)
                    action();
            }
        );
    }

    #endregion

    #region Element operations

    private void RebuildElementList()
    {
        _suppressList = true;
        _elementList.Items.Clear();
        AddListItems(_document.Elements, 0);
        _suppressList = false;

        if (_canvas.Selected != null)
            SelectListItemFor(_canvas.Selected);
    }

    private void AddListItems(List<XmlGumpElement> elements, int depth)
    {
        foreach (XmlGumpElement element in elements)
        {
            var item = new ListItem(new string(' ', depth * 2) + Label(element), null, element);
            _elementList.Items.Add(item);
            AddListItems(element.Children, depth + 1);
        }
    }

    private static string Label(XmlGumpElement element)
    {
        string? id = element.Tag is "image" or "macro_button_graphic" ? element.GetAttribute("id") : null;

        return id is { Length: > 0 } ? $"{element.Tag} #{id}" : element.Tag;
    }

    private void SelectListItemFor(XmlGumpElement element)
    {
        _suppressList = true;

        for (int i = 0; i < _elementList.Items.Count; i++)
        {
            if (_elementList.Items[i].Tag is XmlGumpElement candidate && ReferenceEquals(candidate, element))
            {
                _elementList.SelectedIndex = i;

                break;
            }
        }

        _suppressList = false;
    }

    private void AddSelectedElement()
    {
        if (_addCombo.SelectedItem?.Tag is not string tag)
            return;

        XmlGumpElement element = XmlGumpSchema.CreateElement(tag);
        List<XmlGumpElement> target = _canvas.Selected is { Tag: "control" } container ? container.Children : _document.Elements;
        target.Add(element);

        MarkDirty();
        RebuildElementList();
        _canvas.Selected = element;
    }

    private void DuplicateSelected()
    {
        if (_canvas.Selected is not { } selected)
            return;

        List<XmlGumpElement>? siblings = FindParentList(selected, _document.Elements);

        if (siblings == null)
            return;

        XmlGumpElement copy = selected.Clone();
        siblings.Insert(siblings.IndexOf(selected) + 1, copy);
        MarkDirty();
        RebuildElementList();
        _canvas.Selected = copy;
    }

    private void DeleteSelected()
    {
        if (_canvas.Selected is not { } selected)
            return;

        List<XmlGumpElement>? siblings = FindParentList(selected, _document.Elements);

        if (siblings == null || !siblings.Remove(selected))
            return;

        _canvas.Selected = null;
        MarkDirty();
        RebuildElementList();
    }

    private void MoveSelected(int direction)
    {
        if (_canvas.Selected is not { } selected)
            return;

        List<XmlGumpElement>? siblings = FindParentList(selected, _document.Elements);

        if (siblings == null)
            return;

        int index = siblings.IndexOf(selected);
        int target = index + direction;

        if (target < 0 || target >= siblings.Count)
            return;

        siblings.RemoveAt(index);
        siblings.Insert(target, selected);
        MarkDirty();
        RebuildElementList();
        _canvas.Selected = selected;
    }

    private static List<XmlGumpElement>? FindParentList(XmlGumpElement target, List<XmlGumpElement> list)
    {
        if (list.Contains(target))
            return list;

        foreach (XmlGumpElement element in list)
        {
            List<XmlGumpElement>? found = FindParentList(target, element.Children);

            if (found != null)
                return found;
        }

        return null;
    }

    #endregion

    #region Selection and properties

    private void OnCanvasSelectionChanged(XmlGumpElement? element)
    {
        if (element == null)
        {
            _suppressList = true;
            _elementList.SelectedIndex = null;
            _suppressList = false;
        }
        else
        {
            SelectListItemFor(element);
        }

        BuildProperties();
    }

    private void OnCanvasElementMoved()
    {
        // The preview is already at its new position, so only the title and the X/Y fields need
        // refreshing - a full canvas rebuild here would re-create every preview on each key repeat.
        _dirty = true;
        UpdateTitle();
        BuildProperties();
    }

    private void BuildProperties()
    {
        _properties.Widgets.Clear();

        if (_canvas.Selected is { } element)
            BuildElementProperties(element);
        else
            BuildRootProperties();
    }

    private void BuildElementProperties(XmlGumpElement element)
    {
        _properties.Widgets.Add(new MyraLabel(Label(element), MyraLabel.TextStyle.H5));

        XmlGumpTagDefinition? definition = XmlGumpSchema.Find(element.Tag);

        if (definition == null)
        {
            _properties.Widgets.Add(new MyraLabel(TazLang.Get("xmlgumpeditor_unknown", "Unknown element type."), MyraLabel.TextStyle.P));

            return;
        }

        if (element.Tag == "text")
            AddPropertyRow(
                TazLang.Get("xmlgumpeditor_content", "Text"),
                BuildTextEditor(() => element.InnerText, value =>
                {
                    element.InnerText = value;
                    MarkDirty();
                })
            );

        foreach (XmlGumpAttributeDefinition attribute in definition.Attributes)
            AddPropertyRow(
                TazLang.Get(XmlGumpSchema.AttributeKey(element.Tag, attribute.Name), attribute.Display),
                BuildEditor(element.Tag, attribute, element.GetAttribute, element.SetAttribute)
            );
    }

    private void BuildRootProperties()
    {
        _properties.Widgets.Add(new MyraLabel(TazLang.Get("xmlgumpeditor_gump", "Gump"), MyraLabel.TextStyle.H5));
        _properties.Widgets.Add(new MyraLabel(TazLang.Get("xmlgumpeditor_selecthint", "Select an element to edit it."), MyraLabel.TextStyle.P));

        foreach (XmlGumpAttributeDefinition attribute in RootSchema)
            AddPropertyRow(
                TazLang.Get(XmlGumpSchema.AttributeKey("gump", attribute.Name), attribute.Display),
                BuildEditor("gump", attribute, _document.GetRootAttribute, _document.SetRootAttribute)
            );
    }

    private void AddPropertyRow(string label, Widget editor)
    {
        var row = new HorizontalStackPanel { Spacing = 6 };
        row.Widgets.Add(new MyraLabel(label, MyraLabel.TextStyle.P) { MinWidth = 110 });
        row.Widgets.Add(editor);
        _properties.Widgets.Add(row);
    }

    private Widget BuildEditor(string tag, XmlGumpAttributeDefinition attribute, Func<string, string?> get, Action<string, string?> set)
    {
        switch (attribute.Kind)
        {
            case XmlGumpAttributeKind.Boolean:
            {
                bool value = bool.TryParse(get(attribute.Name), out bool parsed) ? parsed : attribute.Default == "true";

                return MyraCheckButton.CreateWithCallback(value, isChecked =>
                {
                    set(attribute.Name, isChecked.ToString());
                    MarkDirty();
                });
            }
            case XmlGumpAttributeKind.Hue:
            {
                ushort hue = ushort.TryParse(get(attribute.Name), out ushort parsed) ? parsed : (ushort)0;
                var selector = new HueSelector(hue) { Width = EDITOR_WIDTH };
                selector.HueChanged += (_, value) =>
                {
                    set(attribute.Name, value.ToString());
                    MarkDirty();
                };

                return selector;
            }
            case XmlGumpAttributeKind.Graphic:
            {
                ushort graphic = ushort.TryParse(get(attribute.Name), out ushort parsed) ? parsed : (ushort)0;
                var picker = new GumpGraphicPicker(graphic)
                {
                    NumberInput = { Width = SMALL_EDITOR_WIDTH },
                    NameList = { Width = 110 }
                };
                picker.GraphicChanged += (_, value) =>
                {
                    set(attribute.Name, (value ?? 0).ToString());
                    MarkDirty();
                };

                return picker;
            }
            case XmlGumpAttributeKind.Enum:
                return BuildEnumEditor(attribute, get, set);
            case XmlGumpAttributeKind.Float:
            {
                float value = float.TryParse(get(attribute.Name), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
                    ? parsed
                    : 0f;
                var box = new FloatInputBox { Width = EDITOR_WIDTH, Value = value };
                box.ValueChanged += (_, args) =>
                {
                    set(attribute.Name, args.NewValue.ToString(CultureInfo.InvariantCulture));
                    MarkDirty();
                };

                return box;
            }
            case XmlGumpAttributeKind.Rect:
                return BuildRectEditor(attribute, get, set);
            case XmlGumpAttributeKind.UnsignedShort:
            {
                int value = int.TryParse(get(attribute.Name), out int parsed) ? parsed : 0;
                var box = new IntegerInputBox { Width = EDITOR_WIDTH, MinValue = 0, MaxValue = ushort.MaxValue, Value = value };
                box.ValueChanged += (_, args) =>
                {
                    set(attribute.Name, args.NewValue.ToString());
                    MarkDirty();
                };

                return box;
            }
            case XmlGumpAttributeKind.Text:
                return BuildTextEditor(() => get(attribute.Name) ?? string.Empty, value =>
                {
                    set(attribute.Name, value);
                    MarkDirty();
                });
            case XmlGumpAttributeKind.Integer:
            default:
            {
                int value = int.TryParse(get(attribute.Name), out int parsed) ? parsed : 0;
                var box = new IntegerInputBox { Width = EDITOR_WIDTH, Value = value };
                box.ValueChanged += (_, args) =>
                {
                    set(attribute.Name, args.NewValue.ToString());
                    MarkDirty();
                };

                return box;
            }
        }
    }

    private Widget BuildEnumEditor(XmlGumpAttributeDefinition attribute, Func<string, string?> get, Action<string, string?> set)
    {
        string[] options = attribute.Options ?? [];
        string current = get(attribute.Name) ?? attribute.Default;

        var combo = new ComboBox { Width = EDITOR_WIDTH };
        int selected = 0;

        for (int i = 0; i < options.Length; i++)
        {
            combo.Items.Add(new ListItem(TazLang.Get(XmlGumpSchema.OptionKey(options[i]), options[i])));

            if (string.Equals(options[i], current, StringComparison.OrdinalIgnoreCase))
                selected = i;
        }

        combo.SelectedIndex = selected;
        combo.SelectedIndexChanged += (_, _) =>
        {
            if (combo.SelectedIndex is not int index || index < 0 || index >= options.Length)
                return;

            set(attribute.Name, options[index]);
            MarkDirty();
        };

        return combo;
    }

    private Widget BuildRectEditor(XmlGumpAttributeDefinition attribute, Func<string, string?> get, Action<string, string?> set)
    {
        int[] parts = new int[4];
        string? raw = get(attribute.Name);

        if (!string.IsNullOrEmpty(raw))
        {
            string[] split = raw.Split(';');

            for (int i = 0; i < 4 && i < split.Length; i++)
                int.TryParse(split[i], out parts[i]);
        }

        var row = new HorizontalStackPanel { Spacing = 4 };

        for (int i = 0; i < 4; i++)
        {
            int slot = i;
            var box = new IntegerInputBox { Width = 52, Value = parts[i] };
            box.ValueChanged += (_, args) =>
            {
                parts[slot] = args.NewValue;
                set(attribute.Name, string.Join(";", parts));
                MarkDirty();
            };
            row.Widgets.Add(box);
        }

        return row;
    }

    private Widget BuildTextEditor(Func<string> get, Action<string> set)
    {
        var box = new MyraInputBox { Width = TEXT_EDITOR_WIDTH, Text = get() };
        box.TextChangedByUser += (_, _) =>
        {
            set(box.Text ?? string.Empty);
        };

        return box;
    }

    private void MarkDirty()
    {
        _dirty = true;
        UpdateTitle();

        // Deferred so a hue slider dragged across its range rebuilds the canvas once per frame rather
        // than per tick, and so a rebuild never runs inside the Myra input pass that raised the edit.
        Defer(() => _canvas.Rebuild(_document.Elements));
    }

    private void UpdateTitle()
    {
        string name = _document.FilePath != null ? Path.GetFileNameWithoutExtension(_document.FilePath) : TazLang.Get("xmlgumpeditor_untitled", "Untitled");
        string dirty = _dirty ? " *" : string.Empty;
        _rootWindow.Title = $"{TazLang.Get("xmlgumpeditor_title", "XML Gump Editor")} - {name}{dirty}";
    }

    private void SetStatus(string text) => _status.Text = text;

    #endregion
}
