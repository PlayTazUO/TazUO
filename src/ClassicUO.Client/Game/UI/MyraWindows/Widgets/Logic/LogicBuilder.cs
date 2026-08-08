#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Logic;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Game.UI.MyraWindows.Theme;
using ClassicUO.Game.UI.MyraWindows.Widgets.Search;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Logic;

/// <summary>
/// Edits a <see cref="LogicGroup" /> tree: rows of <c>field / operator / value</c>, gathered into
/// brackets, nested as deeply as the expression needs.
/// <code>
/// ┌────────────────────────────────────┐
/// │  [Name] [contains] [bone]      [x] │
/// │  ( OR ▾ )                          │
/// │  [Name] [contains] [banana]    [x] │
/// │  [+ condition]  [+ bracket]        │
/// └────────────────────────────────────┘
/// </code>
/// <para>
/// Each join is its own choice, and the bracket is read strictly top to bottom - so
/// <c>a AND b OR c</c> is <c>(a AND b) OR c</c>. Nothing binds tighter than anything else, because a
/// precedence rule the editor cannot draw is one the user has to already know; nesting a bracket is
/// how a different grouping is expressed, and a bracket is visible.
/// </para>
/// <para>
/// The widget knows nothing about what the tree will be asked about. Everything it offers comes from
/// the <see cref="ILogicSchema" /> handed to it, so the same builder serves any consumer that can
/// describe its subject as a list of named fields - see <see cref="LogicSchema{TSubject}" /> for the
/// evaluating half.
/// </para>
/// </summary>
public sealed class LogicBuilder : Widget
{
    #region Public events

    /// <summary>Raised after any edit to the tree. The tree itself is mutated in place, so handlers
    /// read the object they were given rather than anything carried on the event.</summary>
    public event EventHandler? Changed;

    #endregion

    #region Public accessors

    /// <summary>The tree being edited. Mutated in place.</summary>
    public LogicGroup Root { get; }

    /// <summary>Whether the tree is shown for reading only.</summary>
    public bool ReadOnly
    {
        get => field;
        set
        {
            if (field == value)
                return;

            field = value;
            _context.ReadOnly = value;
            Rebuild();
        }
    }

    #endregion

    #region Private members

    /// <summary>
    /// How deep brackets may nest. Not a limit of the model, which nests without bound - each level
    /// indents, and past this the rows have nowhere left to go on a panel this wide. Anything
    /// needing more is better expressed as two rules.
    /// </summary>
    private const int MAX_DEPTH = 4;

    private const int GROUP_INDENT = 18;
    private const int GROUP_PADDING = 8;
    private const int GROUP_BORDER_THICKNESS = 2;
    private const int CONNECTIVE_WIDTH = 96;

    private static readonly LogicConnective[] _connectives = Enum.GetValues<LogicConnective>();

    private readonly LogicEditorContext _context;

    #endregion

    #region Ctor

    /// <summary>
    /// Builds an editor over one tree.
    /// </summary>
    /// <param name="root">The tree to edit, mutated in place.</param>
    /// <param name="schema">The fields conditions may be written about.</param>
    /// <exception cref="ArgumentNullException">Either argument is null.</exception>
    public LogicBuilder(LogicGroup root, ILogicSchema schema)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(schema);

        Root = root;

        _context = new LogicEditorContext
        {
            Schema = schema,
            Changed = () => Changed?.Invoke(this, EventArgs.Empty),
            Rebuild = () =>
            {
                Changed?.Invoke(this, EventArgs.Empty);
                Rebuild();
            },
            ReadOnly = false
        };

        HorizontalAlignment = HorizontalAlignment.Stretch;
        ChildrenLayout = new StackPanelLayout(Orientation.Vertical) { Spacing = MyraStyle.STANDARD_SPACING };

