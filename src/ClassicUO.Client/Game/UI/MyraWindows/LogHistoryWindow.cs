using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Assets;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows
{
    /// <summary>
    /// Developer window that displays the rolling in-memory log history captured by
    /// <see cref="LogHistory"/> in a single read-only text field. Only the most recent
    /// <see cref="PageSize"/> entries are rendered up front; scrolling to the top loads
    /// another page of older entries, keeping the Myra text layout cheap.
    /// </summary>
    public class LogHistoryWindow : MyraControl
    {
        private const uint UPDATE_INTERVAL = 500;
        private const int PageSize = 50;
        private const int ScrollEdgeTolerance = 4;

        // Severity types shown as filter toggles. Panic is logged through Error, so
        // it shares the Error toggle and is not listed separately.
        private static readonly LogTypes[] _filterableTypes =
        {
            LogTypes.Trace, LogTypes.Debug, LogTypes.Info, LogTypes.Warning, LogTypes.Error,
        };

        private readonly MyraInputBox _textBox;
        private readonly ScrollViewer _scrollViewer;
        private readonly MyraLabel _statusLabel;
        private uint _lastUpdate;
        private long _lastRevision = -1;

        // How many of the most-recent filtered entries are currently rendered, and whether
        // older ones remain to be loaded by scrolling up.
        private int _visibleCount = PageSize;
        private bool _hasMore;

        // Bitmask of which severities are currently shown. Defaults to everything.
        private LogTypes _enabledTypes = LogTypes.All;

        public static void Show()
        {
            foreach (IGui g in UIManager.Gumps)
            {
                if (g is LogHistoryWindow w)
                {
                    w.BringOnTop();
                    return;
                }
            }
            UIManager.Add(new LogHistoryWindow());
        }

        public LogHistoryWindow() : base("Log History")
        {
            var buttons = new HorizontalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
            buttons.Widgets.Add(new MyraButton("Copy Output", CopyToClipboard));
            buttons.Widgets.Add(new MyraButton("Refresh", ResetToLatest));
            buttons.Widgets.Add(new MyraButton("Clear", () =>
            {
                LogHistory.Clear();
                ResetToLatest();
            }));

            var filters = new HorizontalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
            filters.Widgets.Add(new MyraLabel("Show:", MyraLabel.TextStyle.P));
            foreach (LogTypes type in _filterableTypes)
            {
                LogTypes captured = type;
                filters.Widgets.Add(MyraCheckButton.CreateWithCallback(
                    true,
                    isChecked =>
                    {
                        if (isChecked)
                            _enabledTypes |= captured;
                        else
                            _enabledTypes &= ~captured;

                        ResetToLatest();
                    },
                    type.ToString()));
            }

            _statusLabel = new MyraLabel(string.Empty, MyraLabel.TextStyle.P);

            SpriteFontBase monoFont = TrueTypeLoader.Instance.GetFont(EmbeddedFontNames.ROBOTO_MONO, 16);

            _textBox = new MyraInputBox
            {
                Text = "",
                Multiline = true,
                Readonly = true,
                Font = monoFont,
                Background = new SolidBrush(new Color(0, 0, 0, 75)),
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            _scrollViewer = new ScrollViewer
            {
                MinWidth = 550,
                MinHeight = 350,
                MaxWidth = 900,
                MaxHeight = 600,
                Content = _textBox,
            };

            var root = new VerticalStackPanel
            {
                Spacing = MyraStyle.STANDARD_SPACING,
                Padding = new Thickness(4),
            };
            root.Widgets.Add(buttons);
            root.Widgets.Add(filters);
            root.Widgets.Add(_statusLabel);
            root.Widgets.Add(_scrollViewer);

            SetRootContent(root);
            CenterInViewPort();

            ResetToLatest();
        }

        private bool IsTypeEnabled(LogTypes type)
        {
            // Panic is recorded through Error and has no dedicated toggle.
            if (type == LogTypes.Panic)
                type = LogTypes.Error;

            return (_enabledTypes & type) == type;
        }

        private List<LogEntry> GetFilteredEntries()
        {
            LogEntry[] entries = LogHistory.Snapshot();
            var filtered = new List<LogEntry>(entries.Length);

            foreach (LogEntry entry in entries)
            {
                if (IsTypeEnabled(entry.Type))
                    filtered.Add(entry);
            }

            return filtered;
        }

        /// <summary>
        /// Renders the newest <see cref="_visibleCount"/> filtered entries into the text
        /// field and refreshes the status line. Does not touch the scroll position.
        /// </summary>
        private void RenderText()
        {
            List<LogEntry> filtered = GetFilteredEntries();
            int total = filtered.Count;
            int start = Math.Max(0, total - _visibleCount);
            _hasMore = start > 0;

            var sb = new StringBuilder();
            for (int i = start; i < total; i++)
                sb.AppendLine(filtered[i].ToString());

            _textBox.Text = sb.ToString();

            int shown = total - start;
            _statusLabel.Text = _hasMore
                ? $"Showing latest {shown} of {total} — scroll up to load more (max {LogHistory.MaxEntries})"
                : $"Showing {shown} of {total} entries (max {LogHistory.MaxEntries})";
        }

        /// <summary>
        /// Resets the view to the most recent page and snaps to the newest entry. Used on
        /// open, refresh, clear and filter changes.
        /// </summary>
        private void ResetToLatest()
        {
            _visibleCount = PageSize;
            _lastRevision = LogHistory.Revision;
            RenderText();
            ScrollToBottom();
        }

        /// <summary>
        /// Loads another page of older entries, keeping the viewport anchored to the same
        /// content by offsetting the scroll position by the height added above it.
        /// </summary>
        private void LoadOlder()
        {
            int oldMax = _scrollViewer.ScrollMaximum.Y;

            _visibleCount += PageSize;
            // Fold in any entries logged since the last render so this snapshot stays current.
            _lastRevision = LogHistory.Revision;
            RenderText();

            _scrollViewer.UpdateArrange();
            int newMax = _scrollViewer.ScrollMaximum.Y;

            int newY = _scrollViewer.ScrollPosition.Y + (newMax - oldMax);
            _scrollViewer.ScrollPosition = new Point(_scrollViewer.ScrollPosition.X, newY);
        }

        private bool IsAtTop() =>
            _scrollViewer.ScrollMaximum.Y > 0 && _scrollViewer.ScrollPosition.Y <= ScrollEdgeTolerance;

        private bool IsAtBottom()
        {
            int max = _scrollViewer.ScrollMaximum.Y;
            return max <= 0 || _scrollViewer.ScrollPosition.Y >= max - ScrollEdgeTolerance;
        }

        private void ScrollToBottom()
        {
            // Ensure ScrollMaximum reflects the freshly rendered content before snapping.
            _scrollViewer.UpdateArrange();
            _scrollViewer.ScrollPosition = new Point(_scrollViewer.ScrollPosition.X, _scrollViewer.ScrollMaximum.Y);
        }

        private void CopyToClipboard()
        {
            // Copy every filtered entry, not just the currently-rendered page.
            List<LogEntry> filtered = GetFilteredEntries();

            var sb = new StringBuilder();
            foreach (LogEntry entry in filtered)
                sb.AppendLine(entry.ToString());

            Clipboard.SetClipboardText(filtered.Count > 0 ? sb.ToString() : "No log entries to copy.");
            GameActions.Print("Copied log history to clipboard!", Constants.HUE_SUCCESS);
        }

        public override void Update()
        {
            base.Update();

            if (IsDisposed)
                return;

            // Scrolling to the top pages in older entries on demand.
            if (_hasMore && IsAtTop())
            {
                LoadOlder();
                return;
            }

            if (Time.Ticks - _lastUpdate > UPDATE_INTERVAL)
            {
                _lastUpdate = Time.Ticks;

                // Live-tail new entries only while parked at the bottom; if the user has
                // scrolled up to read, leave their view untouched.
                if (IsAtBottom() && LogHistory.Revision != _lastRevision)
                {
                    _lastRevision = LogHistory.Revision;
                    RenderText();
                    ScrollToBottom();
                }
            }
        }
    }
}
