#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.Profiles;
using ClassicUO.Game.Managers;
using ClassicUO.Game.ScreenDecorations.Manager;
using ClassicUO.Game.ScreenDecorations.Shake;
using DecorationSettings = ClassicUO.Configuration.FeatureConfigs.ScreenDecorations.ScreenDecorations;

namespace ClassicUO.LegionScripting.ApiClasses.VisualEffects;

/// <summary>
///     On-screen visual effects and screen shake, reached through `API.VisualEffects`.
///     <tip title="Advanced APIs">
///         This section contains a set of advanced and highly complex APIs, designed for very specific use-cases
///     </tip>
///     <para>
///         Starting an effect hands back a handle, which is what stops it early. A handle of 0 means nothing
///         was raised: the system is off, you are not in the world, or scripts already hold every slot -
///         `SlotsFree` tells those apart.
///     </para>
///     <para>
///         Nothing you raise outlives your script. Whatever is still running when it stops is dropped, as is
///         everything on leaving the world. Getting a setting wrong raises an error instead, since that is a
///         mistake in the script rather than a state of the client.
///     </para>
///     <note title="Performance">
///         Every layer of every running effect is a full-screen pass each frame, so each layer you add costs
///         a little. Keep compositions small, and stop what you are done with.
///     </note>
///     <warn title="Photosensitive Epilepsy Warning">
///         Visual effects may contain flashing lights or rapid color changes.
///
///         If you have an epileptic condition or have had seizures of any kind, please consult your physician before
///         enabling the system or using the VFX APIs.
///
///         Note that while the in-game UI has some safeguards meant to reduce PSE risks, certain API usage patterns can
///         bypass those.
///
///         Near-zero fades make an effect appear and vanish at once, so starting and stopping one in a loop
///         strobes the whole screen.
///         Keep both above a tenth of a second unless the effect is started once and left to run.
///     </warn>
/// </summary>
public class ApiVisualEffects(LegionAPI api)
{
    #region Enums

    /// <summary>
    ///     How a visual effect layer composites against what is already beneath it.
    /// </summary>
    public enum OverlayBlend
    {
        /// <summary>The layer covers what is behind it in proportion to its alpha.</summary>
        Alpha = 0,

        /// <summary>Brightens instead of covering, which is what a highlight pass wants.</summary>
        Additive = 1
    }

    /// <summary>
    ///     Which packed channel of the shared noise texture a field reads.
    /// </summary>
    public enum NoiseChannel
    {
        /// <summary>Perlin fBm, base frequency 4. Soft billowy masses; the general-purpose choice.</summary>
        Red = 0,

        /// <summary>Perlin fBm, base frequency 8. Same character as `Red`, finer.</summary>
        Green = 1,

        /// <summary>Ridged fBm. Thin bright filaments - cracks and lightning, never fluid or gas.</summary>
        Blue = 2,

        /// <summary>
        ///     Worley cell edges. Only right for shattering; on organic content it reads as a
        ///     microscope slide.
        /// </summary>
        Alpha = 3
    }

    /// <summary>
    ///     Samples a distortion layer takes per pixel, which is its entire cost. A closed set because
    ///     each count is a separately compiled unrolled loop in the shader.
    /// </summary>
    public enum OverlaySampleTaps
    {
        /// <summary>Cheapest. Enough only for a small radius; wider blurs break into ghost copies.</summary>
        Four = 4,

        /// <summary>Enough for a subtle blur, or for radial, whose taps fall on a line.</summary>
        Eight = 8,

        /// <summary>The general-purpose disk count.</summary>
        Twelve = 12,

        /// <summary>Four times the cost of `Four`. Needed only at large radii.</summary>
        Sixteen = 16
    }

    /// <summary>
    ///     How a shake's intensity travels across its whole duration, independently of its ramps.
    /// </summary>
    public enum ShakeGradient
    {
        /// <summary>Full intensity throughout.</summary>
        Constant = 0,

        /// <summary>Starts at full intensity and falls to nothing - an impact.</summary>
        Decay = 1,

