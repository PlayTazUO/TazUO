#nullable enable

namespace ClassicUO.Configuration.FeatureConfigs.ScreenDecorations;

/// <summary>
/// The shake half of <see cref="ScreenDecorations"/>. Separate from the overlay toggle on purpose:
/// motion is the part players are most likely to want gone, and turning it off should not cost them
/// the tints.
/// </summary>
public class ShakeSystemSettings : ObservableSettings
{
    public bool Enabled { get; set => SetField(ref field, value); }

    /// <summary>Scales the shake offset. Clamped to [0, 1] where it is consumed.</summary>
    public float Intensity { get; set => SetField(ref field, value); } = 1f;
}
