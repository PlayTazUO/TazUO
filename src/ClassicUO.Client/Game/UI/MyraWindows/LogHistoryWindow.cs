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
    /// <see cref="LogHistory"/> in a single read-only text field, with a button to copy
    /// the whole output to the clipboard.
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

        private readonly MyraInputBox _textBox;
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
            var buttons = new HorizontalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
            buttons.Widgets.Add(new MyraButton("Copy Output", CopyToClipboard));
            buttons.Widgets.Add(new MyraButton("Refresh", () => Rebuild(true)));
            buttons.Widgets.Add(new MyraButton("Clear", () =>
            {
                LogHistory.Clear();
                Rebuild(true);
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

                        Rebuild(true);
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

            Rebuild(true);
        }

        private bool IsTypeEnabled(LogTypes type)
        {
            // Panic is recorded through Error and has no dedicated toggle.
            if (type == LogTypes.Panic)
                type = LogTypes.Error;

            return (_enabledTypes & type) == type;
        }

        /// <summary>
        /// Builds the plain-text dump of the entries matching the active filters and
        /// reports how many were shown out of the total captured.
        /// </summary>
        private string BuildText(out int shown, out int total)
        {
            LogEntry[] entries = LogHistory.Snapshot();
            total = entries.Length;
            shown = 0;

            var sb = new StringBuilder();
            foreach (LogEntry entry in entries)
            {
                if (!IsTypeEnabled(entry.Type))
                    continue;

                sb.AppendLine(entry.ToString());
                shown++;
            }

            return sb.ToString();
        }

        private void Rebuild(bool force = false)
        {
            long revision = LogHistory.Revision;
            if (!force && revision == _lastRevision)
                return;

            _lastRevision = revision;

            // Decide before repopulating whether to snap to the newest entry. We stick to
            // the bottom only when the user hasn't parked the scrollbar somewhere in the
            // middle to read — i.e. it's currently at the top or bottom of the content.
            bool stickToBottom = ShouldAutoScroll();

            _textBox.Text = BuildText(out int shown, out int total);
            _statusLabel.Text = $"Showing {shown} of {total} entries (max {LogHistory.MaxEntries})";

            if (stickToBottom)
                ScrollToBottom();
        }

        /// <summary>
        /// True when the view should snap to the newest entry after a rebuild: when the
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
            string text = BuildText(out int shown, out _);

            Clipboard.SetClipboardText(shown > 0 ? text : "No log entries to copy.");
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
                Rebuild();
            }
        }
    }
}
