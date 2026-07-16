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

        private readonly VerticalStackPanel _logPanel;
        private readonly ScrollViewer _scrollViewer;
        private readonly MyraLabel _statusLabel;
        private uint _lastUpdate;
        private long _lastRevision = -1;
        private bool _autoScroll = true;

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
            root.Widgets.Add(_statusLabel);
            root.Widgets.Add(_scrollViewer);

            SetRootContent(root);
            CenterInViewPort();

            RebuildList(true);
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

            LogEntry[] entries = LogHistory.Snapshot();

            _logPanel.Widgets.Clear();

            if (entries.Length == 0)
            {
                _logPanel.Widgets.Add(new MyraLabel("No log entries recorded yet.", MyraLabel.TextStyle.P));
            }
            else
            {
                foreach (LogEntry entry in entries)
                {
                    var label = new MyraLabel(entry.ToString(), MyraLabel.TextStyle.P)
                    {
                        Wrap = false,
                        TextColor = GetColor(entry.Type),
                    };
                    _logPanel.Widgets.Add(label);
                }
            }

            _statusLabel.Text = $"{entries.Length} / {LogHistory.MaxEntries} entries";

            if (_autoScroll)
                ScrollToBottom();
        }

        private void ScrollToBottom() =>
            _scrollViewer.ScrollPosition = new Point(_scrollViewer.ScrollPosition.X, _scrollViewer.ScrollMaximum.Y);

        private static void CopyToClipboard()
        {
            string text = LogHistory.ToText();

            if (string.IsNullOrEmpty(text))
                text = "No log entries recorded.";

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
