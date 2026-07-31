#nullable enable

using System;
using System.Collections.Generic;
using Myra.Events;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Search;

public class ContainsLevenshteinComboBox<T> : ScoredSearchComboBox<T>
{
    /// <summary>
    /// Deliberately shadows the base's interface-typed property with the concrete strategy, so
    /// its knobs (MaxDistance, MinScore, CaseSensitive, ...) are reachable without a cast.
    /// Resolved from the base property on every read rather than cached at construction: the
    /// strategy can be replaced afterwards (the public setter, or CopyFrom cloning it), and a
    /// cached field would go on exposing knobs that no longer drive what the dropdown searches
    /// with. Null once the strategy has been replaced with an unrelated one.
    /// </summary>
    public new ContainsThenLevenshteinSearchStrategy? Strategy => base.Strategy as ContainsThenLevenshteinSearchStrategy;

    public ContainsLevenshteinComboBox(string styleName = Stylesheet.DefaultStyleName) : base(new ContainsThenLevenshteinSearchStrategy(), styleName)
    {
    }
}

public class ContainsLevenshteinComboBox : ContainsLevenshteinComboBox<string>
{
    private readonly Action<string?>? _onSelected;

    public ContainsLevenshteinComboBox(string styleName = Stylesheet.DefaultStyleName) : base(styleName)
    {
    }

    /// <summary>Creates a combo box pre-populated with <paramref name="items"/>, invoking <paramref name="onSelected"/> on selection change.</summary>
    /// <param name="items">Items to populate the dropdown with</param>
    /// <param name="onSelected">Callback invoked with the newly selected item</param>
    /// <param name="styleName">Myra stylesheet style name to apply</param>
    public ContainsLevenshteinComboBox(IEnumerable<string> items, Action<string?> onSelected, string styleName = Stylesheet.DefaultStyleName) : this(styleName)
    {
        ArgumentNullException.ThrowIfNull(onSelected);

        foreach (string item in items)
            Items.Add(item);
        _onSelected = onSelected;
    }

    /// <summary>Creates a combo box pre-populated with <paramref name="items"/> and pre-selects <paramref name="selectedItem"/>.</summary>
    /// <param name="selectedItem">Item to select initially; must be present in <paramref name="items"/></param>
    /// <param name="items">Items to populate the dropdown with</param>
    /// <param name="onSelected">Callback invoked with the newly selected item</param>
    /// <param name="styleName">Myra stylesheet style name to apply</param>
    public ContainsLevenshteinComboBox(
        string selectedItem,
        IEnumerable<string> items,
        Action<string?> onSelected,
        string styleName = Stylesheet.DefaultStyleName
    ) : this(items, onSelected, styleName)
    {
        // Need to improve this to just accept a selected item...
        SelectedIndex = Items.IndexOf(selectedItem);
    }

    /// <summary>Forwards the new selection to <see cref="_onSelected"/>.</summary>
    private void OnSelectedItemChanged(object? sender, ValueChangedEventArgs<string> e) => _onSelected?.Invoke(e.NewValue);

    /// <summary>Subscribes to selection changes while placed on a desktop, unsubscribes otherwise, to avoid leaking the handler.</summary>
    protected override void OnPlacedChanged()
    {
        base.OnPlacedChanged();
        ManageSubscriptions(Desktop != null);
    }

    /// <summary>Adds or removes the <see cref="SelectedItemChanged"/> subscription.</summary>
    /// <param name="subscribe">True to subscribe, false to unsubscribe</param>
    private void ManageSubscriptions(bool subscribe)
    {
        if (subscribe)
        {
            SelectedItemChanged += OnSelectedItemChanged;
        }
        else
        {
            SelectedItemChanged -= OnSelectedItemChanged;
        }

    }
}
