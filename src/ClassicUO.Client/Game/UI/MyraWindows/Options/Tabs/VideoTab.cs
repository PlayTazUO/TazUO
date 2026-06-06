using System;
using ClassicUO.Common;
using ClassicUO.Configuration;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.MyraWindows.Widgets;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.WrapPanel;

namespace ClassicUO.Game.UI.MyraWindows.Options.Tabs;

public static class VideoTab
{
    internal static OptionItem GetContent()
    {
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        return new OptionItem(lang.LabelVideo, GetVideoMenuTabs);
    }

    private static MyraTabControl GetVideoMenuTabs()
    {
        ModernOptionsGumpLanguage.Video videoLang = Language.Instance.GetModernOptionsGumpLanguage.GetVideo;
        ModernOptionsGumpLanguage gumpLang = Language.Instance.GetModernOptionsGumpLanguage;

        var tabs = new MyraTabControl();
        tabs.AddTab(gumpLang.ButtonGameWindow, GetGameWindowSubTabContent);
        tabs.AddTab(videoLang.ZoomAndScaling, GetZoomAndScalingSubTubContent);
        tabs.AddTab(videoLang.LabelLighting, GetLightningSubTabContent);
        tabs.AddTab(gumpLang.ButtonShadows, GetShadowSubTabContent);
        tabs.AddTab(gumpLang.ButtonMisc, GetMiscSubTabContent);
        return tabs;
    }

