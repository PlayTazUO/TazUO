using ClassicUO.Configuration;
using ClassicUO.Game.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ClassicUO.Game.UI.Gumps.GridHighLight
{
    public class GridHighlightConfig : Gump
    {
        private const int WIDTH = 350, HEIGHT = 500;
        private int lastYitem = 0;
        private int lastXitem = 0;

        public GridHighlightConfig(World world, int x, int y) : base(world, 0, 0)
        {
            Width = (175 + 2) * 6;
            Height = HEIGHT;
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;

            Add(new AlphaBlendControl(0.85f) { Width = Width, Height = HEIGHT });

            Label label;
            Add(label = new Label("Properties configuration (separated by a new line)", true, 0xffff) { X = 0, Y = lastYitem });

            lastYitem += 20;

            List<(string Label, HashSet<string> Set)> categories = new()
                {
                    ("Properties", GridHighlightRules.Properties),
                    ("Super slayers", GridHighlightRules.SuperSlayerProperties),
                    ("Slayers", GridHighlightRules.SlayerProperties),
                    ("Resistances", GridHighlightRules.Resistances),
                    ("Negatives", GridHighlightRules.NegativeProperties),
                    ("Rarity", GridHighlightRules.RarityProperties)
                };
            foreach ((string labelText, HashSet<string> propSet) in categories)
            {
                Add(label = new Label(labelText, true, 0xffff) { X = lastXitem, Y = lastYitem });
                ScrollArea propertiesScrollArea;
                InputField propertiesPropInput;
                Add(propertiesScrollArea = new ScrollArea(lastXitem, lastYitem + 20, 175, HEIGHT - lastYitem - 20, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways });
                propertiesScrollArea.Add(propertiesPropInput = new InputField(0x0BB8, 0xFF, 0xFFFF, true, 175 - 13, (HEIGHT - lastYitem - 20) * 10) { Y = 0 });
                string s = string.Join("\n", propSet);
                propertiesPropInput.SetText(s);
                var cts = new CancellationTokenSource();
                propertiesPropInput.TextChanged += async (s, e) =>
                {
                    CancellationTokenSource oldToken = cts;
                    oldToken?.Cancel();
                    cts = new CancellationTokenSource();
                    CancellationToken token = cts.Token;

                    try
                    {
                        await Task.Delay(500, token);
                        if (!token.IsCancellationRequested)
                        {
                            string text = propertiesPropInput.Text;
                            var parsed = text
                                .Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(p => p.Trim())
                                .Where(p => !string.IsNullOrEmpty(p))
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .ToList();

                            ProfileManager.CurrentProfile.ConfigurableProperties = parsed;
                            GridHighlightRules.SaveGridHighlightConfiguration();
                            propertiesPropInput.Add(new FadingLabel(10, "Saved", true, 0xff) { X = 0, Y = 0 });
                        }
                    }
                    catch (TaskCanceledException) { }
                    oldToken?.Dispose();
                };

                lastXitem += 175 + 2;
            }
        }
    }
}