        Rebuild();
    }

    #endregion

    #region Private methods

    private void Rebuild()
    {
        Children.Clear();
        Children.Add(BuildGroup(Root, depth: 0, remove: null));
    }

    /// <summary>
    /// One bracket: its lines, the connective on each join between them, and the ways of adding to
    /// it.
    /// </summary>
    /// <param name="group">The group to render.</param>
    /// <param name="depth">How deeply nested it is, from zero at the root.</param>
    /// <param name="remove">Detaches the group from its parent, or null for the root, which has
    /// none and must always exist for the tree to have somewhere to put a first condition.</param>
    /// <returns>The bracket.</returns>
    private Widget BuildGroup(LogicGroup group, int depth, Action? remove)
    {
        var panel = new VerticalStackPanel
        {
            Spacing = MyraStyle.STANDARD_SPACING,
            Padding = new Thickness(GROUP_PADDING),

            // Indented on the left only, and given room below, so a nested bracket sits clear of the
            // line above rather than butting against it.
            Margin = new Thickness(depth == 0 ? 0 : GROUP_INDENT, GROUP_PADDING / 2, 0, GROUP_PADDING / 2),
            // Read at build time rather than cached, so a palette swap followed by a rebuild is all
            // a theme change takes.
            Background = new SolidBrush(MyraPalette.AtDepth(MyraTheme.Current.NestingFills, depth)),
            Border = new SolidBrush(MyraPalette.AtDepth(MyraTheme.Current.NestingBorders, depth)),
            BorderThickness = new Thickness(GROUP_BORDER_THICKNESS),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        if (group.IsEmpty)
            panel.Widgets.Add(EmptyNotice(depth));

        for (int i = 0; i < group.Children.Count; i++)
        {
            // On the join, not in a header: this is where the reader is looking when they ask what
            // binds these two lines. The first line has nothing above it to join to.
            if (i > 0)
                panel.Widgets.Add(ConnectiveJoin(group.Children[i]));

            panel.Widgets.Add(BuildChild(group, group.Children[i], depth));
        }

        panel.Widgets.Add(BuildGroupToolbar(group, depth, remove));

        return panel;
    }

    private Widget BuildChild(LogicGroup parent, LogicNode child, int depth) =>
        child switch
        {
            LogicGroup nested => BuildGroup(nested, depth + 1, () => Remove(parent, nested)),
            LogicCondition condition => LogicConditionRow.Build(condition, _context, () => Remove(parent, condition)),
            _ => new Panel()
        };

    private Widget BuildGroupToolbar(LogicGroup group, int depth, Action? remove)
    {
        var widgets = new List<Widget>
        {
            new MyraButton(TazLang.Get("logic_addcondition", "Add condition"), () => Add(group, new LogicCondition()))
            {
                Enabled = !ReadOnly
            }
        };

        // Past the cap a bracket may still be edited, it simply cannot be nested into further.
        if (depth < MAX_DEPTH)
            widgets.Add(
                new MyraButton(TazLang.Get("logic_addgroup", "Add bracket"), () => Add(group, new LogicGroup()))
                {
                    Enabled = !ReadOnly
                }
            );

        if (remove != null)
            widgets.Add(
                MyraStyle.ApplyButtonDangerStyle(
                    new MyraButton(TazLang.Get("logic_removegroup", "Remove bracket"), remove) { Enabled = !ReadOnly }
                )
            );

        WrapPanel toolbar = OptionTabCommons.StyledHorizontalWrapPanel([.. widgets]);
        toolbar.Aligned = true;

        foreach (Widget widget in toolbar.Widgets)
            widget.VerticalAlignment = VerticalAlignment.Center;

        return toolbar;
    }

    /// <summary>
    /// The connective binding one line to the one above it, shown on the join and editable there.
    /// It belongs to the line below the join, so every join in a bracket is its own choice.
    /// </summary>
    /// <param name="child">The node this join leads into.</param>
    /// <returns>The join.</returns>
    private Widget ConnectiveJoin(LogicNode child)
    {
        var combo = new ContainsLevenshteinComboBox(
            LogicText.Name(child.Join),
            _connectives.Select(LogicText.Name),
            chosen =>
            {
                LogicConnective? picked = chosen == null ? null : LogicText.Parse(_connectives, chosen, LogicText.Name);

                if (picked == null || picked == child.Join)
                    return;

                child.Join = picked.Value;

                // Only this join changed and the combo already shows it, so the tree is reported as
                // edited without being torn down and rebuilt under the pointer.
                _context.Changed();
            },
            addSelectedItemIfMissing: false
        )
        {
            Width = CONNECTIVE_WIDTH,
            Enabled = !ReadOnly,
            Margin = new Thickness(GROUP_INDENT, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            TooltipSelector = JoinTooltip
        };

        MyraStyle.ApplySearchComboBoxPopupBorder(combo);

        return combo;
    }

    /// <summary>
    /// What one join does, plus the reading order it is read under - which is the part that is not
    /// guessable from the connective alone.
    /// </summary>
    /// <param name="connectiveName">The name shown on the join.</param>
    /// <returns>The tooltip.</returns>
    private static string JoinTooltip(string connectiveName)
    {
        string order = TazLang.Get(
            "logic_connective_order",
            "Lines are combined top to bottom, in order. Add a bracket to group them differently."
        );

        return LogicText.Parse(_connectives, connectiveName, LogicText.Name) is { } connective
            ? $"{LogicText.Tooltip(connective)}\n{order}"
            : order;
    }

    private static Widget EmptyNotice(int depth) =>
        new MyraLabel(
            depth == 0
                ? TazLang.Get("logic_empty_root", "No conditions - Matches everything")
                : TazLang.Get("logic_empty_group", "Empty bracket - Matches everything"),
            MyraLabel.TextStyle.P
        )
        {
            Margin = new Thickness(GROUP_INDENT, 0, 0, 0)
        };

    private void Add(LogicGroup group, LogicNode child)
    {
        if (ReadOnly)
            return;

        group.Children.Add(child);
        _context.Rebuild();
    }

    private void Remove(LogicGroup parent, LogicNode child)
    {
        if (ReadOnly || !parent.Children.Remove(child))
            return;

        _context.Rebuild();
    }

    #endregion
}
