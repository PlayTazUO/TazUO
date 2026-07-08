using System;
using System.Collections.Generic;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.Gumps.GridHighLight;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for container settings, covering both original and grid-style containers</summary>
public static class ContainersTab
{
    /// <summary>Returns the tab group containing original-containers and grid-containers sub-tabs</summary>
    internal static IOptionSource GetContent() => GetContainerMenuTabs();

    private static OptionTabGroup GetContainerMenuTabs()
    {
        ModernOptionsGumpLanguage.Containers containerLang = Language.Instance.GetModernOptionsGumpLanguage.GetContainers;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

        return new OptionTabGroup()
            .AddTab(
                containerLang.LabelOriginalContainers,
                GetStandardContainerSection,
                new SearchMetadata(containerLang.LabelOriginalContainers, Keywords: [kw.Original, kw.Standard])
            )
            .AddTab(
                containerLang.LabelGridContainers,
                GetGridContainerSection,
                new SearchMetadata(containerLang.LabelGridContainers, Keywords: [kw.Grid])
            );
    }

    private static IOptionSource GetStandardContainerSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Containers containerLang = lang.GetContainers;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        var content = new List<OptionContent>
        {
            GetNormalContainerCheckboxesSection(),
            GetNormalContainersScalingSection(),
            Option.Button(
                containerLang.RebuildContainersTxt,
                () => World.Instance.ContainerManager.BuildContainerFile(true)
            )
        };

        if (Client.Game.UO.Version >= ClientVersion.CV_706000)
            content.Add(
                Option.Checkbox(
                    containerLang.UseLargeContainerGumps,
                    new Accessor<bool>(() => profile.UseLargeContainerGumps)
                )
            );

        if (Client.Game.UO.Version >= ClientVersion.CV_705301)
            content.Add(
                Option.ComboBox(
                    containerLang.CharacterBackpackStyle,
                    profile.BackpackStyle,
                    [
                        containerLang.BackpackOpt_Default,
                        containerLang.BackpackOpt_Suede,
                        containerLang.BackpackOpt_PolarBear,
                        containerLang.BackpackOpt_GhoulSkin
                    ],
                    i => profile.BackpackStyle = i
                )
            );