        /// <summary>Builds from nothing to full intensity - something approaching.</summary>
        Swell = 2,

        /// <summary>Builds to full intensity at the midpoint, then falls away.</summary>
        Pulse = 3
    }

    /// <summary>
    ///     Easing applied to a shake's ramps and gradient.
    /// </summary>
    public enum ShakeCurve
    {
        Linear = 0,

        /// <summary>Smoothstep - eased at both ends, and the least mechanical of the four.</summary>
        Smooth = 1,

        /// <summary>Slow to build, fast to finish.</summary>
        EaseIn = 2,

        /// <summary>Fast to build, slow to finish.</summary>
        EaseOut = 3
    }

    #endregion

    #region Profile Ids

    /// <summary>
    ///     Stable ids of the visual effect compositions shipped with the client, for `Start`. These
    ///     never change.
    ///     <para>
    ///         Ids rather than names because every shipped composition's name is localized and carries a
    ///         "(built-in)" suffix, so `StartByName` cannot reach any of them.
    ///     </para>
    /// </summary>
    public static class Ids
    {
        /// <summary>Creeping green at the edges of the screen.</summary>
        public const string Poison = "1c419034-7c0f-4e65-a3af-81f27a1e0efa";

        /// <summary>Red streaks running in from the edges.</summary>
        public const string Bleed = "0e778268-d89d-4538-80c6-621ca768a4b2";

        /// <summary>Drifting grey haze over the whole view.</summary>
        public const string Fog = "7195989d-5c02-47b8-bcd9-b91aaaafaa99";

        /// <summary>Slow swimming blur, with the centre wandering.</summary>
        public const string Drunk = "271f1b8f-13b1-4a4a-acc4-a9784852398b";

        /// <summary>Hard jolt: a shake, a flash of blur and colour fringing.</summary>
        public const string Concussion = "b7b6a46c-9899-4d6f-8493-cd22d695c0fb";

        /// <summary>The view closes in to a narrow circle.</summary>
        public const string TunnelVision = "b006e04f-3087-4ac7-bdbb-0c903d35792d";

        /// <summary>Everything drains and darkens.</summary>
        public const string Death = "bd6cf532-4dc4-46d7-94eb-bc061ef041da";

        /// <summary>Sustained low rumble. Shake only - nothing is drawn.</summary>
        public const string EarthquakeRumble = "7f67a87a-e30a-441b-9b5a-3a91da98d050";
    }

    #endregion

    #region Public accessors

    /// <summary>
    ///     <note title="Off by default">
    ///         The entire VFX/Shake system ships disabled, and scripts cannot switch it on.
    ///         The user turns it on under `Options > Video > Visual Effects`.
    ///         Until they do, `Start` and `StartShake` return 0 and draw nothing.
    ///         That is not an error - check `Enabled` and `ShakeEnabled` if your script needs to know.
    ///     </note>
    ///     Whether the user has full-screen visual effects switched on. They ship off, and a script
    ///     cannot override that - `Start` answers 0 while this is false.
    ///     Example:
    ///     ```py
    ///     if API.VisualEffects.Enabled:
    ///         API.VisualEffects.Start(API.VisualEffects.Ids.Poison, 3)
    ///     ```
    /// </summary>
    public bool Enabled => DecorationSettings.Current.OverlaysActive;

    /// <summary>
    ///     Whether the user has screen shake switched on. As with visual effects it ships off and a
    ///     script cannot override it - `StartShake` answers 0 while this is false.
    ///     <para>
    ///         The setting alone, so it can read true while `StartShake` still answers 0: shake also
    ///         needs you in the world. `SlotsFree` is what tells those apart - it answers 0 out of
    ///         world too.
    ///     </para>
    /// </summary>
    public bool ShakeEnabled => DecorationSettings.Current.ShakeActive;

