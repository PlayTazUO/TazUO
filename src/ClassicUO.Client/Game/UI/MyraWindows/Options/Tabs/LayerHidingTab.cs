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

public static class LayerHidingTab
{
    internal static IOptionSource GetContent() => GetSection();

    private static OptionFragment GetSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.LayerHidingTabLang lang = Language.Instance.GetModernOptionsGumpLanguage.LayerHidingTab;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

        return OptionsUi.CheckBoxGroup(
            new PropertyBinder(new Accessor<bool>(() => profile.HiddenLayersEnabled), lang.EnableLayerHiding),
            Option.Checkbox(
                lang.OnlyForYourself,
                new Accessor<bool>(() => profile.HideLayersForSelf),
                lang.OnlyForYourselfTooltip,
                search: new SearchMetadata(lang.OnlyForYourself, Keywords: [kw.Self])
            ),
            Option.Spacer(),
            Option.Custom(() => new MyraLabel(lang.HideFollowingLayers, MyraLabel.TextStyle.P), new SearchMetadata(lang.HideFollowingLayers)),
            GetLayerBoxesFragment()
        ).WithSearch(new SearchMetadata(lang.Label, Keywords: [kw.Layer, kw.Hide, kw.Equipment, kw.Clothing], Tags: [kw.Layer, kw.Hide]));
    }

    private static OptionFragment GetLayerBoxesFragment()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

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
            relevantLayers.Select(layer => (OptionContent)Option.Checkbox(layer.ToString(), profile.HiddenLayers.Contains((int)layer), b =>
            {
                if (b) profile.HiddenLayers.Add((int)layer);
                else profile.HiddenLayers.Remove((int)layer);
            }, search: new SearchMetadata(layer.ToString(), Keywords: [kw.Layer])))
        );
    }
}
