// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;

namespace ClassicUO.Game.UI.Gumps
{
    internal static class NamePlatePresets
    {
        public static string[] GetOptions() => new[]
        {
            TazLang.Get("nameplate_preset_custom", "Custom"),
            TazLang.Get("nameplate_preset_orion", "Orion"),
            TazLang.Get("nameplate_preset_wow_blockybars", "WoW - Blocky Bars"),
            TazLang.Get("nameplate_preset_wow_cleanhealth", "WoW - Clean Health"),
            TazLang.Get("nameplate_preset_wow_blockycast", "WoW - Blocky Cast"),
            TazLang.Get("nameplate_preset_wow_redname", "WoW - Red Name"),
            TazLang.Get("nameplate_preset_legacy", "Legacy")
        };

        public static void SetCustom(Profile profile)
        {
            if (profile == null)
            {
                return;
            }

            if (profile.NamePlatePreset != NamePlatePreset.Custom)
            {
                profile.NamePlatePreset = NamePlatePreset.Custom;
            }

            NameOverheadGump.InvalidateAllLayouts();
        }

        public static void Apply(Profile profile, NamePlatePreset preset)
        {
            if (profile == null)
            {
                return;
            }

            profile.NamePlatePreset = preset;

            if (preset != NamePlatePreset.Custom)
            {
                profile.NamePlateUseNotorietyText = preset == NamePlatePreset.Legacy;
                profile.NamePlateShowMissingHealth = preset != NamePlatePreset.Legacy;
            }

            switch (preset)
            {
                case NamePlatePreset.Legacy:
                    profile.NamePlateUseFixedWidth = false;
                    profile.NamePlateFixedWidth = 120;
                    profile.NamePlateUseFixedHealthBarWidth = false;
                    profile.NamePlateHealthBarFixedWidth = 120;
                    profile.NamePlateHeight = 0;
                    profile.NamePlateSplitHealthBar = false;
                    profile.NamePlateCornerRadius = 0;
                    profile.NamePlateHealthBarMode = NamePlateHealthBarMode.StatusColor;
                    profile.NamePlateBackgroundMode = NamePlateBackgroundMode.EntityNotorietyColor;
                    profile.NamePlateBackgroundR = 0;
                    profile.NamePlateBackgroundG = 0;
                    profile.NamePlateBackgroundB = 0;
                    profile.NamePlateHealthBar = true;
                    profile.NamePlateHealthBarOpacity = 50;
                    profile.NamePlateOpacity = 75;
                    profile.NamePlateBorderOpacity = 50;
                    profile.NamePlateShowWordOfDeathIcon = false;
                    profile.NamePlateFont = "avadonian";
                    profile.NamePlateFontSize = 20;
                    break;

                case NamePlatePreset.Orion:
                    profile.NamePlateUseFixedWidth = true;
                    profile.NamePlateFixedWidth = 160;
                    profile.NamePlateUseFixedHealthBarWidth = false;
                    profile.NamePlateHealthBarFixedWidth = 160;
                    profile.NamePlateHeight = 0;
                    profile.NamePlateSplitHealthBar = false;
                    profile.NamePlateCornerRadius = 18;
                    profile.NamePlateHealthBarMode = NamePlateHealthBarMode.StatusColor;
                    profile.NamePlateBackgroundMode = NamePlateBackgroundMode.NotorietyColor;
                    profile.NamePlateBackgroundR = 0;
                    profile.NamePlateBackgroundG = 0;
                    profile.NamePlateBackgroundB = 0;
                    profile.NamePlateHealthBar = true;
                    profile.NamePlateHealthBarOpacity = 75;
                    profile.NamePlateOpacity = 70;
                    profile.NamePlateBorderOpacity = 80;
                    profile.NamePlateAvoidOverlap = true;
                    profile.NamePlateHideAtFullHealth = false;
                    profile.NamePlateHideAtFullHealthInWarmode = false;
                    profile.NamePlateShowWordOfDeathIcon = false;
                    profile.NamePlateFont = "avadonian";
                    profile.NamePlateFontSize = 16;
                    break;

                case NamePlatePreset.WorldOfWarcraftBlockyBars:
                    profile.NamePlateUseFixedWidth = true;
                    profile.NamePlateFixedWidth = 220;
                    profile.NamePlateUseFixedHealthBarWidth = false;
                    profile.NamePlateHealthBarFixedWidth = 220;
                    profile.NamePlateHeight = 44;
                    profile.NamePlateSplitHealthBar = true;
                    profile.NamePlateCornerRadius = 2;
                    profile.NamePlateHealthBarMode = NamePlateHealthBarMode.Green;
                    profile.NamePlateBackgroundMode = NamePlateBackgroundMode.FixedColor;
                    profile.NamePlateBackgroundR = 18;
                    profile.NamePlateBackgroundG = 14;
                    profile.NamePlateBackgroundB = 14;
                    profile.NamePlateHealthBar = true;
                    profile.NamePlateHealthBarOpacity = 100;
                    profile.NamePlateOpacity = 85;
                    profile.NamePlateBorderOpacity = 85;
                    profile.NamePlateAvoidOverlap = true;
                    profile.NamePlateHideAtFullHealth = false;
                    profile.NamePlateHideAtFullHealthInWarmode = false;
                    profile.NamePlateShowWordOfDeathIcon = false;
                    profile.NamePlateFont = "avadonian";
                    profile.NamePlateFontSize = 17;
                    break;

                case NamePlatePreset.WorldOfWarcraftCleanHealth:
                    profile.NamePlateUseFixedWidth = true;
                    profile.NamePlateFixedWidth = 220;
                    profile.NamePlateUseFixedHealthBarWidth = false;
                    profile.NamePlateHealthBarFixedWidth = 220;
                    profile.NamePlateHeight = 54;
                    profile.NamePlateSplitHealthBar = true;
                    profile.NamePlateCornerRadius = 3;
                    profile.NamePlateHealthBarMode = NamePlateHealthBarMode.Green;
                    profile.NamePlateBackgroundMode = NamePlateBackgroundMode.FixedColor;
                    profile.NamePlateBackgroundR = 20;
                    profile.NamePlateBackgroundG = 18;
                    profile.NamePlateBackgroundB = 18;
                    profile.NamePlateHealthBar = true;
                    profile.NamePlateHealthBarOpacity = 100;
                    profile.NamePlateOpacity = 85;
                    profile.NamePlateBorderOpacity = 90;
                    profile.NamePlateAvoidOverlap = true;
                    profile.NamePlateHideAtFullHealth = false;
                    profile.NamePlateHideAtFullHealthInWarmode = false;
                    profile.NamePlateShowWordOfDeathIcon = false;
                    profile.NamePlateFont = "avadonian";
                    profile.NamePlateFontSize = 18;
                    break;

                case NamePlatePreset.WorldOfWarcraftBlockyCast:
                    profile.NamePlateUseFixedWidth = true;
                    profile.NamePlateFixedWidth = 220;
                    profile.NamePlateUseFixedHealthBarWidth = false;
                    profile.NamePlateHealthBarFixedWidth = 220;
                    profile.NamePlateHeight = 36;
                    profile.NamePlateSplitHealthBar = true;
                    profile.NamePlateCornerRadius = 1;
                    profile.NamePlateHealthBarMode = NamePlateHealthBarMode.Green;
                    profile.NamePlateBackgroundMode = NamePlateBackgroundMode.FixedColor;
                    profile.NamePlateBackgroundR = 24;
                    profile.NamePlateBackgroundG = 18;
                    profile.NamePlateBackgroundB = 18;
                    profile.NamePlateHealthBar = true;
                    profile.NamePlateHealthBarOpacity = 100;
                    profile.NamePlateOpacity = 82;
                    profile.NamePlateBorderOpacity = 90;
                    profile.NamePlateAvoidOverlap = true;
                    profile.NamePlateHideAtFullHealth = false;
                    profile.NamePlateHideAtFullHealthInWarmode = false;
                    profile.NamePlateShowWordOfDeathIcon = false;
                    profile.NamePlateFont = "avadonian";
                    profile.NamePlateFontSize = 17;
                    break;

                case NamePlatePreset.WorldOfWarcraftRedName:
                    profile.NamePlateUseFixedWidth = true;
                    profile.NamePlateFixedWidth = 220;
                    profile.NamePlateUseFixedHealthBarWidth = false;
                    profile.NamePlateHealthBarFixedWidth = 220;
                    profile.NamePlateHeight = 38;
                    profile.NamePlateSplitHealthBar = true;
                    profile.NamePlateCornerRadius = 1;
                    profile.NamePlateHealthBarMode = NamePlateHealthBarMode.Green;
                    profile.NamePlateBackgroundMode = NamePlateBackgroundMode.FixedColor;
                    profile.NamePlateBackgroundR = 28;
                    profile.NamePlateBackgroundG = 12;
                    profile.NamePlateBackgroundB = 12;
                    profile.NamePlateHealthBar = true;
                    profile.NamePlateHealthBarOpacity = 100;
                    profile.NamePlateOpacity = 85;
                    profile.NamePlateBorderOpacity = 90;
                    profile.NamePlateAvoidOverlap = true;
                    profile.NamePlateHideAtFullHealth = false;
                    profile.NamePlateHideAtFullHealthInWarmode = false;
                    profile.NamePlateShowWordOfDeathIcon = false;
                    profile.NamePlateFont = "avadonian";
                    profile.NamePlateFontSize = 18;
                    break;
            }

            NameOverheadGump.InvalidateAllLayouts();
        }
    }
}
