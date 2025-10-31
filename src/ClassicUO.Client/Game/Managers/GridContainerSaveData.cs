using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Managers;

public class GridContainerSaveData
{
    private static GridContainerSaveData _instance;
    public static GridContainerSaveData Instance { get
    {
        if (_instance == null)
            _instance = new();
        return _instance;
    }
    }

    private static TimeSpan INACTIVE_CUTOFF = TimeSpan.FromDays(120);

    private Dictionary<uint, GridContainerEntry> _entries = new();
    private string _savePath => Path.Combine(ProfileManager.ProfilePath, "grid_containers.json");

    private GridContainerSaveData()
    {
        Init();
        Log.Debug($"{_entries.Count} grid containers loaded.");
    }

    private void Init()
    {
        if (ConvertOldXMLSave()) return;

        Load();
        RemoveOldContainers();
    }

    private void RemoveOldContainers()
    {
        long cutoffTime = (DateTimeOffset.UtcNow - INACTIVE_CUTOFF).ToUnixTimeSeconds();

        List<GridContainerEntry> toRemove = new();

        foreach (GridContainerEntry entry in _entries.Values)
        {
            // Only remove if LastOpened is valid (not 0) and actually old
            if (entry.LastOpened > 0 && entry.LastOpened < cutoffTime)
                toRemove.Add(entry);
        }

        foreach (GridContainerEntry entry in toRemove)
        {
            _entries.Remove(entry.Serial);
        }
    }

    private string GetBackupSavePath(ushort index) => _savePath + ".backup" + index;

    public void Save()
    {
        Log.Debug($"Saving {_entries.Count} grid containers");
        string tempPath = null;
        try
        {
            string output = JsonSerializer.Serialize(_entries.Values.ToArray(),
                GridContainerSerializerContext.Default.GridContainerEntryArray);

            tempPath = Path.GetTempFileName();
            File.WriteAllText(tempPath, output);

            // Rotate backups: backup2 -> backup3, backup1 -> backup2, main -> backup1
            string backup3Path = GetBackupSavePath(3);
            string backup2Path = GetBackupSavePath(2);
            string backup1Path = GetBackupSavePath(1);

            // Remove oldest backup
            if (File.Exists(backup3Path))
                File.Delete(backup3Path);

            // Rotate existing backups
            if (File.Exists(backup2Path))
                File.Move(backup2Path, backup3Path);

            if (File.Exists(backup1Path))
                File.Move(backup1Path, backup2Path);

            // Move current main file to backup1
            if (File.Exists(_savePath))
                File.Move(_savePath, backup1Path);

            // Move temp file to main
            File.Move(tempPath, _savePath);
            tempPath = null;
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
        }

        // Clean up temp file if it still exists
        if (tempPath != null && File.Exists(tempPath))
        {
            try { File.Delete(tempPath); }
            catch { }
        }
    }

    /// <summary>
    /// Tries to load from main file, then backup1, backup2, backup3 in order.
    /// </summary>
    public void Load()
    {
        string[] filesToTry = new[] { _savePath, GetBackupSavePath(1), GetBackupSavePath(2), GetBackupSavePath(3) };

        foreach (string filePath in filesToTry)
        {
            try
            {
                if (!File.Exists(filePath))
                    continue;

                string json = File.ReadAllText(filePath);
                GridContainerEntry[] entries = JsonSerializer.Deserialize(json,
                    GridContainerSerializerContext.Default.GridContainerEntryArray);

                _entries?.Clear();
                _entries = new();
                foreach (GridContainerEntry entry in entries)
                {
                    _entries[entry.Serial] = entry;
                }

                return;
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to load from {filePath}: {e.Message}");
            }
        }

        // If we get here, all files failed to load
        Log.Error("Failed to load from main file and all backups");
    }

    //Convert old xml saves to new format
    private bool ConvertOldXMLSave()
    {
        try
        {
            string path = Path.Combine(ProfileManager.ProfilePath, "GridContainers.xml");
            if (!File.Exists(path))
                return false;

            var saveDocument = XDocument.Load(path);
            XElement rootElement = saveDocument.Element("grid_gumps");
            if (rootElement == null)
            {
                File.Delete(path);
                return false;
            }

            foreach (XElement container in rootElement.Elements().ToList())
            {
                string name = container.Name.ToString();
                if (!name.StartsWith("container_")) continue;
                if (!uint.TryParse(name.Replace("container_", string.Empty), out uint conSerial)) continue;

                GridContainerEntry entry = CreateEntry(conSerial);

                XAttribute width, height;
                width = container.Attribute("width");
                height = container.Attribute("height");
                if (width != null && height != null)
                {
                    int.TryParse(width.Value, out int w);
                    int.TryParse(height.Value, out int h);
                    entry.Width = w;
                    entry.Height = h;
                }

                XAttribute lastX, lastY;
                lastX = container.Attribute("lastX");
                lastY = container.Attribute("lastY");
                if (lastX != null && lastY != null)
                {
                    int.TryParse(lastX.Value, out int x);
                    int.TryParse(lastY.Value, out int y);
                    entry.X = x;
                    entry.Y = y;
                }

                XAttribute useOriginal;
                useOriginal = container.Attribute("useOriginalContainer");
                if (useOriginal != null)
                {
                    bool.TryParse(useOriginal.Value, out bool useOriginalContainer);
                    entry.UseOriginalContainer = useOriginalContainer;
                }

                XAttribute attribute = container.Attribute("autoSort");
                if (attribute != null)
                {
                    bool.TryParse(attribute.Value, out bool autoSort);
                    entry.AutoSort = autoSort;
                }

                attribute = container.Attribute("stacknonstackables");
                if (attribute != null)
                {
                    bool.TryParse(attribute.Value, out bool stacknoners);
                    entry.VisuallyStackNonStackables = stacknoners;
                }


                foreach (XElement itemSlot in container.Elements("item"))
                {
                    XAttribute slot, serial, isLockedAttribute;
                    slot = itemSlot.Attribute("slot");
                    serial = itemSlot.Attribute("serial");
                    isLockedAttribute = itemSlot.Attribute("locked");
                    if (slot != null && serial != null)
                    {
                        if (int.TryParse(slot.Value, out int slotV))
                            if (uint.TryParse(serial.Value, out uint serialV))
                            {
                                GridContainerSlotEntry slot1 = entry.GetSlot(serialV);
                                slot1.Slot = slotV;
                                if (isLockedAttribute != null &&
                                    bool.TryParse(isLockedAttribute.Value, out bool isLocked))
                                    slot1.Locked = isLocked;
                            }
                    }
                }
            }

            File.Delete(path);
            return true;
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return false;
        }
    }

