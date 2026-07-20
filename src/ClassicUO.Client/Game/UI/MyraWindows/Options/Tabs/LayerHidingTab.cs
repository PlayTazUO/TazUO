using System;
using System.Linq;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for the layer-hiding feature (suppresses rendering of specific equipment layers)</summary>
public static class LayerHidingTab
{
    /// <summary>Returns the option fragment for layer-hiding enable/disable and per-layer toggles</summary>
    internal static IOptionSource GetContent() => GetSection();

    private static OptionFragment GetSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.CheckBoxGroup(
            new PropertyBinder(new Accessor<bool>(() => profile.HiddenLayersEnabled), TazLang.Get("mog_layerhidingtab_enablelayerhiding")),
            Option.Checkbox(
                TazLang.Get("mog_layerhidingtab_onlyforyourself"),
                new Accessor<bool>(() => profile.HideLayersForSelf),
                TazLang.Get("mog_layerhidingtab_onlyforyourselftooltip"),
                search: new SearchMetadata(TazLang.Get("mog_layerhidingtab_onlyforyourself"), Keywords: [TazLang.Get("mog_kw_self")])
            ),
            Option.Spacer(),
            Option.Custom(() => new MyraLabel(TazLang.Get("mog_layerhidingtab_hidefollowinglayers"), MyraLabel.TextStyle.P), new SearchMetadata(TazLang.Get("mog_layerhidingtab_hidefollowinglayers"))),
            GetLayerBoxesFragment()
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_layerhidingtab_label"), Tags: [TazLang.Get("mog_kw_layer"), TazLang.Get("mog_kw_hide")]));
    }

    private static OptionFragment GetLayerBoxesFragment()
    {
        Profile profile = ProfileManager.CurrentProfile;

        Layer[] ignoredLayers =
        [
            Layer.Invalid, Layer.Hair, Layer.Beard, Layer.Backpack,
            Layer.ShopBuyRestock, Layer.ShopBuy, Layer.ShopSell,
            Layer.Bank, Layer.Face, Layer.Talisman, Layer.Mount
        ];

        Layer[] relevantLayers = Enum.GetValues<Layer>().Where(layer => !ignoredLayers.Contains(layer)).ToArray();

        return new OptionFragment(
            () =>
            {
                var panel = new WrapPanel
                {
                    Orientation = Orientation.Vertical,
                    Aligned = true,
                    UniformSizing = true,
                    VerticalSpacing = MyraStyle.STANDARD_SPACING,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(MyraStyle.STANDARD_SPACING, 10, MyraStyle.STANDARD_SPACING, 10),
                    MaxHeight = 300
                };

                foreach (Layer layer in relevantLayers)
                {
                    panel.Widgets.Add(
                        MyraCheckButton.CreatePropBoundCheckButton(
                            new Accessor<bool>(
                                () => profile.HiddenLayers.Contains((int)layer),
                                enabled =>
                                {
                                    if (enabled)
                                        profile.HiddenLayers.Add((int)layer);
                                    else
                                        profile.HiddenLayers.Remove((int)layer);
                                }
                            ),
                            layer.ToString()
                        )
                    );
                }

                return panel;
            },
            relevantLayers.Select(layer => (OptionContent)Option.Checkbox(
                layer.ToString(),
                new Accessor<bool>(
                    () => profile.HiddenLayers.Contains((int)layer),
                    enabled =>
                    {
                        if (enabled)
                            profile.HiddenLayers.Add((int)layer);
                        else
                            profile.HiddenLayers.Remove((int)layer);
                    }
                ),
                search: new SearchMetadata(layer.ToString(), Keywords: [TazLang.Get("mog_kw_layer")])
            ))
        );
    }
}