        return OptionsUi.Vertical([.. content])
            .WithSearch(new SearchMetadata(containerLang.LabelOriginalContainers, Tags: [kw.Container, kw.Original]));
    }

    private static OptionFragment GetNormalContainerCheckboxesSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Containers containerLang = lang.GetContainers;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return OptionsUi.Vertical(
            Option.Checkbox(
                containerLang.DoubleClickToLootItemsInsideContainers,
                new Accessor<bool>(() => profile.DoubleClickToLootInsideContainers),
                search: new SearchMetadata(containerLang.DoubleClickToLootItemsInsideContainers, Keywords: [kw.Double, kw.Click, kw.Loot])
            ),
            Option.Checkbox(
                containerLang.RelativeDragAndDropItemsInContainers,
                new Accessor<bool>(() => profile.RelativeDragAndDropItems),
                search: new SearchMetadata(containerLang.RelativeDragAndDropItemsInContainers, Keywords: [kw.Relative, kw.Drag, kw.Drop])
            ),
            Option.Checkbox(
                containerLang.HighlightContainerOnGroundWhenMouseIsOverAContainerGump,
                new Accessor<bool>(() => profile.HighlightContainerWhenSelected),
                search: new SearchMetadata(
                    containerLang.HighlightContainerOnGroundWhenMouseIsOverAContainerGump,
                    Keywords: [kw.Highlight, kw.Ground, kw.Mouse, kw.Over]
                )
            ),
            Option.Checkbox(
                containerLang.RecolorContainerGumpByWithContainerHue,
                new Accessor<bool>(() => profile.HueContainerGumps),
                search: new SearchMetadata(containerLang.RecolorContainerGumpByWithContainerHue, Keywords: [kw.Recolor, kw.Hue])
            ),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.OverrideContainerLocation), containerLang.OverrideContainerGumpLocations),
                Option.ComboBox(
                    containerLang.OverridePosition,
                    profile.OverrideContainerLocationSetting,
                    [
                        containerLang.PositionOpt_NearContainer,
                        containerLang.PositionOpt_TopRight,
                        containerLang.PositionOpt_LastDraggedPosition,
                        containerLang.RememberEachContainer
                    ],
                    i => profile.OverrideContainerLocationSetting = i,
                    search: new SearchMetadata(containerLang.OverridePosition, Keywords: [kw.Position])
                )
            ).WithSearch(new SearchMetadata(containerLang.LabelOriginalContainers, Tags: [kw.Container], Keywords: [kw.Container, kw.Override, kw.Location]))
        );
    }

    private static OptionFragment GetNormalContainersScalingSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Containers containerLang = lang.GetContainers;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = containerLang.ContainerScale },
            Option.Slider(
                containerLang.ContainerScale,
                Constants.MIN_CONTAINER_SIZE_PERC,
                Constants.MAX_CONTAINER_SIZE_PERC,
                new Accessor<float>(() => profile.ContainersScale, f =>
                {
                    profile.ContainersScale = (byte)f;
                    UIManager.ContainerScale = (byte)f / 100f;
                    UIManager.ForEach<ContainerGump>(c => c.RequestUpdateContents());
                }),
                search: new SearchMetadata(containerLang.ContainerScale, Keywords: [kw.Scale, kw.Size])
            ),
            Option.Checkbox(
                containerLang.AlsoScaleItems,
                new Accessor<bool>(() => profile.ScaleItemsInsideContainers),
                search: new SearchMetadata(containerLang.AlsoScaleItems, Keywords: [kw.Scale, kw.Item])
            )
        );
    }

    private static IOptionSource GetGridContainerSection() =>
        OptionsUi.Horizontal(
            GetGridContainerLeftSide(),
            GetGridContainerRightSide()
        );

    private static OptionFragment GetGridContainerRightSide() =>
        OptionsUi.Vertical(
            GetGridContainerHighlightingSection()
        );

    private static OptionFragment GetGridContainerLeftSide()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Containers containerLang = lang.GetContainers;
        ModernOptionsGumpLanguage.TazUO tuoLang = lang.GetTazUO;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return OptionsUi.VisualContainer(
            new VisualContainerProps
            {
                LabelText = containerLang.LabelGridContainersWiki,
                LabelLink = "https://tazuo.org/wiki/tazuogrid-containers/"
            },
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.UseGridLayoutContainerGumps), tuoLang.EnableGridContainers),
                Option.Checkbox(
                    tuoLang.GridContainersDefaultToOldStyleView,
                    new Accessor<bool>(() => profile.GridContainersDefaultToOldStyleView),
                    search: new SearchMetadata(tuoLang.GridContainersDefaultToOldStyleView, Keywords: [kw.Old, kw.Style, kw.View])
                ),
                Option.ComboBox(
                    TazLang.Get("gridcontainer_defaultview", "Default container view"),
                    profile.GridContainerViewMode,
                    [TazLang.Get("gridcontainer_view_grid_short", "Grid"), TazLang.Get("gridcontainer_view_list_short", "List")],
                    i =>
                    {
                        profile.GridContainerViewMode = i;
                        GridContainer.UpdateAllGridContainers();
                    },
                    search: new SearchMetadata(TazLang.Get("gridcontainer_defaultview", "Default container view"), Keywords: [kw.View, kw.Grid])
                ),
                Option.ComboBox(
                    tuoLang.SearchStyle,
                    profile.GridContainerSearchMode,
                    [tuoLang.OnlyShow, tuoLang.Highlight],
                    i => profile.GridContainerSearchMode = i,
                    search: new SearchMetadata(tuoLang.SearchStyle, Keywords: [kw.Search, kw.Style])
                ),
                Option.Checkbox(
                    tuoLang.EnableContainerPreview,
                    new Accessor<bool>(() => profile.GridEnableContPreview),
                    tuoLang.TooltipPreview,
                    search: new SearchMetadata(tuoLang.EnableContainerPreview, Keywords: [kw.Preview])
                ),
                Option.Checkbox(
                    tuoLang.MakeAnchorable,
                    new Accessor<bool>(
                        () => profile.EnableGridContainerAnchor,
                        b =>
                        {
                            profile.EnableGridContainerAnchor = b;
                            GridContainer.UpdateAllGridContainers();
                        }
                    ),
                    tuoLang.TooltipGridAnchor,
                    search: new SearchMetadata(tuoLang.MakeAnchorable, Keywords: [kw.Anchor])
                ),
                Option.Checkbox(
                    tuoLang.GridDisableTargeting,
                    new Accessor<bool>(() => profile.DisableTargetingGridContainers),
                    search: new SearchMetadata(tuoLang.GridDisableTargeting, Keywords: [kw.Targeting, kw.Disable])
                ),
                GetGridContainerStylingSection()
            ).WithSearch(new SearchMetadata(containerLang.LabelGridContainers, Tags: [kw.Container, kw.Grid], Keywords: [kw.Grid, kw.Container]))
        );
    }

    private static OptionFragment GetGridContainerStylingSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Containers containerLang = lang.GetContainers;
        ModernOptionsGumpLanguage.TazUO tuoLang = lang.GetTazUO;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = containerLang.LabelGridContainerStyling },
            Option.ComboBox(
                tuoLang.ContainerStyle,
                profile.Grid_BorderStyle,
                Enum.GetNames<BorderStyle>(),
                i =>
                {
                    profile.Grid_BorderStyle = i;
                    GridContainer.UpdateAllGridContainers();
                },
                search: new SearchMetadata(tuoLang.ContainerStyle, Keywords: [kw.Style, kw.Border])
            ),
            Option.Slider(
                tuoLang.GridContainerScale,
                50,
                200,
                new Accessor<float>(() => profile.GridContainersScale, f => profile.GridContainersScale = (byte)f),
                search: new SearchMetadata(tuoLang.GridContainerScale, Keywords: [kw.Scale, kw.Size])
            ),
            Option.Checkbox(
                tuoLang.AlsoScaleItems,
                new Accessor<bool>(() => profile.GridContainerScaleItems),
                search: new SearchMetadata(tuoLang.AlsoScaleItems, Keywords: [kw.Scale, kw.Item])
            ),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.GridHighlightLowContrastItems), tuoLang.HighlightLowContrastItems),
                Option.LComboBox(
                    tuoLang.LowContrastHighlightStyle,
                    new Accessor<LowContrastHighlightStyle>(
                        () => (LowContrastHighlightStyle)profile.GridHighlightLowContrastItemsStyle,
                        newValue => profile.GridHighlightLowContrastItemsStyle = (int)newValue
                    ),
                    search: new SearchMetadata(tuoLang.LowContrastHighlightStyle, Keywords: [kw.Style])
                )
            ).WithSearch(new SearchMetadata(tuoLang.HighlightLowContrastItems, Keywords: [kw.Highlight, kw.Low, kw.Contrast, kw.Item])),
            Option.Slider(
                tuoLang.GridItemBorderOpacity,
                0,
                100,
                new Accessor<float>(() => profile.GridBorderAlpha, f =>
                {
                    profile.GridBorderAlpha = (byte)f;
                    GridItem.StaticGridContainerSettingUpdated();
                }),
                search: new SearchMetadata(tuoLang.GridItemBorderOpacity, Keywords: [kw.Border, kw.Opacity])
            ),
            Option.HuePicker(
                tuoLang.BorderColor,
                new Accessor<ushort>(() => profile.GridBorderHue, h =>
                {
                    profile.GridBorderHue = h;
                    GridItem.StaticGridContainerSettingUpdated();
                }),
                search: new SearchMetadata(tuoLang.BorderColor, Keywords: [kw.Border, kw.Color])
            ),
            Option.Slider(
                tuoLang.ContainerOpacity,
                0,
                100,
                new Accessor<float>(() => profile.ContainerOpacity, f =>
                {
                    profile.ContainerOpacity = (byte)f;
                    GridContainer.UpdateAllGridContainers();
                }),
                search: new SearchMetadata(tuoLang.ContainerOpacity, Keywords: [kw.Opacity])
            ),
            Option.HuePicker(
                tuoLang.BackgroundColor,
                new Accessor<ushort>(
                    () => profile.AltGridContainerBackgroundHue,
                    h =>
                    {
                        profile.AltGridContainerBackgroundHue = h;
                        GridContainer.UpdateAllGridContainers();
                    }
                ),
                new SearchMetadata(tuoLang.BackgroundColor, Keywords: [kw.Background, kw.Color])
            ),
            Option.Checkbox(
                tuoLang.UseContainersHue,
                new Accessor<bool>(
                    () => profile.Grid_UseContainerHue,
                    b =>
                    {
                        profile.Grid_UseContainerHue = b;
                        GridContainer.UpdateAllGridContainers();
                    }
                ),
                search: new SearchMetadata(tuoLang.UseContainersHue, Keywords: [kw.Hue])
            ),
            Option.Checkbox(
                tuoLang.HideBorders,
                new Accessor<bool>(
                    () => profile.Grid_HideBorder,
                    b =>
                    {
                        profile.Grid_HideBorder = b;
                        GridContainer.UpdateAllGridContainers();
                    }
                ),
                search: new SearchMetadata(tuoLang.HideBorders, Keywords: [kw.Hide, kw.Border])
            ),
            Option.Slider(
                tuoLang.DefaultGridRows,
                1,
                20,
                new Accessor<int>(() => profile.Grid_DefaultRows),
                search: new SearchMetadata(tuoLang.DefaultGridRows, Keywords: [kw.Row])
            ),
            Option.Slider(
                tuoLang.DefaultGridColumns,
                1,
                20,
                new Accessor<int>(() => profile.Grid_DefaultColumns),
                search: new SearchMetadata(tuoLang.DefaultGridColumns, Keywords: [kw.Column])
            )
        );
    }

    private static OptionFragment GetGridContainerHighlightingSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Containers containerLang = lang.GetContainers;
        ModernOptionsGumpLanguage.TazUO tuoLang = lang.GetTazUO;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        return OptionsUi.VisualContainer(
            new VisualContainerProps
            {
                LabelText = containerLang.LabelGridContainerHighlighting,
                LabelLink = "https://tazuo.org/wiki/grid-highlighting/"
            },
            Option.Slider(
                tuoLang.GridHighlightSize,
                1,
                5,
                new Accessor<int>(() => profile.GridHighlightSize),
                search: new SearchMetadata(tuoLang.GridHighlightSize, Keywords: [kw.Highlight, kw.Size])
            ),
            Option.Checkbox(
                tuoLang.GridHighlightProperties,
                new Accessor<bool>(() => profile.GridHighlightProperties),
                search: new SearchMetadata(tuoLang.GridHighlightProperties, Keywords: [kw.Highlight, kw.Property])
            ),
            Option.Checkbox(
                tuoLang.GridHighlightShowRuleName,
                new Accessor<bool>(() => profile.GridHighlightShowRuleName),
                search: new SearchMetadata(tuoLang.GridHighlightShowRuleName, Keywords: [kw.Highlight, kw.Rule, kw.Name])
            ),
            Option.Button(
                tuoLang.GridHighlightSettings,
                () => GridHighlightMenu.Open(World.Instance),
                search: new SearchMetadata(tuoLang.GridHighlightSettings, Keywords: [kw.Highlight, kw.Setting])
            )
        );
    }
}
