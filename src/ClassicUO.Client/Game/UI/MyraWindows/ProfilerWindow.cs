using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.MyraWindows
{
    public class ProfilerWindow : MyraControl
    {
        private const uint UPDATE_INTERVAL = 250;

        private const int COLUMN_COUNT = 5;

        private static readonly string[] ColumnLabels = { "Context", "Avg(ms)", "Peak(ms)", "Last(ms)", "% Total" };

        private readonly VerticalStackPanel _dataPanel;
        private readonly MyraButton _toggleButton;
        private readonly MyraLabel _statsLabel;
        private readonly MyraLabel _queueLabel;
        private uint _lastUpdate;

        private int _sortColumn = 1;
        private bool _sortAscending;

        // The grid, its header buttons and every cell label are built once and then only updated in
        // place. Rebuilding them every tick was the source of the periodic hitch this window
        // caused, since each MyraLabel clones a stylesheet on construction.
        private readonly MyraGrid _grid;
        private readonly MyraButton[] _headerButtons = new MyraButton[COLUMN_COUNT];
        private readonly List<MyraLabel[]> _activeRows = new();
        private readonly ObjectPool<MyraLabel[]> _rowPool;
        private readonly MyraLabel _statusLabel;
        private readonly Color _defaultTextColor;
        private readonly List<Profiler.ProfileData> _sortBuffer = new();

        private readonly CancellationTokenSource _cpuCts = new();
        private static volatile float _cpuPercent;

        public static void Show()
        {
            foreach (IGui g in UIManager.Gumps)
            {
                if (g is ProfilerWindow w)
                {
                    w.BringOnTop();
                    return;
                }
            }
            UIManager.Add(new ProfilerWindow());
        }

        public ProfilerWindow() : base("Profiler")
        {
            _dataPanel = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };

            _toggleButton = new MyraButton(GetToggleLabel(), OnToggle);

            var buttons = new HorizontalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
            buttons.Widgets.Add(_toggleButton);
            buttons.Widgets.Add(new MyraButton("Reset", () => Profiler.Reset()));
            buttons.Widgets.Add(new MyraButton("Clear Action Queue", () => ObjectActionQueue.Instance.Clear()));
            buttons.Widgets.Add(new MyraButton("Copy Output", CopyToClipboard));

            _statsLabel = new MyraLabel(GetStatsLabel(), MyraLabel.TextStyle.P);
            _queueLabel = new MyraLabel(GetQueueLabel(), MyraLabel.TextStyle.P);

            _rowPool = new ObjectPool<MyraLabel[]>(CreateRow);
            _defaultTextColor = new MyraLabel(string.Empty, MyraLabel.TextStyle.P).TextColor;

            _grid = new MyraGrid();
            Widget[] headers = _grid.SetupWithHeaders(
                OnHeaderClicked,
                GridColumnInfo.Fill(ColumnLabels[0]),
                GridColumnInfo.Numeric(ColumnLabels[1]),
                GridColumnInfo.Numeric(ColumnLabels[2]),
                GridColumnInfo.Numeric(ColumnLabels[3]),
                GridColumnInfo.Numeric(ColumnLabels[4])
            );
            for (int i = 0; i < COLUMN_COUNT; i++)
                _headerButtons[i] = (MyraButton)headers[i];
            ApplyHeaderLabels();

            _statusLabel = new MyraLabel(string.Empty, MyraLabel.TextStyle.P);
            _dataPanel.Widgets.Add(_statusLabel);
            _dataPanel.Widgets.Add(_grid);

            var scrollViewer = new ScrollViewer
            {
                MinWidth = 500,
                MinHeight = 350,
                MaxWidth = 800,
                MaxHeight = 600,
                Content = _dataPanel,
            };

            var root = new VerticalStackPanel
            {
                Spacing = MyraStyle.STANDARD_SPACING,
                Padding = new Thickness(4),
            };
            root.Widgets.Add(buttons);
            root.Widgets.Add(_statsLabel);
            root.Widgets.Add(_queueLabel);
            root.Widgets.Add(scrollViewer);

            SetRootContent(root);
            CenterInViewPort();

            _ = MonitorCpuAsync(_cpuCts.Token);

            UpdateDisplay();
        }

        /// <summary>
        /// Continuously samples CPU usage in the background. The loop ends when the
        /// window is disposed and its <see cref="CancellationTokenSource"/> is cancelled,
        /// so no task is left running after the window closes.
        /// </summary>
        private async Task MonitorCpuAsync(CancellationToken token)
        {
            const int sampleWindowMs = 500;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    Environment.ProcessCpuUsage startSnapshot = Environment.CpuUsage;
                    DateTime startTime = DateTime.UtcNow;

                    await Task.Delay(sampleWindowMs, token);

                    Environment.ProcessCpuUsage endSnapshot = Environment.CpuUsage;
                    DateTime endTime = DateTime.UtcNow;

                    TimeSpan cpuTimeUsed = endSnapshot.TotalTime - startSnapshot.TotalTime;
                    TimeSpan realTimeElapsed = endTime - startTime;

                    double cpuPercent = realTimeElapsed.TotalMilliseconds > 0
                        ? (cpuTimeUsed.TotalMilliseconds / realTimeElapsed.TotalMilliseconds) / Environment.ProcessorCount * 100
                        : 0.0;

                    _cpuPercent = (float)Math.Clamp(Math.Round(cpuPercent, 2), 0.0, 100.0);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private static string GetStatsLabel()
        {
            double memoryMb = Environment.WorkingSet / (1024.0 * 1024.0);
            return $"Memory: {memoryMb:F1} MB    CPU: {_cpuPercent:F2}%";
        }

        private static string GetQueueLabel() =>
            $"Global Action Queue: {ObjectActionQueue.Instance.GetCurrentQueuedCount}    Main Thread Queue: {MainThreadQueue.Count}";

        private void OnToggle()
        {
            Profiler.Enabled = !Profiler.Enabled;
            if (Profiler.Enabled)
                Profiler.Reset();
            _toggleButton.Content = new MyraLabel(GetToggleLabel(), MyraLabel.TextStyle.P);
        }

        private static string GetToggleLabel() =>
            Profiler.Enabled ? "Disable Profiler" : "Enable Profiler";

        private static void CopyToClipboard()
        {
            var data = Profiler.AllFrameData
                .OrderByDescending(pd => pd.AverageTime)
                .ToList();

            if (data.Count == 0)
            {
                Clipboard.SetClipboardText("No profiler data available.");
                GameActions.Print("Copied to clipboard!", Constants.HUE_SUCCESS);
                return;
            }

            double totalAvg = data.Sum(pd => pd.AverageTime);
            var sb = new StringBuilder();
            sb.AppendLine("TazUO Profiler Output");
            sb.AppendLine(GetStatsLabel());
            sb.AppendLine($"{"Context",-40}  {"Avg(ms)",7}  {"Peak(ms)",8}  {"Last(ms)",8}  {"% Total",7}");
            sb.AppendLine(new string('-', 76));

            foreach (Profiler.ProfileData pd in data)
            {
                string name = string.Join(" > ", pd.Context);
                if (name.Length > 40)
                    name = name.Substring(name.Length - 40);
                double pct = totalAvg > 0 ? 100.0 * pd.AverageTime / totalAvg : 0.0;
                sb.AppendLine($"{name,-40}  {pd.AverageTime,7:F2}  {pd.PeakTime,8:F2}  {pd.LastTime,8:F2}  {pct,6:F1}%");
            }

            Clipboard.SetClipboardText(sb.ToString());
            GameActions.Print("Copied to clipboard!", Constants.HUE_SUCCESS);
        }

        public override void Update()
        {
            base.Update();

            // The close button (X) disposes through the base class without routing
            // through Dispose(), so stop the CPU monitor here too once disposed.
            if (IsDisposed)
            {
                StopCpuMonitor();
                return;
            }

            if (Time.Ticks - _lastUpdate > UPDATE_INTERVAL)
            {
                _lastUpdate = Time.Ticks;
                _statsLabel.Text = GetStatsLabel();
                _queueLabel.Text = GetQueueLabel();
                UpdateDisplay();
            }
        }

        public override void Dispose()
        {
            StopCpuMonitor();
            base.Dispose();
        }

        private void StopCpuMonitor()
        {
            if (_cpuCts.IsCancellationRequested)
                return;

            _cpuCts.Cancel();
            _cpuCts.Dispose();
        }

        /// <summary>
        /// Flips the sort on a repeat click of the active column, otherwise switches to the clicked
        /// one. Numeric columns default to descending so the slowest contexts stay at the top.
        /// </summary>
        /// <param name="column">Index of the clicked header, matching the column order on screen.</param>
        private void OnHeaderClicked(int column)
        {
            if (_sortColumn == column)
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _sortColumn = column;
                _sortAscending = column == 0;
            }

            ApplyHeaderLabels();
            UpdateDisplay();
        }

        /// <summary>Redraws the header labels with the arrow marking the active sort column.</summary>
        private void ApplyHeaderLabels()
        {
            for (int i = 0; i < _headerButtons.Length; i++)
            {
                string label = ColumnLabels[i];
                if (_sortColumn == i)
                    label += _sortAscending ? " ↑" : " ↓";

                _headerButtons[i].Text = label;
            }
        }

        /// <summary>Orders the current snapshot in place in <see cref="_sortBuffer" />.</summary>
        /// <returns>The shared buffer, valid only until the next call.</returns>
        private List<Profiler.ProfileData> GetSortedData()
        {
            _sortBuffer.Clear();
            _sortBuffer.AddRange(Profiler.AllFrameData);

            Comparison<Profiler.ProfileData> comparison = _sortColumn switch
            {
                0 => (a, b) => CompareContext(a.Context, b.Context),
                2 => (a, b) => a.PeakTime.CompareTo(b.PeakTime),
                3 => (a, b) => a.LastTime.CompareTo(b.LastTime),
                _ => (a, b) => a.AverageTime.CompareTo(b.AverageTime),
            };

            _sortBuffer.Sort((a, b) => _sortAscending ? comparison(a, b) : comparison(b, a));
            return _sortBuffer;
        }

        /// <summary>
        /// Orders two context paths lexicographically without joining them into a string, which the
        /// display name does but which would allocate on every comparison of a sort.
        /// </summary>
        private static int CompareContext(string[] a, string[] b)
        {
            int length = Math.Min(a.Length, b.Length);
            for (int i = 0; i < length; i++)
            {
                int cmp = string.Compare(a[i], b[i], StringComparison.OrdinalIgnoreCase);
                if (cmp != 0)
                    return cmp;
            }

            return a.Length.CompareTo(b.Length);
        }

        /// <summary>
        /// Builds one row's worth of cell labels, right-aligning the numeric columns. Only ever
        /// called by the pool when it has no idle row to hand out.
        /// </summary>
        private static MyraLabel[] CreateRow()
        {
            var row = new MyraLabel[COLUMN_COUNT];
            row[0] = new MyraLabel(string.Empty, MyraLabel.TextStyle.P);
            for (int i = 1; i < COLUMN_COUNT; i++)
                row[i] = new MyraLabel(string.Empty, MyraLabel.TextStyle.P) { HorizontalAlignment = HorizontalAlignment.Right };

            return row;
        }

        /// <summary>Appends a pooled row to the bottom of the grid.</summary>
        private void AddRow()
        {
            MyraLabel[] row = _rowPool.Get();
            int gridRow = _activeRows.Count + 1;

            for (int c = 0; c < COLUMN_COUNT; c++)
                _grid.AddWidget(row[c], gridRow, c);

            _grid.AddRow();
            _activeRows.Add(row);
        }

        /// <summary>Detaches the bottom row and returns its labels to the pool.</summary>
        private void RemoveLastRow()
        {
            int last = _activeRows.Count - 1;
            MyraLabel[] row = _activeRows[last];

            for (int c = 0; c < COLUMN_COUNT; c++)
                _grid.Widgets.Remove(row[c]);

            if (_grid.RowsProportions.Count > 0)
                _grid.RowsProportions.RemoveAt(_grid.RowsProportions.Count - 1);

            _activeRows.RemoveAt(last);
            _rowPool.Return(row);
        }

        private void UpdateDisplay()
        {
            List<Profiler.ProfileData> data = GetSortedData();

            if (data.Count == 0)
            {
                while (_activeRows.Count > 0)
                    RemoveLastRow();

                _grid.Visible = false;
                _statusLabel.Visible = true;
                _statusLabel.Text = Profiler.Enabled
                    ? "No data collected yet — play the game to populate."
                    : "Profiler is disabled. Click 'Enable Profiler' to start.";
                return;
            }

            _grid.Visible = true;

            // Keep the last captured snapshot on screen after the profiler is stopped so the
            // numbers can still be read; the data is only replaced when a new run resets it.
            _statusLabel.Visible = !Profiler.Enabled;
            if (!Profiler.Enabled)
                _statusLabel.Text = "Profiler is disabled — showing the last captured data.";

            double totalAvg = data.Sum(pd => pd.AverageTime);

            while (_activeRows.Count < data.Count)
                AddRow();

            while (_activeRows.Count > data.Count)
                RemoveLastRow();

            for (int i = 0; i < data.Count; i++)
            {
                Profiler.ProfileData pd = data[i];
                MyraLabel[] row = _activeRows[i];
                Color color = pd.AverageTime > 8.0 ? Color.OrangeRed : _defaultTextColor;

                row[0].Text = string.Join(" > ", pd.Context);
                row[1].Text = $"{pd.AverageTime:F2}";
                row[2].Text = $"{pd.PeakTime:F2}";
                row[3].Text = $"{pd.LastTime:F2}";
                row[4].Text = totalAvg > 0 ? $"{100.0 * pd.AverageTime / totalAvg:F1}%" : "0.0%";

                for (int c = 0; c < COLUMN_COUNT; c++)
                    row[c].TextColor = color;
            }
        }
    }
}
