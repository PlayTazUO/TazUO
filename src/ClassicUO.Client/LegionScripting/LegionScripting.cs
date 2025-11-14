using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using ClassicUO.LegionScripting.PyClasses;
using Microsoft.Scripting;

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
        }

        private static void EventSink_JournalEntryAdded(object sender, JournalEntry e)
        {
            if (e is null)
                return;

            foreach (ScriptFile script in RunningScripts)
            {
                script?.ScopedApi?.JournalEntries.Enqueue(new PyJournalEntry(e));

                while (script?.ScopedApi?.JournalEntries.Count > ProfileManager.CurrentProfile.MaxJournalEntries) script.ScopedApi?.JournalEntries.TryDequeue(out _);
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

                if (file.EndsWith(".lscript") || file.EndsWith(".py"))
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

            if (script.PythonThread == null || !script.PythonThread.IsAlive)
            {
                script.ReadFromFile();
                script.PythonThread = new Thread(() => ExecutePythonScript(script));

                if(!PyThreads.TryAdd(script.PythonThread.ManagedThreadId, script))
                    PyThreads[script.PythonThread.ManagedThreadId] = script;

                script.PythonThread.Start();
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
                ExceptionOperations eo = script.PythonEngine.GetService<ExceptionOperations>();
                string error = e.Message;
                if (eo != null)
                    error = eo.FormatException(e);

                GameActions.Print(_world, "Python Script Error:");
                GameActions.Print(_world, error);
                Log.Warn(e.ToString());
            }

            MainThreadQueue.EnqueueAction(() => { StopScript(script); });
        }

        public static void StopScript(ScriptFile script)
        {
            if (script == null) return;

            RunningScripts.Remove(script);

            if (script.PythonThread is { IsAlive: true })
            {
                if (script.ScopedApi != null)
                {
                    script.ScopedApi.StopRequested = true;
                    script.ScopedApi.CancellationToken.Cancel();
                }

                if (script.PythonEngine != null)
                    script.PythonEngine.Runtime.Shutdown();

                script.PythonThread.Interrupt();
            }
            else
            {
                if (script.PythonThread != null)
                    PyThreads.Remove(script.PythonThread.ManagedThreadId);
                script.PythonScriptStopped();
                script.PythonThread = null;
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

                }
            );
    }
}
