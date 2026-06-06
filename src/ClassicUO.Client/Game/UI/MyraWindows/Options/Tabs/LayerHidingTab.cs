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
    internal static OptionItem GetContent()
    {
        ModernOptionsGumpLanguage.LayerHidingTabLang lang = Language.Instance.GetModernOptionsGumpLanguage.LayerHidingTab;
        return new OptionItem(lang.LayerHiding, GetSection);
    }

    private static WrapPanel GetSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.LayerHidingTabLang lang = Language.Instance.GetModernOptionsGumpLanguage.LayerHidingTab;

        return OptionTabCommons.StyledVerticalWrapPanel(
            new CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.HiddenLayersEnabled), lang.EnableLayerHiding),
                OptionsFactory.CreateCheckboxOption(lang.OnlyForYourself, new Accessor<bool>(() => profile.HideLayersForSelf), lang.OnlyForYourselfTooltip),
                OptionsFactory.CreateSpacer(),
                new MyraLabel(lang.HideFollowingLayers, MyraLabel.TextStyle.P),
                GetLayerBoxes()
            )
        );
    }

    private static WrapPanel GetLayerBoxes()
    {
        Profile profile = ProfileManager.CurrentProfile;

        Layer[] ignoredLayers =
        [
            Layer.Invalid, Layer.Hair, Layer.Beard, Layer.Backpack,
            Layer.ShopBuyRestock, Layer.ShopBuy, Layer.ShopSell,
            Layer.Bank, Layer.Face, Layer.Talisman, Layer.Mount
        ];

        Layer[] relevantLayers = Enum.GetValues<Layer>().Where(layer => !ignoredLayers.Contains(layer)).ToArray();

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
            panel.Widgets.Add(
                OptionsFactory.CreateCheckboxOption(
                    layer.ToString(), // Consider localizing at some point
                    profile.HiddenLayers.Contains((int)layer),
                    enabled =>
                    {
                        if (enabled)
                            profile.HiddenLayers.Add((int)layer);
                        else
                            profile.HiddenLayers.Remove((int)layer);
                    }
                )
            );

        return panel;
    }
}
