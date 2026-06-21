#if DEBUG
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Assets;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Gumps
{
    public class ProfilerWindow : Gump
    {
        private const int WIDTH = 480;
        private const int HEIGHT = 420;
        private const uint UPDATE_INTERVAL = 500;

        private readonly DataBox _dataBox;
        private NiceButton _toggleButton;
        private uint _lastUpdate;

        public ProfilerWindow(World world) : base(world, 0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            Width = WIDTH;
            Height = HEIGHT;

            Add(new AlphaBlendControl(0.85f) { Width = WIDTH, Height = HEIGHT });

            int y = 8;

            Add(new Label("Profiler", true, 0xFFFF, WIDTH, align: TEXT_ALIGN_TYPE.TS_CENTER) { X = 0, Y = y });
            y += 24;

            _toggleButton = new NiceButton(8, y, 170, 22, ButtonAction.Activate, GetToggleLabel())
            {
                ButtonParameter = 1,
                DisplayBorder = true,
                IsSelectable = false
            };
            Add(_toggleButton);

            var resetButton = new NiceButton(184, y, 80, 22, ButtonAction.Activate, "Reset")
            {
                ButtonParameter = 2,
                DisplayBorder = true,
                IsSelectable = false
            };
            Add(resetButton);

            var copyButton = new NiceButton(270, y, 110, 22, ButtonAction.Activate, "Copy Output")
            {
                ButtonParameter = 3,
                DisplayBorder = true,
                IsSelectable = false
            };
            Add(copyButton);
            y += 30;

            var scrollArea = new ScrollArea(0, y, WIDTH, HEIGHT - y - 5, true);
            var innerBox = new DataBox(0, 0, 0, 0);
            innerBox.WantUpdateSize = true;
            scrollArea.Add(innerBox);
            Add(scrollArea);

            _dataBox = innerBox;

            _lastUpdate = 0;
            UpdateDisplay();
        }

        private static string GetToggleLabel() =>
            Profiler.Enabled ? "Disable Profiler" : "Enable Profiler";

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 1:
                    Profiler.Enabled = !Profiler.Enabled;
                    if (Profiler.Enabled)
                        Profiler.Reset();
                    _toggleButton.SetText(GetToggleLabel());
                    break;
                case 2:
                    Profiler.Reset();
                    break;
                case 3:
                    CopyToClipboard();
                    break;
            }
        }

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

            var sb = new System.Text.StringBuilder();
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
            _dataBox.Clear();

            if (!Profiler.Enabled)
            {
                _dataBox.Add(new Label("Profiler is disabled. Click 'Enable Profiler' to start.", true, 0xFFFF, Width - 20));
                _dataBox.ReArrangeChildren(5);
                return;
            }

            List<Profiler.ProfileData> data = Profiler.AllFrameData
                .OrderByDescending(pd => pd.AverageTime)
                .ToList();

            if (data.Count == 0)
            {
                _dataBox.Add(new Label("No data collected yet — play the game to populate.", true, 0xFFFF, Width - 20));
                _dataBox.ReArrangeChildren(5);
                return;
            }

            double totalAvg = data.Sum(pd => pd.AverageTime);

            _dataBox.Add(new Label("Context                        Avg(ms)  Last(ms)  %Total", true, 0x44, Width - 20));

            foreach (Profiler.ProfileData pd in data)
            {
                string name = string.Join(" > ", pd.Context);
                double pct = totalAvg > 0 ? 100.0 * pd.AverageTime / totalAvg : 0.0;
                ushort color = pd.AverageTime > 8.0 ? (ushort)0x20 : (ushort)0xFFFF;
                _dataBox.Add(new Label(
                    $"{name,-28}  {pd.AverageTime,7:F2}  {pd.LastTime,8:F2}  {pct,6:F1}%",
                    true, color, Width - 20));
            }

            _dataBox.ReArrangeChildren(3);
        }
    }
}
#endif
