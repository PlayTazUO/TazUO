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
    public static GridContainerSaveData Instance { get; private set; } = new GridContainerSaveData();

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

        if (!File.Exists(_savePath)) //No save file, use empty default list.
            return;

        Load();
        RemoveOldContainers();
    }

    private void RemoveOldContainers()
    {
        var now = (DateTimeOffset.UtcNow - INACTIVE_CUTOFF).ToUnixTimeSeconds();

        List<GridContainerEntry> toRemove = new();

        foreach (var entry in _entries.Values)
        {
            if (entry.LastOpened < now)
                toRemove.Add(entry);
        }

        foreach (var entry in toRemove)
        {
            _entries.Remove(entry.Serial);
        }
    }

    private string GetBackupSavePath(ushort index) => _savePath + ".backup" + index;

    public void Save()
    {
        string tempPath = null;
        try
        {
            var output = JsonSerializer.Serialize(_entries.Values.ToArray(),
                GridContainerSerializerContext.Default.GridContainerEntryArray);

            tempPath = Path.GetTempFileName();
            File.WriteAllText(tempPath, output);

            // Rotate backups: backup2 -> backup3, backup1 -> backup2, main -> backup1
            var backup3Path = GetBackupSavePath(3);
            var backup2Path = GetBackupSavePath(2);
            var backup1Path = GetBackupSavePath(1);

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
        var filesToTry = new[] { _savePath, GetBackupSavePath(1), GetBackupSavePath(2), GetBackupSavePath(3) };

        foreach (var filePath in filesToTry)
        {
            try
            {
                if (!File.Exists(filePath))
                    continue;

                var json = File.ReadAllText(filePath);
                var entries = JsonSerializer.Deserialize(json,
                    GridContainerSerializerContext.Default.GridContainerEntryArray);

                if (entries != null)
                {
                    _entries.Clear();
                    foreach (var entry in entries)
                    {
                        _entries[entry.Serial] = entry;
                    }

                    return;
                }
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
            var path = Path.Combine(ProfileManager.ProfilePath, "GridContainers.xml");
            if (!File.Exists(path))
                return false;

            var saveDocument = XDocument.Load(path);
            var rootElement = saveDocument.Element("grid_gumps");
            if (rootElement == null)
            {
                File.Delete(path);
                return false;
            }

            foreach (var container in rootElement.Elements().ToList())
            {
                var name = container.Name.ToString();
                if (!name.StartsWith("container_")) continue;
                if (!uint.TryParse(name.Replace("container_", string.Empty), out uint conSerial)) continue;

                var entry = CreateEntry(conSerial);

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
                                var slot1 = entry.GetSlot(serialV);
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
            return false;
        }
    }

    /// <summary>
    /// This does not save.
    /// </summary>
    public static void Reset()
    {
        Instance = new();
    }

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
        if (_entries.TryGetValue(serial, out var entry))
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

    [JsonPropertyName("ls")] public Dictionary<uint, GridContainerSlotEntry> Slots { get; set; } = new();

    public GridContainerSlotEntry GetSlot(uint serial)
    {
        if (Slots.TryGetValue(serial, out var entry))
            return entry;

        GridContainerSlotEntry newEntry = new() { Serial = serial };
        Slots.Add(serial, newEntry);
        return newEntry;
    }

    public Point GetPosition()
    {
        return new Point(X, Y);
    }

    public Point GetSize()
    {
        return new Point(Width, Height);
    }

    public void UpdateSaveDataEntry(GridContainer container)
    {
        GridContainerSaveData.Instance.AddOrReplaceContainer(container);
    }

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
