#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using ClassicUO.Assets;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;
using Container = Myra.Graphics2D.UI.Container;

namespace ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;

/// <summary>
/// A self-contained CRUD widget for managing an ordered list of rules: renders the rules as a
/// <see cref="RulebaseTableView{TRule}"/>, provides a toolbar (add/edit/delete/reorder), and
/// optionally swaps in an <see cref="IRuleConfigurator{TRule}"/> widget in-place for create/edit flows.
/// </summary>
/// <typeparam name="TRule">The rule type managed by this rulebase. Must be a reference type with a parameterless constructor.</typeparam>
public class Rulebase<TRule> : Container, INotifyPropertyChanged where TRule : class, IRule, new()
{
    #region Events

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Raised when a rule is created, updated, or deleted (either via a configurator or, with no configurator, the default add/delete flow)</summary>
    public event EventHandler<RuleCrudEventArgs<TRule>>? RuleCrud;

    /// <summary>Raised when a rule's position changes via the move up/down/top/bottom toolbar buttons</summary>
    public event EventHandler<RulebaseOrderChangedEventArgs<TRule>>? Reordered;

    #endregion

    #region Accessors

    /// <summary>The rules currently managed by this rulebase, in display order</summary>
    public ObservableCollection<TRule> Rules { get; } = [];

    /// <summary>The columns rendered by the underlying table view</summary>
    public ObservableCollection<RulebaseColumn<TRule>> Columns { get; } = [];

    /// <summary>Visual styling applied to the underlying table view</summary>
    public RulebaseStyleOptions TableStyleOptions { get; } = new();

    /// <summary>The label displayed above the toolbar; set its <c>Text</c> to title the rulebase</summary>
    public Label TitleLabel => _titleLabel;

    /// <summary>Whether the rulebase is currently showing a configurator widget instead of the table</summary>
    public bool IsInEditor
    {
        get => field;
        private set
        {
            if (SetField(ref field, value) && !value)
                SetCurrentContent(_tableView);
        }
    }

    /// <summary>Index of the currently selected rule, or null if none is selected</summary>
    public int? SelectedIndex
    {
        get => field;
        set
        {
            if (SetField(ref field, value))
            {
                _tableView.SetSelectedIndex(value);
                UpdateToolbarState();
            }
        }
    }

    #endregion

    #region Members

    private readonly IRuleConfigurator<TRule>? _ruleConfigurator;
    private readonly Panel _contentPanel;
    private readonly RulebaseTableView<TRule> _tableView;

    // These are initialized in the InitializeToolbar method
    private WrapPanel _toolbar = null!;
    private BasicButton _addButton = null!;
    private Widget _editButton = null!;
    private Widget _deleteButton = null!;
    private Widget _moveTopButton = null!;
    private Widget _moveUpButton = null!;
    private Widget _moveDownButton = null!;
    private Widget _moveBottomButton = null!;

    private readonly MyraLabel _titleLabel = new(null, MyraLabel.TextStyle.H5) { HorizontalAlignment = HorizontalAlignment.Center };
    private Desktop? _subscribedDesktop;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="Rulebase{TRule}"/> class.
    /// </summary>
    /// <param name="ruleConfigurator">
    /// Optional widget provider for create/edit flows. When null, "Add" creates a default
    /// <typeparamref name="TRule"/> instance directly and the "Edit" button is hidden, leaving
    /// inline editing to the column cell factories.
    /// </param>
    public Rulebase(IRuleConfigurator<TRule>? ruleConfigurator = null)
    {
        _ruleConfigurator = ruleConfigurator;

        InitializeToolbar();

        _tableView = new RulebaseTableView<TRule>(Columns, TableStyleOptions);
        _contentPanel = CreateContentPanel();

        ConfigureContainer();
        ManageSubscriptions(true);

        Children.Add(CreateComponent());
        ChildrenLayout = new StackPanelLayout(Orientation.Vertical);
    }

    #region Public Methods

    /// <summary>Forces the underlying table view to re-render</summary>
    /// <param name="force">When true, skips change-detection and rebuilds the grid structure unconditionally</param>
    public void RefreshTable(bool force = false) => _tableView.Refresh(force);

    #endregion

    #region Protected Methods

    #region Overrides

    /// <summary>Resubscribes desktop touch handling whenever the control is attached to or detached from a desktop</summary>
    protected override void OnPlacedChanged()
    {
        base.OnPlacedChanged();
        ManageSubscriptions(Desktop != null);
    }

    #endregion

    /// <summary>Raises <see cref="PropertyChanged"/> for the given property</summary>
    /// <param name="propertyName">The property that changed; defaults to the calling member's name</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>Updates a backing field and raises <see cref="PropertyChanged"/> if the value changed</summary>
    /// <param name="field">Reference to the backing field</param>
    /// <param name="value">The new value</param>
    /// <param name="propertyName">The property being set; defaults to the calling member's name</param>
    /// <returns>True if the value changed; false if it was equal to the current value</returns>
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion

