using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.MyraWindows;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using System;
using System.Collections.Generic;
using System.Linq;
using Myra.Graphics2D.UI;

namespace ClassicUO.Game.UI.Gumps.GridHighLight
{
    /// <summary>
    /// Myra-based editor for the shared, configurable property lists used by grid highlighting.
    /// Each category is a multiline text box (one property per line); edits are parsed, de-duplicated
    /// and persisted to the profile when the box loses focus.
    /// </summary>
    public class GridHighlightConfig : MyraControl
    {
        private readonly World _world;

        public GridHighlightConfig(World world) : base(TazLang.Get("gridhighlight_config_title"))
        {
            _world = world;
            Build();
            CenterInViewPort();
        }

        public static void Show(World world)
        {
            foreach (IGui gump in UIManager.Gumps)
            {
                if (gump is GridHighlightConfig w && !w.IsDisposed)
                {
                    w.BringOnTop();
                    return;
                }
            }

            UIManager.Add(new GridHighlightConfig(world));
        }

        private void Build()
        {
            Profile profile = ProfileManager.CurrentProfile;

            (string Label, HashSet<string> Set, Action<List<string>> Save)[] categories =
            {
                (TazLang.Get("gridhighlight_cat_properties"),   GridHighlightRules.Properties,             v => profile.ConfigurableProperties = v),
                (TazLang.Get("gridhighlight_cat_superslayers"), GridHighlightRules.SuperSlayerProperties,  v => profile.ConfigurableSuperSlayers = v),
                (TazLang.Get("gridhighlight_cat_slayers"),      GridHighlightRules.SlayerProperties,       v => profile.ConfigurableSlayers = v),
                (TazLang.Get("gridhighlight_cat_resistances"),  GridHighlightRules.Resistances,            v => profile.ConfigurableResistances = v),
                (TazLang.Get("gridhighlight_cat_negatives"),    GridHighlightRules.NegativeProperties,     v => profile.ConfigurableNegatives = v),
                (TazLang.Get("gridhighlight_cat_rarity"),       GridHighlightRules.RarityProperties,       v => profile.ConfigurableRarities = v),
            };

            var root = new VerticalStackPanel { Spacing = MyraStyle.STANDARD_SPACING };
            root.Widgets.Add(new MyraLabel(TazLang.Get("gridhighlight_config_title"), MyraLabel.TextStyle.P));

            var columns = new HorizontalStackPanel { Spacing = 4 };

            foreach ((string label, HashSet<string> set, Action<List<string>> save) in categories)
            {
                var column = new VerticalStackPanel { Spacing = 2 };
                column.Widgets.Add(new MyraLabel(label, MyraLabel.TextStyle.P));

                var input = new MyraInputBox
                {
                    Text = string.Join("\n", set),
                    Width = 175,
                    MinHeight = 420,
                    Multiline = true,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                Action<List<string>> capturedSave = save;
                input.LostFocus = () =>
                {
                    List<string> parsed = (input.Text ?? "")
                        .Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .Where(p => !string.IsNullOrEmpty(p))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    capturedSave(parsed);
                    GridHighlightRules.SaveGridHighlightConfiguration();
                    GridHighlightData.RecheckMatchStatus();
                };

                column.Widgets.Add(new ScrollViewer { MaxHeight = 420, Content = input });
                columns.Widgets.Add(column);
            }

            root.Widgets.Add(columns);
            SetRootContent(root);
        }
    }
}
