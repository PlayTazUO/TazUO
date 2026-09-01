#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers;

public static class LastEquipmentManager
{
    private const string ID = "lem_equipment_";
    private const string DELIMITER = ";";

    private static string GetSaveID(string serverName, string charName, string accountName) => ID + serverName + accountName + charName;

    #warning Remove migration >= 10/26/26
    /// <summary>
    /// One-time migration of the legacy per-server/account/character last-equipment rows from SQL
    /// settings into the machine-wide <see cref="GlobalSettingsSave"/>. The legacy rows are cleared so
    /// this is idempotent.
    /// </summary>
    public static void MigrateLegacySqlSettings()
    {
        if (Client.Settings == null || ProfileManager.GlobalSettings == null)
        {
            return;
        }

        GlobalSettingsSave globalSettings = ProfileManager.GlobalSettings;

        foreach (KeyValuePair<string, string> kvp in Client.Settings.GetAll(SettingsScope.Global))
        {
            if (!kvp.Key.StartsWith(ID))
                continue;

            globalSettings.LastEquipmentData[kvp.Key] = kvp.Value;
            Client.Settings.Set(SettingsScope.Global, kvp.Key, string.Empty);
        }
    }

    public static void Save(Item[] items, string serverName, string charName, string accountName, ushort playerGraphic, ushort bodyHue, bool isFemale)
    {
        var sb = new StringBuilder();

        // Format: playerGraphic;bodyHue;isFemale;items...
        sb.Append($"{playerGraphic}{DELIMITER}");
        sb.Append($"{bodyHue}{DELIMITER}");
        sb.Append($"{(isFemale ? 1 : 0)}{DELIMITER}");

        foreach (Item item in items)
        {
            if (item.Graphic != 0 && item.Layer != Layer.Invalid)
            {
                // Format: graphic,layer,hue,animID,isPartialHue
                sb.Append($"{item.Graphic},{(byte)item.Layer},{item.Hue},{item.ItemData.AnimID},{(item.ItemData.IsPartialHue ? 1 : 0)}{DELIMITER}");
            }
        }

        string id = GetSaveID(serverName, charName, accountName);
        Log.TraceDebug($"Saving LEM data for ({id}): [{sb}]");

        GlobalSettingsSave globalSettings = ProfileManager.GlobalSettings;
        if (globalSettings == null)
            return;

        globalSettings.LastEquipmentData[id] = sb.ToString();
    }

    public static LemCharData? Load(string serverName, string charName, string accountName)
    {
        string id = GetSaveID(serverName, charName, accountName);

        GlobalSettingsSave globalSettings = ProfileManager.GlobalSettings;
        string res = globalSettings?.LastEquipmentData.TryGetValue(id, out string value) == true ? value : string.Empty;
        Log.TraceDebug($"Loading LEM data for ({id}): [{res}]");

        if (!res.NotNullNotEmpty()) return null;

        try
        {
            string[] parts = res.Split(";");
            if (parts.Length < 3) return null;

            ushort playerGraphic = ushort.Parse(parts[0]);
            ushort bodyHue = ushort.Parse(parts[1]);
            bool isFemale = parts[2] == "1";

            var equipment = new Dictionary<Layer, LemEquipmentEntry>();

            for (int i = 3; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i])) continue;

                string[] split = parts[i].Split(",");
                if (split.Length < 5) continue;

                if (ushort.TryParse(split[0], out ushort graphic) &&
                    Enum.TryParse(split[1], out Layer layer) &&
                    ushort.TryParse(split[2], out ushort hue) &&
                    ushort.TryParse(split[3], out ushort animID))
                {
                    bool isPartialHue = split[4] == "1";
                    equipment[layer] = new LemEquipmentEntry(graphic, hue, animID, isPartialHue);
                }
            }

            return new LemCharData(playerGraphic, bodyHue, isFemale, equipment);
        }
        catch (Exception e)
        {
            Log.ErrorDebug(e.ToString());
            return null;
        }
    }
}

/// <summary>
/// Equipment entry containing all data needed to display an item on a paperdoll.
/// </summary>
public readonly struct LemEquipmentEntry
{
    public readonly ushort Graphic;
    public readonly ushort Hue;
    public readonly ushort AnimID;
    public readonly bool IsPartialHue;

    public LemEquipmentEntry(ushort graphic, ushort hue, ushort animID, bool isPartialHue)
    {
        Graphic = graphic;
        Hue = hue;
        AnimID = animID;
        IsPartialHue = isPartialHue;
    }
}

/// <summary>
/// Character data for last equipment display.
/// </summary>
public readonly struct LemCharData
{
    public readonly ushort PlayerGraphic;
    public readonly ushort BodyHue;
    public readonly bool IsFemale;
    public readonly Dictionary<Layer, LemEquipmentEntry> Equipment;

    public LemCharData(ushort playerGraphic, ushort bodyHue, bool isFemale, Dictionary<Layer, LemEquipmentEntry> equipment)
    {
        PlayerGraphic = playerGraphic;
        BodyHue = bodyHue;
        IsFemale = isFemale;
        Equipment = equipment;
    }
}