    /// <summary>
    /// This does not save.
    /// </summary>
    public static void Reset() => _instance = null;

    public GridContainerEntry CreateEntry(uint serial)
    {
        var entry = new GridContainerEntry() { Serial = serial };
        _entries[serial] = entry;
        return entry;
    }

    public void AddOrReplaceContainer(GridContainer container)
    {
        GridContainerEntry entry = container.GridContainerEntry;
        if (entry == null && !_entries.TryGetValue(container.LocalSerial, out entry))
            entry = new GridContainerEntry();

        entry.UpdateFromContainer(container);
        entry.LastOpened = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); //Update last opened time

        _entries[container.LocalSerial] = entry;
    }

    public GridContainerEntry GetContainer(uint serial)
    {
        if (_entries.TryGetValue(serial, out GridContainerEntry entry))
            return entry;

        return new GridContainerEntry();
    }
}

public class GridContainerEntry
{
    [JsonPropertyName("s")] public uint Serial { get; set; }

    [JsonPropertyName("l")] public long LastOpened { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    [JsonPropertyName("w")] public int Width { get; set; }

    [JsonPropertyName("h")] public int Height { get; set; }

    [JsonPropertyName("x")] public int X { get; set; }

    [JsonPropertyName("y")] public int Y { get; set; }

    [JsonPropertyName("og")] public bool UseOriginalContainer { get; set; }

    [JsonPropertyName("as")] public bool AutoSort { get; set; }

    [JsonPropertyName("vs")] public bool VisuallyStackNonStackables { get; set; }

    [JsonPropertyName("sm")] public int SortMode { get; set; }

    [JsonPropertyName("ls")] public Dictionary<uint, GridContainerSlotEntry> Slots { get; set; } = new();

    public GridContainerSlotEntry GetSlot(uint serial)
    {
        if (Slots.TryGetValue(serial, out GridContainerSlotEntry entry))
            return entry;

        GridContainerSlotEntry newEntry = new() { Serial = serial };
        Slots.Add(serial, newEntry);
        return newEntry;
    }

    public Point GetPosition() => new Point(X, Y);

    public Point GetSize() => new Point(Width, Height);

    public void UpdateSaveDataEntry(GridContainer container) => GridContainerSaveData.Instance.AddOrReplaceContainer(container);

    public GridContainerEntry UpdateFromContainer(GridContainer container)
    {
        Serial = container.LocalSerial;
        Width = container.Width;
        Height = container.Height;
        X = container.X;
        Y = container.Y;
        UseOriginalContainer = container.UseOldContainerStyle ?? false;
        AutoSort = container.AutoSortContainer;
        VisuallyStackNonStackables = container.StackNonStackableItems;
        SortMode = (int)container.SortMode;

        // Sync all item positions from GridSlotManager to Slots
        // First, remove any entries for items no longer in ItemPositions (they were removed/moved)
        Dictionary<int, uint> itemPositions = container.SlotManager?.ItemPositions;
        if (itemPositions != null)
        {
            // Get list of serials currently in ItemPositions
            var currentSerials = new HashSet<uint>(itemPositions.Values);

            // Remove stale entries from Slots
            var staleSerials = Slots.Keys.Where(serial => !currentSerials.Contains(serial)).ToList();
            foreach (uint serial in staleSerials)
            {
                Slots.Remove(serial);
            }

            // Now sync current positions
            foreach (KeyValuePair<int, uint> kvp in itemPositions)
            {
                int slotIndex = kvp.Key;
                uint itemSerial = kvp.Value;

                // Ensure this item has a slot entry with the correct position
                GridContainerSlotEntry entry = GetSlot(itemSerial);
                entry.Slot = slotIndex;
            }
        }

        return this;
    }
}

public class GridContainerSlotEntry
{
    [JsonPropertyName("s")] public uint Serial { get; set; }

    [JsonPropertyName("k")] public bool Locked { get; set; }

    [JsonPropertyName("sl")] public int Slot { get; set; }
}

[JsonSerializable(typeof(GridContainerEntry))]
[JsonSerializable(typeof(GridContainerSlotEntry))]
[JsonSerializable(typeof(GridContainerEntry[]))]
[JsonSerializable(typeof(Dictionary<uint, GridContainerSlotEntry>))]
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    IgnoreReadOnlyProperties = false,
    IncludeFields = false)]
public partial class GridContainerSerializerContext : JsonSerializerContext
{
}
