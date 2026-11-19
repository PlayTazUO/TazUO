#nullable enable

namespace ClassicUO.Game.UI.MyraWindows.Widgets;

/// <summary>What a highlightable widget is, which decides how much of the page the cue covers</summary>
public enum HighlightScope
{
    /// <summary>One setting and its editor</summary>
    Setting,

    /// <summary>A framed group of settings, named by a breadcrumb segment</summary>
    Section
}

/// <summary>
///     A widget that can be pointed out by name: the owner of a setting, a gated group, or a framed
///     section. Implement it on anything a breadcrumb segment or a search result may refer to.
///     <para>
///         The point is that the cue matches on this interface alone, never on a list of concrete
///         widget types. A new kind of option widget becomes highlightable by implementing this,
///         with no central switch to remember to extend.
///     </para>
/// </summary>
public interface IHighlightTarget
{
    /// <summary>
    ///     The name this widget goes by, spelled exactly as the search index recorded it, or
    ///     <see langword="null" /> when it has none and so cannot be pointed out.
    /// </summary>
    string? HighlightLabel { get; }

    /// <summary>
    ///     Whether this is a single setting or a whole section. A setting wins over a section of the
    ///     same name, since a section encloses - and is therefore reached before - what it contains.
    /// </summary>
    HighlightScope HighlightScope => HighlightScope.Setting;
}
