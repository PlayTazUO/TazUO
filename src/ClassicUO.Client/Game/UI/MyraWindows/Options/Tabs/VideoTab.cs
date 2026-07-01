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
        ModernOptionsGumpLanguage.VideoTabLang lang = Language.Instance.GetModernOptionsGumpLanguage.VideoTab;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

        return new OptionTabGroup()
            .AddTab(
                lang.GameWindow.Label,
                GetGameWindowSubTabContent,
                new SearchMetadata(lang.GameWindow.Label, Keywords: [kw.Window, kw.Viewport, kw.Fullscreen, kw.Fps, kw.VSync])
            )
            .AddTab(
                lang.Zoom.Label,
                GetZoomAndScalingSubTubContent,
                new SearchMetadata(lang.Zoom.Label, Keywords: [kw.Zoom, kw.Scale, kw.Scaling, kw.Paperdoll, kw.Global])
            )
            .AddTab(
                lang.Lighting.Label,
                GetLightningSubTabContent,
                new SearchMetadata(lang.Lighting.Label, Keywords: [kw.Light, kw.Darkness, kw.Night, kw.Color])
            )
            .AddTab(
                lang.Shadows.Label,
                GetShadowSubTabContent,
                new SearchMetadata(lang.Shadows.Label, Keywords: [kw.Shadow, kw.Static, kw.Terrain])
            )
            .AddTab(
                lang.Misc.Label,
                GetMiscSubTabContent,
                new SearchMetadata(lang.Misc.Label, Keywords: [kw.Misc, kw.Miscellaneous])
            );
    }

    private static IOptionSource GetGameWindowSubTabContent()
    {
        ModernOptionsGumpLanguage.VideoTabLang lang = Language.Instance.GetModernOptionsGumpLanguage.VideoTab;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;
        return OptionsUi.Vertical(
            GetRendererSection(),
            GetViewportSettingsGroup()
        ).WithSearch(new SearchMetadata(lang.GameWindow.Label, Tags: [kw.Window, kw.Viewport]));
    }

    private static OptionFragment GetRendererSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.VideoTabLang.GameWindowSection lang = Language.Instance.GetModernOptionsGumpLanguage.VideoTab.GameWindow;
        ModernOptionsGumpLanguage.TazUO tuoLang = Language.Instance.GetModernOptionsGumpLanguage.GetTazUO;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = lang.RendererLabel },
            Option.Slider(
                lang.FPSCap,
                Constants.MIN_FPS,
                Constants.MAX_FPS,
                new Accessor<float>(() => Settings.GlobalSettings.FPS, f =>
                {
                    Settings.GlobalSettings.FPS = (int)f;
                    Client.Game.SetRefreshRate((int)f);
                }),
                search: new SearchMetadata(lang.FPSCap, Keywords: [kw.Fps, kw.Refresh])
            ),
            Option.Checkbox(
                lang.BackgroundFPS,
                new Accessor<bool>(() => profile.ReduceFPSWhenInactive),
                search: new SearchMetadata(lang.BackgroundFPS, Keywords: [kw.Fps, kw.Background])
            ),
            Option.Checkbox(
                lang.EnableVSync,
                new Accessor<bool>(() => profile.EnableVSync, b =>
                {
                    profile.EnableVSync = b;
                    Client.Game?.SetVSync(b);
                }),
                search: new SearchMetadata(lang.EnableVSync, Keywords: [kw.VSync])
            ),
            Option.HuePicker(
                tuoLang.MainGameWindowBackground,
                profile.MainWindowBackgroundHue,
                newValue =>
                {
                    profile.MainWindowBackgroundHue = newValue;
                    GameController.UpdateBackgroundHueShader();
                },
                new SearchMetadata(tuoLang.MainGameWindowBackground, Keywords: [kw.Main, kw.Window, kw.Background, kw.Hue])
            )
        );
    }

    private static OptionFragment GetViewportSettingsGroup()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.VideoTabLang.GameWindowSection lang = Language.Instance.GetModernOptionsGumpLanguage.VideoTab.GameWindow;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = lang.ViewportLabel },
            Option.Checkbox(
                lang.FullsizeViewport,
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
                search: new SearchMetadata(lang.FullsizeViewport, Keywords: [kw.Full, kw.Size])
            ),
            Option.Checkbox(
                lang.FullScreen,
                profile.WindowBorderless,
                newValue =>
                {
                    profile.WindowBorderless = newValue;
                    Client.Game.SetWindowBorderless(newValue);
                },
                search: new SearchMetadata(lang.FullScreen, Keywords: [kw.Fullscreen, kw.Borderless])
            ),
            Option.Checkbox(
                lang.LockViewport,
                new Accessor<bool>(() => profile.GameWindowLock),
                search: new SearchMetadata(lang.LockViewport, Keywords: [kw.Lock])
            ),
            Option.Slider(
                lang.ViewportX,
                0,
                Client.Game.Window.ClientBounds.Width,
                new Accessor<float>(() => profile.GameWindowPosition.X, f =>
                {
                    profile.GameWindowPosition = new Point((int)f, profile.GameWindowPosition.Y);
                    WorldViewportGump.Instance?.SetGameWindowPosition(profile.GameWindowPosition);
                }),
                search: new SearchMetadata(lang.ViewportX, Keywords: [kw.X])
            ),
            Option.Slider(
                lang.ViewportY,
                0,
                Client.Game.Window.ClientBounds.Height,
                new Accessor<float>(() => profile.GameWindowPosition.Y, f =>
                {
                    profile.GameWindowPosition = new Point(profile.GameWindowPosition.X, (int)f);
                    WorldViewportGump.Instance?.SetGameWindowPosition(profile.GameWindowPosition);
                }),
                search: new SearchMetadata(lang.ViewportY, Keywords: [kw.Y])
            ),
            Option.Slider(
                lang.ViewportW,
                0,
                Client.Game.Window.ClientBounds.Width,
                new Accessor<float>(() => profile.GameWindowSize.X, f =>
                {
                    profile.GameWindowSize = new Point((int)f, profile.GameWindowSize.Y);
                    WorldViewportGump.Instance?.ResizeGameWindow(profile.GameWindowSize);
                }),
                search: new SearchMetadata(lang.ViewportW, Keywords: [kw.Width])
            ),
            Option.Slider(
                lang.ViewportH,
                0,
                Client.Game.Window.ClientBounds.Height,
                new Accessor<float>(() => profile.GameWindowSize.Y, f =>
                {
                    profile.GameWindowSize = new Point(profile.GameWindowSize.X, (int)f);
                    WorldViewportGump.Instance?.ResizeGameWindow(profile.GameWindowSize);
                }),
                search: new SearchMetadata(lang.ViewportH, Keywords: [kw.Height])
            )
        );
    }

    private static IOptionSource GetZoomAndScalingSubTubContent()
    {
        ModernOptionsGumpLanguage.VideoTabLang lang = Language.Instance.GetModernOptionsGumpLanguage.VideoTab;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;
        return OptionsUi.Vertical(
            GetZoomSection(),
            GetScalingSection()
        ).WithSearch(new SearchMetadata(lang.Zoom.Label, Tags: [kw.Zoom, kw.Scale]));
    }

    private static OptionFragment GetZoomSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.VideoTabLang.ZoomSection lang = Language.Instance.GetModernOptionsGumpLanguage.VideoTab.Zoom;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

        Camera camera = Client.Game.Scene.Camera;
        int cameraZoomCount = (int)((camera.ZoomMax - camera.ZoomMin) / camera.ZoomStep);
        int cameraZoomIndex = cameraZoomCount - (int)((camera.ZoomMax - camera.Zoom) / camera.ZoomStep);

        return OptionsUi.VisualContainer(
            new VisualContainerProps { LabelText = lang.ZoomLabel },
            Option.Slider(
                lang.DefaultZoom,
                0,
                cameraZoomCount,
                new Accessor<float>(() => cameraZoomIndex, f =>
                {
                    profile.DefaultScale = camera.Zoom = (int)f * camera.ZoomStep + camera.ZoomMin;
                }),
                search: new SearchMetadata(lang.DefaultZoom, Keywords: [kw.Zoom])
            ),
            Option.Checkbox(
                lang.ZoomWheel,
                new Accessor<bool>(() => profile.EnableMousewheelScaleZoom),
                search: new SearchMetadata(lang.ZoomWheel, Keywords: [kw.Wheel])
            ),
            Option.Checkbox(
                lang.ReturnDefaultZoom,
                new Accessor<bool>(() => profile.RestoreScaleAfterUnpressCtrl),
                search: new SearchMetadata(lang.ReturnDefaultZoom, Keywords: [kw.Restore, kw.Ctrl])
            )
        );
    }

    private static OptionFragment GetScalingSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.VideoTabLang.ZoomSection videoLang = lang.VideoTab.Zoom;
        ModernOptionsGumpLanguage.KeywordsLang kw = lang.Kw;

        float? scale = null;

        return OptionsUi.VisualContainer(
            new VisualContainerProps
            {
                LabelText = lang.ButtonScaling,
                LabelLink = "https://tazuo.org/wiki/tazuoglobal-scaling/",
                Spacing = VisualContainerSpacing.Comfortable
            },
            Option.Slider(
                videoLang.PaperdollScaling,
                50,
                300,
                new Accessor<float>(() => (int)(profile.PaperdollScale * 100), newValue =>
                {
                    profile.PaperdollScale = Math.Clamp(newValue / 100, 0.5f, 3.0f);
                }),
                search: new SearchMetadata(videoLang.PaperdollScaling, Keywords: [kw.Paperdoll, kw.Scale])
            ),
            Option.Slider(
                TazLang.Get("gumpscaling_statusgumpscaling", "Status gump scaling"),
                50,
                300,
                new Accessor<float>(() => (int)(profile.StatusGumpScale * 100), newValue =>
                {
                    profile.StatusGumpScale = Math.Clamp(newValue / 100, 0.5f, 3.0f);
                }),
                search: new SearchMetadata(TazLang.Get("gumpscaling_statusgumpscaling", "Status gump scaling"), Keywords: [kw.Scale])
            ),
            Option.Slider(
                TazLang.Get("gumpscaling_contextmenuscaling", "Context menu scaling"),
                50,
                300,
                new Accessor<float>(() => (int)(profile.ContextMenuScale * 100), newValue =>
                {
                    profile.ContextMenuScale = newValue / 100;
                }),
                search: new SearchMetadata(TazLang.Get("gumpscaling_contextmenuscaling", "Context menu scaling"), Keywords: [kw.Scale])
            ),
            Option.Slider(
                TazLang.Get("gumpscaling_servergumpscaling", "Server gump scaling"),
                50,
                300,
                new Accessor<float>(() => (int)(profile.ServerGumpScale * 100), newValue =>
                {
                    profile.ServerGumpScale = Math.Clamp(newValue / 100, 0.5f, 3.0f);
                }),
                search: new SearchMetadata(TazLang.Get("gumpscaling_servergumpscaling", "Server gump scaling"), Keywords: [kw.Scale])
            ),
            OptionsUi.Horizontal(
                Option.Slider(
                    videoLang.GlobalScaling,
                    50,
                    Client.Game.MaxRenderScale * 100,
                    new Accessor<float>(
                        () => Client.Game.RenderScale * 100,
                        newValue =>
                        {
                            scale = Math.Clamp(newValue / 100, 0.5f, Client.Game.MaxRenderScale);
                        }
                    ),
                    search: new SearchMetadata(videoLang.GlobalScaling, Keywords: [kw.Global, kw.Scale])
                ),
                Option.Button(
                    lang.Apply,
                    () =>
                    {
                        if (scale != null)
                        {
                            Client.Game.SetScale(scale.Value);
                            _ = Client.Settings.SetAsync(SettingsScope.Global, Constants.SqlSettings.GAME_SCALE, scale);
                        }
                    },
                    search: new SearchMetadata(lang.Apply)
                )
            )
        ).AsSearchGroup();
    }

    private static IOptionSource GetLightningSubTabContent()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.VideoTabLang lang = Language.Instance.GetModernOptionsGumpLanguage.VideoTab;
        ModernOptionsGumpLanguage.VideoTabLang.LightingSection lightLang = lang.Lighting;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

        return OptionsUi.Vertical(
            Option.Checkbox(
                lightLang.AltLights,
                new Accessor<bool>(() => profile.UseAlternativeLights),
                search: new SearchMetadata(lightLang.AltLights, Keywords: [kw.Alt, kw.Light])
            ),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(
                    new Accessor<bool>(() => profile.UseCustomLightLevel, b =>
                    {
                        profile.UseCustomLightLevel = b;
                        UpdateLight();
                    }),
                    lightLang.CustomLLevel
                ),
                Option.Slider(
                    lightLang.Level,
                    0,
                    0x1E,
                    new Accessor<float>(() => 0x1E - profile.LightLevel, f =>
                    {
                        profile.LightLevel = (byte)(0x1E - (int)f);
                        UpdateLight();
                    }),
                    search: new SearchMetadata(lightLang.Level, Keywords: [kw.Level])
                ),
                Option.ComboBox(
                    lightLang.LightType,
                    profile.LightLevelType,
                    [lightLang.LightType_Absolute, lightLang.LightType_Minimum],
                    i => profile.LightLevelType = i,
                    search: new SearchMetadata(lightLang.LightType, Keywords: [kw.Type])
                )
            ).WithSearch(new SearchMetadata(lightLang.Label, Tags: [kw.Light], Keywords: [kw.Custom])),
            Option.Checkbox(
                lightLang.DarkNight,
                new Accessor<bool>(() => profile.UseDarkNights),
                search: new SearchMetadata(lightLang.DarkNight, Keywords: [kw.Dark, kw.Night])
            ),
            Option.Checkbox(
                lightLang.ColoredLight,
                new Accessor<bool>(() => profile.UseColoredLights),
                search: new SearchMetadata(lightLang.ColoredLight, Keywords: [kw.Color, kw.Light])
            )
        ).WithSearch(new SearchMetadata(lightLang.Label, Tags: [kw.Light]));

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
        ModernOptionsGumpLanguage.VideoTabLang lang = Language.Instance.GetModernOptionsGumpLanguage.VideoTab;
        ModernOptionsGumpLanguage.VideoTabLang.ShadowsSection shadowLang = lang.Shadows;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

        return OptionsUi.Vertical(
            Option.Checkbox(
                shadowLang.EnableShadows,
                new Accessor<bool>(() => profile.ShadowsEnabled),
                search: new SearchMetadata(shadowLang.EnableShadows, Keywords: [kw.Shadow])
            ),
            Option.Checkbox(
                shadowLang.RockTreeShadows,
                new Accessor<bool>(() => profile.ShadowsStatics),
                search: new SearchMetadata(shadowLang.RockTreeShadows, Keywords: [kw.Static, kw.Rock, kw.Tree])
            ),
            Option.Slider(
                shadowLang.TerrainShadowLevel,
                Constants.MIN_TERRAIN_SHADOWS_LEVEL,
                Constants.MAX_TERRAIN_SHADOWS_LEVEL,
                new Accessor<float>(() => profile.TerrainShadowsLevel, f => profile.TerrainShadowsLevel = (int)f),
                search: new SearchMetadata(shadowLang.TerrainShadowLevel, Keywords: [kw.Terrain])
            )
        ).WithSearch(new SearchMetadata(shadowLang.Label, Tags: [kw.Shadow]));
    }

    private static IOptionSource GetMiscSubTabContent()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage.VideoTabLang lang = Language.Instance.GetModernOptionsGumpLanguage.VideoTab;
        ModernOptionsGumpLanguage.VideoTabLang.MiscSection miscLang = lang.Misc;
        ModernOptionsGumpLanguage.General genLang = Language.Instance.GetModernOptionsGumpLanguage.GetGeneral;
        ModernOptionsGumpLanguage.KeywordsLang kw = Language.Instance.GetModernOptionsGumpLanguage.Kw;

        string disableGargAnim = TazLang.Get("disable_gargoyle_flying_animation", "Disable gargoyle flying animation");

        return OptionsUi.Vertical(
            Option.Checkbox(
                miscLang.EnableDeathScreen,
                new Accessor<bool>(() => profile.EnableDeathScreen),
                search: new SearchMetadata(miscLang.EnableDeathScreen, Keywords: [kw.Death])
            ),
            Option.Checkbox(
                miscLang.BWDead,
                new Accessor<bool>(() => profile.EnableBlackWhiteEffect),
                search: new SearchMetadata(miscLang.BWDead, Keywords: [kw.Dead, kw.Bw])
            ),
            Option.Checkbox(
                miscLang.MouseThread,
                new Accessor<bool>(() => Settings.GlobalSettings.RunMouseInASeparateThread),
                search: new SearchMetadata(miscLang.MouseThread, Keywords: [kw.Mouse, kw.Thread])
            ),
            Option.Checkbox(
                miscLang.TargetAura,
                new Accessor<bool>(() => profile.AuraOnMouse),
                search: new SearchMetadata(miscLang.TargetAura, Keywords: [kw.Target, kw.Aura])
            ),
            Option.Checkbox(
                miscLang.AnimWater,
                new Accessor<bool>(() => profile.AnimatedWaterEffect),
                search: new SearchMetadata(miscLang.AnimWater, Keywords: [kw.Water, kw.Anim])
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
                        Keywords: [kw.Splash, kw.Ripple])
                )
            ).WithSearch(new SearchMetadata(TazLang.Get("enhanced_weather"), [kw.Enhanced, kw.Weather], [kw.Weather])),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(
                    new Accessor<bool>(() => profile.EnablePostProcessingEffects, b =>
                    {
                        profile.EnablePostProcessingEffects = b;
                        GameScene.Instance?.SetPostProcessingSettings();
                    }),
                    miscLang.EnablePostProcessing
                ),
                Option.ComboBox(
                    miscLang.PostProcessingEffectType,
                    profile.PostProcessingType,
                    ["point", "linear", "anisotropic", "xbr"],
                    i =>
                    {
                        profile.PostProcessingType = (ushort)i;
                        GameScene.Instance?.SetPostProcessingSettings();
                    },
                    search: new SearchMetadata(miscLang.PostProcessingEffectType, Keywords: [kw.Type])
                )
            ).WithSearch(new SearchMetadata(miscLang.Label, [kw.PostProcessing], [kw.Post, kw.Process])),
            OptionsUi.CheckBoxGroup(
                new PropertyBinder(new Accessor<bool>(() => profile.UseCircleOfTransparency), genLang.EnableCOT),
                Option.Slider(
                    genLang.COTDistance,
                    Constants.MIN_CIRCLE_OF_TRANSPARENCY_RADIUS,
                    Constants.MAX_CIRCLE_OF_TRANSPARENCY_RADIUS,
                    new Accessor<float>(() => profile.CircleOfTransparencyRadius, f => profile.CircleOfTransparencyRadius = (int)f),
                    search: new SearchMetadata(genLang.COTDistance, Keywords: [kw.Cot, kw.Distance])
                ),
                Option.ComboBox(
                    genLang.COTType,
                    profile.CircleOfTransparencyType,
                    [genLang.COTTypeOptFull, genLang.COTTypeOptGrad, genLang.COTTypeOptModern],
                    i => profile.CircleOfTransparencyType = i,
                    search: new SearchMetadata(genLang.COTType, Keywords: [kw.Cot, kw.Type])
                )
            ).WithSearch(new SearchMetadata(miscLang.Label, [kw.Misc], [kw.Cot, kw.Circle])),
            Option.Checkbox(
                disableGargAnim,
                new Accessor<bool>(() => profile.DisableGargoyleFlyingAnimation),
                search: new SearchMetadata(disableGargAnim, Keywords: [kw.Gargoyle, kw.Flying, kw.Animation])
            ),
            OptionsUi.VisualContainer(
                new VisualContainerProps { LabelText = miscLang.Perspective },
                Option.Slider(
                    miscLang.PlayerPositionOffsetX,
                    -20,
                    20,
                    new Accessor<float>(() => profile.PlayerOffset.X, newValue =>
                    {
                        profile.PlayerOffset = new Point((int)newValue, profile.PlayerOffset.Y);
                    }),
                    true,
                    search: new SearchMetadata(miscLang.PlayerPositionOffsetX, Keywords: [kw.X])
                ),
                Option.Slider(
                    miscLang.PlayerPositionOffsetY,
                    -20,
                    20,
                    new Accessor<float>(() => profile.PlayerOffset.Y, newValue =>
                    {
                        profile.PlayerOffset = new Point(profile.PlayerOffset.X, (int)newValue);
                    }),
                    true,
                    search: new SearchMetadata(miscLang.PlayerPositionOffsetY, Keywords: [kw.Y])
                )
            )
        ).WithSearch(new SearchMetadata(miscLang.Label, Tags: [kw.Death, kw.Water, kw.Aura, kw.PostProcessing, kw.Perspective]));
    }
}
