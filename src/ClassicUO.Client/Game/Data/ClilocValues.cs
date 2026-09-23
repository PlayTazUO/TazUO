namespace ClassicUO.Game.Data;

/// <summary>
///     Cliloc IDs that carry meaning to the client, for matching against an object's property list
///     (see <see cref="ClassicUO.Game.Managers.ObjectPropertiesListManager"/>). Only IDs the client
///     actually reacts to belong here; the full cliloc table lives in the UO data files.
/// </summary>
/// <remarks>
///     Backed by <see cref="int"/> to match <c>ItemProperty.Clilocs</c>. The wire format is a big-endian
///     uint32, but every ID in use is well inside the positive int range and the packet reader narrows to
///     <see cref="int"/> on arrival.
/// </remarks>
public enum ClilocValues : int
{
    /// <summary>"Locked down" - item is fixed in place inside a house and cannot be picked up.</summary>
    LockedDown = 501643, // 0x7A78B

    /// <summary>"Locked down and secured" - locked down container that only the owner may access.</summary>
    LockedDownAndSecured = 501644 // 0x7A78C
}
