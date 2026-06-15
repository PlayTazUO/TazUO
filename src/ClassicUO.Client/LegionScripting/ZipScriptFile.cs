using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using ClassicUO.Game;
using ClassicUO.Utility.Logging;
using IronPython.Hosting;

namespace ClassicUO.LegionScripting;

public class ZipScriptFile : ScriptFile
{
    public string ZipPath;
    public string EntryPath;

    public ZipScriptFile(World world, string zipPath, string entryPath, string group, string subGroup)
        : base(world, LegionScripting.ScriptPath, System.IO.Path.GetFileName(entryPath))
    {
        ZipPath = zipPath;
        EntryPath = entryPath;
        Group = group;
        SubGroup = subGroup;
        FullPath = $"{zipPath}::{entryPath}";
        FileContents = ReadFromFile();
    }

    public override bool FileExists()
    {
        if (!File.Exists(ZipPath)) return false;
        try
        {
            using var archive = ZipFile.OpenRead(ZipPath);
            return archive.GetEntry(EntryPath) != null;
        }
        catch { return false; }
    }

    public override string[] ReadFromFile()
    {
        if (ZipPath == null) return [];

        try
        {
            using var archive = ZipFile.OpenRead(ZipPath);
            ZipArchiveEntry entry = archive.GetEntry(EntryPath);
            if (entry == null) return [];

            using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
            string text = reader.ReadToEnd();

            string pattern = @"^\s*(?:from\s+[\w.]+\s+import\s+API|import\s+API)\s*$";
            string stripped = Regex.Replace(text, pattern, string.Empty, RegexOptions.Multiline);

            if (Type == ScriptType.CSharp && FileContentsJoined != stripped)
                CSharpCompiledScript = null;

            FileContentsJoined = stripped;
            return text.Split('\n');
        }
        catch (Exception e)
        {
            Log.Error($"Error reading zip script entry '{EntryPath}' in '{ZipPath}': {e}");
            return [];
        }
    }

    public override void OverrideFileContents(string contents)
    {
        try
        {
            using var archive = ZipFile.Open(ZipPath, ZipArchiveMode.Update);
            archive.GetEntry(EntryPath)?.Delete();
            ZipArchiveEntry newEntry = archive.CreateEntry(EntryPath);
            using var writer = new StreamWriter(newEntry.Open(), Encoding.UTF8);
            writer.Write(contents);

            GameActions.Print(World, $"Saved {FileName}.");
        }
        catch (Exception ex)
        {
            GameActions.Print(World, ex.ToString());
        }
    }

    public override void SetupPythonEngine()
    {
        if (PythonEngine != null && !LegionScripting.LScriptSettings.DisableModuleCache)
            return;

        PythonEngine = Python.CreateEngine(new Dictionary<string, object>() { { "RecursionLimit", 100 } });

        ICollection<string> paths = PythonEngine.GetSearchPaths();
        paths.Add(System.IO.Path.Combine(CUOEnviroment.ExecutablePath, "iplib"));
        paths.Add(System.IO.Path.Combine(CUOEnviroment.ExecutablePath, "LegionScripts"));
        paths.Add(ZipPath);
        paths.Add(System.IO.Path.GetDirectoryName(ZipPath) ?? Environment.CurrentDirectory);

        PythonEngine.SetSearchPaths(paths);
    }
}
