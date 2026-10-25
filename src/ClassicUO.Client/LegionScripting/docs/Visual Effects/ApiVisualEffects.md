---
title: ApiVisualEffects
description: "On-screen visual effects and screen shake, reached through `API.VisualEffects`."
---

## Class Description
On-screen visual effects and screen shake, reached through `API.VisualEffects`.

:::tip[Advanced APIs]
This section contains a set of advanced and highly complex APIs, designed for very specific use-cases
:::

Starting an effect hands back a handle, which is what stops it early. A handle of 0 means nothing
was raised: the system is off, you are not in the world, or scripts already hold every slot -
`SlotsFree` tells those apart.

Nothing you raise outlives your script. Whatever is still running when it stops is dropped, as is
everything on leaving the world. Getting a setting wrong raises an error instead, since that is a
mistake in the script rather than a state of the client.

:::note[Performance]
Every layer of every running effect is a full-screen pass each frame, so each layer you add costs
a little. Keep compositions small, and stop what you are done with.
:::

:::caution[Photosensitive Epilepsy Warning]
Visual effects may contain flashing lights or rapid color changes.

If you have an epileptic condition or have had seizures of any kind, please consult your physician before
enabling the system or using the VFX APIs.

Note that while the in-game UI has some safeguards meant to reduce PSE risks, certain API usage patterns can
bypass those.

Near-zero fades make an effect appear and vanish at once, so starting and stopping one in a loop
strobes the whole screen.
Keep both above a tenth of a second unless the effect is started once and left to run.
:::

## Properties
### `Enabled`

**Type:** `bool`

:::note[Off by default]
The entire VFX/Shake system ships disabled, and scripts cannot switch it on.
The user turns it on under `Options > Video > Visual Effects`.
Until they do, `Start` and `StartShake` return 0 and draw nothing.
That is not an error - check `Enabled` and `ShakeEnabled` if your script needs to know.
:::

 Whether the user has full-screen visual effects switched on. They ship off, and a script
cannot override that - `Start` answers 0 while this is false.

**Example:**

```py
if API.VisualEffects.Enabled:
    API.VisualEffects.Start(API.VisualEffects.Ids.Poison, 3)
```

### `ShakeEnabled`

**Type:** `bool`

Whether the user has screen shake switched on. As with visual effects it ships off and a
script cannot override it - `StartShake` answers 0 while this is false.

The setting alone, so it can read true while `StartShake` still answers 0: shake also
needs you in the world. `SlotsFree` is what tells those apart - it answers 0 out of
world too.

### `SlotsFree`

**Type:** `int`

How many more effects every script on the client may raise between them, and 0 while you are not
in the world. Shared rather than per script, so a script holding effects it no longer needs takes
them from the others - stop what you are done with.

**Example:**

```py
h = API.VisualEffects.Start(API.VisualEffects.Ids.Fog, 10)
if not h and API.VisualEffects.SlotsFree == 0:
    API.SysMsg("another script is using every effect slot")
```



## Enums
### OverlayBlend

:::note[Description]
How a visual effect layer composites against what is already beneath it.
:::

**Values:**
- `Alpha`
- `Additive`

### NoiseChannel

:::note[Description]
Which packed channel of the shared noise texture a field reads.
:::

**Values:**
- `Red`
- `Green`
- `Blue`
- `Alpha`

### OverlaySampleTaps

:::note[Description]
Samples a distortion layer takes per pixel, which is its entire cost. A closed set because
each count is a separately compiled unrolled loop in the shader.
:::

**Values:**
- `Four`
- `Eight`
- `Twelve`
- `Sixteen`

### ShakeGradient

:::note[Description]
How a shake's intensity travels across its whole duration, independently of its ramps.
:::

**Values:**
- `Constant`
- `Decay`
- `Swell`
- `Pulse`

### ShakeCurve

:::note[Description]
Easing applied to a shake's ramps and gradient.
:::

**Values:**
- `Linear`
- `Smooth`
- `EaseIn`
- `EaseOut`


## Constants
### Ids

:::note[Description]
Stable ids of the visual effect compositions shipped with the client, for `Start`. These
never change.

Ids rather than names because every shipped composition's name is localized and carries a
"(built-in)" suffix, so `StartByName` cannot reach any of them.
:::

