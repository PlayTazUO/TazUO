#nullable enable

namespace ClassicUO.LegionScripting.ApiClasses.VisualEffects;

/// <summary>
/// One entry in the client's visual effect library, as `API.GetVisualEffectProfiles` lists it.
/// Enough to pick a composition and no more; `API.GetVisualEffectProfile` fetches the editable copy.
/// </summary>
public class ApiEffectProfileInfo
{
    #region Public accessors

    /// <summary>Stable identity, and what `API.StartVisualEffect` takes.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name. Shipped compositions are localized and suffixed "(built-in)", so this is only
    /// a reliable key into `API.StartVisualEffectByName` for the user's own.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether this is one of the compositions shipped with the client.</summary>
    public bool IsBuiltIn { get; set; }

    #endregion

    #region Public methods

    public override string ToString() => $"{Name} [{Id}]";

    #region Python Emulation

    public string __str__() => ToString();

    public string __repr__() => ToString();

    #endregion

    #endregion
}