    #region Private Methos

    /// <summary>Creates the toolbar buttons (add/edit/delete/reorder) and sets their initial enabled state</summary>
    private void InitializeToolbar()
    {
        SpriteFontBase smallNoto = TrueTypeLoader.Instance.GetFont(EmbeddedFontNames.NOTO_SANS_2_SYMBOLS, 28);
        SpriteFontBase largeNoto = TrueTypeLoader.Instance.GetFont(EmbeddedFontNames.NOTO_SANS_2_SYMBOLS, 42);

        _addButton = OptionTabCommons.StyledTextIconButton("+", largeNoto, () => OpenRuleEditor(false), topOffset: -4);
        _editButton = OptionTabCommons.StyledTextIconButton("🖉", smallNoto, () => OpenRuleEditor(true), topOffset: 1);
        _deleteButton = OptionTabCommons.StyledTextIconButton("🗙", smallNoto, DeleteRule, topOffset: -1);
        _moveTopButton = OptionTabCommons.StyledTextIconButton("⭱", largeNoto, MoveSelectedToTop, topOffset: -4);
        _moveUpButton = OptionTabCommons.StyledTextIconButton("⭡", largeNoto, () => MoveSelectedBy(-1), topOffset: -4);
        _moveDownButton = OptionTabCommons.StyledTextIconButton("⭣", largeNoto, () => MoveSelectedBy(1), topOffset: -4);
        _moveBottomButton = OptionTabCommons.StyledTextIconButton("⭳", largeNoto, MoveSelectedToBottom, topOffset: -4);

        _toolbar = OptionTabCommons.StyledHorizontalWrapPanel(
            _addButton,
            _editButton,
            _deleteButton,
            _moveTopButton,
            _moveUpButton,
            _moveDownButton,
            _moveBottomButton
        );

        _toolbar.VerticalAlignment = VerticalAlignment.Top;
        _toolbar.HorizontalAlignment = HorizontalAlignment.Center;
        _toolbar.HorizontalSpacing += 2;
        _toolbar.VerticalSpacing += 2;

        // A bit hacky - we create the toolbar first and give it a one-pass update
        UpdateToolbarState();
    }

    /// <summary>Subscribes or unsubscribes from collection, table, configurator, and desktop touch events</summary>
    /// <param name="subscribe">True to subscribe; false to unsubscribe everything</param>
    private void ManageSubscriptions(bool subscribe)
    {
        Rules.CollectionChanged -= OnRuleCollectionChanged;
        Columns.CollectionChanged -= OnColumnsChanged;
        _tableView.SelectedIndexChanged -= OnTableSelectedIndexChanged;
        _ruleConfigurator?.EditorClosed -= OnEditorClosed;
        _ruleConfigurator?.Crud -= OnConfiguratorCrud;

        if (_subscribedDesktop != null)
        {
            _subscribedDesktop.TouchDown -= OnDesktopTouchDown;
            _subscribedDesktop = null;
        }

        if (!subscribe)
            return;

        Rules.CollectionChanged += OnRuleCollectionChanged;
        Columns.CollectionChanged += OnColumnsChanged;
        _tableView.SelectedIndexChanged += OnTableSelectedIndexChanged;
        _ruleConfigurator?.EditorClosed += OnEditorClosed;
        _ruleConfigurator?.Crud += OnConfiguratorCrud;

        if (Desktop == null)
            return;

        _subscribedDesktop = Desktop;
        _subscribedDesktop.TouchDown += OnDesktopTouchDown;
    }

    /// <summary>Checks whether a touch position landed on an open context menu belonging to a combo box nested in the table</summary>
    /// <param name="touchPos">The touch position to test, in screen coordinates</param>
    private bool IsTouchDownInChildContextMenu(Point touchPos)
    {
        if (Desktop?.ContextMenu == null)
            return false;

        // Combo boxes are... special, in that their items (held by a ListView) are completely external to the normal component chain.
        // Basically, in terms of the component tree, the ListView lives as an orphan - it's not a child of anything, and nothing has it as one of its children.
        // The only 'anchor' we have is the ComboView itself, whose 'Widgets' property actually returns the ListView's items;
        // Those items do have the ListView as the parent, and that's currently the only way we can determine if a click was done on a context menu that's somewhere in our component tree.
        Widget? comboContextChild = null;

        // First, check if we've a context menu open.
        // If so, the table view holds the 'consumer' UI and is our 'visual root'.
        _ = _tableView.FindChild(w =>
        {
            // A quick ref check in case other components handle context menus more gracefully
            if (ReferenceEquals(w, Desktop.ContextMenu))
                return true;

            if (w is not ComboView combo)
                return false;

            // Now we handle the 'ComboView' case - since the Widgets are the internal ListView's, we can use them to get to the ListView itself and compare it to the context menu.
            // If they're equal, it means the context menu is logically a child of out _tableView.
            comboContextChild = combo.Widgets?.FirstOrDefault(comboChild =>
                Desktop.ContextMenu.FindChild(ctxMenuChild => ReferenceEquals(comboChild, ctxMenuChild)) != null
            );

            return comboContextChild != null;
        }) != null;


        return comboContextChild != null && Desktop!.ContextMenu.HitTest(touchPos) != null;
    }