| Name | Value | Description |
| --- | --- | --- |
| `Poison` | `1c419034-7c0f-4e65-a3af-81f27a1e0efa` | Creeping green at the edges of the screen. |
| `Bleed` | `0e778268-d89d-4538-80c6-621ca768a4b2` | Red streaks running in from the edges. |
| `Fog` | `7195989d-5c02-47b8-bcd9-b91aaaafaa99` | Drifting grey haze over the whole view. |
| `Drunk` | `271f1b8f-13b1-4a4a-acc4-a9784852398b` | Slow swimming blur, with the centre wandering. |
| `Concussion` | `b7b6a46c-9899-4d6f-8493-cd22d695c0fb` | Hard jolt: a shake, a flash of blur and colour fringing. |
| `TunnelVision` | `b006e04f-3087-4ac7-bdbb-0c903d35792d` | The view closes in to a narrow circle. |
| `Death` | `bd6cf532-4dc4-46d7-94eb-bc061ef041da` | Everything drains and darkens. |
| `EarthquakeRumble` | `7f67a87a-e30a-441b-9b5a-3a91da98d050` | Sustained low rumble. Shake only - nothing is drawn. |


## Methods
### GetProfiles

List the visual effect compositions this client knows about, shipped and user-authored.

**Example:**

```py
for info in API.VisualEffects.GetProfiles():
    API.SysMsg(f"{info.Name} -> {info.Id}")
```

**Return Type:** `IList<ApiEffectProfileInfo>` - The library, in the order the options list it.

---

### GetProfile
`(id)`
Get an editable copy of one of the client's visual effect compositions, as a starting point
for a variation on it. The copy is yours - changing it does not touch the client's, and there
is no way to save it back.

**Example:**

```py
effect = API.VisualEffects.GetProfile(API.VisualEffects.Ids.Poison)
effect.Layers[0].Effect.Strength = 0.9
handle = API.VisualEffects.Start(effect, 5)
```

**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `id` | `string` | ❌ No | The composition's id, from `GetProfiles` or `Ids`. |

**Return Type:** `ApiEffectProfile` - The copy.

**Raises:**

- `ArgumentException` - The ID is malformed or was not found

---

### NewProfile

Build an empty visual effect composition, to add layers to with `AddLayer`.

**Example:**

```py
effect = API.VisualEffects.NewProfile()
tint = API.VisualEffects.NewTintLayer()
tint.R, tint.G, tint.B = 150, 0, 0
effect.AddLayer(tint)
handle = API.VisualEffects.Start(effect, 4)
```

**Return Type:** `ApiEffectProfile` - The composition, with the client's own fade timing and no layers.

---

### NewTintLayer

Build a color layer, set to the client's defaults. The only technique that paints rather
than distorting, and so the only one that may sit anywhere in a stack.

Like every layer, it costs a full-screen pass per frame while the effect runs.

**Return Type:** `ApiTintLayer` - The layer.

---

### NewBlurLayer

Build a disk blur layer, set to the client's defaults. Out-of-focus vision.

The priciest technique: a full-screen pass per frame, at `Taps` samples per pixel.

**Return Type:** `ApiBlurLayer` - The layer.

---

### NewRadialBlurLayer

Build a zoom blur layer, set to the client's defaults. Reads as vertigo rather than poor
focus, because the centre of the screen stays sharp however strong it is.

A full-screen pass per frame, at `Taps` samples per pixel.

**Return Type:** `ApiRadialBlurLayer` - The layer.

---

### NewChromaticLayer

Build a chromatic split layer, set to the client's defaults. The cheapest technique by a wide
margin - three samples per pixel - so it is worth reaching for first when something needs to
look wrong.

**Return Type:** `ApiChromaticLayer` - The layer.

---

### NewShakeSpec

Build the shake a visual effect fires as it arrives, to hang on `ApiEffectProfile.Shake`. For
a shake on its own use `NewShakeConfig` instead.

**Return Type:** `ApiShakeSpec` - The shake, set to the client's defaults - an impact that decays.

---

### NewShakeConfig

Build a standalone screen shake, for `StartShake`.

**Example:**

```py
shake = API.VisualEffects.NewShakeConfig()
shake.Seconds = 2.5
shake.Intensity = 0.8
shake.Gradient = API.VisualEffects.ShakeGradient.Decay
API.VisualEffects.StartShake(shake)
```

**Return Type:** `ApiShakeConfig` - The shake.

---

### NewVector2
`(x, y)`
Build a per-axis value pair, for the visual effect knobs that take one.

**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `x` | `double` | ✅ Yes | The X component. |
| `y` | `double` | ✅ Yes | The Y component. |

**Return Type:** `ApiVector2` - The pair.

---

### Start
`(profileId, seconds, intensity)`
Start one of the client's visual effect compositions by id.

