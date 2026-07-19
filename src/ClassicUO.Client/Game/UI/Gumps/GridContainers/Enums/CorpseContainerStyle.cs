namespace ClassicUO.Game.UI.Gumps;

/// <summary>
///     Controls which container style corpses open in, independent of the global
///     <see cref="ClassicUO.Configuration.Profile.GridContainersDefaultToOldStyleView" /> preference.
/// </summary>
public enum CorpseContainerStyle
{
    /// <summary>Use the global default (the "Open new containers in the original view" setting).</summary>
    Default,

    /// <summary>Always open corpses as grid containers.</summary>
    Grid,

    /// <summary>Always open corpses as original-style containers.</summary>
    Original
}