    /// <summary>Applies the container's own margin, padding, background, and border styling</summary>
    private void ConfigureContainer()
    {
        Margin = new Thickness(4);
        Padding = new Thickness(4, 6, 4, 12);
        Background = new SolidBrush(new Color(0, 0, 0, 25));
        Border = new SolidBrush(new Color(0, 0, 0, 75));
        BorderThickness = new Thickness(2);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    /// <summary>Assembles the title label, toolbar, and content panel into the widget's root layout</summary>
    private StackPanel CreateComponent()
    {
        StackPanel primaryControl = OptionTabCommons.StyledStackPanel(
            Orientation.Vertical,
            _titleLabel,
            OptionTabCommons.StyledStackPanel(
                Orientation.Vertical,
                _toolbar,
                _contentPanel
            )
        );
        primaryControl.HorizontalAlignment = HorizontalAlignment.Stretch;
        primaryControl.VerticalAlignment = VerticalAlignment.Top;
        return primaryControl;
    }

    /// <summary>Creates the panel that hosts the table view and, when editing, the configurator widget</summary>
    private Panel CreateContentPanel()
    {
        var panel = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Border = new SolidBrush(MyraStyle.GridBorderColor),
            BorderThickness = new Thickness(1)
        };
        panel.Widgets.Add(_tableView);
        return panel;
    }

    /// <summary>Appends a rule, assigns it the next order value, selects it, and raises <see cref="RuleCrud"/></summary>
    /// <param name="rule">The rule to add</param>
    private void AddRule(TRule rule)
    {
        rule.Order = GetNextOrder();
        Rules.Add(rule);
        SelectedIndex = Rules.Count - 1;
        RuleCrud?.Invoke(this, new RuleCrudEventArgs<TRule>(rule, RuleCrudEventType.Create));
    }

    /// <summary>Computes the order value for a newly added rule (one past the current max)</summary>
    private uint GetNextOrder() =>
        Rules.Count == 0 ? 0 : Rules.Max(rule => rule.Order) + 1;

    /// <summary>Swaps in the configurator widget for creating or editing a rule</summary>
    /// <param name="isEdit">True to edit the currently selected rule; false to create a new instance</param>
    private void OpenRuleEditor(bool isEdit)
    {
        if (_ruleConfigurator == null)
            return;

        TRule? rule = isEdit ? GetSelectedRule() : new TRule();
        if (rule == null)
            return;

        IsInEditor = true;
        SetCurrentContent(_ruleConfigurator.GetConfiguratorWidget(rule, isEdit));
    }

    /// <summary>Removes the currently selected rule, if deletable, and raises <see cref="RuleCrud"/></summary>
    private void DeleteRule()
    {
        TRule? rule = GetSelectedRule();
        if (rule is not { CanDelete: true })
            return;

        Rules.RemoveAt(SelectedIndex!.Value);
        TRule.DeleteRule(rule);

        RecalculateOrder();
        SelectedIndex = null;
        RuleCrud?.Invoke(this, new RuleCrudEventArgs<TRule>(rule, RuleCrudEventType.Delete));
    }

    /// <summary>Resolves the rule at <see cref="SelectedIndex"/>, or null if there is no valid selection</summary>
    private TRule? GetSelectedRule()
    {
        if (!SelectedIndex.HasValue)
            return null;

        int index = SelectedIndex.Value;
        return index >= 0 && index < Rules.Count ? Rules[index] : null;
    }

    /// <summary>Moves the selected rule by a relative offset (negative moves it up, positive moves it down)</summary>
    /// <param name="offset">The number of positions to move the selected rule by</param>
    private void MoveSelectedBy(int offset)
    {
        if (!SelectedIndex.HasValue)
            return;

        MoveSelectedTo(SelectedIndex.Value + offset);
    }

    /// <summary>Moves the selected rule to the first position</summary>
    private void MoveSelectedToTop() => MoveSelectedTo(0);

    /// <summary>Moves the selected rule to the last position</summary>
    private void MoveSelectedToBottom() => MoveSelectedTo(Rules.Count - 1);

