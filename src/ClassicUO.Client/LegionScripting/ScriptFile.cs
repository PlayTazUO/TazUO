using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using ClassicUO.Game;
using ClassicUO.Utility.Logging;
using IronPython.Hosting;
using LScript;
using Microsoft.Scripting.Hosting;

namespace ClassicUO.LegionScripting;

public class ScriptFile
{
    public string Path;
    public string FileName;
    public string FullPath;
    public string Group = string.Empty;
    public string SubGroup = string.Empty;
    public Script GetScript;
    public string[] FileContents;
    public string FileContentsJoined;
    public ScriptType ScriptType = ScriptType.LegionScript;
    public Thread PythonThread;
    public ScriptEngine PythonEngine;
    public ScriptScope PythonScope;
    public API ScopedApi;

    public bool IsPlaying
    {
        get
        {
            if (ScriptType == ScriptType.LegionScript && GetScript != null)
                return GetScript.IsPlaying;

            return PythonThread != null;
        }
    }

    private World World;

    public ScriptFile(World world, string path, string fileName)
    {
        World = world;
        Path = path;

        string cleanPath = path.Replace(System.IO.Path.DirectorySeparatorChar, '/');
        string cleanBasePath = LegionScripting.ScriptPath.Replace(System.IO.Path.DirectorySeparatorChar, '/');
        cleanPath = cleanPath.Substring(cleanPath.IndexOf(cleanBasePath) + cleanBasePath.Length);

        if (cleanPath.Length > 0)
        {
            string[] paths = cleanPath.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
            if (paths.Length > 0)
                Group = paths[0];
            if (paths.Length > 1)
                SubGroup = paths[1];
        }

        FileName = fileName;
        FullPath = System.IO.Path.Combine(Path, FileName);
        FileContents = ReadFromFile();

        if (FileName.EndsWith(".py"))
            ScriptType = ScriptType.Python;

        if (ScriptType == ScriptType.LegionScript)
            GenerateScript();
    }

    public void OverrideFileContents(string contents)
    {
        string temp = System.IO.Path.GetTempFileName();

        try
        {
            File.WriteAllText(temp, contents);
            File.Move(temp, FullPath, true);

            GameActions.Print(World, $"Saved {FileName}.");
        }
        catch (Exception ex)
        {
            GameActions.Print(World, ex.ToString());
        }
    }

    public string[] ReadFromFile()
    {
        try
        {
            string[] c = File.ReadAllLines(FullPath, Encoding.UTF8);
            FileContentsJoined = string.Join("\n", c);
            if (ScriptType == ScriptType.Python)
            {
                string pattern = @"^\s*(?:from\s+[\w.]+\s+import\s+API|import\s+API)\s*$";
                FileContentsJoined = System.Text.RegularExpressions.Regex.Replace(FileContentsJoined, pattern, string.Empty, System.Text.RegularExpressions.RegexOptions.Multiline);
            }
            return c;
        }
        catch (Exception e)
        {
            Log.Error($"Error reading script file: {e}");
            return new string[0];
        }
    }

    public void GenerateScript()
    {
        LegionScripting.StopScript(this);

        try
        {
            if (GetScript == null)
                GetScript = new Script(Lexer.Lex(FullPath), World);
            else
                GetScript.UpdateScript(Lexer.Lex(FullPath));
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    public bool FileExists() => File.Exists(FullPath);

    public void SetupPythonEngine()
    {
        if (PythonEngine != null && !LegionScripting.LScriptSettings.DisableModuleCache)
            return;

        PythonEngine = Python.CreateEngine();

        string dir = System.IO.Path.GetDirectoryName(FullPath);
        ICollection<string> paths = PythonEngine.GetSearchPaths();
        paths.Add(System.IO.Path.Combine(CUOEnviroment.ExecutablePath, "iplib"));
        paths.Add(System.IO.Path.Combine(CUOEnviroment.ExecutablePath, "LegionScripts"));

        paths.Add(!string.IsNullOrWhiteSpace(dir) ? dir : Environment.CurrentDirectory);

        PythonEngine.SetSearchPaths(paths);
    }

    public void SetupPythonScope()
    {
        PythonScope = PythonEngine.CreateScope();
        var api = new API(PythonEngine);
        ScopedApi = api;
        PythonEngine.GetBuiltinModule().SetVariable("API", api);
    }

    public void PythonScriptStopped()
    {
        ScopedApi?.CloseGumps();
        ScopedApi?.Dispose();

        PythonScope = null;
        ScopedApi = null;
        if (LegionScripting.LScriptSettings.DisableModuleCache)
            PythonEngine = null;
    }
}
