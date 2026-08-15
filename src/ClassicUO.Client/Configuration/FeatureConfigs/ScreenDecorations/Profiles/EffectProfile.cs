#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using ClassicUO.Game.ScreenDecorations.Overlays;
using ClassicUO.Game.UI.MyraWindows.Options.Editors.Profile;
using ClassicUO.Renderer.Effects;

namespace ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;

/// <summary>
/// A named, reusable look: which effects, in what order, blended how, plus how it arrives and leaves
/// and what it shakes.
/// <para>
/// Shared by reference. Several rules may point at one profile, and editing it changes all of them -
/// which is the point of a pool: retuning "poison" once is meant to retune every rule that raises
/// it.
/// </para>
/// <para>
/// Authored profiles are hand-editable JSON and are not trusted. Every layer is re-clamped by
/// <see cref="OverlayParams.Clamp" /> as it is baked, so a file cannot raise the pulse frequency
/// past the photosensitivity ceiling or exceed <see cref="OverlayLayerStack.MaxLayers" />.
/// </para>
/// </summary>
public sealed class EffectProfile : ObservableSettings, IProfile
{
    #region Public accessors

    /// <summary>
    /// Stable identity. Rules reference this rather than the name, so renaming a profile is free and
    /// cannot orphan anything.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Shown in the profile library. Need not be unique; the id is what identifies it.</summary>
    public string Name { get; set => SetField(ref field, value); } = string.Empty;

    /// <summary>
    /// Shipped profiles cannot be edited or deleted, only copied. Never persisted - built-ins are
    /// resolved from code every session, so they stay correct as the client is retuned.
    /// </summary>
    [JsonIgnore]
    public bool IsBuiltIn { get; init; }

    /// <inheritdoc />
    [JsonIgnore]
    public bool Deletable => !IsBuiltIn;

    /// <summary>
    /// Whether this look covers the whole window rather than the game world alone. Governs both
    /// halves of it: which pass draws the layers, and which rectangle <see cref="Shake"/> displaces.
    /// <para>
    /// One switch for both because they are the same decision - an effect that shakes the world
    /// while tinting the UI, or the reverse, reads as two unrelated things happening.
    /// </para>
    /// <para>
    /// Off by default: covering the gumps and the cursor, or moving them about, makes the UI hard to
    /// use for as long as the effect lasts.
    /// </para>
    /// </summary>
    public bool FullScreen { get; set; }

    // Deliberately not observable, unlike Name. The options UI rebuilds an editor whenever its
    // profile raises PropertyChanged, which would tear the inputs out from under the user mid-edit.

    /// <summary>Back-to-front: index 0 is drawn first and ends up underneath.</summary>
    public List<ProfileLayer> Layers { get; set; } = [];

    /// <summary>How it arrives and leaves.</summary>
    public FadeSpec Fade { get; set; } = new();

    /// <summary>Screen shake this look includes, or null for a purely visual one.</summary>
    public ShakeSpec? Shake { get; set; }

    #endregion

    #region Public methods

    /// <summary>
    /// Deep copy, with a fresh <see cref="Id" /> and no built-in marking - a copy of a shipped
    /// profile is the user's, and rules pointing at the original must keep pointing at it.
    /// </summary>
    /// <param name="name">Name for the copy; the original's when null.</param>
    /// <returns>The copy.</returns>
    public EffectProfile Clone(string? name = null)
    {
        var copy = new EffectProfile
        {
            Id = Guid.NewGuid(),
            Name = name ?? Name,
            FullScreen = FullScreen,
            Fade = Fade.Clone(),
            Shake = Shake?.Clone()
        };

        foreach (ProfileLayer layer in Layers)
            copy.Layers.Add(layer.Clone());

        return copy;
    }

    #endregion

    #region Internal methods

    /// <summary>
    /// Refills <paramref name="layers"/> with this profile's baked, clamped stack. Every layer is
    /// clamped independently as it bakes, so composing layers can never be used to route around the
    /// pulse-frequency ceiling.
    /// </summary>
    /// <param name="layers">The list to fill; cleared first.</param>
    internal void BakeClamped(List<OverlayLayer> layers)
    {
        layers.Clear();
        layers.AddRange(Layers.Select(layer => layer.Bake()));
        OverlayLayerStack.Finish(layers, string.IsNullOrEmpty(Name) ? nameof(EffectProfile) : Name);
    }

    #endregion
}
