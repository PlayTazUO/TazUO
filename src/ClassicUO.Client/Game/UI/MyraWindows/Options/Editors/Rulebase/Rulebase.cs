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

public class Rulebase<TRule> : Container, INotifyPropertyChanged where TRule : class, IRule, new()
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<RuleCrudEventArgs<TRule>>? RuleCrud;

    private readonly IRuleConfigurator<TRule> _ruleConfigurator;
    private readonly Panel _contentPanel;
    private readonly RulebaseTableView<TRule> _tableView;

    private WrapPanel _toolbar;
    private Widget _addButton;
    private Widget _editButton;
    private Widget _deleteButton;
    private Widget _moveTopButton;
    private Widget _moveUpButton;
    private Widget _moveDownButton;
    private Widget _moveBottomButton;

    private readonly MyraLabel _titleLabel = new(null, MyraLabel.TextStyle.H5)
    {
        HorizontalAlignment = HorizontalAlignment.Center
    };
    private Desktop? _subscribedDesktop;

    public ObservableCollection<TRule> Rules { get; } = [];
    public ObservableCollection<RulebaseColumn<TRule>> Columns { get; } = [];
    public RulebaseStyleOptions TableStyleOptions { get; } = new();

    public Label TitleLabel => _titleLabel;

    public bool IsInEditor
    {
        get => field;
        private set
        {
            if (SetField(ref field, value) && !value)
                SetCurrentContent(_tableView);
        }
    }

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

    public Rulebase(IRuleConfigurator<TRule> ruleConfigurator)
    {
        ArgumentNullException.ThrowIfNull(ruleConfigurator);
        _ruleConfigurator = ruleConfigurator;

        InitializeToolbar();

        _tableView = new RulebaseTableView<TRule>(Columns, TableStyleOptions);
        _contentPanel = CreateContentPanel();

        ConfigureContainer();
        ManageSubscriptions(true);

        Children.Add(CreateComponent());
        ChildrenLayout = new StackPanelLayout(Orientation.Vertical);
    }

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
    }

    public void RefreshTable() => _tableView.Refresh();

    protected override void OnPlacedChanged()
    {
        base.OnPlacedChanged();
        ManageSubscriptions(Desktop != null);
    }

    private void ManageSubscriptions(bool subscribe)
    {
        Rules.CollectionChanged -= OnRuleCollectionChanged;
        Columns.CollectionChanged -= OnColumnsChanged;
        _tableView.SelectedIndexChanged -= OnTableSelectedIndexChanged;
        _ruleConfigurator.EditorClosed -= OnEditorClosed;
        _ruleConfigurator.RuleCrud -= OnConfiguratorRuleCrud;

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
        _ruleConfigurator.EditorClosed += OnEditorClosed;
        _ruleConfigurator.RuleCrud += OnConfiguratorRuleCrud;

        if (Desktop == null)
            return;

        _subscribedDesktop = Desktop;
        _subscribedDesktop.TouchDown += OnDesktopTouchDown;
    }

    private void OnColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e) => _tableView.Refresh();
    private void OnTableSelectedIndexChanged(object? sender, EventArgs e) => SelectedIndex = _tableView.SelectedIndex;
    private void OnEditorClosed(object? sender, EventArgs e) => IsInEditor = false;

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

            if (!hitTable && !hitToolbar)
                SelectedIndex = null;
        }
    }

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

    private Panel CreateContentPanel()
    {
        var panel = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Border = new SolidBrush(MyraStyle.GridBorderColor),
            BorderThickness = new Thickness(1),
        };
        panel.Widgets.Add(_tableView);
        return panel;
    }

    private void OnConfiguratorRuleCrud(object? sender, RuleCrudEventArgs<TRule> args)
    {
        IsInEditor = false;

        if (args.Event == RuleCrudEventType.Create)
            AddRule(args.Rule);
        else
            RefreshTable();

        RuleCrud?.Invoke(this, args);
    }

    private void AddRule(TRule rule)
    {
        rule.Order = GetNextOrder();
        Rules.Add(rule);
        SelectedIndex = Rules.Count - 1;
        RuleCrud?.Invoke(this, new RuleCrudEventArgs<TRule>(rule, RuleCrudEventType.Create));
    }

    private uint GetNextOrder() =>
        Rules.Count == 0 ? 1 : Rules.Max(rule => rule.Order) + 1;

    private void OpenRuleEditor(bool isEdit)
    {
        TRule? rule = isEdit ? GetSelectedRule() : new TRule();
        if (rule == null)
            return;

        IsInEditor = true;
        SetCurrentContent(_ruleConfigurator.GetConfiguratorWidget(rule, isEdit));
    }

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

    private TRule? GetSelectedRule()
    {
        if (!SelectedIndex.HasValue)
            return null;

        int index = SelectedIndex.Value;
        return index >= 0 && index < Rules.Count ? Rules[index] : null;
    }

    private void MoveSelectedBy(int offset)
    {
        if (!SelectedIndex.HasValue)
            return;

        MoveSelectedTo(SelectedIndex.Value + offset);
    }

    private void MoveSelectedToTop() => MoveSelectedTo(0);

    private void MoveSelectedToBottom() => MoveSelectedTo(Rules.Count - 1);

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
        RaiseReorderEvent();
    }

    private void RecalculateOrder()
    {
        for (int i = 0; i < Rules.Count; i++)
            Rules[i].Order = (uint)i;

        RefreshTable();
    }

    private void RaiseReorderEvent()
    {
        TRule? selectedRule = GetSelectedRule();
        if (selectedRule != null)
            RuleCrud?.Invoke(this, new RuleCrudEventArgs<TRule>(selectedRule, RuleCrudEventType.Reorder));
    }

    private void UpdateToolbarState()
    {
        TRule? selectedRule = GetSelectedRule();
        bool hasSelection = selectedRule != null;

        _addButton.Enabled = !IsInEditor;
        _editButton.Enabled = hasSelection && selectedRule!.CanEdit;
        _deleteButton.Enabled = hasSelection && selectedRule!.CanDelete;
        _moveTopButton.Enabled = hasSelection && SelectedIndex > 0;
        _moveUpButton.Enabled = hasSelection && SelectedIndex > 0;
        _moveDownButton.Enabled = hasSelection && SelectedIndex < Rules.Count - 1;
        _moveBottomButton.Enabled = hasSelection && SelectedIndex < Rules.Count - 1;
    }

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

    private void OnRuleCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _tableView.SetRules(Rules);
        UpdateToolbarState();
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