    /// <summary>
    ///     How many more effects every script on the client may raise between them, and 0 while you are not
    ///     in the world. Shared rather than per script, so a script holding effects it no longer needs takes
    ///     them from the others - stop what you are done with.
    ///     Example:
    ///     ```py
    ///     h = API.VisualEffects.Start(API.VisualEffects.Ids.Fog, 10)
    ///     if not h and API.VisualEffects.SlotsFree == 0:
    ///         API.SysMsg("another script is using every effect slot")
    ///     ```
    /// </summary>
    public int SlotsFree => OnMain(ScreenOverlayManager.Instance.ScriptSlotsFree);

    #endregion

    #region Private members

    private readonly CancellationToken _cachedToken = api.CancellationToken.Token;

    #endregion

    #region Public methods

    /// <summary>
    ///     List the visual effect compositions this client knows about, shipped and user-authored.
    ///     Example:
    ///     ```py
    ///     for info in API.VisualEffects.GetProfiles():
    ///         API.SysMsg(f"{info.Name} -> {info.Id}")
    ///     ```
    /// </summary>
    /// <returns>The library, in the order the options list it.</returns>
    public IList<ApiEffectProfileInfo> GetProfiles() => BubblingOnMain<IList<ApiEffectProfileInfo>>(() =>
    {
        return DecorationSettings.Current.Overlays.AllProfiles()
            .Select(profile => new ApiEffectProfileInfo { Id = profile.Id.ToString(), Name = profile.Name, IsBuiltIn = profile.IsBuiltIn })
            .ToList();
    });

    /// <summary>
    ///     Get an editable copy of one of the client's visual effect compositions, as a starting point
    ///     for a variation on it. The copy is yours - changing it does not touch the client's, and there
    ///     is no way to save it back.
    ///     Example:
    ///     ```py
    ///     effect = API.VisualEffects.GetProfile(API.VisualEffects.Ids.Poison)
    ///     effect.Layers[0].Effect.Strength = 0.9
    ///     handle = API.VisualEffects.Start(effect, 5)
    ///     ```
    /// </summary>
    /// <param name="id">The composition's id, from `GetProfiles` or `Ids`.</param>
    /// <returns>The copy.</returns>
    /// <exception cref="ArgumentException">The ID is malformed or was not found</exception>
    public ApiEffectProfile GetProfile(string id) =>
        BubblingOnMain(() => ApiEffectProfile.From(FindProfileOrThrow(id)));

    /// <summary>
    ///     Build an empty visual effect composition, to add layers to with `AddLayer`.
    ///     Example:
    ///     ```py
    ///     effect = API.VisualEffects.NewProfile()
    ///     tint = API.VisualEffects.NewTintLayer()
    ///     tint.R, tint.G, tint.B = 150, 0, 0
    ///     effect.AddLayer(tint)
    ///     handle = API.VisualEffects.Start(effect, 4)
    ///     ```
    /// </summary>
    /// <returns>The composition, with the client's own fade timing and no layers.</returns>
    public ApiEffectProfile NewProfile() => new();

    /// <summary>
    ///     Build a color layer, set to the client's defaults. The only technique that paints rather
    ///     than distorting, and so the only one that may sit anywhere in a stack.
    ///     <para>Like every layer, it costs a full-screen pass per frame while the effect runs.</para>
    /// </summary>
    /// <returns>The layer.</returns>
    public ApiTintLayer NewTintLayer() => new();

    /// <summary>
    ///     Build a disk blur layer, set to the client's defaults. Out-of-focus vision.
    ///     <para>The priciest technique: a full-screen pass per frame, at `Taps` samples per pixel.</para>
    /// </summary>
    /// <returns>The layer.</returns>
    public ApiBlurLayer NewBlurLayer() => new();

    /// <summary>
    ///     Build a zoom blur layer, set to the client's defaults. Reads as vertigo rather than poor
    ///     focus, because the centre of the screen stays sharp however strong it is.
    ///     <para>A full-screen pass per frame, at `Taps` samples per pixel.</para>
    /// </summary>
    /// <returns>The layer.</returns>
    public ApiRadialBlurLayer NewRadialBlurLayer() => new();

    /// <summary>
    ///     Build a chromatic split layer, set to the client's defaults. The cheapest technique by a wide
    ///     margin - three samples per pixel - so it is worth reaching for first when something needs to
    ///     look wrong.
    /// </summary>
    /// <returns>The layer.</returns>
    public ApiChromaticLayer NewChromaticLayer() => new();

