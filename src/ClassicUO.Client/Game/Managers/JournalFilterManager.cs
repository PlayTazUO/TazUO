using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using ClassicUO.Configuration;
using ClassicUO.Utility;

namespace ClassicUO.Game.Managers;

public class JournalFilterManager
{
    private string _savePath;

    private HashSet<string> _filters = new();
    public HashSet<string> Filters => _filters;

    private static JournalFilterManager _instance;
    public static JournalFilterManager Instance { get
        {
            if (_instance == null)
                _instance = new();
            return _instance;
        }
    }

    private JournalFilterManager()
    {
        _savePath = Path.Combine(ProfileManager.ProfilePath, "journal_filters.json");
        Load();
    }

    public void AddFilter(string filter) => _filters.Add(filter);

    public void RemoveFilter(string filter) => _filters.Remove(filter);

    public bool IgnoreMessage(string message)
    {
        foreach (string filter in _filters)
        {
            if (message.Contains(filter, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public void Save(bool resetInstance = true)
    {
        JsonHelper.SaveAndBackup(_filters, _savePath, HashSetContext.Default.HashSetString);
        _instance = null;
    }

    public void Load()
    {
        if(JsonHelper.Load(_savePath, HashSetContext.Default.HashSetString, out HashSet<string> obj))
            _filters = obj;
    }

    #nullable enable
    public string? GetJsonExport()
    {
        try
        {
            return System.Text.Json.JsonSerializer.Serialize(_filters, HashSetContext.Default.HashSetString);
        }
        catch (Exception e)
        {
            Utility.Logging.Log.Error($"Error exporting journal filters to JSON: {e}");
        }

        return null;
    }
    #nullable disable

    public bool ImportFromJson(string json)
    {
        try
        {
            HashSet<string> importedFilters = System.Text.Json.JsonSerializer.Deserialize(json, HashSetContext.Default.HashSetString);

            if (importedFilters != null)
            {
                int addedCount = 0;
                int duplicateCount = 0;

                foreach (string filter in importedFilters)
                {
                    if (_filters.Add(filter))
                    {
                        addedCount++;
                    }
                    else
                    {
                        duplicateCount++;
                    }
                }

                Save(false);

                string message = $"Imported {addedCount} journal filters from clipboard";
                if (duplicateCount > 0)
                    message += $" ({duplicateCount} duplicates skipped)";
                GameActions.Print(message, Constants.HUE_SUCCESS);
                return true;
            }
        }
        catch (Exception e)
        {
            Utility.Logging.Log.Error($"Error importing journal filters from JSON: {e}");
        }

        return false;
    }
}


[JsonSerializable(typeof(HashSet<string>))]
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    IgnoreReadOnlyProperties = false,
    IncludeFields = false)]
public partial class HashSetContext : JsonSerializerContext
{
}
