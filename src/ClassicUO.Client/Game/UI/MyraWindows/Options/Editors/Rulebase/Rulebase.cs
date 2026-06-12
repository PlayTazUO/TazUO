#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;
using Container = Myra.Graphics2D.UI.Container;

namespace ClassicUO.Game.UI.MyraWindows.Options.Editors.Rulebase;

public class Rulebase<TRule> : Container, INotifyPropertyChanged where TRule : IRule, new()
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<RuleCrudEventArgs<TRule>>? RuleCrud;

    private readonly IRuleConfigurator<TRule> _ruleConfigurator;
    private readonly Panel _contentPanel;
    private readonly RulebaseTableView<TRule> _tableView;

    private readonly WrapPanel _toolbar;
    private readonly MyraButton _editButton;
    private readonly MyraButton _deleteButton;
    private readonly MyraButton _moveTopButton;
    private readonly MyraButton _moveUpButton;
    private readonly MyraButton _moveDownButton;
    private readonly MyraButton _moveBottomButton;

    private readonly MyraLabel _titleLabel = new(null, MyraLabel.TextStyle.H5);
    private int? _selectedIndex;

    public ObservableCollection<TRule> Rules { get; } = [];
    public ObservableCollection<RulebaseColumn<TRule>> Columns { get; } = [];
    public RulebaseStyleOptions StyleOptions { get; } = new();

    public string? Title
    {
        get => field;
        set
        {
            if (SetField(ref field, value))
                UpdateTitle();
        }
    }

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
        get => _selectedIndex;
        set
        {
            if (SetField(ref _selectedIndex, value))
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

        _editButton = new MyraButton("Edit", () => OpenRuleEditor(true));
        _deleteButton = new MyraButton("Delete", DeleteRule);
        _moveTopButton = new MyraButton("Top", MoveSelectedToTop);
        _moveUpButton = new MyraButton("Up", () => MoveSelectedBy(-1));
        _moveDownButton = new MyraButton("Down", () => MoveSelectedBy(1));
        _moveBottomButton = new MyraButton("Bottom", MoveSelectedToBottom);

        _toolbar = OptionTabCommons.StyledHorizontalWrapPanel(
                new MyraButton("Add", () => OpenRuleEditor(false)),
                _editButton,
                _deleteButton,
                _moveTopButton,
                _moveUpButton,
                _moveDownButton,
                _moveBottomButton
            );

        _tableView = new RulebaseTableView<TRule>(Columns, StyleOptions);
        PlacedChanged +=
            (_, _) =>
            {
                Desktop?.TouchDown +=
                    (_, _) =>
                    {
                        if (Desktop?.TouchPosition == null || !_tableView.SelectedIndex.HasValue || IsInEditor)
                            return;

                        // Listen in to 'general' touch events; If one occurs outside the table
                        // and the table currently has a selection, deselect it.
                        if (_tableView.HitTest(Desktop.TouchPosition.Value) == null
                            && _toolbar.HitTest(Desktop.TouchPosition.Value) == null
                           )
                            _tableView.SetSelectedIndex(null);
                    };
            };

        _tableView.TouchLeft +=
            (_, _) =>
            {
                int a = 0;
            };

        _contentPanel = CreateContentPanel();

        ConfigureContainer();
        ConfigureEvents();

        Children.Add(CreateComponent());
        ChildrenLayout = new WrapPanelLayout();
    }

    public void RefreshTable() => _tableView.Refresh();

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

    private void ConfigureEvents()
    {
        Rules.CollectionChanged += OnRuleCollectionChanged;
        Columns.CollectionChanged += (_, _) => _tableView.Refresh();
        _tableView.SelectedIndexChanged += (_, _) => SelectedIndex = _tableView.SelectedIndex;

        _ruleConfigurator.EditorClosed += (_, _) => IsInEditor = false;
        _ruleConfigurator.RuleCrud += OnConfiguratorRuleCrud;
    }

    private StackPanel CreateComponent() =>
        OptionTabCommons.StyledStackPanel(
            Orientation.Vertical,
            _titleLabel,
            OptionTabCommons.StyledStackPanel(
                Orientation.Vertical,
                _toolbar,
                _contentPanel
            )
        );

    private Panel CreateContentPanel()
    {
        var panel = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Border = StyleOptions.OuterBorder.Brush,
            BorderThickness = StyleOptions.OuterBorder.Thickness
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
        if (rule == null || !rule.CanDelete)
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
            return default;

        int index = SelectedIndex.Value;
        return index >= 0 && index < Rules.Count ? Rules[index] : default;
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
            Rules[i].Order = (uint)(i + 1);

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

        _editButton.Enabled = hasSelection && selectedRule!.CanEdit;
        _deleteButton.Enabled = hasSelection && selectedRule!.CanDelete;
        _moveTopButton.Enabled = hasSelection && SelectedIndex > 0;
        _moveUpButton.Enabled = hasSelection && SelectedIndex > 0;
        _moveDownButton.Enabled = hasSelection && SelectedIndex < Rules.Count - 1;
        _moveBottomButton.Enabled = hasSelection && SelectedIndex < Rules.Count - 1;
    }

    private void UpdateTitle()
    {
        _titleLabel.Text = Title;
        _titleLabel.Visible = !string.IsNullOrWhiteSpace(Title);
    }

    private void SetCurrentContent(Widget content)
    {
        content.VerticalAlignment = VerticalAlignment.Top;
        content.HorizontalAlignment = HorizontalAlignment.Stretch;

        if (_contentPanel.Widgets.Count == 0)
            _contentPanel.Widgets.Add(content);
        else
            _ = UiEffects.FadeReplace(_contentPanel, 0, content);
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
