using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Utility.Logging;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ClassicUO.Game.UI;
using ClassicUO.Game.UI.MyraWindows;
using ClassicUO.LegionScripting.ApiClasses;
using ClassicUO.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using SourceCodeKind = Microsoft.Scripting.SourceCodeKind;

namespace ClassicUO.LegionScripting
{
    [JsonSerializable(typeof(LScriptSettings))]
    public partial class LScriptJsonContext : JsonSerializerContext
    {
    }

    internal static class LegionScripting
    {
        public static string ScriptPath;
        public static LScriptSettings LScriptSettings { get; private set; }
        public static readonly List<ScriptFile> LoadedScripts = [];
        public static List<ScriptFile> RunningScripts { get; } = [];
        public static readonly Dictionary<int, ScriptFile> PyThreads = new();

        private static bool _enabled, _loaded;
        private static World _world;

        public static void Init(World world)
        {
            _world = world;
            Task.Factory.StartNew(Python.CreateEngine); //This is to preload engine stuff, helps with faster script startup later
            ScriptPath = Path.GetFullPath(Path.Combine(CUOEnviroment.ExecutablePath, "LegionScripts"));

            if (!_loaded)
            {
                EventSink.JournalEntryAdded += EventSink_JournalEntryAdded;
                EventSink.SoundPlayed += EventSink_SoundPlayed;
                _loaded = true;
            }

            LoadScriptsFromFile();
            LoadLScriptSettings();
            AutoPlayGlobal();
            AutoPlayChar();
            _enabled = true;

            world.CommandManager.Register
            (
                "playlscript", a =>
                {
                    if (a.Length < 2)
                    {
                        GameActions.Print(world, "Usage: playlscript <filename>");

                        return;
                    }

                    foreach (ScriptFile f in LoadedScripts)
                        if (f.FileName == string.Join(" ", a.Skip(1)))
                        {
                            PlayScript(f);

                            return;
                        }
                }
            );

            world.CommandManager.Register
            (
                "stoplscript", a =>
                {
                    if (a.Length < 2)
                    {
                        GameActions.Print(world, "Usage: stoplscript <filename>");

                        return;
                    }

                    foreach (ScriptFile sf in RunningScripts)
                        if (sf.FileName == string.Join(" ", a.Skip(1)))
                        {
                            StopScript(sf);

                            return;
                        }
                }
            );

            world.CommandManager.Register
            (
                "togglelscript", a =>
                {
                    if (a.Length < 2)
                    {
                        GameActions.Print(world, "Usage: togglelscript <filename>");

                        return;
                    }

                    foreach (ScriptFile sf in RunningScripts)
                        if (sf.FileName == string.Join(" ", a.Skip(1)))
                        {
                            StopScript(sf);

                            return;
                        }

                    foreach (ScriptFile f in LoadedScripts)
                        if (f.FileName == string.Join(" ", a.Skip(1)))
                        {
                            PlayScript(f);

                            return;
                        }
                }
            );

            world.CommandManager.Register
            (
                "stopall", a =>
                {
                    if (RunningScripts.Count == 0)
                    {
                        GameActions.Print(world, "No scripts are currently running.");
                        return;
                    }

                    int count = RunningScripts.Count;
                    // Create a copy of the list to avoid modification during iteration
                    var scriptsToStop = RunningScripts.ToList();

                    foreach (ScriptFile sf in scriptsToStop)
                    {
                        StopScript(sf);
                    }

                    GameActions.Print(world, $"Stopped {count} running script(s).");
                }
            );
        }

        private static void EventSink_JournalEntryAdded(object sender, JournalEntry e)
        {
            if (e is null)
                return;

            foreach (ScriptFile script in RunningScripts)
            {
                script?.ScopedApi?.JournalEntries.Enqueue(new ApiJournalEntry(e));

                while (script?.ScopedApi?.JournalEntries.Count > ProfileManager.CurrentProfile.MaxJournalEntries) script.ScopedApi?.JournalEntries.TryDequeue(out _);
            }
        }

