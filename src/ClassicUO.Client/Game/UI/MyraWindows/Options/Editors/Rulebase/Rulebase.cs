#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ClassicUO.Game.UI.MyraWindows.Options.Tabs;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Microsoft.Scripting.Utils;
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

    private readonly IRuleConfigurator<TRule> _ruleConfigurator;
    private bool _dirty = true;

    private readonly VerticalStackPanel _controlCorpus = new();
    private readonly ListView _listView = new();
    private readonly Panel _contentPanel = new() { Border = new SolidBrush(MyraStyle.GridBorderColor), BorderThickness = new Thickness(1) };

    private int? _selectedIndex;

    private readonly MyraButton _editButton;
    private readonly MyraButton _deleteButton;

    private MyraLabel? _titleLabel;

    public string? Title
    {
        get => field;
        set
        {
            if (SetField(ref field, value))
                _titleLabel = value != null
                    ? new MyraLabel(value, MyraLabel.TextStyle.H5)
                    {
                        Margin = new Thickness(2, 4),
                        HorizontalAlignment = HorizontalAlignment.Center
                    }
                    : null;
        }
    }

    public ObservableCollection<TRule> Rules { get; } = [];

    public bool IsInEditor
    {
        get => field;
        private set => SetField(ref field, value);
    }

    public int? SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            _listView.SelectedIndex = value;
            SetField(ref _selectedIndex, value);
        }
    }

    public event EventHandler<RuleCrudEventArgs<TRule>>? RuleCrud;

    public Rulebase(IRuleConfigurator<TRule> ruleConfigurator)
    {
        ArgumentNullException.ThrowIfNull(ruleConfigurator);
        _ruleConfigurator = ruleConfigurator;

        // Satisfy the nullable constraint by stuffing this in the constructor
        _editButton = new MyraButton("Edit", () => OpenRuleEditor(true));
        _deleteButton = new MyraButton("Delete", DeleteRule);

        Margin = new Thickness(4);
        Padding = new Thickness(4, 6, 4, 12);
        Background = new SolidBrush(new Color(0, 0, 0, 25));
        Border = new SolidBrush(new Color(0, 0, 0, 75));
        BorderThickness = new Thickness(2);

        Rules.CollectionChanged += OnRuleCollectionChanged;
        ChildrenLayout = new WrapPanelLayout();

        _ruleConfigurator.EditorClosed += (_, _) => IsInEditor = false;
        _ruleConfigurator.RuleCrud += (_, args) =>
        {
            if (args.Event == RuleCrudEventType.Create)
                AddRule(args.Rule);

            IsInEditor = false;
            RuleCrud?.Invoke(this, args);
        };

        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        // List view is completely fucked up and uses ref comparison whilst checking index...
        // Since we may re-create the widgets in between renders, this comparison fails...
        _listView.SelectedIndexChanged += OnListSelectionChanged;

        Render();
    }

    private void Render()
    {
        if (!_dirty)
            return;

        _controlCorpus.Widgets.Clear();
        _controlCorpus.Widgets.Add(
            OptionTabCommons.StyledStackPanel(
                Orientation.Vertical,
                _titleLabel,
                _contentPanel
            )
        );

        Children.Clear();
        Children.Add(_controlCorpus);

        if (IsInEditor)
            return;

        RenderInView();
    }

    private void RenderInView() =>
        SetCurrentContent(
            OptionTabCommons.StyledVerticalWrapPanel(
                GetToolbar(),
                _listView
            )
        );

    private WrapPanel GetToolbar()
    {
        _editButton.Enabled = SelectedIndex.HasValue && Rules[SelectedIndex.Value].CanEdit;
        _deleteButton.Enabled = SelectedIndex.HasValue && Rules[SelectedIndex.Value].CanDelete;

        return OptionTabCommons.StyledHorizontalWrapPanel(
            new MyraButton("Add", () => OpenRuleEditor(false)),
            _editButton,
            _deleteButton
        );
    }

    private void OnListSelectionChanged(object? sender, EventArgs e)
    {
        _selectedIndex = _listView.SelectedIndex;
        Render();
    }

    private void AddRule(TRule rule)
    {
        Rules.Add(rule);
        RuleCrud?.Invoke(this, new RuleCrudEventArgs<TRule>(rule, RuleCrudEventType.Create));
    }

    private void OpenRuleEditor(bool isEdit)
    {
        TRule rule;
        if (isEdit)
        {
            if (SelectedIndex.HasValue)
                rule = Rules[SelectedIndex.Value];
            else
                return;
        }
        else
            rule = new TRule();

        IsInEditor = true;
        SetCurrentContent(_ruleConfigurator.GetConfiguratorWidget(rule, isEdit));
    }

    private void DeleteRule()
    {
        if (!SelectedIndex.HasValue)
            return;

        TRule rule = Rules[SelectedIndex.Value];
        if (!rule.CanDelete)
            return;

        Rules.RemoveAt(SelectedIndex.Value);

        SelectedIndex = null;
        RuleCrud?.Invoke(this, new RuleCrudEventArgs<TRule>(rule, RuleCrudEventType.Delete));
    }

    private void SetCurrentContent(Widget content)
    {
        if (_contentPanel.Widgets.Count == 0)
            _contentPanel.Widgets.Add(content);
        else
            _ = UiEffects.FadeReplace(_contentPanel, 0, content);
    }

    private void OnRuleCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Need to memoize this
        if (_listView.Widgets.Count != 0)
            _listView.Widgets.Clear();

        foreach (TRule rule in Rules)
            _listView.Widgets.Add(rule.DisplayComponent);

        Render();
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        Render();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
