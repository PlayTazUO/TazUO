using System;
using System.Collections.Generic;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.Gumps.GridContainers;
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

        return new OptionTabGroup()
            .AddTab(
                TazLang.Get("mog_containers_labelgeneral"),
                GetGeneralContainerSection,
                new SearchMetadata(TazLang.Get("mog_containers_labelgeneral"), Keywords: [TazLang.Get("mog_kw_general")])
            )
            .AddTab(
                TazLang.Get("mog_containers_labeloriginalcontainers"),
                GetStandardContainerSection,
                new SearchMetadata(TazLang.Get("mog_containers_labeloriginalcontainers"), Keywords: [TazLang.Get("mog_kw_original"), TazLang.Get("mog_kw_standard")])
            )
            .AddTab(
                TazLang.Get("mog_containers_labelgridcontainers"),
                GetGridContainerSection,
                new SearchMetadata(TazLang.Get("mog_containers_labelgridcontainers"), Keywords: [TazLang.Get("mog_kw_grid")])
            );
    }

    private static IOptionSource GetGeneralContainerSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = TazLang.Get("mog_containers_labelgeneral") },
                Option.LComboBox(
                    TazLang.Get("mog_general_containerstyle"),
                    new Accessor<ContainerStyle>(() => profile.ContainerStyle, v => profile.ContainerStyle = v),
                    "mog_containerstyleopt_",
                    search: new SearchMetadata(TazLang.Get("mog_general_containerstyle"), Keywords: [TazLang.Get("mog_kw_container"), TazLang.Get("mog_kw_style")])
                ),
                Option.LComboBox(
                    TazLang.Get("mog_tazuo_corpsecontainerstyle"),
                    new Accessor<CorpseContainerStyle>(() => profile.CorpseContainerStyle, v => profile.CorpseContainerStyle = v),
                    "mog_tazuo_corpsestyleopt_",
                    TazLang.Get("mog_tazuo_tooltipcorpsecontainerstyle"),
                    search: new SearchMetadata(TazLang.Get("mog_tazuo_corpsecontainerstyle"), Keywords: [TazLang.Get("mog_kw_corpse"), TazLang.Get("mog_kw_style"), TazLang.Get("mog_kw_container")])
                )
            )
            .WithSearch(new SearchMetadata(TazLang.Get("mog_containers_labelgeneral"), Tags: [TazLang.Get("mog_kw_container")], Keywords: [TazLang.Get("mog_kw_container"), TazLang.Get("mog_kw_style")]));
    }

    private static IOptionSource GetStandardContainerSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        var content = new List<OptionContent>
        {
            GetNormalContainerCheckboxesSection(),
            GetNormalContainersScalingSection(),
            Option.Button(
                TazLang.Get("mog_containers_rebuildcontainerstxt"),
                () => World.Instance.ContainerManager.BuildContainerFile(true)
            )
        };

        if (Client.Game.UO.Version >= ClientVersion.CV_706000)
            content.Add(
                Option.Checkbox(
                    TazLang.Get("mog_containers_uselargecontainergumps"),
                    new Accessor<bool>(() => profile.UseLargeContainerGumps)
                )
            );

        if (Client.Game.UO.Version >= ClientVersion.CV_705301)
            content.Add(
                Option.ComboBox(
                    TazLang.Get("mog_containers_characterbackpackstyle"),
                    profile.BackpackStyle,
                    [
                        TazLang.Get("mog_containers_backpackopt_default"),
                        TazLang.Get("mog_containers_backpackopt_suede"),
                        TazLang.Get("mog_containers_backpackopt_polarbear"),
                        TazLang.Get("mog_containers_backpackopt_ghoulskin")
                    ],
                    i => profile.BackpackStyle = i
                )
            );

        return OptionsUi.Vertical([.. content])
            .WithSearch(new SearchMetadata(TazLang.Get("mog_containers_labeloriginalcontainers"), Tags: [TazLang.Get("mog_kw_container"), TazLang.Get("mog_kw_original")]));
    }

    private static OptionFragment GetNormalContainerCheckboxesSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.Vertical(
            Option.Checkbox(
                TazLang.Get("mog_containers_doubleclicktolootitemsinsidecontainers"),
                new Accessor<bool>(() => profile.DoubleClickToLootInsideContainers),
                search: new SearchMetadata(TazLang.Get("mog_containers_doubleclicktolootitemsinsidecontainers"), Keywords: [TazLang.Get("mog_kw_double"), TazLang.Get("mog_kw_click"), TazLang.Get("mog_kw_loot")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_containers_relativedraganddropitemsincontainers"),
                new Accessor<bool>(() => profile.RelativeDragAndDropItems),
                search: new SearchMetadata(TazLang.Get("mog_containers_relativedraganddropitemsincontainers"), Keywords: [TazLang.Get("mog_kw_relative"), TazLang.Get("mog_kw_drag"), TazLang.Get("mog_kw_drop")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_containers_highlightcontainerongroundwhenmouseisoveracontainergump"),
                new Accessor<bool>(() => profile.HighlightContainerWhenSelected),
                search: new SearchMetadata(
                    TazLang.Get("mog_containers_highlightcontainerongroundwhenmouseisoveracontainergump"),
                    Keywords: [TazLang.Get("mog_kw_highlight"), TazLang.Get("mog_kw_ground"), TazLang.Get("mog_kw_mouse"), TazLang.Get("mog_kw_over")]
                )
            ),
            Option.Checkbox(
                TazLang.Get("mog_containers_recolorcontainergumpbywithcontainerhue"),
                new Accessor<bool>(() => profile.HueContainerGumps),
                search: new SearchMetadata(TazLang.Get("mog_containers_recolorcontainergumpbywithcontainerhue"), Keywords: [TazLang.Get("mog_kw_recolor"), TazLang.Get("mog_kw_hue")])
            ),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.OverrideContainerLocation), TazLang.Get("mog_containers_overridecontainergumplocations")),
                Option.ComboBox(
                    TazLang.Get("mog_containers_overrideposition"),
                    profile.OverrideContainerLocationSetting,
                    [
                        TazLang.Get("mog_containers_positionopt_nearcontainer"),
                        TazLang.Get("mog_containers_positionopt_topright"),
                        TazLang.Get("mog_containers_positionopt_lastdraggedposition"),
                        TazLang.Get("mog_containers_remembereachcontainer")
                    ],
                    i => profile.OverrideContainerLocationSetting = i,
                    search: new SearchMetadata(TazLang.Get("mog_containers_overrideposition"), Keywords: [TazLang.Get("mog_kw_position")])
                )
            ).WithSearch(new SearchMetadata(TazLang.Get("mog_containers_labeloriginalcontainers"), Tags: [TazLang.Get("mog_kw_container")], Keywords: [TazLang.Get("mog_kw_container"), TazLang.Get("mog_kw_override"), TazLang.Get("mog_kw_location")]))
        );
    }

    private static OptionFragment GetNormalContainersScalingSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = TazLang.Get("mog_containers_containerscale") },
            Option.Slider(
                TazLang.Get("mog_containers_containerscale"),
                Constants.MIN_CONTAINER_SIZE_PERC,
                Constants.MAX_CONTAINER_SIZE_PERC,
                new Accessor<float>(() => profile.ContainersScale, f =>
                {
                    profile.ContainersScale = (byte)f;
                    UIManager.ContainerScale = (byte)f / 100f;
                    UIManager.ForEach<ContainerGump>(c => c.RequestUpdateContents());
                }),
                search: new SearchMetadata(TazLang.Get("mog_containers_containerscale"), Keywords: [TazLang.Get("mog_kw_scale"), TazLang.Get("mog_kw_size")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_containers_alsoscaleitems"),
                new Accessor<bool>(() => profile.ScaleItemsInsideContainers),
                search: new SearchMetadata(TazLang.Get("mog_containers_alsoscaleitems"), Keywords: [TazLang.Get("mog_kw_scale"), TazLang.Get("mog_kw_item")])
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

        return OptionsUi.VisualContainer(
            new VisualContainerProps
            {
                LabelText = TazLang.Get("mog_containers_labelgridcontainerswiki"),
                LabelLink = "https://tazuo.org/wiki/tazuogrid-containers/"
            },
            Option.Checkbox(
                TazLang.Get("mog_tazuo_gridcontainersdefaulttooldstyleview"),
                new Accessor<bool>(() => profile.GridContainersDefaultToOldStyleView),
                search: new SearchMetadata(TazLang.Get("mog_tazuo_gridcontainersdefaulttooldstyleview"), Keywords: [TazLang.Get("mog_kw_old"), TazLang.Get("mog_kw_style"), TazLang.Get("mog_kw_view")])
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
                search: new SearchMetadata(TazLang.Get("gridcontainer_defaultview", "Default container view"), Keywords: [TazLang.Get("mog_kw_view"), TazLang.Get("mog_kw_grid")])
            ),
            Option.ComboBox(
                TazLang.Get("mog_tazuo_searchstyle"),
                profile.GridContainerSearchMode,
                [TazLang.Get("mog_tazuo_onlyshow"), TazLang.Get("mog_tazuo_highlight")],
                i => profile.GridContainerSearchMode = i,
                search: new SearchMetadata(TazLang.Get("mog_tazuo_searchstyle"), Keywords: [TazLang.Get("mog_kw_search"), TazLang.Get("mog_kw_style")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_tazuo_enablecontainerpreview"),
                new Accessor<bool>(() => profile.GridEnableContPreview),
                TazLang.Get("mog_tazuo_tooltippreview"),
                search: new SearchMetadata(TazLang.Get("mog_tazuo_enablecontainerpreview"), Keywords: [TazLang.Get("mog_kw_preview")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_tazuo_makeanchorable"),
                new Accessor<bool>(
                    () => profile.EnableGridContainerAnchor,
                    b =>
                    {
                        profile.EnableGridContainerAnchor = b;
                        GridContainer.UpdateAllGridContainers();
                    }
                ),
                TazLang.Get("mog_tazuo_tooltipgridanchor"),
                search: new SearchMetadata(TazLang.Get("mog_tazuo_makeanchorable"), Keywords: [TazLang.Get("mog_kw_anchor")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_tazuo_griddisabletargeting"),
                new Accessor<bool>(() => profile.DisableTargetingGridContainers),
                search: new SearchMetadata(TazLang.Get("mog_tazuo_griddisabletargeting"), Keywords: [TazLang.Get("mog_kw_targeting"), TazLang.Get("mog_kw_disable")])
            ),
            GetGridContainerBandsSection(),
            GetGridContainerStylingSection()
        );
    }

    private static OptionFragment GetGridContainerBandsSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = TazLang.Get("gridbands_section", "Grid Container Bands") },
            Option.Button(
                TazLang.Get("gridbands_configure", "Configure bands"),
                () => GridContainerBandsMenu.Open(World.Instance),
                search: new SearchMetadata(TazLang.Get("gridbands_configure", "Configure bands"), Keywords: [TazLang.Get("mog_kw_grid"), TazLang.Get("gridbands_kw", "bands")])
            )
        );
    }

    private static OptionFragment GetGridContainerStylingSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = TazLang.Get("mog_containers_labelgridcontainerstyling") },
            Option.ComboBox(
                TazLang.Get("mog_tazuo_containerstyle"),
                profile.Grid_BorderStyle,
                Enum.GetNames<BorderStyle>(),
                i =>
                {
                    profile.Grid_BorderStyle = i;
                    GridContainer.UpdateAllGridContainers();
                },
                search: new SearchMetadata(TazLang.Get("mog_tazuo_containerstyle"), Keywords: [TazLang.Get("mog_kw_style"), TazLang.Get("mog_kw_border")])
            ),
            Option.Slider(
                TazLang.Get("mog_tazuo_gridcontainerscale"),
                50,
                200,
                new Accessor<float>(() => profile.GridContainersScale, f => profile.GridContainersScale = (byte)f),
                search: new SearchMetadata(TazLang.Get("mog_tazuo_gridcontainerscale"), Keywords: [TazLang.Get("mog_kw_scale"), TazLang.Get("mog_kw_size")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_tazuo_alsoscaleitems"),
                new Accessor<bool>(() => profile.GridContainerScaleItems),
                search: new SearchMetadata(TazLang.Get("mog_tazuo_alsoscaleitems"), Keywords: [TazLang.Get("mog_kw_scale"), TazLang.Get("mog_kw_item")])
            ),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.GridHighlightLowContrastItems), TazLang.Get("mog_tazuo_highlightlowcontrastitems")),
                Option.LComboBox(
                    TazLang.Get("mog_tazuo_lowcontrasthighlightstyle"),
                    new Accessor<LowContrastHighlightStyle>(
                        () => (LowContrastHighlightStyle)profile.GridHighlightLowContrastItemsStyle,
                        newValue => profile.GridHighlightLowContrastItemsStyle = (int)newValue
                    ),
                    search: new SearchMetadata(TazLang.Get("mog_tazuo_lowcontrasthighlightstyle"), Keywords: [TazLang.Get("mog_kw_style")])
                )
            ).WithSearch(new SearchMetadata(TazLang.Get("mog_tazuo_highlightlowcontrastitems"), Keywords: [TazLang.Get("mog_kw_highlight"), TazLang.Get("mog_kw_low"), TazLang.Get("mog_kw_contrast"), TazLang.Get("mog_kw_item")])),
            Option.Slider(
                TazLang.Get("mog_tazuo_griditemborderopacity"),
                0,
                100,
                new Accessor<float>(() => profile.GridBorderAlpha, f =>
                {
                    profile.GridBorderAlpha = (byte)f;
                    GridItem.StaticGridContainerSettingUpdated();
                }),
                search: new SearchMetadata(TazLang.Get("mog_tazuo_griditemborderopacity"), Keywords: [TazLang.Get("mog_kw_border"), TazLang.Get("mog_kw_opacity")])
            ),
            Option.HuePicker(
                TazLang.Get("mog_tazuo_bordercolor"),
                new Accessor<ushort>(() => profile.GridBorderHue, h =>
                {
                    profile.GridBorderHue = h;
                    GridItem.StaticGridContainerSettingUpdated();
                }),
                search: new SearchMetadata(TazLang.Get("mog_tazuo_bordercolor"), Keywords: [TazLang.Get("mog_kw_border"), TazLang.Get("mog_kw_color")])
            ),
            Option.Slider(
                TazLang.Get("mog_tazuo_containeropacity"),
                0,
                100,
                new Accessor<float>(() => profile.ContainerOpacity, f =>
                {
                    profile.ContainerOpacity = (byte)f;
                    GridContainer.UpdateAllGridContainers();
                }),
                search: new SearchMetadata(TazLang.Get("mog_tazuo_containeropacity"), Keywords: [TazLang.Get("mog_kw_opacity")])
            ),
            Option.HuePicker(
                TazLang.Get("mog_tazuo_backgroundcolor"),
                new Accessor<ushort>(
                    () => profile.AltGridContainerBackgroundHue,
                    h =>
                    {
                        profile.AltGridContainerBackgroundHue = h;
                        GridContainer.UpdateAllGridContainers();
                    }
                ),
                new SearchMetadata(TazLang.Get("mog_tazuo_backgroundcolor"), Keywords: [TazLang.Get("mog_kw_background"), TazLang.Get("mog_kw_color")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_tazuo_usecontainershue"),
                new Accessor<bool>(
                    () => profile.Grid_UseContainerHue,
                    b =>
                    {
                        profile.Grid_UseContainerHue = b;
                        GridContainer.UpdateAllGridContainers();
                    }
                ),
                search: new SearchMetadata(TazLang.Get("mog_tazuo_usecontainershue"), Keywords: [TazLang.Get("mog_kw_hue")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_tazuo_hideborders"),
                new Accessor<bool>(
                    () => profile.Grid_HideBorder,
                    b =>
                    {
                        profile.Grid_HideBorder = b;
                        GridContainer.UpdateAllGridContainers();
                    }
                ),
                search: new SearchMetadata(TazLang.Get("mog_tazuo_hideborders"), Keywords: [TazLang.Get("mog_kw_hide"), TazLang.Get("mog_kw_border")])
            ),
            Option.Slider(
                TazLang.Get("mog_tazuo_defaultgridrows"),
                1,
                20,
                new Accessor<int>(() => profile.Grid_DefaultRows),
                search: new SearchMetadata(TazLang.Get("mog_tazuo_defaultgridrows"), Keywords: [TazLang.Get("mog_kw_row")])
            ),
            Option.Slider(
                TazLang.Get("mog_tazuo_defaultgridcolumns"),
                1,
                20,
                new Accessor<int>(() => profile.Grid_DefaultColumns),
                search: new SearchMetadata(TazLang.Get("mog_tazuo_defaultgridcolumns"), Keywords: [TazLang.Get("mog_kw_column")])
            )
        );
    }

    private static OptionFragment GetGridContainerHighlightingSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.VisualContainer(
            new VisualContainerProps
            {
                LabelText = TazLang.Get("mog_containers_labelgridcontainerhighlighting"),
                LabelLink = "https://tazuo.org/wiki/grid-highlighting/"
            },
            Option.Slider(
                TazLang.Get("mog_tazuo_gridhighlightsize"),
                1,
                5,
                new Accessor<int>(() => profile.GridHighlightSize),
                search: new SearchMetadata(TazLang.Get("mog_tazuo_gridhighlightsize"), Keywords: [TazLang.Get("mog_kw_highlight"), TazLang.Get("mog_kw_size")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_tazuo_gridhighlightproperties"),
                new Accessor<bool>(() => profile.GridHighlightProperties),
                search: new SearchMetadata(TazLang.Get("mog_tazuo_gridhighlightproperties"), Keywords: [TazLang.Get("mog_kw_highlight"), TazLang.Get("mog_kw_property")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_tazuo_gridhighlightshowrulename"),
                new Accessor<bool>(() => profile.GridHighlightShowRuleName),
                search: new SearchMetadata(TazLang.Get("mog_tazuo_gridhighlightshowrulename"), Keywords: [TazLang.Get("mog_kw_highlight"), TazLang.Get("mog_kw_rule"), TazLang.Get("mog_kw_name")])
            ),
            Option.Button(
                TazLang.Get("mog_tazuo_gridhighlightsettings"),
                () => GridHighlightMenu.Open(World.Instance),
                search: new SearchMetadata(TazLang.Get("mog_tazuo_gridhighlightsettings"), Keywords: [TazLang.Get("mog_kw_highlight"), TazLang.Get("mog_kw_setting")])
            )
        );
    }
}
