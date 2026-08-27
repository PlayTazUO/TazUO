using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility;
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

    /// <summary>Locked grid entries are cleared once the item has been absent from the container this long (seconds).</summary>
    internal const long LOCK_EXPIRY_SECONDS = 60L * 24 * 60 * 60;

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
        PruneExpiredLocks();
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

    /// <summary>
    /// Clears locked slot entries whose item has been absent from the container for longer than
    /// <see cref="LOCK_EXPIRY_SECONDS"/>. Runs at load so a lock can't silently resurface after a long absence.
    /// </summary>
    private void PruneExpiredLocks()
    {
        long cutoff = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - LOCK_EXPIRY_SECONDS;

        foreach (GridContainerEntry entry in _entries.Values)
        {
            List<uint> expired = null;

            foreach (KeyValuePair<uint, GridContainerSlotEntry> slot in entry.Slots)
            {
                if (slot.Value.Locked && slot.Value.LastSeen < cutoff)
                    (expired ??= new List<uint>()).Add(slot.Key);
            }

            if (expired != null)
                foreach (uint serial in expired)
                    entry.Slots.Remove(serial);
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
            FileSystemHelper.WriteAllTextSafe(tempPath, output);

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
    [JsonPropertyName("cn")] public string CustomName { get; set; }

    [JsonPropertyName("s")] public uint Serial { get; set; }

    [JsonPropertyName("l")] public long LastOpened { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    [JsonPropertyName("w")] public int Width { get; set; }

    [JsonPropertyName("h")] public int Height { get; set; }

    [JsonPropertyName("x")] public int X { get; set; }

    [JsonPropertyName("y")] public int Y { get; set; }

    [JsonPropertyName("mx")] public int MaximizedX { get; set; }

    [JsonPropertyName("my")] public int MaximizedY { get; set; }

    [JsonPropertyName("mnx")] public int MinimizedX { get; set; }

    [JsonPropertyName("mny")] public int MinimizedY { get; set; }

    /// <summary>
    /// The container's gump style preference.
    /// <br/>
    /// When null, the container should be opened in accordance to the <see cref="Profile.GridContainersDefaultToOldStyleView"/> setting
    /// </summary>
    [JsonPropertyName("og")] public bool? UseOriginalContainer { get; set; }

    [JsonPropertyName("as")] public bool AutoSort { get; set; }

    /// <summary>Per-container override to disable band layout even when bands are enabled globally.</summary>
    [JsonPropertyName("bd")] public bool BandsDisabled { get; set; }

    [JsonPropertyName("vs")] public bool VisuallyStackNonStackables { get; set; }

    [JsonPropertyName("sm")] public int SortMode { get; set; }

    [JsonPropertyName("vm")] public int ViewModeOverride { get; set; }

    [JsonPropertyName("m")] public bool IsMinimized { get; set; }

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

    public Point GetPositionForState(bool isMinimized)
    {
        if (isMinimized)
            return new Point(MinimizedX != 0 ? MinimizedX : X, MinimizedY != 0 ? MinimizedY : Y);
        else
            return new Point(MaximizedX != 0 ? MaximizedX : X, MaximizedY != 0 ? MaximizedY : Y);
    }

    public void SetPositionForState(int x, int y, bool isMinimized)
    {
        if (isMinimized)
        {
            MinimizedX = x;
            MinimizedY = y;
        }
        else
        {
            MaximizedX = x;
            MaximizedY = y;
        }

        // Also update legacy X/Y for backward compatibility
        X = x;
        Y = y;
    }

    public void UpdateSaveDataEntry(GridContainer container) => GridContainerSaveData.Instance.AddOrReplaceContainer(container);

    public GridContainerEntry UpdateFromContainer(GridContainer container)
    {
        Serial = container.LocalSerial;
        Width = container.Width;
        // Store the full height, not the minimized height
        Height = container.IsMinimized ? container.HeightBeforeMinimize : container.Height;
        SetPositionForState(container.X, container.Y, container.IsMinimized);
        // If the container was given a new explicit preference, use it, otherwise, use whatever we already have stored.
        // Null is also fine here and indicates a 'default', ergo, go with the profile's `GridContainersDefaultToOldStyleView` settings
        UseOriginalContainer = container.UseOldContainerStyle ?? container.GridContainerEntry.UseOriginalContainer;
        AutoSort = container.AutoSortContainer;
        BandsDisabled = container.BandsDisabledForContainer;
        VisuallyStackNonStackables = container.StackNonStackableItems;
        SortMode = (int)container.SortMode;
        IsMinimized = container.IsMinimized;

        // Sync all item positions from GridSlotManager to Slots
        Dictionary<int, uint> itemPositions = container.SlotManager?.ItemPositions;
        if (itemPositions != null)
        {
            // Serials with a current slot position. Items that left the container linger in
            // ItemPositions until another item takes their slot, so actual presence below is
            // checked against the real contents instead.
            var itemPositionsSerials = new HashSet<uint>(itemPositions.Values);

            // Serials actually in the container right now (from the last grid rebuild).
            var presentSerials = new HashSet<uint>();
            foreach (Item item in container.SlotManager.ContainerContents)
                presentSerials.Add(item.Serial);

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long lockExpirySeconds = GridContainerSaveData.LOCK_EXPIRY_SECONDS;

            // Remove stale entries from Slots:
            // - Unlocked entries are dropped once they lose their slot position (item moved/removed).
            // - Locked entries survive while the item is merely away, but are cleared once the item
            //   has been absent for LOCK_EXPIRY (checked against the real contents, not ItemPositions).
            foreach (uint serial in Slots.Keys.ToList())
            {
                GridContainerSlotEntry slotEntry = Slots[serial];

                if (slotEntry.Locked)
                {
                    if (!presentSerials.Contains(serial) && now - slotEntry.LastSeen >= lockExpirySeconds)
                        Slots.Remove(serial);
                }
                else if (!itemPositionsSerials.Contains(serial))
                {
                    Slots.Remove(serial);
                }
            }

            // Now sync current positions and refresh last-seen for items present in the container.
            // Stale ItemPositions entries (items that left without another item taking their slot)
            // are kept for position memory but don't refresh the lock timestamp.
            foreach (KeyValuePair<int, uint> kvp in itemPositions)
            {
                int slotIndex = kvp.Key;
                uint itemSerial = kvp.Value;

                // Ensure this item has a slot entry with the correct position
                GridContainerSlotEntry entry = GetSlot(itemSerial);
                entry.Slot = slotIndex;

                if (presentSerials.Contains(itemSerial))
                    entry.LastSeen = now;
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

    /// <summary>
    /// Unix timestamp of the last time this item was seen in its container. Used to expire locked
    /// entries after 60 days of absence. Defaults to "now" so entries from saves without the field
    /// don't expire immediately.
    /// </summary>
    [JsonPropertyName("ls")] public long LastSeen { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
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
