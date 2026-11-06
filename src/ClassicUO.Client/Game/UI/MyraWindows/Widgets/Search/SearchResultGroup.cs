#nullable enable

using System;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.MyraWindows.Widgets.Search;

/// <summary>
///     One node of the tree that search results are displayed in: a path segment, the hits that
///     live directly at that segment, and any deeper segments beneath it.
///     <para>
///         Built by <see cref="Build" /> rather than directly, because sibling ordering and
///         single-child collapsing are properties of the whole tree, not of one node.
///     </para>
/// </summary>
public sealed class SearchResultGroup
{
    #region Ctor

    private SearchResultGroup(SearchPath path, string segment)
    {
        Path = path;
        Segments = [segment];
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Groups <paramref name="hits" /> into a tree by their paths.
    ///     <para>
    ///         Siblings are ordered by their best contained score so the most relevant branch reads
    ///         first, while hits inside a node keep their index order so rows don't reshuffle while
    ///         the user types. Chains of single-child nodes are collapsed.
    ///     </para>
    /// </summary>
    /// <param name="hits">The hits to group; order is irrelevant</param>
    /// <returns>The top-level groups, best first. Hits with an empty path land in a root-level group with no segments.</returns>
    public static IReadOnlyList<SearchResultGroup> Build(IEnumerable<SearchHit> hits)
    {
        List<SearchResultGroup> roots = [];
        List<SearchHit> pathless = [];

        foreach (SearchHit hit in hits)
        {
            if (hit.Entry.Path.IsEmpty)
            {
                pathless.Add(hit);
                continue;
            }

            Insert(roots, hit);
        }

        foreach (SearchResultGroup root in roots)
            root.Finalise();

        roots.Sort(CompareByScore);

        if (pathless.Count == 0)
            return roots;

        var ungrouped = new SearchResultGroup(SearchPath.Empty, string.Empty) { Segments = [] };
        ungrouped._hits.AddRange(pathless);
        ungrouped.Finalise();
        roots.Insert(0, ungrouped);

        return roots;
    }

    #endregion

    #region Members

    private readonly List<SearchResultGroup> _children = [];
    private readonly List<SearchHit> _hits = [];

    #endregion

    #region Public Accessors

    /// <summary>
    ///     This node's own segment labels. Usually one, but a chain of segments with nothing else in
    ///     it is collapsed into a single node so a lone setting doesn't cost several indent levels -
    ///     such a node displays as <c>Visual Effects › Overlay Rules</c>.
    /// </summary>
    public IReadOnlyList<string> Segments { get; private set; }

    /// <summary>Full path to this node, used as the collapse-state key and for navigation</summary>
    public SearchPath Path { get; private set; }

    /// <summary>Deeper groups beneath this one, best-scoring first</summary>
    public IReadOnlyList<SearchResultGroup> Children => _children;

    /// <summary>Hits that live directly at this node, in index order</summary>
    public IReadOnlyList<SearchHit> Hits => _hits;

    /// <summary>Total hits at this node and everywhere below it</summary>
    public int TotalHits { get; private set; }

    /// <summary>Best score at this node or below, which is what orders siblings</summary>
    public double BestScore { get; private set; }

    /// <summary>Joined <see cref="Segments" />, as displayed on the group header</summary>
    public string DisplayName => string.Join(SearchPath.SEPARATOR, Segments);

    #endregion

    #region Private Methods

    /// <summary>Walks <paramref name="hit" />'s path, creating nodes as needed, and files it at the leaf</summary>
    private static void Insert(List<SearchResultGroup> roots, SearchHit hit)
    {
        SearchPath path = hit.Entry.Path;
        List<SearchResultGroup> level = roots;
        SearchResultGroup? node = null;

        for (int depth = 0; depth < path.Length; depth++)
        {
            string segment = path[depth];
            node = Find(level, segment);

            if (node == null)
            {
                node = new SearchResultGroup(path.Take(depth + 1), segment);
                level.Add(node);
            }

            level = node._children;
        }

        node!._hits.Add(hit);
    }

    private static SearchResultGroup? Find(List<SearchResultGroup> level, string segment)
    {
        foreach (SearchResultGroup candidate in level)
            if (candidate.Segments.Count == 1 && string.Equals(candidate.Segments[0], segment, StringComparison.Ordinal))
                return candidate;

        return null;
    }

    /// <summary>
    ///     Depth-first pass that collapses single-child chains, orders siblings and hits, and rolls
    ///     up the counts and scores the header row displays.
    /// </summary>
    private void Finalise()
    {
        foreach (SearchResultGroup child in _children)
            child.Finalise();

        // A node that holds nothing itself and leads to exactly one child is pure indentation;
        // absorb the child so the header reads as a single joined crumb.
        while (_hits.Count == 0 && _children.Count == 1)
        {
            SearchResultGroup only = _children[0];

            Segments = [.. Segments, .. only.Segments];
            Path = only.Path;

            _children.Clear();
            _children.AddRange(only._children);
            _hits.AddRange(only._hits);
        }

        _hits.Sort(CompareByOrder);
        _children.Sort(CompareByScore);

        TotalHits = _hits.Count;
        BestScore = 0d;

        foreach (SearchHit hit in _hits)
            BestScore = Math.Max(BestScore, hit.Score);

        foreach (SearchResultGroup child in _children)
        {
            TotalHits += child.TotalHits;
            BestScore = Math.Max(BestScore, child.BestScore);
        }
    }

    private static int CompareByScore(SearchResultGroup left, SearchResultGroup right) =>
        right.BestScore.CompareTo(left.BestScore);

    private static int CompareByOrder(SearchHit left, SearchHit right) =>
        left.Order.CompareTo(right.Order);

    #endregion
}