    /// <summary>
    ///     Build the shake a visual effect fires as it arrives, to hang on `ApiEffectProfile.Shake`. For
    ///     a shake on its own use `NewShakeConfig` instead.
    /// </summary>
    /// <returns>The shake, set to the client's defaults - an impact that decays.</returns>
    public ApiShakeSpec NewShakeSpec() => new();

    /// <summary>
    ///     Build a standalone screen shake, for `StartShake`.
    ///     Example:
    ///     ```py
    ///     shake = API.VisualEffects.NewShakeConfig()
    ///     shake.Seconds = 2.5
    ///     shake.Intensity = 0.8
    ///     shake.Gradient = API.VisualEffects.ShakeGradient.Decay
    ///     API.VisualEffects.StartShake(shake)
    ///     ```
    /// </summary>
    /// <returns>The shake.</returns>
    public ApiShakeConfig NewShakeConfig() => new();

    /// <summary>
    ///     Build a per-axis value pair, for the visual effect knobs that take one.
    /// </summary>
    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    /// <returns>The pair.</returns>
    public ApiVector2 NewVector2(double x = 0, double y = 0) => new(x, y);

    /// <summary>
    ///     Start one of the client's visual effect compositions by id.
    ///     <para>
    ///         A composition raised this way claims the look against the rulebase, so starting poison while
    ///         the player is poisoned shows one poison rather than two at doubled alpha.
    ///     </para>
    ///     <para>
    ///         Effects do not survive leaving the world: the handle goes stale and nothing comes back on the
    ///         next login. Stop them yourself, give them a duration, or let the script end - whatever a
    ///         script leaves running is dropped when it stops.
    ///     </para>
    ///     Example:
    ///     ```py
    ///     handle = API.VisualEffects.Start(API.VisualEffects.Ids.Drunk, 10)
    ///     API.Pause(2)
    ///     API.VisualEffects.Stop(handle)
    ///     ```
    /// </summary>
    /// <param name="profileId">The composition's id, from `GetProfiles` or `Ids`.</param>
    /// <param name="seconds">How long to run for. Zero or less runs until stopped; capped at 86400.</param>
    /// <param name="intensity">Strength to draw at, 0-1. Scales the composition rather than replacing anything in it.</param>
    /// <returns>
    ///     A handle for `Stop`, or 0 if the user has visual effects switched off, you are not in the
    ///     world, or every slot is in use.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     The ID is not in the right format, no effect in the
    ///     client has it, or `seconds` or `intensity` is not a real number.
    /// </exception>
    public uint Start(string profileId, double seconds = 0, double intensity = 1.0) =>
        BubblingOnMain(() =>
        {
            EffectProfile profile = FindProfileOrThrow(profileId);

            return ScreenOverlayManager.Instance.StartScripted(
                profile,
                profile.Id,
                VfxInputValidator.Intensity(intensity, nameof(intensity)),
                Duration(seconds),
                api
            );
        });

    /// <summary>
    ///     Start a visual effect the script built itself.
    ///     <para>
    ///         Unlike the two by-name forms this claims nothing, because there is no library composition
    ///         behind it to claim - so two scripts starting identical hand-built effects do composite
    ///         together, at doubled strength. Editing `config` afterwards cannot reach the running effect;
    ///         start it again to change it.
    ///     </para>
    /// </summary>
    /// <param name="config">The composition to draw. Needs at least one and at most four layers.</param>
    /// <param name="seconds">How long to run for. Zero or less runs until stopped; capped at 86400.</param>
    /// <param name="intensity">Strength to draw at, 0-1.</param>
    /// <returns>
    ///     A handle for `Stop`, or 0 if the user has visual effects switched off, you are not in the
    ///     world, or every slot is in use.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     `config` has no layers or more than four, one of its
    ///     layers is empty, a setting is not one of the options the API offers, or a value is not a
    ///     real number.
    /// </exception>
    public uint Start(ApiEffectProfile config, double seconds = 0, double intensity = 1.0)
    {
        ArgumentNullException.ThrowIfNull(config);

        // Flattened before the hop, so a malformed composition faults on the script's own stack
        // rather than from inside a queued main-thread action.
        EffectProfile profile = config.ToInternal();
        float strength = VfxInputValidator.Intensity(intensity, nameof(intensity));
        TimeSpan? duration = Duration(seconds);

        return BubblingOnMain(() => ScreenOverlayManager.Instance.StartScripted(profile, Guid.Empty, strength, duration, api)
        );
    }

