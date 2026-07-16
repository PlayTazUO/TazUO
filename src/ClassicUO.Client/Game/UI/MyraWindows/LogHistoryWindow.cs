using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows
{
    /// <summary>
    /// Developer window that displays the rolling in-memory log history captured by
    /// <see cref="LogHistory"/>, with a button to copy the whole output to the clipboard.
    /// </summary>
    public class LogHistoryWindow : MyraControl
    {
        private const uint UPDATE_INTERVAL = 500;

        // Severity types shown as filter toggles. Panic is logged through Error, so
        // it shares the Error toggle and is not listed separately.
        private static readonly LogTypes[] _filterableTypes =
        {
            LogTypes.Trace, LogTypes.Debug, LogTypes.Info, LogTypes.Warning, LogTypes.Error,
        };

        private readonly VerticalStackPanel _logPanel;
        private readonly ScrollViewer _scrollViewer;
        private readonly MyraLabel _statusLabel;
        private uint _lastUpdate;
        private long _lastRevision = -1;

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
            _logPanel = new VerticalStackPanel { Spacing = 0 };

            var buttons = new HorizontalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
            buttons.Widgets.Add(new MyraButton("Copy Output", CopyToClipboard));
            buttons.Widgets.Add(new MyraButton("Refresh", () => RebuildList(true)));
            buttons.Widgets.Add(new MyraButton("Clear", () =>
            {
                LogHistory.Clear();
                RebuildList(true);
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

                        RebuildList(true);
                    },
                    type.ToString()));
            }

            _statusLabel = new MyraLabel(string.Empty, MyraLabel.TextStyle.P);

            _scrollViewer = new ScrollViewer
            {
                MinWidth = 550,
                MinHeight = 350,
                MaxWidth = 900,
                MaxHeight = 600,
                Content = _logPanel,
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

            RebuildList(true);
        }

        private bool IsTypeEnabled(LogTypes type)
        {
            // Panic is recorded through Error and has no dedicated toggle.
            if (type == LogTypes.Panic)
                type = LogTypes.Error;

            return (_enabledTypes & type) == type;
        }

        private static Color GetColor(LogTypes type) => type switch
        {
            LogTypes.Error or LogTypes.Panic => Color.OrangeRed,
            LogTypes.Warning => Color.Gold,
            LogTypes.Info => Color.LightGreen,
            LogTypes.Trace => Color.LightGray,
            LogTypes.Debug => Color.Violet,
            _ => Color.White,
        };

        private void RebuildList(bool force = false)
        {
            long revision = LogHistory.Revision;
            if (!force && revision == _lastRevision)
                return;

            _lastRevision = revision;

            // Decide before repopulating whether to snap to the newest entry. We stick to
            // the bottom only when the user hasn't parked the scrollbar somewhere in the
            // middle to read — i.e. it's currently at the top or bottom of the list.
            bool stickToBottom = ShouldAutoScroll();

            LogEntry[] entries = LogHistory.Snapshot();

            _logPanel.Widgets.Clear();

            int shown = 0;
            foreach (LogEntry entry in entries)
            {
                if (!IsTypeEnabled(entry.Type))
                    continue;

                var label = new MyraLabel(entry.ToString(), MyraLabel.TextStyle.P)
                {
                    Wrap = false,
                    TextColor = GetColor(entry.Type),
                };
                _logPanel.Widgets.Add(label);
                shown++;
            }

            if (shown == 0)
            {
                _logPanel.Widgets.Add(new MyraLabel(
                    entries.Length == 0
                        ? "No log entries recorded yet."
                        : "No entries match the current filters.",
                    MyraLabel.TextStyle.P));
            }

            _statusLabel.Text = $"Showing {shown} of {entries.Length} entries (max {LogHistory.MaxEntries})";

            if (stickToBottom)
                ScrollToBottom();
        }

        /// <summary>
        /// True when the list should snap to the newest entry after a rebuild: when the
        /// scrollbar is at the top or bottom (or there's nothing to scroll yet), but not
        /// when the user has scrolled to a position in the middle.
        /// </summary>
        private bool ShouldAutoScroll()
        {
            int max = _scrollViewer.ScrollMaximum.Y;
            if (max <= 0)
                return true;

            const int tolerance = 2;
            int y = _scrollViewer.ScrollPosition.Y;

            return y <= tolerance || y >= max - tolerance;
        }

        private void ScrollToBottom()
        {
            // Ensure ScrollMaximum reflects the freshly rebuilt content before snapping.
            _scrollViewer.UpdateArrange();
            _scrollViewer.ScrollPosition = new Point(_scrollViewer.ScrollPosition.X, _scrollViewer.ScrollMaximum.Y);
        }

        private void CopyToClipboard()
        {
            var sb = new System.Text.StringBuilder();

            foreach (LogEntry entry in LogHistory.Snapshot())
            {
                if (IsTypeEnabled(entry.Type))
                    sb.AppendLine(entry.ToString());
            }

            string text = sb.Length > 0 ? sb.ToString() : "No log entries to copy.";

            Clipboard.SetClipboardText(text);
            GameActions.Print("Copied log history to clipboard!", Constants.HUE_SUCCESS);
        }

        public override void Update()
        {
            base.Update();

            if (IsDisposed)
                return;

            if (Time.Ticks - _lastUpdate > UPDATE_INTERVAL)
            {
                _lastUpdate = Time.Ticks;
                RebuildList();
            }
        }
    }
}
