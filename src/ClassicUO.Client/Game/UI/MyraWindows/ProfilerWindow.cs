#if DEBUG
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private const uint UPDATE_INTERVAL = 500;

        private readonly VerticalStackPanel _dataPanel;
        private readonly MyraButton _toggleButton;
        private uint _lastUpdate;

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
            buttons.Widgets.Add(new MyraButton("Copy Output", CopyToClipboard));

            var scrollViewer = new ScrollViewer
            {
                Width = 500,
                Height = 350,
                Content = _dataPanel,
            };

            var root = new VerticalStackPanel
            {
                Spacing = MyraStyle.STANDARD_SPACING,
                Padding = new Thickness(4),
            };
            root.Widgets.Add(buttons);
            root.Widgets.Add(scrollViewer);

            SetRootContent(root);
            CenterInViewPort();

            UpdateDisplay();
        }

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
            List<Profiler.ProfileData> data = Profiler.AllFrameData
                .OrderByDescending(pd => pd.AverageTime)
                .ToList();

            if (data.Count == 0)
            {
                Clipboard.SetClipboardText("No profiler data available.");
                return;
            }

            double totalAvg = data.Sum(pd => pd.AverageTime);
            var sb = new StringBuilder();
            sb.AppendLine("TazUO Profiler Output");
            sb.AppendLine($"{"Context",-30}  {"Avg(ms)",7}  {"Last(ms)",8}  {"% Total",7}");
            sb.AppendLine(new string('-', 60));

            foreach (Profiler.ProfileData pd in data)
            {
                string name = string.Join(" > ", pd.Context);
                double pct = totalAvg > 0 ? 100.0 * pd.AverageTime / totalAvg : 0.0;
                sb.AppendLine($"{name,-30}  {pd.AverageTime,7:F2}  {pd.LastTime,8:F2}  {pct,6:F1}%");
            }

            Clipboard.SetClipboardText(sb.ToString());
        }

        public override void Update()
        {
            base.Update();

            if (Time.Ticks - _lastUpdate > UPDATE_INTERVAL)
            {
                _lastUpdate = Time.Ticks;
                UpdateDisplay();
            }
        }

        private void UpdateDisplay()
        {
            _dataPanel.Widgets.Clear();

            if (!Profiler.Enabled)
            {
                _dataPanel.Widgets.Add(new MyraLabel("Profiler is disabled. Click 'Enable Profiler' to start.", MyraLabel.TextStyle.P));
                return;
            }

            List<Profiler.ProfileData> data = Profiler.AllFrameData
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

                var lastLabel = new MyraLabel($"{pd.LastTime:F2}", MyraLabel.TextStyle.P) { HorizontalAlignment = HorizontalAlignment.Right };
                if (isSlow) lastLabel.TextColor = Color.OrangeRed;
                grid.AddWidget(lastLabel, row, 2);

                var pctLabel = new MyraLabel($"{pct:F1}%", MyraLabel.TextStyle.P) { HorizontalAlignment = HorizontalAlignment.Right };
                if (isSlow) pctLabel.TextColor = Color.OrangeRed;
                grid.AddWidget(pctLabel, row, 3);

                row++;
            }

            _dataPanel.Widgets.Add(grid);
        }
    }
}
#endif
