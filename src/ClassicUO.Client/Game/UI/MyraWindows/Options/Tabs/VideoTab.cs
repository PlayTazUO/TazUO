using System;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

/// <summary>Options tab source for video, display, zoom, lighting, and frame-rate settings</summary>
public static class VideoTab
{
    /// <summary>Returns the tab group containing game-window, zoom/scaling, and lighting sub-tabs</summary>
    internal static IOptionSource GetContent() => GetVideoMenuTabs();

    private static OptionTabGroup GetVideoMenuTabs()
    {

        return new OptionTabGroup()
            .AddTab(
                TazLang.Get("mog_videotab_gamewindow_label"),
                GetGameWindowSubTabContent,
                new SearchMetadata(TazLang.Get("mog_videotab_gamewindow_label"), Keywords: [TazLang.Get("mog_kw_window"), TazLang.Get("mog_kw_viewport"), TazLang.Get("mog_kw_fullscreen"), TazLang.Get("mog_kw_fps"), TazLang.Get("mog_kw_vsync")])
            )
            .AddTab(
                TazLang.Get("mog_videotab_zoom_label"),
                GetZoomAndScalingSubTubContent,
                new SearchMetadata(TazLang.Get("mog_videotab_zoom_label"), Keywords: [TazLang.Get("mog_kw_zoom"), TazLang.Get("mog_kw_scale"), TazLang.Get("mog_kw_scaling"), TazLang.Get("mog_kw_paperdoll"), TazLang.Get("mog_kw_global")])
            )
            .AddTab(
                TazLang.Get("mog_videotab_lighting_label"),
                GetLightningSubTabContent,
                new SearchMetadata(TazLang.Get("mog_videotab_lighting_label"), Keywords: [TazLang.Get("mog_kw_light"), TazLang.Get("mog_kw_darkness"), TazLang.Get("mog_kw_night"), TazLang.Get("mog_kw_color")])
            )
            .AddTab(
                TazLang.Get("mog_videotab_shadows_label"),
                GetShadowSubTabContent,
                new SearchMetadata(TazLang.Get("mog_videotab_shadows_label"), Keywords: [TazLang.Get("mog_kw_shadow"), TazLang.Get("mog_kw_static"), TazLang.Get("mog_kw_terrain")])
            )
            .AddTab(
                TazLang.Get("mog_videotab_misc_label"),
                GetMiscSubTabContent,
                new SearchMetadata(TazLang.Get("mog_videotab_misc_label"), Keywords: [TazLang.Get("mog_kw_misc"), TazLang.Get("mog_kw_miscellaneous")])
            );
    }