        private static void EventSink_SoundPlayed(object sender, SoundEventArgs e)
        {
            if (e is null)
                return;

            foreach (ScriptFile script in RunningScripts)
            {
                script?.ScopedApi?.SoundEntries.Enqueue(new ApiSoundEntry(e));

                while (script?.ScopedApi?.SoundEntries.Count > ProfileManager.CurrentProfile.MaxSoundEntries) script.ScopedApi?.SoundEntries.TryDequeue(out _);
            }
        }

        public static void LoadScriptsFromFile()
        {
            if (!Directory.Exists(ScriptPath))
                Directory.CreateDirectory(ScriptPath);

            LoadedScripts.RemoveAll(ls => !ls.FileExists());

            List<string> groups = [ScriptPath, .. HandleScriptsInDirectory(ScriptPath)];

            var subgroups = new List<string>();

            //First level directory(groups)
            foreach (string file in groups)
                subgroups.AddRange(HandleScriptsInDirectory(file));

            foreach (string file in subgroups)
                HandleScriptsInDirectory(file); //No third level supported, ignore directories
        }

        private static void AddScriptFromFile(string path)
        {
            string p = Path.GetDirectoryName(path);
            string fname = Path.GetFileName(path);

            LoadedScripts.Add(new ScriptFile(_world, p, fname));
        }

        /// <summary>
        /// Returns a list of sub directories
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static List<string> HandleScriptsInDirectory(string path)
        {
            var loadedScripts = new HashSet<string>();

            foreach (ScriptFile script in LoadedScripts)
                loadedScripts.Add(script.FullPath);

            var groups = new List<string>();

            foreach (string file in Directory.EnumerateFileSystemEntries(path))
            {
                string fname = Path.GetFileName(file);

                if (fname == "API.py" || fname.StartsWith("_"))
                    continue;

                if (file.EndsWith(".py") || file.EndsWith(".cs"))
                {
                    if (loadedScripts.Contains(file))
                        continue;

                    AddScriptFromFile(file);
                    loadedScripts.Add(file);
                }
                else if (Directory.Exists(file)) groups.Add(file);
            }

            return groups;
        }

        public static void SetAutoPlay(ScriptFile script, bool global, bool enabled)
        {
            if (global)
            {
                if (enabled)
                {
                    if (!LScriptSettings.GlobalAutoStartScripts.Contains(script.FileName))
                        LScriptSettings.GlobalAutoStartScripts.Add(script.FileName);
                }
                else
                    LScriptSettings.GlobalAutoStartScripts.Remove(script.FileName);
            }
            else
            {
                if (LScriptSettings.CharAutoStartScripts.ContainsKey(GetAccountCharName()))
                {
                    if (enabled)
                    {
                        if (!LScriptSettings.CharAutoStartScripts[GetAccountCharName()].Contains(script.FileName))
                            LScriptSettings.CharAutoStartScripts[GetAccountCharName()].Add(script.FileName);
                    }
                    else
                        LScriptSettings.CharAutoStartScripts[GetAccountCharName()].Remove(script.FileName);
                }
                else
                {
                    if (enabled)
                        LScriptSettings.CharAutoStartScripts.Add
                        (
                            GetAccountCharName(), [script.FileName]
                        );
                }
            }
        }

        public static bool AutoLoadEnabled(ScriptFile script, bool global)
        {
            if (!_enabled)
                return false;

            if (global)
                return LScriptSettings.GlobalAutoStartScripts.Contains(script.FileName);

            if (LScriptSettings.CharAutoStartScripts.TryGetValue(GetAccountCharName(), out List<string> scripts)) return scripts.Contains(script.FileName);

            return false;
        }