    /// <summary>
    ///     Start one of the user's own visual effect compositions by name.
    ///     <para>
    ///         User-authored compositions only. Every shipped one is named through the client's language file
    ///         and carries a "(built-in)" suffix, so no name here could reach them on a non-English client -
    ///         use `Start` with `Ids` for those.
    ///     </para>
    /// </summary>
    /// <param name="name">The composition's name, matched ignoring case. Must match exactly one.</param>
    /// <param name="seconds">How long to run for. Zero or less runs until stopped; capped at 86400.</param>
    /// <param name="intensity">Strength to draw at, 0-1.</param>
    /// <returns>
    ///     A handle for `Stop`, or 0 if the user has visual effects switched off, you are not in the
    ///     world, or every slot is in use.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     You have no effect with that name, several of yours
    ///     share it, or `seconds` or `intensity` is not a real number.
    /// </exception>
    public uint StartByName(string name, double seconds = 0, double intensity = 1.0) =>
        BubblingOnMain(() =>
        {
            EffectProfile profile = FindUserProfileOrThrow(name);

            return ScreenOverlayManager.Instance.StartScripted(
                profile,
                profile.Id,
                VfxInputValidator.Intensity(intensity, nameof(intensity)),
                Duration(seconds),
                api
            );
        });

    /// <summary>
    ///     Stop one visual effect this script started. It fades out rather than cutting.
    /// </summary>
    /// <param name="handle">
    ///     The handle a `Start` returned. Handles that already lapsed, were
    ///     stopped, came back as 0, or belong to another script are ignored.
    /// </param>
    public void Stop(uint handle) => OnMain(() => ScreenOverlayManager.Instance.StopScripted(handle, api));

    /// <summary>
    ///     Stop every visual effect this script started. Other scripts' effects, and the ones the
    ///     player's own state raises, are left alone.
    /// </summary>
    public void StopAll() => OnMain(() => ScreenOverlayManager.Instance.StopScriptedByOwner(api));

    /// <summary>
    ///     Shake the screen, with no visual effect attached.
    ///     Example:
    ///     ```py
    ///     shake = API.VisualEffects.NewShakeConfig()
    ///     shake.Seconds = 0.4
    ///     shake.Intensity = 1.0
    ///     API.VisualEffects.StartShake(shake)
    ///     ```
    /// </summary>
    /// <param name="config">The shake to run, from `NewShakeConfig`.</param>
    /// <returns>
    ///     A handle for `StopShake`, or 0 if the user has screen shake switched off or you are
    ///     not in the world.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     A setting on `config` is not one of the options the API
    ///     offers, or one of its values is not a real number.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     `config.Seconds` is zero or less. A shake needs
    ///     a length - there is nothing that would end an open-ended one.
    /// </exception>
    public uint StartShake(ApiShakeConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        // No main-thread hop: the shake accumulators take their own lock precisely so a shake can be
        // raised from whichever thread handled whatever caused it.
        return ScreenShake.For(config.FullScreen).Trauma(config.ToInternal(), ShakeOrigin.Script(api));
    }

    /// <summary>
    ///     Stop one screen shake this script started.
    /// </summary>
    /// <param name="handle">
    ///     The handle `StartShake` returned. Handles that already finished, were
    ///     stopped, came back as 0, or belong to another script are ignored.
    /// </param>
    public void StopShake(uint handle)
    {
        // A handle does not say which rectangle its shake was moving, and a caller has no reason to
        // care; the accumulator that does not hold it does nothing.
        if (!ScreenShake.Viewport.Cancel(handle, api))
            ScreenShake.Window.Cancel(handle, api);
    }

