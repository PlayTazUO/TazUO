namespace ClassicUO.Game.ScreenDecorations.Overlays
{
    /// <summary>
    /// What one occurrence asks for on top of whatever the effect's profile already says. Lets a
    /// trigger describe how its instance differs - a quake underfoot against one on the horizon -
    /// without every trigger needing a profile of its own.
    /// <para>
    /// This is the growth point: a trigger that wants to modulate something new adds a property
    /// here rather than a parameter to every method between itself and the compositor.
    /// </para>
    /// <para>
    /// Use <see cref="Default" /> rather than <c>default</c>. The parameterless constructor is what
    /// sets <see cref="Intensity" /> to 1, and <c>default(OverlayModulation)</c> bypasses it and
    /// leaves the overlay at zero strength.
    /// </para>
    /// </summary>
    public readonly record struct OverlayModulation
    {
        /// <summary>Scales the drawn strength of every layer. 1 is the profile as authored.</summary>
        public float Intensity { get; init; } = 1f;

        /// <summary>Screen shake to fire when the effect starts; zero for none.</summary>
        public float OnsetTrauma { get; init; } = 0f;

        public OverlayModulation()
        {
        }

        /// <summary>The profile untouched: full intensity, no shake.</summary>
        public static OverlayModulation Default => new();
    }
}