        private static void AutoPlayGlobal()
        {
            foreach (string script in LScriptSettings.GlobalAutoStartScripts)
                foreach (ScriptFile f in LoadedScripts)
                    if (f.FileName == script)
                        PlayScript(f);
        }

        private static void AutoPlayChar()
        {
            if (_world.Player == null)
                return;

            if (!LScriptSettings.CharAutoStartScripts.TryGetValue(GetAccountCharName(), out List<string> scripts)) return;

            foreach (ScriptFile f in LoadedScripts)
                if (scripts.Contains(f.FileName))
                    PlayScript(f);
        }

        private static string GetAccountCharName() => ProfileManager.CurrentProfile.Username + ProfileManager.CurrentProfile.CharacterName;

        public static bool IsGroupCollapsed(string group, string subgroup = "")
        {
            string path = group;

            if (!string.IsNullOrEmpty(subgroup))
                path += "/" + subgroup;

            return LScriptSettings.GroupCollapsed.GetValueOrDefault(path, false);
        }

        public static void SetGroupCollapsed(string group, string subgroup = "", bool expanded = false)
        {
            string path = group;

            if (!string.IsNullOrEmpty(subgroup))
                path += "/" + subgroup;

            LScriptSettings.GroupCollapsed[path] = expanded;
        }

        private static void LoadLScriptSettings()
        {
            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "lscript.json");

            try
            {
                if (File.Exists(path))
                {
                    LScriptSettings = JsonSerializer.Deserialize(File.ReadAllText(path), LScriptJsonContext.Default.LScriptSettings);

                    for (int i = 0; i < LScriptSettings.CharAutoStartScripts.Count; i++)
                    {
                        KeyValuePair<string, List<string>> val = LScriptSettings.CharAutoStartScripts.ElementAt(i);
                        val.Value.RemoveAll(script => LoadedScripts.All(s => s.FileName != script));
                    }

                    LScriptSettings.GlobalAutoStartScripts.RemoveAll(script => LoadedScripts.All(s => s.FileName != script));

                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Unexpected error: {ex}");
            }

            LScriptSettings = new LScriptSettings();
        }

        private static void SaveScriptSettings()
        {
            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "lscript.json");

            string json = JsonSerializer.Serialize(LScriptSettings, LScriptJsonContext.Default.LScriptSettings);

            try
            {
                File.WriteAllText(path, json);
            }
            catch (Exception e)
            {
                Log.Error($"Error saving lscript settings: {e}");
            }
        }

        public static void Unload()
        {
            while (RunningScripts.Count > 0)
                StopScript(RunningScripts[0]);

            PyThreads.Clear();

            SaveScriptSettings();

            _enabled = false;
        }

        public static void PlayScript(ScriptFile script)
        {
            if (script == null) return;

            if (RunningScripts.Contains(script)) //Already playing
                return;

            if (script.ScriptThread == null || !script.ScriptThread.IsAlive)
            {
                script.ReadFromFile();

                // Route to correct executor based on script type
                if (script.Type == ScriptFile.ScriptType.CSharp)
                    script.ScriptThread = new Thread(() => ExecuteCSharpScript(script));
                else
                    script.ScriptThread = new Thread(() => ExecutePythonScript(script));

                if(!PyThreads.TryAdd(script.ScriptThread.ManagedThreadId, script))
                    PyThreads[script.ScriptThread.ManagedThreadId] = script;

                script.ScriptThread.Start();
            }

            RunningScripts.Add(script);
        }

        private static void ExecutePythonScript(ScriptFile script)
        {
            script.SetupPythonEngine();
            script.SetupPythonScope();

            try
            {
                ScriptSource source = script.PythonEngine.CreateScriptSourceFromString(script.FileContentsJoined, script.FullPath, SourceCodeKind.File);
                source?.Execute(script.PythonScope);
            }
            catch (ThreadInterruptedException) { }
            catch (ThreadAbortException) { }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                ShowScriptError(script, e);
            }