    private static OptionItem GetViewportSettingsGroup()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        return new OptionItem(
            lang.LabelViewport,
            () =>
                new VisualContainer(
                    new VisualContainerProps { LabelText = lang.LabelViewport },
                    OptionsFactory.CreateCheckboxOption(
                        lang.GetVideo.FullsizeViewport,
                        profile.GameWindowFullSize,
                        b =>
                        {
                            profile.GameWindowFullSize = b;

                            WorldViewportGump viewport = WorldViewportGump.Instance;
                            if (viewport == null) return;

                            if (b)
                            {
                                viewport.ResizeGameWindow(new Point(Client.Game.Window.ClientBounds.Width, Client.Game.Window.ClientBounds.Height));
                                viewport.SetGameWindowPosition(new Point(0, 0));
                                profile.GameWindowPosition = new Point(0, 0);
                            }
                            else
                            {
                                viewport.ResizeGameWindow(new Point(600, 480));
                                viewport.SetGameWindowPosition(new Point(25, 25));
                                profile.GameWindowPosition = new Point(25, 25);
                            }

                            // Trigger a full update to ensure borders and positioning are correct
                            viewport.OnWindowResized();
                        }
                    ),
                    OptionsFactory.CreateCheckboxOption(
                        lang.GetVideo.FullScreen,
                        profile.WindowBorderless,
                        b =>
                        {
                            profile.WindowBorderless = b;
                            Client.Game.SetWindowBorderless(b);
                        }
                    ),
                    OptionsFactory.CreateCheckboxOption(
                        lang.GetVideo.LockViewport,
                        new Accessor<bool>(() => profile.GameWindowLock)
                    ),
                    OptionsFactory.CreateSliderOption(
                        lang.GetVideo.ViewportX,
                        0,
                        Client.Game.Window.ClientBounds.Width,
                        profile.GameWindowPosition.X,
                        f =>
                        {
                            profile.GameWindowPosition = new Point((int)f, profile.GameWindowPosition.Y);
                            WorldViewportGump.Instance?.SetGameWindowPosition(profile.GameWindowPosition);
                        }
                    ),
                    OptionsFactory.CreateSliderOption(
                        lang.GetVideo.ViewportY,
                        0,
                        Client.Game.Window.ClientBounds.Height,
                        profile.GameWindowPosition.Y,
                        f =>
                        {
                            profile.GameWindowPosition = new Point(profile.GameWindowPosition.Y, (int)f);
                            WorldViewportGump.Instance?.SetGameWindowPosition(profile.GameWindowPosition);
                        }
                    ),
                    OptionsFactory.CreateSliderOption(
                        lang.GetVideo.ViewportW,
                        0,
                        Client.Game.Window.ClientBounds.Width,
                        profile.GameWindowSize.X,
                        f =>
                        {
                            profile.GameWindowSize = new Point((int)f, profile.GameWindowSize.Y);
                            WorldViewportGump.Instance?.SetGameWindowPosition(profile.GameWindowPosition);
                        }
                    ),
                    OptionsFactory.CreateSliderOption(
                        lang.GetVideo.ViewportH,
                        0,
                        Client.Game.Window.ClientBounds.Height,
                        profile.GameWindowSize.Y,
                        f =>
                        {
                            profile.GameWindowSize = new Point(profile.GameWindowSize.X, (int)f);
                            WorldViewportGump.Instance?.SetGameWindowPosition(profile.GameWindowPosition);
                        }
                    )
                )
        );
    }

    private static WrapPanel GetZoomAndScalingSubTubContent() =>
        OptionTabCommons.StyledVerticalWrapPanel(
            GetZoomSection(),
            GetScalingSection()
        );

    private static WrapPanel GetGameWindowSubTabContent() =>
        OptionTabCommons.StyledVerticalWrapPanel(
            OptionsFactory.CreateSpacer(),
            GetRendererSection(),
            GetViewportSettingsGroup()
        );


    private static VisualContainer GetRendererSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Video videoLang = lang.GetVideo;

        return new VisualContainer(
            new VisualContainerProps { LabelText = videoLang.LabelRenderer },
            OptionsFactory.CreateSliderOption(
                videoLang.FPSCap,
                Constants.MIN_FPS,
                Constants.MAX_FPS,
                Settings.GlobalSettings.FPS,
                f =>
                {
                    Settings.GlobalSettings.FPS = (int)f;
                    Client.Game.SetRefreshRate((int)f);
                }
            ),
            OptionsFactory.CreateCheckboxOption(videoLang.BackgroundFPS, new Accessor<bool>(() => profile.ReduceFPSWhenInactive)),
            OptionsFactory.CreateCheckboxOption(videoLang.EnableVSync,
                profile.EnableVSync,
                b =>
                {
                    profile.EnableVSync = b;
                    Client.Game?.SetVSync(b);
                }
            )
        );
    }

    private static VisualContainer GetZoomSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Video videoLang = lang.GetVideo;

        int cameraZoomCount = (int)(
            (Client.Game.Scene.Camera.ZoomMax - Client.Game.Scene.Camera.ZoomMin)
            / Client.Game.Scene.Camera.ZoomStep
        );

        int cameraZoomIndex =
            cameraZoomCount
            - (int)(
                (Client.Game.Scene.Camera.ZoomMax - Client.Game.Scene.Camera.Zoom)
                / Client.Game.Scene.Camera.ZoomStep
            );

        return new VisualContainer(
            new VisualContainerProps { LabelText = videoLang.Zoom },
            OptionsFactory.CreateSpacer(),
            OptionsFactory.CreateSliderOption(
                videoLang.DefaultZoom,
                0,
                cameraZoomCount,
                cameraZoomIndex,
                f =>
                {
                    profile.DefaultScale = Client.Game.Scene.Camera.Zoom =
                        (int)f * Client.Game.Scene.Camera.ZoomStep + Client.Game.Scene.Camera.ZoomMin;
                }
            ),
            OptionsFactory.CreateCheckboxOption(
                videoLang.ZoomWheel,
                new Accessor<bool>(() => profile.EnableMousewheelScaleZoom)
            ),
            OptionsFactory.CreateCheckboxOption(
                videoLang.ReturnDefaultZoom,
                new Accessor<bool>(() => profile.RestoreScaleAfterUnpressCtrl)
            )
        );
    }

    private static WrapPanel GetLightningSubTabContent()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Video videoLang = lang.GetVideo;

        return OptionTabCommons.StyledVerticalWrapPanel(
            OptionsFactory.CreateSpacer(),
            OptionsFactory.CreateCheckboxOption(videoLang.AltLights, new Accessor<bool>(() => profile.UseAlternativeLights)),
            new CheckBoxGroup(
                new PropertyBinder(
                    new Accessor<bool>(
                        () => profile.UseCustomLightLevel,
                        b =>
                        {
                            profile.UseCustomLightLevel = b;

                            if (b)
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
                    ),
                    videoLang.CustomLLevel
                ),
                OptionsFactory.CreateSliderOption(
                    videoLang.Level,
                    0,
                    0x1E,
                    0x1E - profile.LightLevel,
                    f =>
                    {
                        profile.LightLevel = (byte)(0x1E - (int)f);

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
                ),
                OptionsFactory.CreateComboBox(
                    videoLang.LightType,
                    profile.LightLevelType,
                    [videoLang.LightType_Absolute, videoLang.LightType_Minimum],
                    i => profile.LightLevelType = i
                )
            ),
            OptionsFactory.CreateCheckboxOption(
                videoLang.DarkNight,
                new Accessor<bool>(() => profile.UseDarkNights)
            ),
            OptionsFactory.CreateCheckboxOption(
                videoLang.ColoredLight,
                new Accessor<bool>(() => profile.UseColoredLights)
            )
        );
    }

    private static WrapPanel GetMiscSubTabContent()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Video videoLang = lang.GetVideo;


        return OptionTabCommons.StyledVerticalWrapPanel(
            OptionsFactory.CreateSpacer(),
            OptionsFactory.CreateCheckboxOption(
                videoLang.EnableDeathScreen,
                new Accessor<bool>(() => profile.EnableDeathScreen)
            ),
            OptionsFactory.CreateCheckboxOption(videoLang.BWDead, new Accessor<bool>(() => profile.EnableBlackWhiteEffect)),
            OptionsFactory.CreateCheckboxOption(videoLang.MouseThread, new Accessor<bool>(() => Settings.GlobalSettings.RunMouseInASeparateThread)),
            OptionsFactory.CreateCheckboxOption(videoLang.TargetAura, new Accessor<bool>(() => profile.AuraOnMouse)),
            OptionsFactory.CreateCheckboxOption(videoLang.AnimWater, new Accessor<bool>(() => profile.AnimatedWaterEffect)),
            new CheckBoxGroup(
                new PropertyBinder(
                    new Accessor<bool>(
                        () => profile.EnablePostProcessingEffects,
                        b =>
                        {
                            profile.EnablePostProcessingEffects = b;
                            GameScene.Instance?.SetPostProcessingSettings();
                        }
                    ),
                    videoLang.EnablePostProcessing
                ),
                OptionsFactory.CreateComboBox(
                    videoLang.PostProcessingEffectType,
                    profile.PostProcessingType,
                    ["point", "linear", "anisotropic", "xbr"],
                    i =>
                    {
                        profile.PostProcessingType = (ushort)i;
                        GameScene.Instance?.SetPostProcessingSettings();
                    }
                )
            ),
            new VisualContainer(
                new VisualContainerProps { LabelText = videoLang.Perspective },
                OptionsFactory.CreateSliderOption(
                    videoLang.PlayerPositionOffsetX,
                    -20,
                    20,
                    profile.PlayerOffset.X,
                    newValue => { profile.PlayerOffset = new Point((int)newValue, profile.PlayerOffset.Y); },
                    true
                ),
                OptionsFactory.CreateSliderOption(
                    videoLang.PlayerPositionOffsetY,
                    -20,
                    20,
                    profile.PlayerOffset.Y,
                    newValue => { profile.PlayerOffset = new Point(profile.PlayerOffset.X, (int)newValue); },
                    true
                )
            )
        );
    }

    private static VisualContainer GetScalingSection()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;
        ModernOptionsGumpLanguage.Video videoLang = Language.Instance.GetModernOptionsGumpLanguage.GetVideo;

        float? previousScale = null;
        float? scale = null;

        return new VisualContainer(
            new VisualContainerProps
            {
                LabelText = lang.ButtonScaling, LabelLink = "https://tazuo.org/wiki/tazuoglobal-scaling/", Spacing = VisualContainerSpacing.Comfortable
            },
            OptionsFactory.CreateSliderOption(
                videoLang.PaperdollScaling,
                50,
                300,
                (int)(profile.PaperdollScale * 100),
                newValue => profile.PaperdollScale = Math.Clamp(newValue / 100, 0.5f, 3.0f)
            ),
            OptionTabCommons.StyledStackPanel(
                Orientation.Horizontal,
                OptionsFactory.CreateSliderOption(
                    videoLang.GlobalScaling,
                    50, // Limiting min scale to something sane at the UI level, even though render supports it
                    Client.Game.MaxRenderScale *
                    100, // For max scale, there's a hard cap at the property level to avoid outright crashing systems when accidentally setting too high
                    Client.Game.RenderScale * 100,
                    newValue => scale = Math.Clamp(newValue / 100, 0.5f, Client.Game.MaxRenderScale)
                ),
                // Apply button for renderer (global) scale
                new MyraButton(
                    lang.Apply,
                    () =>
                    {
                        if (scale == null || scale.Equals(previousScale))
                            return;

                        previousScale = scale;
                        Client.Game.SetScale(scale.Value);
                        _ = Client.Settings.SetAsync(SettingsScope.Global, Constants.SqlSettings.GAME_SCALE, scale);
                    },
                    MyraLabel.TextStyle.H5
                )
            )
        );
    }

    private static WrapPanel GetShadowSubTabContent()
    {
        Profile profile = ProfileManager.CurrentProfile;
        ModernOptionsGumpLanguage lang = Language.Instance.GetModernOptionsGumpLanguage;

        return OptionTabCommons.StyledVerticalWrapPanel(
            OptionsFactory.CreateSpacer(),
            OptionsFactory.CreateCheckboxOption(lang.GetVideo.EnableShadows, new Accessor<bool>(() => profile.ShadowsEnabled)),
            OptionsFactory.CreateCheckboxOption(lang.GetVideo.RockTreeShadows, new Accessor<bool>(() => profile.ShadowsStatics)),
            OptionsFactory.CreateSliderOption(
                lang.GetVideo.TerrainShadowLevel,
                Constants.MIN_TERRAIN_SHADOWS_LEVEL,
                Constants.MAX_TERRAIN_SHADOWS_LEVEL,
                profile.TerrainShadowsLevel,
                f => profile.TerrainShadowsLevel = (int)f)
        );
    }
}
