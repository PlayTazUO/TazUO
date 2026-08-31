using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Utility.Logging;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using System.Text.RegularExpressions;
using ClassicUO.Game.UI.MyraWindows;
using ClassicUO.LegionScripting.ApiClasses;
using ClassicUO.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using SourceCodeKind = Microsoft.Scripting.SourceCodeKind;

namespace ClassicUO.LegionScripting
{
    internal static class LegionScripting
    {
        public static string ScriptPath;
        public static LScriptSettings LScriptSettings { get; private set; }
        public static readonly List<ScriptFile> LoadedScripts = [];
        public static List<ScriptFile> RunningScripts { get; } = [];
        public static readonly Dictionary<int, ScriptFile> PyThreads = new();

        public static event EventHandler<ScriptFile> ScriptStarted;
        public static event EventHandler<ScriptFile> ScriptStopped;

        private static bool _enabled, _loaded;
        private static World _world;

        private const int STOP_THREAD_DETACH_TIMEOUT_MS = 2000;

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

            foreach (ScriptFile sf in LoadedScripts)
                sf.ReadFromFile();
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
                else if (file.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) && File.Exists(file))
                    HandleScriptsInZip(file, loadedScripts);
                else if (Directory.Exists(file)) groups.Add(file);
            }

            return groups;
        }

        private static void HandleScriptsInZip(string zipPath, HashSet<string> loadedScripts)
        {
            try
            {
                using ZipArchive archive = ZipFile.OpenRead(zipPath);

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue; // directory entry

                    string entryName = entry.FullName.Replace('\\', '/');
                    string ext = Path.GetExtension(entry.Name);

                    if (!ext.Equals(".py", StringComparison.OrdinalIgnoreCase) &&
                        !ext.Equals(".cs", StringComparison.OrdinalIgnoreCase)) continue;

                    string[] segments = entryName.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (segments.Length == 0 || segments.Length > 3) continue;

                    // Skip if any path segment (dir or file) starts with _ or .
                    bool hasHiddenSegment = false;
                    foreach (string seg in segments)
                        if (seg.StartsWith("_") || seg.StartsWith(".")) { hasHiddenSegment = true; break; }
                    if (hasHiddenSegment || entry.Name == "API.py") continue;

                    string group    = segments.Length >= 2 ? segments[0] : string.Empty;
                    string subGroup = segments.Length == 3 ? segments[1] : string.Empty;

                    string syntheticKey = $"{zipPath}::{entryName}";
                    if (loadedScripts.Contains(syntheticKey)) continue;

                    LoadedScripts.Add(new ZipScriptFile(_world, zipPath, entryName, group, subGroup));
                    loadedScripts.Add(syntheticKey);
                }

                ClassicUO.Assets.ExternalImageLoader.Instance.RegisterZipPNGs(archive);
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading scripts from zip '{zipPath}': {ex}");
            }
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
            LScriptSettings = JsonSave<LScriptSettings>.Load();

            for (int i = 0; i < LScriptSettings.CharAutoStartScripts.Count; i++)
            {
                KeyValuePair<string, List<string>> val = LScriptSettings.CharAutoStartScripts.ElementAt(i);
                val.Value.RemoveAll(script => LoadedScripts.All(s => s.FileName != script));
            }

            LScriptSettings.GlobalAutoStartScripts.RemoveAll(script => LoadedScripts.All(s => s.FileName != script));
        }

        private static void SaveScriptSettings()
        {
            LScriptSettings?.Save();
        }

        public static void Unload()
        {
            while (RunningScripts.Count > 0)
                StopScript(RunningScripts[0], force: true);

            PyThreads.Clear();

            SaveScriptSettings();

            _enabled = false;
        }

        public static void PlayScript(ScriptFile script)
        {
            if (script == null) return;

            if (RunningScripts.Contains(script)) //Already playing
                return;

            // Thread is alive but not registered - a stop is in flight for this script and it is
            // being torn down (or is a pending detach). Don't start over it.
            if (script.ScriptThread is { IsAlive: true })
                return;

            // A previous run may still be stuck in the background (see StopScript). It is inert but
            // would double-execute if we started a fresh thread, so refuse until it dies.
            if (script.IsZombie)
            {
                GameActions.Print(_world, $"Script '{script.FileName}' is still running in the background and cannot be restarted until it exits.", Constants.HUE_WARN);
                return;
            }

            if (script.ScriptThread == null || !script.ScriptThread.IsAlive)
            {
                script.ReadFromFile();

                WarnAboutUninterruptibleLoops(script);

                script.ZombieThread = null;

                // Route to correct executor based on script type
                if (script.Type == ScriptFile.ScriptType.CSharp)
                    script.ScriptThread = new Thread(() => ExecuteCSharpScript(script)) { Name = $"Legion: {script.FileName}", IsBackground = true };
                else
                    script.ScriptThread = new Thread(() => ExecutePythonScript(script)) { Name = $"Legion: {script.FileName}", IsBackground = true };

                if(!PyThreads.TryAdd(script.ScriptThread.ManagedThreadId, script))
                    PyThreads[script.ScriptThread.ManagedThreadId] = script;

                script.ScriptThread.Start();
            }

            RunningScripts.Add(script);
            ScriptStarted?.Invoke(null, script);
        }

        /// <summary>
        /// Warns when a script containing an unbounded loop is started. Such loops only stop if they
        /// check API.StopRequested (or block via API.Pause/API.ProcessCallbacks), since a pure CPU
        /// loop never blocks and therefore can't be interrupted by the stop logic.
        /// </summary>
        private static void WarnAboutUninterruptibleLoops(ScriptFile script)
        {
            string code = script.FileContentsJoined;

            if (code.IndexOf("while True", StringComparison.OrdinalIgnoreCase) >= 0 ||
                code.IndexOf("while (true)", StringComparison.OrdinalIgnoreCase) >= 0 ||
                code.IndexOf("while(true)", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                GameActions.Print(_world, $"Script '{script.FileName}' contains an unbounded 'while' loop. Change it to {(script.Type == ScriptFile.ScriptType.Python ? "while API.StopRequested:" : "while (API.StopRequested)")}", Constants.HUE_WARN);
            }
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
                try
                {
                    ShowScriptError(script, e);
                }
                // Formatting the error runs IronPython dynamic code that takes internal locks.
                // If the script is stopped at that exact moment (StopScript -> Thread.Interrupt),
                // the interrupt surfaces here as a ThreadInterruptedException/ThreadAbortException.
                // Swallow it so tearing down an already-errored script never crashes the client.
                catch (ThreadInterruptedException) { }
                catch (ThreadAbortException) { }
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

                // Keep a reference so a task that ignores cancellation (a busy loop) is recognized
                // as a zombie and blocks a restart.
                script.CSharpRunTask = task;

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
            MainThreadQueue.EnqueueAction(() => GameActions.Print(_world, $"Legion Script '{script.FileName}' encountered an error.", Constants.HUE_ERROR));

            ExceptionOperations eo = script.PythonEngine?.GetService<ExceptionOperations>();
            if (eo != null)
            {
                string formattedEx = eo.FormatException(e);
                Log.Warn(formattedEx);

                Regex exParserRx = RegexHelper.GetRegex("File \"(?<filepath>.+?)\", line (?<lineno>\\d+)", RegexOptions.Compiled | RegexOptions.Multiline);

                MatchCollection matches = exParserRx.Matches(formattedEx);
                var errorLocations = new List<ScriptErrorLocation>();

                ScriptErrorLocation? last = null;

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

                    if (filePath.TryReadFileLines(out string[] fileLines))
                        lineContent = GetContents(fileLines, first? lineNumber - 1 : lineNumber);

                    var sel = new ScriptErrorLocation(fileName, filePath, lineNumber, lineContent);

                    if(last != null && !sel.Equals(last))
                        errorLocations.Add(sel);
                    
                    last = sel;

                    first = false;
                }

                if (errorLocations.Count > 0)
                    MainThreadQueue.EnqueueAction(() => { new ScriptErrorWindow(new ScriptErrorDetails(e.Message, errorLocations, script)); });
                else
                    MainThreadQueue.EnqueueAction(() => GameActions.Print(_world, formattedEx, Constants.HUE_ERROR));
            }
            else
                MainThreadQueue.EnqueueAction(() => GameActions.Print(_world, e.Message, Constants.HUE_ERROR));

            if (e.InnerException != null)
                ShowScriptError(script, e.InnerException);
        }

        /// <summary>
        /// Get file line + <paramref name="context"/> lines before and after for error line indication.
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="index"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static string GetContents(string[] lines, int index, int context = 1)
        {
            // Clamp the range to stay within the array bounds
            int start = Math.Max(0, index - context);
            int end = Math.Min(lines.Length - 1, index + context);

            var result = new List<string>();

            for (int i = start; i <= end; i++)
            {
                string text = lines[i];
                if (i == index) text += "  <-- Error line";
                result.Add(text);
            }

            return string.Join(Environment.NewLine, result);
        }

        private static void ShowCSharpCompilationError(ScriptFile script, CompilationErrorException e)
        {
            MainThreadQueue.EnqueueAction(() => GameActions.Print(_world, $"Legion Script '{script.FileName}' has compilation errors.", Constants.HUE_ERROR));

            var errorLocations = new List<ScriptErrorLocation>();

            foreach (Diagnostic diagnostic in e.Diagnostics)
            {
                if (diagnostic.Severity != DiagnosticSeverity.Error)
                    continue;

                FileLinePositionSpan lineSpan = diagnostic.Location.GetLineSpan();
                // Since we're injecting code into the script, we need to account for the actual user code's start line
                int lineNumber = lineSpan.StartLinePosition.Line - script.UserCodeStartLine;

                string lineContent = "";
                if (script.FullPath.TryReadFileLines(out string[] fileLines))
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

                MainThreadQueue.EnqueueAction(() => { new ScriptErrorWindow(new ScriptErrorDetails(errorMsg, errorLocations, script)); });
            }
            else
            {
                MainThreadQueue.EnqueueAction(() => GameActions.Print(_world, e.Message, Constants.HUE_ERROR));
            }
        }

        private static void ShowCSharpRuntimeError(ScriptFile script, Exception e)
        {
            MainThreadQueue.EnqueueAction(() => GameActions.Print(_world, $"Legion Script '{script.FileName}' encountered a runtime error.", Constants.HUE_ERROR));

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
                if (fileName.TryReadFileLines(out string[] fileLines))
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
                MainThreadQueue.EnqueueAction(() => { new ScriptErrorWindow(new ScriptErrorDetails(actualException.Message, errorLocations, script)); });
            }
            else
            {
                MainThreadQueue.EnqueueAction(() => GameActions.Print(_world, actualException.Message, Constants.HUE_ERROR));
            }
        }

        public static void StopScript(ScriptFile script, bool force = false)
        {
            if (script == null) return;

            LegionAPI api = script.ScopedApi;

            // If the script registered an OnStop callback, give it a chance to run before we
            // tear everything down. The callback only runs while the script keeps calling
            // API.ProcessCallbacks, so we can't force it - we wait for it to complete or for a
            // maximum of 5 seconds, whichever comes first. Skipped when force is requested
            // (e.g. during client shutdown).
            if (!force && script.ScriptThread is { IsAlive: true } && api is { StopRequested: false } && api.HasPendingStopCallback)
            {
                if (api.BeginStopCallback())
                    WaitForStopCallbackThenStop(script);

                return;
            }

            RunningScripts.Remove(script);

            if (script.ScriptThread is { IsAlive: true })
            {
                if (api != null)
                {
                    api.StopRequested = true;
                    api.CancellationToken.Cancel();
                }

                if (script.PythonEngine != null)
                    script.PythonEngine.Runtime.Shutdown();

                script.ScriptThread.Interrupt();

                // Interrupt only lands while the script is blocked (Pause, OnMain, ...); a script
                // stuck in a pure CPU loop never blocks and can't be interrupted. Give it a bounded
                // grace period to exit on its own - without blocking the main thread - then detach it
                // so the manager state stays consistent. The abandoned thread is inert (API disposed,
                // main-thread calls no-op on the canceled token) and dies with the process. Skipped
                // on force (client shutdown).
                if (!force)
                    ScheduleDetachCheck(script);
            }
            else if (script.ScriptThread != null)
            {
                DetachScript(script, null, warn: false);
            }
        }

        /// <summary>
        /// Schedules a main-thread check (without blocking it) for a script that was just asked to
        /// stop. If the thread is still alive after the grace period - stuck in a loop the interrupt
        /// couldn't land in - it is detached. A thread that exits on its own is cleaned up by its own
        /// follow-up stop, so nothing is done here.
        /// </summary>
        private static void ScheduleDetachCheck(ScriptFile script)
        {
            var timer = new System.Timers.Timer(STOP_THREAD_DETACH_TIMEOUT_MS) { AutoReset = false };

            timer.Elapsed += (_, _) =>
            {
                timer.Dispose();

                MainThreadQueue.EnqueueAction(() =>
                {
                    // Thread already exited and was cleaned up by its follow-up stop.
                    if (script.ScriptThread == null || !script.ScriptThread.IsAlive)
                        return;

                    DetachScript(script, script.ScriptThread, warn: true);
                });
            };

            timer.Start();
        }

        /// <summary>
        /// Tears down a script's runtime state and marks it stopped. Used after the script thread has
        /// exited, or - with a <paramref name="zombieThread"/> - after a bounded wait when a stuck
        /// thread refuses to exit. The zombie is recorded so the script can't be restarted over it.
        /// </summary>
        /// <param name="script">The script being stopped</param>
        /// <param name="zombieThread">The still-alive thread being abandoned, or null if the thread already exited</param>
        /// <param name="warn">Whether to tell the user the script kept running in the background</param>
        private static void DetachScript(ScriptFile script, Thread zombieThread, bool warn)
        {
            if (zombieThread != null)
            {
                PyThreads.Remove(zombieThread.ManagedThreadId);
                script.ZombieThread = zombieThread;
            }
            else if (script.ScriptThread != null)
                PyThreads.Remove(script.ScriptThread.ManagedThreadId);

            // Route to correct cleanup based on script type
            if (script.Type == ScriptFile.ScriptType.CSharp)
                script.CSharpScriptStopped();
            else
                script.PythonScriptStopped();

            script.ScriptThread = null;
            ScriptStopped?.Invoke(null, script);

            if (warn)
                GameActions.Print(_world, $"Script '{script.FileName}' did not stop and keeps running in the background. It is inert and will exit when the client closes or it exits itself.", Constants.HUE_WARN);
        }

        /// <summary>
        /// Waits (without blocking the main thread) for a script's OnStop callback to finish,
        /// or for a maximum of 5 seconds, then performs the actual stop.
        /// </summary>
        private static void WaitForStopCallbackThenStop(ScriptFile script)
        {
            LegionAPI api = script.ScopedApi;
            DateTime start = DateTime.UtcNow;

            var timer = new System.Timers.Timer(100) { AutoReset = true };

            timer.Elapsed += (_, _) =>
            {
                bool completed = api == null || api.OnStopCompleted;
                bool timedOut = (DateTime.UtcNow - start).TotalSeconds >= 5;

                if (!completed && !timedOut)
                    return;

                timer.Stop();
                timer.Dispose();

                MainThreadQueue.EnqueueAction(() =>
                {
                    // The script may have already stopped on its own during the grace period.
                    if (!RunningScripts.Contains(script))
                        return;

                    // Ensure the delayed path is not taken again on the follow-up stop.
                    if (script.ScopedApi != null)
                        script.ScopedApi.StopRequested = true;

                    StopScript(script);
                });
            };

            timer.Start();
        }

        /// <summary>
        /// Download the latest API.py file for legion scripting.
        /// </summary>
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

                    string pybuiltins = Path.Combine(CUOEnviroment.ExecutablePath, "LegionScripts", "__builtins__.py");
                    if (!File.Exists(pybuiltins))
                    {
                        try
                        {
                            File.WriteAllText(pybuiltins, "import API");
                        }
                        catch
                        {
                            Log.ErrorDebug("Unable to create builtins file.");
                        }
                    }

                    CreateCSScriptingProjFiles();
                }
            );

        /// <summary>
        /// Solution for providing a ready-to-go project for players scripting with CS
        /// </summary>
        private static void CreateCSScriptingProjFiles()
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

            FileSystemHelper.WriteAllTextSafe(Path.Combine(CUOEnviroment.ExecutablePath, "LegionScripts", "_ScriptContext.cs"), scriptContext);
            FileSystemHelper.WriteAllTextSafe(Path.Combine(CUOEnviroment.ExecutablePath, "LegionScripts", "LegionScripts.csproj"), csProj);
        }
    }
}