            MainThreadQueue.EnqueueAction(() => { StopScript(script); });
        }

        private static void ExecuteCSharpScript(ScriptFile script)
        {
            try
            {
                script.SetupCSharpScript();
                script.SetupCSharpGlobals();

                // Execute with cancellation support
                Task<ScriptState<object>> task = script.CSharpCompiledScript.RunAsync(
                    new ScriptGlobals { GlobalApiInstance = script.ScopedApi },
                    script.ScopedApi.CancellationToken.Token
                );

                // Block thread until the script completes or is canceled
                task.Wait(script.ScopedApi.CancellationToken.Token);
            }
            catch (CompilationErrorException e)
            {
                ShowCSharpCompilationError(script, e);
            }
            catch (AggregateException ae) when (ae.InnerException is OperationCanceledException or ThreadInterruptedException or ThreadAbortException)
            {
                // Script was canceled via the stop button
            }
            catch (OperationCanceledException)
            {
                // Script was canceled
            }
            catch (ThreadInterruptedException) { }
            catch (ThreadAbortException) { }
            catch (Exception e)
            {
                ShowCSharpRuntimeError(script, e);
            }

            MainThreadQueue.EnqueueAction(() => { StopScript(script); });
        }

        /// <summary>
        /// Formats a script execution exception returned by IronPython/ScriptHost
        /// </summary>
        /// <param name="script">The script that triggered the error</param>
        /// <param name="e">The thrown error</param>
        private static void ShowScriptError(ScriptFile script, Exception e)
        {
            GameActions.Print(_world, $"Legion Script '{script.FileName}' encountered an error.", Constants.HUE_ERROR);

            ExceptionOperations eo = script.PythonEngine.GetService<ExceptionOperations>();
            if (eo != null)
            {
                string formattedEx = eo.FormatException(e);
                Log.Warn(formattedEx);

                Regex exParserRx = RegexHelper.GetRegex("File \"(?<filepath>.+?)\", line (?<lineno>\\d+)", RegexOptions.Compiled | RegexOptions.Multiline);

                MatchCollection matches = exParserRx.Matches(formattedEx);
                var errorLocations = new List<ScriptErrorLocation>();

                bool first = true;
                foreach (Match match in matches)
                {
                    string filePath = match.Groups["filepath"].Value;

                    // Skip internal IronPython frames (e.g. File "<string>", ...)
                    if (filePath.StartsWith("<"))
                        continue;

                    if (!int.TryParse(match.Groups["lineno"].Value, out int lineNumber))
                        continue;

                    string fileName = Path.GetFileName(filePath);
                    string lineContent = "";

                    if (TryReadFileLines(filePath, out string[] fileLines))
                        lineContent = GetContents(fileLines, first? lineNumber + 1 : lineNumber); //Offset for removal of import API line

                    errorLocations.Add(new ScriptErrorLocation(fileName, filePath, lineNumber, lineContent));

                    first = false;
                }

                if (errorLocations.Count > 0)
                {
                    new ScriptErrorWindow(new ScriptErrorDetails(e.Message, errorLocations, script));
                }
                else
                    GameActions.Print(_world, formattedEx, Constants.HUE_ERROR);
            }
            else
                GameActions.Print(_world, e.Message, Constants.HUE_ERROR);

            if (e.InnerException != null)
                ShowScriptError(script, e.InnerException);
        }

        private static string GetContents(string[] lines, int line, int outerLines = 1)
        {
            var sb = new StringBuilder();
            int errorIndex = line - 1;

            for (int i = errorIndex - outerLines; i <= errorIndex + outerLines; i++)
            {
                if (i < 0 || i >= lines.Length)
                    continue;

                sb.AppendLine(i == errorIndex ? lines[i] + "  <-- Error line" : lines[i]);
            }

            return sb.ToString();
        }

        private static bool TryReadFileLines(string filePath, out string[] lines)
        {
            try
            {
                lines = File.ReadAllText(filePath).Split("\n");
                return true;
            }
            catch
            {
                lines = null;
                return false;
            }
        }

        private static void ShowCSharpCompilationError(ScriptFile script, CompilationErrorException e)
        {
            GameActions.Print(_world, $"Legion Script '{script.FileName}' has compilation errors.", Constants.HUE_ERROR);

            var errorLocations = new List<ScriptErrorLocation>();

            foreach (Diagnostic diagnostic in e.Diagnostics)
            {
                if (diagnostic.Severity != DiagnosticSeverity.Error)
                    continue;

                FileLinePositionSpan lineSpan = diagnostic.Location.GetLineSpan();
                // Since we're injecting code into the script, we need to account for the actual user code's start line
                int lineNumber = lineSpan.StartLinePosition.Line - script.UserCodeStartLine;

                string lineContent = "";
                if (TryReadFileLines(script.FullPath, out string[] fileLines))
                    lineContent = GetContents(fileLines, lineNumber);

                errorLocations.Add(new ScriptErrorLocation(
                    script.FileName,
                    script.FullPath,
                    lineNumber,
                    lineContent
                ));

                Log.Warn($"{script.FileName}({lineNumber}): {diagnostic.GetMessage()}");
            }

            if (errorLocations.Count > 0)
            {
                string errorMsg = string.Join("\n", e.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.GetMessage()));

                new ScriptErrorWindow(new ScriptErrorDetails(errorMsg, errorLocations, script));
            }
            else
            {
                GameActions.Print(_world, e.Message, Constants.HUE_ERROR);
            }
        }

        private static void ShowCSharpRuntimeError(ScriptFile script, Exception e)
        {
            GameActions.Print(_world, $"Legion Script '{script.FileName}' encountered a runtime error.", Constants.HUE_ERROR);

            // Unwrap AggregateException if present
            Exception actualException = e;
            if (e is AggregateException { InnerException: not null } ae)
                actualException = ae.InnerException;

            Log.Warn($"C# Script Error: {actualException}");

            var errorLocations = new List<ScriptErrorLocation>();
            var stackTrace = new StackTrace(actualException, true);

            foreach (StackFrame frame in stackTrace.GetFrames())
            {
                string fileName = frame.GetFileName();
                if (string.IsNullOrEmpty(fileName))
                    continue;

                // Only show frames from the script file
                if (!fileName.Equals(script.FullPath, StringComparison.OrdinalIgnoreCase))
                    continue;

                // We have to account for the hidden injected code here, in terms of the actual line numbers
                int lineNumber = frame.GetFileLineNumber() - script.UserCodeStartLine + 2;
                if (lineNumber <= 0)
                    continue;

                string lineContent = "";
                if (TryReadFileLines(fileName, out string[] fileLines))
                    lineContent = GetContents(fileLines, lineNumber);

                errorLocations.Add(new ScriptErrorLocation(
                    Path.GetFileName(fileName),
                    fileName,
                    lineNumber,
                    lineContent
                ));
            }

            if (errorLocations.Count > 0)
            {
                new ScriptErrorWindow(new ScriptErrorDetails(actualException.Message, errorLocations, script));
            }
            else
            {
                GameActions.Print(_world, actualException.Message, Constants.HUE_ERROR);
            }
        }

        public static void StopScript(ScriptFile script)
        {
            if (script == null) return;

            RunningScripts.Remove(script);

            if (script.ScriptThread is { IsAlive: true })
            {
                if (script.ScopedApi != null)
                {
                    script.ScopedApi.StopRequested = true;
                    script.ScopedApi.CancellationToken.Cancel();
                }

                if (script.PythonEngine != null)
                    script.PythonEngine.Runtime.Shutdown();

                script.ScriptThread.Interrupt();
            }
            else
            {
                if (script.ScriptThread != null)
                    PyThreads.Remove(script.ScriptThread.ManagedThreadId);

                // Route to correct cleanup based on script type
                if (script.Type == ScriptFile.ScriptType.CSharp)
                    script.CSharpScriptStopped();
                else
                    script.PythonScriptStopped();

                script.ScriptThread = null;
            }
        }

        public static void DownloadApiPy() => Task.Run
            (() =>
                {
                    try
                    {
                        var client = new System.Net.WebClient();
                        string api = client.DownloadString(new Uri("https://raw.githubusercontent.com/PlayTazUO/TazUO/refs/heads/dev/src/ClassicUO.Client/LegionScripting/docs/API.py"));
                        File.WriteAllText(Path.Combine(CUOEnviroment.ExecutablePath, "LegionScripts", "API.py"), api);
                        MainThreadQueue.EnqueueAction(() => { GameActions.Print(_world, "Updated API!"); });
                    }
                    catch (Exception ex)
                    {
                        MainThreadQueue.EnqueueAction(() => { GameActions.Print(_world, "Failed to update the API..", 32); });
                        Log.Error(ex.ToString());
                    }
                    CreateCSScriptingProjFiles();
                }
            );

        /// <summary>
        /// Solution for providing a ready-to-go project for players scripting with CS
        /// </summary>
        public static void CreateCSScriptingProjFiles()
        {
            const string scriptContext = """
                                   global using static ScriptContext;

                                   using ClassicUO.LegionScripting;

                                   /// <summary>
                                   /// Provides the global API instance for script IntelliSense.
                                   /// At runtime, the actual API is injected by TazUO's scripting engine.
                                   /// </summary>
                                   public static class ScriptContext
                                   {
                                       public static LegionAPI API { get; } = null!;
                                   }
                                   """;
            const string csProj = """
                                  <Project Sdk="Microsoft.NET.Sdk">

                                    <!--
                                      This project provides IntelliSense for C# scripts.
                                      Build errors are EXPECTED and can be ignored - scripts run independently in TazUO.
                                    -->

                                    <PropertyGroup>
                                      <TargetFramework>net10.0</TargetFramework>
                                      <ImplicitUsings>enable</ImplicitUsings>
                                      <Nullable>disable</Nullable>
                                      <IsPackable>false</IsPackable>
                                      <OutputType>Library</OutputType>
                                      <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
                                      <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
                                    </PropertyGroup>

                                    <!-- Reference game assemblies for API IntelliSense -->
                                    <ItemGroup>
                                      <Reference Include="TazUO">
                                        <HintPath>../TazUO.dll</HintPath>
                                        <Private>false</Private>
                                      </Reference>
                                      <Reference Include="FNA">
                                        <HintPath>../FNA.dll</HintPath>
                                        <Private>false</Private>
                                      </Reference>
                                    </ItemGroup>

                                    <!-- Include all scripts for IntelliSense (build errors are normal) -->
                                    <ItemGroup>
                                      <Compile Include="**/*.cs"/>
                                    </ItemGroup>

                                    <!-- Common imports for all scripts -->
                                    <ItemGroup>
                                      <Using Include="System" />
                                      <Using Include="System.Linq" />
                                      <Using Include="System.Collections.Generic" />
                                      <Using Include="System.Threading.Tasks" />
                                      <Using Include="ClassicUO.LegionScripting" />
                                      <Using Include="ClassicUO.LegionScripting.ApiClasses" />
                                      <Using Include="ScriptContext" Static="true" />
                                    </ItemGroup>

                                  </Project>
                                  """;

            try
            {
                File.WriteAllText(Path.Combine(CUOEnviroment.ExecutablePath, "LegionScripts", "_ScriptContext.cs"), scriptContext);
                File.WriteAllText(Path.Combine(CUOEnviroment.ExecutablePath, "LegionScripts", "LegionScripts.csproj"), csProj);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
    }
}
