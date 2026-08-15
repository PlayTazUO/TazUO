#nullable enable

using System.Text.Json.Serialization;
using ClassicUO.Game.ScreenDecorations.Triggers.Implementations;

namespace ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Triggers;

/// <summary>
/// Values for one parameterizable trigger. One subtype per definition that needs them, so a
/// definition's knobs are narrowly typed the same way a layer's are - a bag of strings would push
/// every type error to runtime, and the generated serializer contexts this project requires cannot
/// describe one.
/// <para>
/// Only the base lives here. Each concrete set sits beside the trigger that reads it, since nothing
/// else can interpret one: the fields mean whatever that trigger does with them. This type still has
/// to name them for the polymorphic serializer, which is the one place the direction is reversed.
/// </para>
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ChatMessageParameters), ChatMessageParameters.Discriminator)]
[JsonDerivedType(typeof(ObjectPropertiesParameters), ObjectPropertiesParameters.Discriminator)]
[JsonDerivedType(typeof(PlayerAttributeParameters), PlayerAttributeParameters.Discriminator)]
[JsonDerivedType(typeof(SoundPlayedParameters), SoundPlayedParameters.Discriminator)]
public abstract class TriggerParameters
{
    /// <summary>Copy, so editing a rule's parameters cannot write into another rule's.</summary>
    /// <returns>An independent copy.</returns>
    public abstract TriggerParameters Clone();
}
