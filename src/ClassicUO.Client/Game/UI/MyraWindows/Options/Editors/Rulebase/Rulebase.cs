#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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

    private readonly IRuleConfigurator<TRule> _ruleConfigurator;

    private ListView _ruleList;
    private Panel _contentPanel;

    private int? _selectedIndex;

    private readonly MyraButton _editButton;
    private readonly MyraButton _deleteButton;

    private readonly MyraLabel _titleLabel = new(null, MyraLabel.TextStyle.H5);

    public string? Title
    {
        get => field;
        set
        {
            if (SetField(ref field, value))
            {
                _titleLabel.Text = value;
                _titleLabel.Visible = !string.IsNullOrWhiteSpace(value);
            }
        }
    }

    public ObservableCollection<TRule> Rules { get; } = [];

    public bool IsInEditor
    {
        get => field;
        private set
        {
            if (SetField(ref field, value) && !value)
                SetCurrentContent(_ruleList);
        }
    }

    public int? SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            _ruleList.SelectedIndex = value;
            if (SetField(ref _selectedIndex, value))
            {
                _editButton.Enabled = value.HasValue && Rules[value.Value].CanEdit;
                _deleteButton.Enabled = value.HasValue && Rules[value.Value].CanDelete;
            }
        }
    }

    public event EventHandler<RuleCrudEventArgs<TRule>>? RuleCrud;

    public Rulebase(IRuleConfigurator<TRule> ruleConfigurator)
    {
        ArgumentNullException.ThrowIfNull(ruleConfigurator);
        _ruleConfigurator = ruleConfigurator;

        Point editorSize = ruleConfigurator.GetConfiguratorWidget(new TRule(), false).Measure(new Point(Bounds.X, Bounds.Y));
        MinWidth = editorSize.X + MBPWidth;
        MinHeight = editorSize.Y + MBPHeight;

        // Satisfy the nullable constraint by stuffing this in the constructor
        _editButton = new MyraButton("Edit", () => OpenRuleEditor(true));
        _deleteButton = new MyraButton("Delete", DeleteRule);

        Margin = new Thickness(4);
        Padding = new Thickness(4, 6, 4, 12);
        Background = new SolidBrush(new Color(0, 0, 0, 25));
        Border = new SolidBrush(new Color(0, 0, 0, 75));
        BorderThickness = new Thickness(2);

        Rules.CollectionChanged += OnRuleCollectionChanged;

        _ruleConfigurator.EditorClosed += (_, _) => IsInEditor = false;
        _ruleConfigurator.RuleCrud += (_, args) =>
        {
            IsInEditor = false;
            if (args.Event == RuleCrudEventType.Create)
                AddRule(args.Rule);

            RuleCrud?.Invoke(this, args);
        };

        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        // List view is completely fucked up and uses ref comparison whilst checking index...
        // Since we may re-create the widgets in between renders, this comparison fails...

        Children.Add(CreateComponent());
        ChildrenLayout = new WrapPanelLayout();
    }

    private StackPanel CreateComponent()
    {
        // We build from bottom to top, basically.

        // First, the rulebase list itself goes into a 'container', the content panel
        _ruleList = new ListView();
        _ruleList.SelectedIndexChanged += OnRuleListSelectionChanged;

        _contentPanel = new Panel { Border = new SolidBrush(MyraStyle.GridBorderColor), BorderThickness = new Thickness(1) };
        _contentPanel.Widgets.Add(_ruleList);

        // The entire control
        return OptionTabCommons.StyledStackPanel(
            Orientation.Vertical,
            // The rulebase's title label
            _titleLabel,
            OptionTabCommons.StyledStackPanel(
                Orientation.Vertical,
                // A permanently present toolbar
                GetToolbar(),
                // The rule base list itself
                _contentPanel
            )
        );
    }

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

    private void OnRuleListSelectionChanged(object? sender, EventArgs e) => _selectedIndex = _ruleList.SelectedIndex;

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
        content.VerticalAlignment = VerticalAlignment.Top;
        content.HorizontalAlignment = HorizontalAlignment.Center;
        if (_contentPanel.Widgets.Count == 0)
            _contentPanel.Widgets.Add(content);
        else
            _ = UiEffects.FadeReplace(_contentPanel, 0, content);
    }

    private void OnRuleCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (object? ruleItem in e.NewItems ?? Array.Empty<object?>())
                    if (ruleItem != null)
                        _ruleList.Widgets.Add((ruleItem as IRule)!.DisplayComponent);
                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (object? ruleItem in e.OldItems ?? Array.Empty<object?>())
                    if (ruleItem != null)
                        _ruleList.Widgets.Remove(ruleItem as Widget);
                break;
            case NotifyCollectionChangedAction.Move:
                _ruleList.Widgets.RemoveAt(e.OldStartingIndex);
                _ruleList.Widgets.Insert(e.NewStartingIndex, (e.NewItems?[0] as IRule)!.DisplayComponent);
                break;

            case NotifyCollectionChangedAction.Replace:
                if (e.OldItems != null)
                    _ruleList.Widgets[e.OldStartingIndex] = (e.NewItems?[0] as IRule)!.DisplayComponent;
                break;

            case NotifyCollectionChangedAction.Reset:
                _ruleList.Widgets.Clear();
                break;
        }
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