A composition raised this way claims the look against the rulebase, so starting poison while
the player is poisoned shows one poison rather than two at doubled alpha.

Effects do not survive leaving the world: the handle goes stale and nothing comes back on the
next login. Stop them yourself, give them a duration, or let the script end - whatever a
script leaves running is dropped when it stops.

**Example:**

```py
handle = API.VisualEffects.Start(API.VisualEffects.Ids.Drunk, 10)
API.Pause(2)
API.VisualEffects.Stop(handle)
```

**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `profileId` | `string` | ❌ No | The composition's id, from `GetProfiles` or `Ids`. |
| `seconds` | `double` | ✅ Yes | How long to run for. Zero or less runs until stopped; capped at 86400. |
| `intensity` | `double` | ✅ Yes | Strength to draw at, 0-1. Scales the composition rather than replacing anything in it. |

**Return Type:** `uint` - A handle for `Stop`, or 0 if the user has visual effects switched off, you are not in the world, or every slot is in use.

**Raises:**

- `ArgumentException` - The ID is not in the right format, no effect in the client has it, or `seconds` or `intensity` is not a real number.

---

### Start
`(config, seconds, intensity)`
Start a visual effect the script built itself.

Unlike the two by-name forms this claims nothing, because there is no library composition
behind it to claim - so two scripts starting identical hand-built effects do composite
together, at doubled strength. Editing `config` afterwards cannot reach the running effect;
start it again to change it.

**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `config` | `ApiEffectProfile` | ❌ No | The composition to draw. Needs at least one and at most four layers. |
| `seconds` | `double` | ✅ Yes | How long to run for. Zero or less runs until stopped; capped at 86400. |
| `intensity` | `double` | ✅ Yes | Strength to draw at, 0-1. |

**Return Type:** `uint` - A handle for `Stop`, or 0 if the user has visual effects switched off, you are not in the world, or every slot is in use.

**Raises:**

- `ArgumentException` - `config` has no layers or more than four, one of its layers is empty, a setting is not one of the options the API offers, or a value is not a real number.

---

### StartByName
`(name, seconds, intensity)`
Start one of the user's own visual effect compositions by name.

User-authored compositions only. Every shipped one is named through the client's language file
and carries a "(built-in)" suffix, so no name here could reach them on a non-English client -
use `Start` with `Ids` for those.

**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `name` | `string` | ❌ No | The composition's name, matched ignoring case. Must match exactly one. |
| `seconds` | `double` | ✅ Yes | How long to run for. Zero or less runs until stopped; capped at 86400. |
| `intensity` | `double` | ✅ Yes | Strength to draw at, 0-1. |

**Return Type:** `uint` - A handle for `Stop`, or 0 if the user has visual effects switched off, you are not in the world, or every slot is in use.

**Raises:**

- `ArgumentException` - You have no effect with that name, several of yours share it, or `seconds` or `intensity` is not a real number.

---

### Stop
`(handle)`
Stop one visual effect this script started. It fades out rather than cutting.

**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `handle` | `uint` | ❌ No | The handle a `Start` returned. Handles that already lapsed, were stopped, came back as 0, or belong to another script are ignored. |

**Return Type:** `void` *(Does not return anything)*

---

### StopAll

Stop every visual effect this script started. Other scripts' effects, and the ones the
player's own state raises, are left alone.

**Return Type:** `void` *(Does not return anything)*

---

### StartShake
`(config)`
Shake the screen, with no visual effect attached.

**Example:**

```py
shake = API.VisualEffects.NewShakeConfig()
shake.Seconds = 0.4
shake.Intensity = 1.0
API.VisualEffects.StartShake(shake)
```

**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `config` | `ApiShakeConfig` | ❌ No | The shake to run, from `NewShakeConfig`. |

**Return Type:** `uint` - A handle for `StopShake`, or 0 if the user has screen shake switched off or you are not in the world.

**Raises:**

- `ArgumentException` - A setting on `config` is not one of the options the API offers, or one of its values is not a real number.
- `ArgumentOutOfRangeException` - `config.Seconds` is zero or less. A shake needs a length - there is nothing that would end an open-ended one.

---

### StopShake
`(handle)`
Stop one screen shake this script started.

**Parameters:**

| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| `handle` | `uint` | ❌ No | The handle `StartShake` returned. Handles that already finished, were stopped, came back as 0, or belong to another script are ignored. |

**Return Type:** `void` *(Does not return anything)*

---

### StopAllShakes

Stop every screen shake this script started, including the ones its visual effects fired as
they arrived.

**Return Type:** `void` *(Does not return anything)*

---

