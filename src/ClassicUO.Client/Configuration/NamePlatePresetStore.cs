// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Configuration;

internal enum SaveNamePlatePresetResult
{
    Saved,
    InvalidName,
    DuplicateName
}

internal sealed class NamePlatePresetStore(string directory)
{
    public static NamePlatePresetStore Shared => new(Path.Combine(JsonSaveLocationHelper.DataDirectory, "NameplatePresets"));

    public IReadOnlyList<SavedNamePlatePreset> Load()
    {
        var presets = new List<SavedNamePlatePreset>();
        if (!Directory.Exists(directory))
            return presets;

        string[] files;
        try
        {
            files = Directory.GetFiles(directory, "*.json");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log.Warn($"Unable to read nameplate presets: {e.Message}");
            return presets;
        }

        foreach (string file in files)
        {
            try
            {
                SavedNamePlatePreset preset = JsonSerializer.Deserialize(File.ReadAllText(file), SavedNamePlatePresetJsonContext.Default.SavedNamePlatePreset);
                if (preset != null && IsValidName(preset.Name))
                    presets.Add(preset);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
            {
                Log.Warn($"Unable to read nameplate preset '{file}': {e.Message}");
            }
        }

        return presets.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public SaveNamePlatePresetResult Save(SavedNamePlatePreset snapshot, string name, IEnumerable<string> reservedNames, out SavedNamePlatePreset saved)
    {
        saved = null;
        name = name?.Trim();
        if (!IsValidName(name))
            return SaveNamePlatePresetResult.InvalidName;

        if (reservedNames.Concat(Load().Select(p => p.Name)).Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)))
            return SaveNamePlatePresetResult.DuplicateName;

        // User text is stored inside the JSON, never used as a file path. A stable
        // case-insensitive key also prevents concurrent clients replacing each other.
        string key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(name.ToUpperInvariant())));
        Directory.CreateDirectory(directory);
        string destination = Path.Combine(directory, key + ".json");
        string temporary = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
        SavedNamePlatePreset preset = snapshot with { Name = name };
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(preset, SavedNamePlatePresetJsonContext.Default.SavedNamePlatePreset));
            try
            {
                File.Move(temporary, destination, overwrite: false);
            }
            catch (IOException) when (File.Exists(destination))
            {
                return SaveNamePlatePresetResult.DuplicateName;
            }

            saved = preset;
            return SaveNamePlatePresetResult.Saved;
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }

    private static bool IsValidName(string name) =>
        !string.IsNullOrWhiteSpace(name) && name.Length <= 60 && !name.Any(char.IsControl);
}