    private static IOptionSource GetGameWindowSubTabContent()
    {
        return OptionsUi.Vertical(
            GetRendererSection(),
            GetViewportSettingsGroup()
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_videotab_gamewindow_label"), Tags: [TazLang.Get("mog_kw_window"), TazLang.Get("mog_kw_viewport")]));
    }

    private static OptionFragment GetRendererSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = TazLang.Get("mog_videotab_gamewindow_rendererlabel") },
            Option.Slider(
                TazLang.Get("mog_videotab_gamewindow_fpscap"),
                Constants.MIN_FPS,
                Constants.MAX_FPS,
                new Accessor<float>(() => Settings.GlobalSettings.FPS, f =>
                {
                    Settings.GlobalSettings.FPS = (int)f;
                    Client.Game.SetRefreshRate((int)f);
                }),
                search: new SearchMetadata(TazLang.Get("mog_videotab_gamewindow_fpscap"), Keywords: [TazLang.Get("mog_kw_fps"), TazLang.Get("mog_kw_refresh")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_videotab_gamewindow_backgroundfps"),
                new Accessor<bool>(() => profile.ReduceFPSWhenInactive),
                search: new SearchMetadata(TazLang.Get("mog_videotab_gamewindow_backgroundfps"), Keywords: [TazLang.Get("mog_kw_fps"), TazLang.Get("mog_kw_background")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_videotab_gamewindow_enablevsync"),
                new Accessor<bool>(() => profile.EnableVSync, b =>
                {
                    profile.EnableVSync = b;
                    Client.Game?.SetVSync(b);
                }),
                search: new SearchMetadata(TazLang.Get("mog_videotab_gamewindow_enablevsync"), Keywords: [TazLang.Get("mog_kw_vsync")])
            ),
            Option.HuePicker(
                TazLang.Get("mog_tazuo_maingamewindowbackground"),
                profile.MainWindowBackgroundHue,
                newValue =>
                {
                    profile.MainWindowBackgroundHue = newValue;
                    GameController.UpdateBackgroundHueShader();
                },
                new SearchMetadata(TazLang.Get("mog_tazuo_maingamewindowbackground"), Keywords: [TazLang.Get("mog_kw_main"), TazLang.Get("mog_kw_window"), TazLang.Get("mog_kw_background"), TazLang.Get("mog_kw_hue")])
            )
        );
    }

    private static OptionFragment GetViewportSettingsGroup()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = TazLang.Get("mog_videotab_gamewindow_viewportlabel") },
            Option.Checkbox(
                TazLang.Get("mog_videotab_gamewindow_fullsizeviewport"),
                new Accessor<bool>(() => profile.GameWindowFullSize, b =>
                {
                    profile.GameWindowFullSize = b;
                    WorldViewportGump viewport = WorldViewportGump.Instance;
                    if (viewport == null) return;
                    if (b)
                    {
                        viewport.ResizeGameWindow(
                            new Point(Client.Game.Window.ClientBounds.Width, Client.Game.Window.ClientBounds.Height)
                        );
                        viewport.SetGameWindowPosition(new Point(0, 0));
                        profile.GameWindowPosition = new Point(0, 0);
                    }
                    else
                    {
                        viewport.ResizeGameWindow(new Point(600, 480));
                        viewport.SetGameWindowPosition(new Point(25, 25));
                        profile.GameWindowPosition = new Point(25, 25);
                    }
                    viewport.OnWindowResized();
                }),
                search: new SearchMetadata(TazLang.Get("mog_videotab_gamewindow_fullsizeviewport"), Keywords: [TazLang.Get("mog_kw_full"), TazLang.Get("mog_kw_size")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_videotab_gamewindow_fullscreen"),
                profile.WindowBorderless,
                newValue =>
                {
                    profile.WindowBorderless = newValue;
                    Client.Game.SetWindowBorderless(newValue);
                },
                search: new SearchMetadata(TazLang.Get("mog_videotab_gamewindow_fullscreen"), Keywords: [TazLang.Get("mog_kw_fullscreen"), TazLang.Get("mog_kw_borderless")])
            ),
            Option.Checkbox(
                TazLang.Get("video_borderless_window", "Borderless window (no title bar)"),
                profile.BorderlessWindow,
                newValue =>
                {
                    profile.BorderlessWindow = newValue;
                    if (!profile.WindowBorderless)
                        Client.Game.SetWindowBordered(!newValue);
                },
                search: new SearchMetadata(TazLang.Get("video_borderless_window", "Borderless window (no title bar)"), Keywords: [TazLang.Get("mog_kw_borderless"), TazLang.Get("mog_kw_window")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_videotab_gamewindow_lockviewport"),
                new Accessor<bool>(() => profile.GameWindowLock),
                search: new SearchMetadata(TazLang.Get("mog_videotab_gamewindow_lockviewport"), Keywords: [TazLang.Get("mog_kw_lock")])
            ),
            Option.Slider(
                TazLang.Get("mog_videotab_gamewindow_viewportx"),
                0,
                Client.Game.Window.ClientBounds.Width,
                new Accessor<float>(() => profile.GameWindowPosition.X, f =>
                {
                    profile.GameWindowPosition = new Point((int)f, profile.GameWindowPosition.Y);
                    WorldViewportGump.Instance?.SetGameWindowPosition(profile.GameWindowPosition);
                }),
                search: new SearchMetadata(TazLang.Get("mog_videotab_gamewindow_viewportx"), Keywords: [TazLang.Get("mog_kw_x")])
            ),
            Option.Slider(
                TazLang.Get("mog_videotab_gamewindow_viewporty"),
                0,
                Client.Game.Window.ClientBounds.Height,
                new Accessor<float>(() => profile.GameWindowPosition.Y, f =>
                {
                    profile.GameWindowPosition = new Point(profile.GameWindowPosition.X, (int)f);
                    WorldViewportGump.Instance?.SetGameWindowPosition(profile.GameWindowPosition);
                }),
                search: new SearchMetadata(TazLang.Get("mog_videotab_gamewindow_viewporty"), Keywords: [TazLang.Get("mog_kw_y")])
            ),
            Option.Slider(
                TazLang.Get("mog_videotab_gamewindow_viewportw"),
                0,
                Client.Game.Window.ClientBounds.Width,
                new Accessor<float>(() => profile.GameWindowSize.X, f =>
                {
                    profile.GameWindowSize = new Point((int)f, profile.GameWindowSize.Y);
                    WorldViewportGump.Instance?.ResizeGameWindow(profile.GameWindowSize);
                }),
                search: new SearchMetadata(TazLang.Get("mog_videotab_gamewindow_viewportw"), Keywords: [TazLang.Get("mog_kw_width")])
            ),
            Option.Slider(
                TazLang.Get("mog_videotab_gamewindow_viewporth"),
                0,
                Client.Game.Window.ClientBounds.Height,
                new Accessor<float>(() => profile.GameWindowSize.Y, f =>
                {
                    profile.GameWindowSize = new Point(profile.GameWindowSize.X, (int)f);
                    WorldViewportGump.Instance?.ResizeGameWindow(profile.GameWindowSize);
                }),
                search: new SearchMetadata(TazLang.Get("mog_videotab_gamewindow_viewporth"), Keywords: [TazLang.Get("mog_kw_height")])
            )
        );
    }

    private static IOptionSource GetZoomAndScalingSubTubContent()
    {
        return OptionsUi.Vertical(
            GetZoomSection(),
            GetScalingSection()
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_videotab_zoom_label"), Tags: [TazLang.Get("mog_kw_zoom"), TazLang.Get("mog_kw_scale")]));
    }

    private static OptionFragment GetZoomSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        Camera camera = Client.Game.Scene.Camera;
        int cameraZoomCount = (int)((camera.ZoomMax - camera.ZoomMin) / camera.ZoomStep);
        int cameraZoomIndex = cameraZoomCount - (int)((camera.ZoomMax - camera.Zoom) / camera.ZoomStep);

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = TazLang.Get("mog_videotab_zoom_zoomlabel") },
            Option.Slider(
                TazLang.Get("mog_videotab_zoom_defaultzoom"),
                0,
                cameraZoomCount,
                new Accessor<float>(() => cameraZoomIndex, f =>
                {
                    profile.DefaultScale = camera.Zoom = (int)f * camera.ZoomStep + camera.ZoomMin;
                }),
                search: new SearchMetadata(TazLang.Get("mog_videotab_zoom_defaultzoom"), Keywords: [TazLang.Get("mog_kw_zoom")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_videotab_zoom_zoomwheel"),
                new Accessor<bool>(() => profile.EnableMousewheelScaleZoom),
                search: new SearchMetadata(TazLang.Get("mog_videotab_zoom_zoomwheel"), Keywords: [TazLang.Get("mog_kw_wheel")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_videotab_zoom_returndefaultzoom"),
                new Accessor<bool>(() => profile.RestoreScaleAfterUnpressCtrl),
                search: new SearchMetadata(TazLang.Get("mog_videotab_zoom_returndefaultzoom"), Keywords: [TazLang.Get("mog_kw_restore"), TazLang.Get("mog_kw_ctrl")])
            )
        );
    }

    private static OptionFragment GetScalingSection()
    {
        Profile profile = ProfileManager.CurrentProfile;

        float? scale = null;

        return OptionsUi.VisualContainer(
            new VisualContainerProps
            {
                LabelText = TazLang.Get("mog_buttonscaling"),
                LabelLink = "https://tazuo.org/wiki/tazuoglobal-scaling/",
                Spacing = VisualContainerSpacing.Comfortable
            },
            Option.Slider(
                TazLang.Get("mog_videotab_zoom_paperdollscaling"),
                50,
                300,
                new Accessor<float>(() => (int)(profile.PaperdollScale * 100), newValue =>
                {
                    profile.PaperdollScale = Math.Clamp(newValue / 100, 0.5f, 3.0f);
                }),
                search: new SearchMetadata(TazLang.Get("mog_videotab_zoom_paperdollscaling"), Keywords: [TazLang.Get("mog_kw_paperdoll"), TazLang.Get("mog_kw_scale")])
            ),
            Option.Slider(
                TazLang.Get("gumpscaling_statusgumpscaling", "Status gump scaling"),
                50,
                300,
                new Accessor<float>(() => (int)(profile.StatusGumpScale * 100), newValue =>
                {
                    profile.StatusGumpScale = Math.Clamp(newValue / 100, 0.5f, 3.0f);
                }),
                search: new SearchMetadata(TazLang.Get("gumpscaling_statusgumpscaling", "Status gump scaling"), Keywords: [TazLang.Get("mog_kw_scale")])
            ),
            Option.Slider(
                TazLang.Get("gumpscaling_contextmenuscaling", "Context menu scaling"),
                50,
                300,
                new Accessor<float>(() => (int)(profile.ContextMenuScale * 100), newValue =>
                {
                    profile.ContextMenuScale = newValue / 100;
                }),
                search: new SearchMetadata(TazLang.Get("gumpscaling_contextmenuscaling", "Context menu scaling"), Keywords: [TazLang.Get("mog_kw_scale")])
            ),
            Option.Slider(
                TazLang.Get("gumpscaling_tradegumpscaling", "Trade gump scaling"),
                50,
                300,
                new Accessor<float>(() => (int)(profile.TradeGumpScale * 100), newValue =>
                {
                    profile.TradeGumpScale = Math.Clamp(newValue / 100, 0.5f, 3.0f);
                }),
                search: new SearchMetadata(TazLang.Get("gumpscaling_tradegumpscaling", "Trade gump scaling"), Keywords: [TazLang.Get("mog_kw_scale")])
            ),
            Option.Slider(
                TazLang.Get("gumpscaling_servergumpscaling", "Server gump scaling"),
                50,
                300,
                new Accessor<float>(() => (int)(profile.ServerGumpScale * 100), newValue =>
                {
                    profile.ServerGumpScale = Math.Clamp(newValue / 100, 0.5f, 3.0f);
                }),
                search: new SearchMetadata(TazLang.Get("gumpscaling_servergumpscaling", "Server gump scaling"), Keywords: [TazLang.Get("mog_kw_scale")])
            ),
            OptionsUi.Horizontal(
                Option.Slider(
                    TazLang.Get("mog_videotab_zoom_globalscaling"),
                    50,
                    Client.Game.MaxRenderScale * 100,
                    new Accessor<float>(
                        () => Client.Game.RenderScale * 100,
                        newValue =>
                        {
                            scale = Math.Clamp(newValue / 100, 0.5f, Client.Game.MaxRenderScale);
                        }
                    ),
                    search: new SearchMetadata(TazLang.Get("mog_videotab_zoom_globalscaling"), Keywords: [TazLang.Get("mog_kw_global"), TazLang.Get("mog_kw_scale")])
                ),
                Option.Button(
                    TazLang.Get("mog_apply"),
                    () =>
                    {
                        if (scale != null)
                        {
                            Client.Game.SetScale(scale.Value);
                            _ = Client.Settings.SetAsync(SettingsScope.Global, Constants.SqlSettings.GAME_SCALE, scale);
                        }
                    },
                    search: new SearchMetadata(TazLang.Get("mog_apply"))
                )
            )
        ).AsSearchGroup();
    }

    private static IOptionSource GetLightningSubTabContent()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.Vertical(
            Option.Checkbox(
                TazLang.Get("mog_videotab_lighting_altlights"),
                new Accessor<bool>(() => profile.UseAlternativeLights),
                search: new SearchMetadata(TazLang.Get("mog_videotab_lighting_altlights"), Keywords: [TazLang.Get("mog_kw_alt"), TazLang.Get("mog_kw_light")])
            ),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(
                    new Accessor<bool>(() => profile.UseCustomLightLevel, b =>
                    {
                        profile.UseCustomLightLevel = b;
                        UpdateLight();
                    }),
                    TazLang.Get("mog_videotab_lighting_customllevel")
                ),
                Option.Slider(
                    TazLang.Get("mog_videotab_lighting_level"),
                    0,
                    0x1E,
                    new Accessor<float>(() => 0x1E - profile.LightLevel, f =>
                    {
                        profile.LightLevel = (byte)(0x1E - (int)f);
                        UpdateLight();
                    }),
                    search: new SearchMetadata(TazLang.Get("mog_videotab_lighting_level"), Keywords: [TazLang.Get("mog_kw_level")])
                ),
                Option.ComboBox(
                    TazLang.Get("mog_videotab_lighting_lighttype"),
                    profile.LightLevelType,
                    [TazLang.Get("mog_videotab_lighting_lighttype_absolute"), TazLang.Get("mog_videotab_lighting_lighttype_minimum")],
                    i => profile.LightLevelType = i,
                    search: new SearchMetadata(TazLang.Get("mog_videotab_lighting_lighttype"), Keywords: [TazLang.Get("mog_kw_type")])
                )
            ).WithSearch(new SearchMetadata(TazLang.Get("mog_videotab_lighting_label"), Tags: [TazLang.Get("mog_kw_light")], Keywords: [TazLang.Get("mog_kw_custom")])),
            Option.Checkbox(
                TazLang.Get("mog_videotab_lighting_darknight"),
                new Accessor<bool>(() => profile.UseDarkNights),
                search: new SearchMetadata(TazLang.Get("mog_videotab_lighting_darknight"), Keywords: [TazLang.Get("mog_kw_dark"), TazLang.Get("mog_kw_night")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_videotab_lighting_coloredlight"),
                new Accessor<bool>(() => profile.UseColoredLights),
                search: new SearchMetadata(TazLang.Get("mog_videotab_lighting_coloredlight"), Keywords: [TazLang.Get("mog_kw_color"), TazLang.Get("mog_kw_light")])
            )
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_videotab_lighting_label"), Tags: [TazLang.Get("mog_kw_light")]));

        void UpdateLight()
        {
            if (profile.UseCustomLightLevel)
            {
                World.Instance.Light.Overall = profile.LightLevelType == 1
                    ? Math.Min(World.Instance.Light.RealOverall, profile.LightLevel)
                    : profile.LightLevel;
                World.Instance.Light.Personal = 0;
            }
            else
            {
                World.Instance.Light.Overall = World.Instance.Light.RealOverall;
                World.Instance.Light.Personal = World.Instance.Light.RealPersonal;
            }
        }
    }

    private static IOptionSource GetShadowSubTabContent()
    {
        Profile profile = ProfileManager.CurrentProfile;

        return OptionsUi.Vertical(
            Option.Checkbox(
                TazLang.Get("mog_videotab_shadows_enableshadows"),
                new Accessor<bool>(() => profile.ShadowsEnabled),
                search: new SearchMetadata(TazLang.Get("mog_videotab_shadows_enableshadows"), Keywords: [TazLang.Get("mog_kw_shadow")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_videotab_shadows_rocktreeshadows"),
                new Accessor<bool>(() => profile.ShadowsStatics),
                search: new SearchMetadata(TazLang.Get("mog_videotab_shadows_rocktreeshadows"), Keywords: [TazLang.Get("mog_kw_static"), TazLang.Get("mog_kw_rock"), TazLang.Get("mog_kw_tree")])
            ),
            Option.Slider(
                TazLang.Get("mog_videotab_shadows_terrainshadowlevel"),
                Constants.MIN_TERRAIN_SHADOWS_LEVEL,
                Constants.MAX_TERRAIN_SHADOWS_LEVEL,
                new Accessor<float>(() => profile.TerrainShadowsLevel, f => profile.TerrainShadowsLevel = (int)f),
                search: new SearchMetadata(TazLang.Get("mog_videotab_shadows_terrainshadowlevel"), Keywords: [TazLang.Get("mog_kw_terrain")])
            )
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_videotab_shadows_label"), Tags: [TazLang.Get("mog_kw_shadow")]));
    }

    private static IOptionSource GetMiscSubTabContent()
    {
        Profile profile = ProfileManager.CurrentProfile;

        string disableGargAnim = TazLang.Get("disable_gargoyle_flying_animation", "Disable gargoyle flying animation");
        string mobileDepthSlice = TazLang.Get("mobile_depth_slice_step", "Character wall clipping (lower = less feet through walls)");

        return OptionsUi.Vertical(
            Option.Checkbox(
                TazLang.Get("mog_videotab_misc_enabledeathscreen"),
                new Accessor<bool>(() => profile.EnableDeathScreen),
                search: new SearchMetadata(TazLang.Get("mog_videotab_misc_enabledeathscreen"), Keywords: [TazLang.Get("mog_kw_death")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_videotab_misc_bwdead"),
                new Accessor<bool>(() => profile.EnableBlackWhiteEffect),
                search: new SearchMetadata(TazLang.Get("mog_videotab_misc_bwdead"), Keywords: [TazLang.Get("mog_kw_dead"), TazLang.Get("mog_kw_bw")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_videotab_misc_mousethread"),
                new Accessor<bool>(() => Settings.GlobalSettings.RunMouseInASeparateThread),
                search: new SearchMetadata(TazLang.Get("mog_videotab_misc_mousethread"), Keywords: [TazLang.Get("mog_kw_mouse"), TazLang.Get("mog_kw_thread")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_videotab_misc_targetaura"),
                new Accessor<bool>(() => profile.AuraOnMouse),
                search: new SearchMetadata(TazLang.Get("mog_videotab_misc_targetaura"), Keywords: [TazLang.Get("mog_kw_target"), TazLang.Get("mog_kw_aura")])
            ),
            Option.Checkbox(
                TazLang.Get("mog_videotab_misc_animwater"),
                new Accessor<bool>(() => profile.AnimatedWaterEffect),
                search: new SearchMetadata(TazLang.Get("mog_videotab_misc_animwater"), Keywords: [TazLang.Get("mog_kw_water"), TazLang.Get("mog_kw_anim")])
            ),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(
                    new Accessor<bool>(() => profile.EnableEnhancedWeather, b =>
                    {
                        profile.EnableEnhancedWeather = b;
                        World.Instance?.SwitchWeather(b);
                    }),
                    TazLang.Get("enhanced_weather")
                ),
                Option.Checkbox(
                    TazLang.Get("enhanced_weather_particle_effects"),
                    new Accessor<bool>(() => profile.EnableWeatherEffects),
                    search: new SearchMetadata(
                        TazLang.Get("enhanced_weather_particle_effects"),
                        Keywords: [TazLang.Get("mog_kw_splash"), TazLang.Get("mog_kw_ripple")])
                )
            ).WithSearch(new SearchMetadata(TazLang.Get("enhanced_weather"), [TazLang.Get("mog_kw_enhanced"), TazLang.Get("mog_kw_weather")], [TazLang.Get("mog_kw_weather")])),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(
                    new Accessor<bool>(() => profile.EnablePostProcessingEffects, b =>
                    {
                        profile.EnablePostProcessingEffects = b;
                        GameScene.Instance?.SetPostProcessingSettings();
                    }),
                    TazLang.Get("mog_videotab_misc_enablepostprocessing")
                ),
                Option.ComboBox(
                    TazLang.Get("mog_videotab_misc_postprocessingeffecttype"),
                    profile.PostProcessingType,
                    ["point", "linear", "anisotropic", "xbr"],
                    i =>
                    {
                        profile.PostProcessingType = (ushort)i;
                        GameScene.Instance?.SetPostProcessingSettings();
                    },
                    search: new SearchMetadata(TazLang.Get("mog_videotab_misc_postprocessingeffecttype"), Keywords: [TazLang.Get("mog_kw_type")])
                )
            ).WithSearch(new SearchMetadata(TazLang.Get("mog_videotab_misc_label"), [TazLang.Get("mog_kw_postprocessing")], [TazLang.Get("mog_kw_post"), TazLang.Get("mog_kw_process")])),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.UseCircleOfTransparency), TazLang.Get("mog_general_enablecot")),
                Option.Slider(
                    TazLang.Get("mog_general_cotdistance"),
                    Constants.MIN_CIRCLE_OF_TRANSPARENCY_RADIUS,
                    Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS,
                    new Accessor<float>(() => profile.CircleOfTransparencyRadius, f => profile.CircleOfTransparencyRadius = (int)f),
                    search: new SearchMetadata(TazLang.Get("mog_general_cotdistance"), Keywords: [TazLang.Get("mog_kw_cot"), TazLang.Get("mog_kw_distance")])
                ),
                Option.ComboBox(
                    TazLang.Get("mog_general_cottype"),
                    profile.CircleOfTransparencyType,
                    [TazLang.Get("mog_general_cottypeoptfull"), TazLang.Get("mog_general_cottypeoptgrad"), TazLang.Get("mog_general_cottypeoptmodern")],
                    i => profile.CircleOfTransparencyType = i,
                    search: new SearchMetadata(TazLang.Get("mog_general_cottype"), Keywords: [TazLang.Get("mog_kw_cot"), TazLang.Get("mog_kw_type")])
                )
            ).WithSearch(new SearchMetadata(TazLang.Get("mog_videotab_misc_label"), [TazLang.Get("mog_kw_misc")], [TazLang.Get("mog_kw_cot"), TazLang.Get("mog_kw_circle")])),
            Option.Checkbox(
                disableGargAnim,
                new Accessor<bool>(() => profile.DisableGargoyleFlyingAnimation),
                search: new SearchMetadata(disableGargAnim, Keywords: [TazLang.Get("mog_kw_gargoyle"), TazLang.Get("mog_kw_flying"), TazLang.Get("mog_kw_animation")])
            ),
            Option.Slider(
                mobileDepthSlice,
                0,
                2,
                new Accessor<int>(() => profile.MobileDepthSliceStep, v => profile.MobileDepthSliceStep = v),
                search: new SearchMetadata(mobileDepthSlice, Keywords: [TazLang.Get("mog_kw_character"), TazLang.Get("mog_kw_mobile")])
            ),
            OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = TazLang.Get("mog_videotab_misc_perspective") },
                Option.Slider(
                    TazLang.Get("mog_videotab_misc_playerpositionoffsetx"),
                    -20,
                    20,
                    new Accessor<float>(() => profile.PlayerOffset.X, newValue =>
                    {
                        profile.PlayerOffset = new Point((int)newValue, profile.PlayerOffset.Y);
                    }),
                    true,
                    search: new SearchMetadata(TazLang.Get("mog_videotab_misc_playerpositionoffsetx"), Keywords: [TazLang.Get("mog_kw_x")])
                ),
                Option.Slider(
                    TazLang.Get("mog_videotab_misc_playerpositionoffsety"),
                    -20,
                    20,
                    new Accessor<float>(() => profile.PlayerOffset.Y, newValue =>
                    {
                        profile.PlayerOffset = new Point(profile.PlayerOffset.X, (int)newValue);
                    }),
                    true,
                    search: new SearchMetadata(TazLang.Get("mog_videotab_misc_playerpositionoffsety"), Keywords: [TazLang.Get("mog_kw_y")])
                )
            )
        ).WithSearch(new SearchMetadata(TazLang.Get("mog_videotab_misc_label"), Tags: [TazLang.Get("mog_kw_death"), TazLang.Get("mog_kw_water"), TazLang.Get("mog_kw_aura"), TazLang.Get("mog_kw_postprocessing"), TazLang.Get("mog_kw_perspective")]));
    }
}
