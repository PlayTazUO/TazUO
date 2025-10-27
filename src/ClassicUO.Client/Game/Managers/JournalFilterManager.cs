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
        if(_filters.Contains(message))
            return true;
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