    /// <summary>Moves the selected rule to an absolute index, recalculates order, and raises <see cref="Reordered"/></summary>
    /// <param name="newIndex">The destination index</param>
    private void MoveSelectedTo(int newIndex)
    {
        if (!SelectedIndex.HasValue || newIndex < 0 || newIndex >= Rules.Count)
            return;

        int oldIndex = SelectedIndex.Value;
        if (oldIndex == newIndex)
            return;

        Rules.Move(oldIndex, newIndex);
        SelectedIndex = newIndex;
        RecalculateOrder();

        Reordered?.Invoke(this, new RulebaseOrderChangedEventArgs<TRule>(Rules[newIndex], oldIndex, newIndex));
    }

    /// <summary>Re-stamps each rule's <see cref="IRule.Order"/> to match its current index and refreshes the table</summary>
    private void RecalculateOrder()
    {
        for (int i = 0; i < Rules.Count; i++)
        {
            if (i == Rules[i].Order)
                continue;

            Rules[i].Order = (uint)i;
        }

        RefreshTable(true);
    }

    /// <summary>Re-evaluates enabled/visible state of every toolbar button based on selection and editor state</summary>
    private void UpdateToolbarState()
    {
        TRule? selectedRule = GetSelectedRule();
        bool hasSelection = selectedRule != null;

        _addButton.Enabled = !IsInEditor;

        // If we've no configurator, the 'Add' button simply adds a 'default' rule instance.
        _addButton.OnClick = _ruleConfigurator != null
            ? () => OpenRuleEditor(false)
            : () => AddRule(new TRule());

        _editButton.Enabled = hasSelection && selectedRule!.CanEdit;
        // If we've no configurator, we don't show the Edit button.
        // The idea is consumers can provide inline editing in their cell factories to reduce the number of clicks necesary to perform configurations.
        _editButton.Visible = _ruleConfigurator != null;

        _deleteButton.Enabled = hasSelection && selectedRule!.CanDelete;
        _moveTopButton.Enabled = hasSelection && SelectedIndex > 0;
        _moveUpButton.Enabled = hasSelection && SelectedIndex > 0;
        _moveDownButton.Enabled = hasSelection && SelectedIndex < Rules.Count - 1;
        _moveBottomButton.Enabled = hasSelection && SelectedIndex < Rules.Count - 1;
    }

    /// <summary>Swaps the content panel's widget, fading the transition if content is already present</summary>
    /// <param name="content">The widget to display (the table view or a configurator widget)</param>
    private void SetCurrentContent(Widget content)
    {
        content.VerticalAlignment = VerticalAlignment.Top;
        content.HorizontalAlignment = HorizontalAlignment.Stretch;

        if (_contentPanel.Widgets.Count == 0)
            _contentPanel.Widgets.Add(content);
        else
            _ = UiEffects.FadeReplace(_contentPanel, 0, content);
        UpdateToolbarState();
    }

    #region Event Handlers

    /// <summary>Pushes the updated <see cref="Rules"/> collection into the table view and refreshes toolbar state</summary>
    private void OnRuleCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _tableView.SetRules(Rules);
        UpdateToolbarState();
    }

    /// <summary>Handles a create/update/delete result from the configurator, closing the editor and forwarding <see cref="RuleCrud"/></summary>
    private void OnConfiguratorCrud(object? sender, RuleCrudEventArgs<TRule> args)
    {
        IsInEditor = false;

        if (args.Event == RuleCrudEventType.Create)
            AddRule(args.Rule);
        else
            RefreshTable();

        RuleCrud?.Invoke(this, args);
    }

    /// <summary>Refreshes the table view when the column set changes</summary>
    private void OnColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e) => _tableView.Refresh();

    /// <summary>Mirrors the table view's selection into <see cref="SelectedIndex"/></summary>
    private void OnTableSelectedIndexChanged(object? sender, EventArgs e) => SelectedIndex = _tableView.SelectedIndex;

    /// <summary>Returns to the table view when the configurator's editor UI is dismissed</summary>
    private void OnEditorClosed(object? sender, EventArgs e) => IsInEditor = false;

    /// <summary>Updates selection based on where the desktop was touched, ignoring touches on the toolbar or an open context menu</summary>
    private void OnDesktopTouchDown(object? sender, EventArgs e)
    {
        if (Desktop?.TouchPosition == null || IsInEditor)
            return;

        Point touchPos = Desktop.TouchPosition.Value;
        int? hitRow = _tableView.GetRowIndexAt(touchPos);

        if (hitRow.HasValue)
            SelectedIndex = hitRow;
        else
        {
            bool hitTable = _tableView.HitTest(touchPos) != null;
            bool hitToolbar = _toolbar.HitTest(touchPos) != null;

            if (!hitTable && !hitToolbar && !IsTouchDownInChildContextMenu(touchPos))
                SelectedIndex = null;
        }
    }

    #endregion

    #endregion
}
