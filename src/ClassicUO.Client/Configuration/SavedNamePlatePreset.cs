// SPDX-License-Identifier: BSD-2-Clause

using System.Text.Json.Serialization;

namespace ClassicUO.Configuration;

/// <summary>An immutable snapshot of the nameplate display settings, shared between characters.</summary>
public sealed record SavedNamePlatePreset
{
    public string Name { get; init; } = string.Empty;
    public bool NamePlateHealthBar { get; init; }
    public byte NamePlateOpacity { get; init; }
    public byte NamePlateHealthBarOpacity { get; init; }
    public bool NamePlateHideAtFullHealth { get; init; }
    public bool NamePlateHideAtFullHealthInWarmode { get; init; }
    public byte NamePlateBorderOpacity { get; init; }
    public bool NamePlateAvoidOverlap { get; init; }
    public bool NamePlateUseFixedWidth { get; init; }
    public int NamePlateFixedWidth { get; init; }
    public bool NamePlateUseFixedHealthBarWidth { get; init; }
    public int NamePlateHealthBarFixedWidth { get; init; }
    public bool NamePlateShowWordOfDeathIcon { get; init; }
    public int NamePlateHeight { get; init; }
    public bool NamePlateSplitHealthBar { get; init; }
    public bool NamePlateUseNotorietyText { get; init; }
    public bool NamePlateShowMissingHealth { get; init; }
    public int NamePlateCornerRadius { get; init; }
    public NamePlateHealthBarMode NamePlateHealthBarMode { get; init; }
    public NamePlateBackgroundMode NamePlateBackgroundMode { get; init; }
    public byte NamePlateBackgroundR { get; init; }
    public byte NamePlateBackgroundG { get; init; }
    public byte NamePlateBackgroundB { get; init; }
    public string NamePlateFont { get; init; }
    public int NamePlateFontSize { get; init; }
    public bool ShowNewMobileNameIncoming { get; init; }
    public bool ShowNewCorpseNameIncoming { get; init; }

    public static SavedNamePlatePreset Capture(Profile profile) => new()
    {
        NamePlateHealthBar = profile.NamePlateHealthBar,
        NamePlateOpacity = profile.NamePlateOpacity,
        NamePlateHealthBarOpacity = profile.NamePlateHealthBarOpacity,
        NamePlateHideAtFullHealth = profile.NamePlateHideAtFullHealth,
        NamePlateHideAtFullHealthInWarmode = profile.NamePlateHideAtFullHealthInWarmode,
        NamePlateBorderOpacity = profile.NamePlateBorderOpacity,
        NamePlateAvoidOverlap = profile.NamePlateAvoidOverlap,
        NamePlateUseFixedWidth = profile.NamePlateUseFixedWidth,
        NamePlateFixedWidth = profile.NamePlateFixedWidth,
        NamePlateUseFixedHealthBarWidth = profile.NamePlateUseFixedHealthBarWidth,
        NamePlateHealthBarFixedWidth = profile.NamePlateHealthBarFixedWidth,
        NamePlateShowWordOfDeathIcon = profile.NamePlateShowWordOfDeathIcon,
        NamePlateHeight = profile.NamePlateHeight,
        NamePlateSplitHealthBar = profile.NamePlateSplitHealthBar,
        NamePlateUseNotorietyText = profile.NamePlateUseNotorietyText,
        NamePlateShowMissingHealth = profile.NamePlateShowMissingHealth,
        NamePlateCornerRadius = profile.NamePlateCornerRadius,
        NamePlateHealthBarMode = profile.NamePlateHealthBarMode,
        NamePlateBackgroundMode = profile.NamePlateBackgroundMode,
        NamePlateBackgroundR = profile.NamePlateBackgroundR,
        NamePlateBackgroundG = profile.NamePlateBackgroundG,
        NamePlateBackgroundB = profile.NamePlateBackgroundB,
        NamePlateFont = profile.NamePlateFont,
        NamePlateFontSize = profile.NamePlateFontSize,
        ShowNewMobileNameIncoming = profile.ShowNewMobileNameIncoming,
        ShowNewCorpseNameIncoming = profile.ShowNewCorpseNameIncoming,
    };

    public void ApplyTo(Profile profile)
    {
        profile.NamePlateHealthBar = NamePlateHealthBar;
        profile.NamePlateOpacity = NamePlateOpacity;
        profile.NamePlateHealthBarOpacity = NamePlateHealthBarOpacity;
        profile.NamePlateHideAtFullHealth = NamePlateHideAtFullHealth;
        profile.NamePlateHideAtFullHealthInWarmode = NamePlateHideAtFullHealthInWarmode;
        profile.NamePlateBorderOpacity = NamePlateBorderOpacity;
        profile.NamePlateAvoidOverlap = NamePlateAvoidOverlap;
        profile.NamePlateUseFixedWidth = NamePlateUseFixedWidth;
        profile.NamePlateFixedWidth = NamePlateFixedWidth;
        profile.NamePlateUseFixedHealthBarWidth = NamePlateUseFixedHealthBarWidth;
        profile.NamePlateHealthBarFixedWidth = NamePlateHealthBarFixedWidth;
        profile.NamePlateShowWordOfDeathIcon = NamePlateShowWordOfDeathIcon;
        profile.NamePlateHeight = NamePlateHeight;
        profile.NamePlateSplitHealthBar = NamePlateSplitHealthBar;
        profile.NamePlateUseNotorietyText = NamePlateUseNotorietyText;
        profile.NamePlateShowMissingHealth = NamePlateShowMissingHealth;
        profile.NamePlateCornerRadius = NamePlateCornerRadius;
        profile.NamePlateHealthBarMode = NamePlateHealthBarMode;
        profile.NamePlateBackgroundMode = NamePlateBackgroundMode;
        profile.NamePlateBackgroundR = NamePlateBackgroundR;
        profile.NamePlateBackgroundG = NamePlateBackgroundG;
        profile.NamePlateBackgroundB = NamePlateBackgroundB;
        profile.NamePlateFont = NamePlateFont;
        profile.NamePlateFontSize = NamePlateFontSize;
        profile.ShowNewMobileNameIncoming = ShowNewMobileNameIncoming;
        profile.ShowNewCorpseNameIncoming = ShowNewCorpseNameIncoming;
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SavedNamePlatePreset))]
internal partial class SavedNamePlatePresetJsonContext : JsonSerializerContext;
