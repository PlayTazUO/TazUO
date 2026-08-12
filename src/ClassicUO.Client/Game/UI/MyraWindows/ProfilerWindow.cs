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

        private readonly VerticalStackPanel _dataPanel;
        private readonly MyraButton _toggleButton;
        private readonly MyraLabel _statsLabel;
        private readonly MyraLabel _queueLabel;
        private uint _lastUpdate;

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

        private void UpdateDisplay()
        {
            _dataPanel.Widgets.Clear();

            if (!Profiler.Enabled)
            {
                _dataPanel.Widgets.Add(new MyraLabel("Profiler is disabled. Click 'Enable Profiler' to start.", MyraLabel.TextStyle.P));
                return;
            }

            var data = Profiler.AllFrameData
                .OrderByDescending(pd => pd.AverageTime)
                .ToList();

            if (data.Count == 0)
            {
                _dataPanel.Widgets.Add(new MyraLabel("No data collected yet — play the game to populate.", MyraLabel.TextStyle.P));
                return;
            }

            double totalAvg = data.Sum(pd => pd.AverageTime);

            var grid = new MyraGrid();
            grid.SetupWithHeaders(
                GridColumnInfo.Fill("Context"),
                GridColumnInfo.Numeric("Avg(ms)"),
                GridColumnInfo.Numeric("Peak(ms)"),
                GridColumnInfo.Numeric("Last(ms)"),
                GridColumnInfo.Numeric("% Total")
            );

            int row = 1;
            foreach (Profiler.ProfileData pd in data)
            {
                string name = string.Join(" > ", pd.Context);
                double pct = totalAvg > 0 ? 100.0 * pd.AverageTime / totalAvg : 0.0;
                bool isSlow = pd.AverageTime > 8.0;

                grid.AddRow();

                var nameLabel = new MyraLabel(name, MyraLabel.TextStyle.P);
                if (isSlow) nameLabel.TextColor = Color.OrangeRed;
                grid.AddWidget(nameLabel, row, 0);

                var avgLabel = new MyraLabel($"{pd.AverageTime:F2}", MyraLabel.TextStyle.P) { HorizontalAlignment = HorizontalAlignment.Right };
                if (isSlow) avgLabel.TextColor = Color.OrangeRed;
                grid.AddWidget(avgLabel, row, 1);

                var peakLabel = new MyraLabel($"{pd.PeakTime:F2}", MyraLabel.TextStyle.P) { HorizontalAlignment = HorizontalAlignment.Right };
                if (isSlow) peakLabel.TextColor = Color.OrangeRed;
                grid.AddWidget(peakLabel, row, 2);

                var lastLabel = new MyraLabel($"{pd.LastTime:F2}", MyraLabel.TextStyle.P) { HorizontalAlignment = HorizontalAlignment.Right };
                if (isSlow) lastLabel.TextColor = Color.OrangeRed;
                grid.AddWidget(lastLabel, row, 3);

                var pctLabel = new MyraLabel($"{pct:F1}%", MyraLabel.TextStyle.P) { HorizontalAlignment = HorizontalAlignment.Right };
                if (isSlow) pctLabel.TextColor = Color.OrangeRed;
                grid.AddWidget(pctLabel, row, 4);

                row++;
            }

            _dataPanel.Widgets.Add(grid);
        }
    }
}
