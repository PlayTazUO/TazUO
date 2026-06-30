#nullable enable

using System;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows.Options;

/// <summary>
/// A lazy-loading <see cref="ContentControl"/> that defers widget creation until first render
/// and supports text- and tag-based search filtering
/// </summary>
internal class OptionItem : ContentControl
{
    private readonly SingleItemLayout<Widget> _layout;
    private string? _tags;
    private readonly Func<Widget> _createWidget;
    private readonly string _searchText;
    private readonly bool _skipSearch;

    /// <inheritdoc/>
    public override Widget Content
    {
        get => _layout.Child ?? CreateOrUpdateChild();
        set => _layout.Child = value;
    }

    /// <param name="searchText">Primary text matched against user search input</param>
    /// <param name="createWidget">Factory invoked once to create the underlying control</param>
    /// <param name="tags">Optional comma-separated secondary terms also matched against search input</param>
    /// <param name="skipSearch">
    /// When <see langword="true"/>, <see cref="MatchesSearch"/> always returns <see langword="false"/>,
    /// preventing the item from appearing in search results
    /// </param>
    public OptionItem(
        string searchText,
        Func<Widget> createWidget,
        string? tags = null,
        bool skipSearch = false
    )
    {
        _searchText = searchText;
        _createWidget = createWidget;
        _skipSearch = skipSearch;
        _tags = tags;
        _layout = new SingleItemLayout<Widget>(this);
        ChildrenLayout = _layout;
        VerticalAlignment = VerticalAlignment.Center;
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="text"/> appears in this item's search text or tags
    /// (case-insensitive). Always returns <see langword="false"/> when <c>skipSearch</c> was set to <see langword="true"/>.
    /// </summary>
    /// <param name="text">The search string to test</param>
    /// <returns>Whether this item matches the given search string</returns>
    public bool MatchesSearch(string text)
    {
        if (_skipSearch)
            return false;

        if (_searchText.Contains(text, StringComparison.OrdinalIgnoreCase))
            return true;

        return _tags.NotNullNotEmpty() && _tags!.Contains(text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Replaces the item's secondary search tags and returns this item for fluent chaining</summary>
    /// <param name="tags">Comma-separated tag terms to match against search input</param>
    /// <returns>This item</returns>
    public OptionItem SetTags(string tags)
    {
        _tags = tags;
        return this;
    }

    private Widget CreateOrUpdateChild()
    {
        _layout.Child ??= _createWidget();
        _layout.Child.Enabled = Enabled; // Make sure enablement is propagated to the child.
        return _layout.Child;
    }

    /// <inheritdoc/>
    protected override Point InternalMeasure(Point availableSize)
    {
        CreateOrUpdateChild();
        return base.InternalMeasure(availableSize);
    }

    /// <inheritdoc/>
    public override void InternalRender(RenderContext context)
    {
        CreateOrUpdateChild();
        base.InternalRender(context);
    }
}
