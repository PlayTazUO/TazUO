using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.Game.UI.Gumps
{
    public class UpdateTimerViewer : Gump
    {
        private const long UPDATE_INTERVAL = 2000;

        private ScrollArea scrollArea;
        private DataBox dataBox;
        private long lastUpdate = Time.Ticks;
        public UpdateTimerViewer(World world) : base(world, 0, 0)
        {
            UIManager.UpdateTimerEnabled = true;

            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            Width = 500;
            Height = 800;

            Add(new AlphaBlendControl() { Width = Width, Height = Height });

            Add(scrollArea = new ScrollArea(0, 0, Width, Height, true));
            scrollArea.Add(dataBox = new DataBox(0, 0, 0, 0));
            dataBox.WantUpdateSize = true;

            UpdateData();
        }

        private void UpdateData()
        {
            lastUpdate = Time.Ticks;
            dataBox.Clear();


            var sortedDict = new Dictionary<Type, double>();

            foreach (KeyValuePair<Type, double> kvp in UIManager.UpdateTimerTotalTime)
            {
                sortedDict.Add(kvp.Key, kvp.Value / UIManager.UpdateTimerCount[kvp.Key]);
            }

            foreach (KeyValuePair<Type, double> kvp in sortedDict.OrderByDescending(x => x.Value))
            {
                dataBox.Add(new Label($"{kvp.Key.Name}: {kvp.Value} ", true, 32, Width));
            }

            dataBox.ReArrangeChildren(10);
        }

        public override void Dispose()
        {
            base.Dispose();
            UIManager.UpdateTimerEnabled = false;
        }

        public override void Update()
        {
            base.Update();

            if(Time.Ticks - lastUpdate > UPDATE_INTERVAL)
            {
                UpdateData();
            }
        }
    }
}
