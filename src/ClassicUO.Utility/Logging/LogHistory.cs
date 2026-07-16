// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Utility.Logging
{
    /// <summary>
    /// A single recorded log message, kept in the in-memory rolling history.
    /// </summary>
    public readonly struct LogEntry
    {
        public readonly DateTime Timestamp;
        public readonly LogTypes Type;
        public readonly string Text;

        public LogEntry(DateTime timestamp, LogTypes type, string text)
        {
            Timestamp = timestamp;
            Type = type;
            Text = text;
        }

        public override string ToString() => $"{Timestamp:yyyy-MM-dd HH:mm:ss} | {Type,-7} | {Text}";
    }

    /// <summary>
    /// Keeps the most recent <see cref="MaxEntries"/> log messages in a rolling
    /// in-memory buffer, independent of the console logger and any log-type filter.
    /// Used by the developer Log History window.
    /// </summary>
    public static class LogHistory
    {
        public const int MaxEntries = 1000;

        private static readonly Queue<LogEntry> _entries = new(MaxEntries);
        private static readonly object _sync = new();
        private static long _revision;

        /// <summary>
        /// Monotonically increasing counter bumped on every change to the history.
        /// Lets consumers cheaply detect when they need to refresh without diffing.
        /// </summary>
        public static long Revision
        {
            get
            {
                lock (_sync)
                    return _revision;
            }
        }

        public static void Add(LogTypes type, string text)
        {
            lock (_sync)
            {
                if (_entries.Count >= MaxEntries)
                    _entries.Dequeue();

                _entries.Enqueue(new LogEntry(DateTime.Now, type, text));
                _revision++;
            }
        }

        /// <summary>
        /// Returns a point-in-time snapshot of all stored entries, oldest first.
        /// </summary>
        public static LogEntry[] Snapshot()
        {
            lock (_sync)
            {
                return _entries.ToArray();
            }
        }

        public static void Clear()
        {
            lock (_sync)
            {
                _entries.Clear();
                _revision++;
            }
        }

        /// <summary>
        /// Builds a plain-text dump of every stored entry, oldest first.
        /// </summary>
        public static string ToText()
        {
            var sb = new StringBuilder();

            foreach (LogEntry entry in Snapshot())
                sb.AppendLine(entry.ToString());

            return sb.ToString();
        }
    }
}
