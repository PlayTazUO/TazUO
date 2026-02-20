using ClassicUO.Configuration;

namespace ClassicUO.LegionScripting.ApiClasses;

public class ApiUserProfile
{
    /// <summary>
    /// Get the current character name
    /// </summary>
    public static string CharacterName => ProfileManager.CurrentProfile?.CharacterName ?? "";

    /// <summary>
    /// Get the current server name
    /// </summary>
    public static string ServerName => ProfileManager.CurrentProfile?.ServerName ?? "";

    /// <summary>
    /// Serial of the player's loot bag, otherwise 0
    /// </summary>
    public static uint LootBagSerial => ProfileManager.CurrentProfile?.GrabBagSerial ?? 0;

    /// <summary>
    /// Serial of the player's favorite move bag, otherwise 0
    /// </summary>
    public static uint FavoriteBagSerial => ProfileManager.CurrentProfile?.SetFavoriteMoveBagSerial ?? 0;

    /// <summary>
    /// The player's move item delay in milliseconds.
    /// </summary>
    public static int MoveItemDelay => ProfileManager.CurrentProfile?.MoveMultiObjectDelay ?? 1000;

    /// <summary>
    /// Does the player have auto loot enabled?
    /// </summary>
    public static bool AutoLootEnabled => ProfileManager.CurrentProfile?.EnableAutoLoot ?? false;
}
