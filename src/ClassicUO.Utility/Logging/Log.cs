// SPDX-License-Identifier: BSD-2-Clause

using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace ClassicUO.Utility.Logging;

public class Log
{
    private static Logger _logger;

    public static void Start(LogTypes logTypes, LogFile logFile = null)
    {
        _logger ??= new Logger { LogTypes = logTypes };

        _logger.Start(logFile);
    }

    public static void Stop()
    {
        _logger?.Stop();
        _logger = null;
    }

    public static void Resume(LogTypes logTypes) => _logger?.LogTypes = logTypes;

    public static void Pause() => _logger?.LogTypes = LogTypes.None;

    // Records the message into the rolling in-memory history (used by the Log History
    // window) and forwards it to the console logger. The history is kept independent of
    // the logger's LogTypes filter so it always reflects everything that was logged.
    private static void Write(LogTypes type, string text)
    {
        LogHistory.Add(type, text);
        _logger?.Message(type, text);
    }

    public static void Trace(
        string text,
        [CallerFilePath] string callerPath = "Unk",
        [CallerMemberName] string callerName = "Unk"
    ) => Write(LogTypes.Trace, $"[{Path.GetFileNameWithoutExtension(callerPath)}.{callerName}] {text}");

    [Conditional("DEBUG")]
    public static void TraceDebug(
        string text,
        [CallerFilePath] string callerPath = "Unk",
        [CallerMemberName] string callerName = "Unk"
    ) => Trace(text, callerPath, callerName);

    [Conditional("DEBUG")]
    public static void Debug(
        string text,
        [CallerFilePath] string callerPath = "Unk",
        [CallerMemberName] string callerName = "Unk"
    ) => Write(LogTypes.Debug, $"[{Path.GetFileNameWithoutExtension(callerPath)}.{callerName}] {text}");


    public static void Info(
        string text,
        [CallerFilePath] string callerPath = "Unk",
        [CallerMemberName] string callerName = "Unk"
    ) => Write(LogTypes.Info, $"[{Path.GetFileNameWithoutExtension(callerPath)}.{callerName}] {text}");

    public static void Warn(
        string text,
        [CallerFilePath] string callerPath = "Unk",
        [CallerMemberName] string callerName = "Unk"
    ) => Write(LogTypes.Warning, $"[{Path.GetFileNameWithoutExtension(callerPath)}.{callerName}] {text}");

    [Conditional("DEBUG")]
    public static void WarnDebug(
        string text,
        [CallerFilePath] string callerPath = "Unk",
        [CallerMemberName] string callerName = "Unk"
    ) => Warn(text, callerPath, callerName);

    public static void Error(
        string text,
        [CallerFilePath] string callerPath = "Unk",
        [CallerMemberName] string callerName = "Unk"
    ) => Write(LogTypes.Error, $"[{Path.GetFileNameWithoutExtension(callerPath)}.{callerName}] {text}");

    [Conditional("DEBUG")]
    public static void ErrorDebug(
        string text,
        [CallerFilePath] string callerPath = "Unk",
        [CallerMemberName] string callerName = "Unk"
    ) => Error(text, callerPath, callerName);

    public static void Panic(
        string text,
        [CallerFilePath] string callerPath = "Unk",
        [CallerMemberName] string callerName = "Unk"
    ) => Error(text, callerPath, callerName);

    public static void NewLine() => _logger?.NewLine();

    public static void Clear() => _logger?.Clear();

    public static void PushIndent() => _logger?.PushIndent();

    public static void PopIndent() => _logger?.PopIndent();
}
