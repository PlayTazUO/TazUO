#nullable enable

namespace ClassicUO.LegionScripting.ApiClasses.VisualEffects;

/// <summary>
/// One entry in the client's visual effect library, as `API.VisualEffects.GetProfiles` lists it.
/// Enough to pick a composition and no more; `API.VisualEffects.GetProfile` fetches the editable copy.
/// </summary>
public class ApiEffectProfileInfo : PyApiObject
{
    #region Public accessors

    /// <summary>Stable identity, and what `API.VisualEffects.Start` takes.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name. Shipped compositions are localized and suffixed "(built-in)", so this is only
    /// a reliable key into `API.VisualEffects.StartByName` for the user's own.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether this is one of the compositions shipped with the client.</summary>
    public bool IsBuiltIn { get; set; }

    #endregion

    #region Public methods

    /// <summary>Both halves, since neither alone identifies a composition to a reader: names repeat
    /// and ids are unreadable.</summary>
    /// <returns><c>Name [Id]</c>.</returns>
    public override string ToString() => $"{Name} [{Id}]";

    #endregion
}