    /// <summary>
    ///     Stop every screen shake this script started, including the ones its visual effects fired as
    ///     they arrived.
    /// </summary>
    public void StopAllShakes()
    {
        ScreenShake.Viewport.CancelByOwner(api);
        ScreenShake.Window.CancelByOwner(api);
    }

    #endregion

    #region Private methods

    private T OnMain<T>(Func<T> func) => MainThreadQueue.InvokeOnMainThread(func, _cachedToken);

    private void OnMain(Action action) => MainThreadQueue.InvokeOnMainThread(action, _cachedToken);

    private T BubblingOnMain<T>(Func<T> func) => MainThreadQueue.BubblingInvokeOnMainThread(func, _cachedToken);

    /// <summary>
    ///     The library composition behind an id. Main thread, like everything that reads the pools.
    /// </summary>
    /// <param name="id">The id as a script spelled it.</param>
    /// <returns>
    ///     The composition, shared by reference the way a rule's is - so a retune of it reaches
    ///     a running script effect too.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     The ID is not in the right format, or no effect in the
    ///     client has it.
    /// </exception>
    private static EffectProfile FindProfileOrThrow(string id)
    {
        if (!Guid.TryParse(id, out Guid parsed))
            throw new ArgumentException(
                $"'{id}' is not a visual effect ID. An ID looks like "
                + "'1c419034-7c0f-4e65-a3af-81f27a1e0efa'. Use API.VisualEffects.Ids for the effects "
                + "that ship with the client, or API.VisualEffects.GetProfiles() to list them all.",
                nameof(id)
            );

        return DecorationSettings.Current.Overlays.FindProfile(parsed)
               ?? throw new ArgumentException(
                   $"No visual effect in this client has the ID '{id}'. It may have been deleted, or "
                   + "belong to a different profile. API.VisualEffects.GetProfiles() lists what is here.",
                   nameof(id)
               );
    }

    /// <summary>
    ///     The user's own composition with this name. Built-ins are excluded deliberately: their names
    ///     are localized, so matching one would work on an English client and nowhere else.
    /// </summary>
    /// <param name="name">The name as a script spelled it.</param>
    /// <returns>The composition.</returns>
    /// <exception cref="ArgumentException">
    ///     No effect you made has that name, or several do and
    ///     there is no way to tell which you meant.
    /// </exception>
    private static EffectProfile FindUserProfileOrThrow(string name)
    {
        var matches = DecorationSettings.Current.Overlays.Profiles
            .Where(profile => string.Equals(profile.Name, name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return matches.Count switch
        {
            1 => matches[0],
            0 => throw new ArgumentException(
                $"You have no visual effect named '{name}'. This only finds effects you made yourself - "
                + "the ones that ship with the client are named in your own language, so start those "
                + "with API.VisualEffects.Start and an ID from API.VisualEffects.Ids.",
                nameof(name)
            ),
            _ => throw new ArgumentException(
                $"You have {matches.Count} visual effects named '{name}', so this cannot tell which you "
                + "mean. Rename one, or start it by ID instead - API.VisualEffects.GetProfiles() lists "
                + "every effect with its ID.",
                nameof(name)
            )
        };
    }

    /// <summary>
    ///     Script-supplied seconds as a span, where anything non-positive means no timeout. Bounded
    ///     before conversion: <see cref="TimeSpan.FromSeconds(double)" /> throws on NaN and overflows far
    ///     below what a script can type, and the span is then added to the wall clock.
    /// </summary>
    /// <param name="seconds">What the script asked for.</param>
    /// <returns>The span, or null to run until stopped.</returns>
    /// <exception cref="ArgumentException"><paramref name="seconds" /> is not a real number.</exception>
    private static TimeSpan? Duration(double seconds)
    {
        double bounded = VfxInputValidator.Seconds(seconds, nameof(seconds));

        return bounded > 0 ? TimeSpan.FromSeconds(bounded) : null;
    }

    #endregion
}
