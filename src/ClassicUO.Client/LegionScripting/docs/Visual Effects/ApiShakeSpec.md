---
title: ApiShakeSpec
description: "Screen shake a visual effect includes, fired once as the effect arrives. Build one with `API.VisualEffects.NewShakeSpec` and hang it on `ApiEffectProfile.Shake`."
---

## Class Description
Screen shake a visual effect includes, fired once as the effect arrives. Build one with
`API.VisualEffects.NewShakeSpec` and hang it on `ApiEffectProfile.Shake`.

For a shake on its own, with no look attached, use `ApiShakeConfig` and `API.VisualEffects.StartShake`
instead. This type is the one that travels with a composition, so it has no scope of its own -
`ApiEffectProfile.FullScreen` decides which rectangle it displaces - and no gradient, since a
composition shapes its arc with the two ramps.

The shape is `Trauma x rampUp x rampDown`. An impact sets only the ramp down, so it starts at
full and decays; a quake sets both and leaves a hold between them, so it builds, holds, subsides.

## Properties
### `Trauma`

**Type:** `double`

How far the screen moves at the peak, 0 to 1, before the effect's own intensity and the
user's shake setting scale it down. Linear in pixels: 0.5 really is half as far as 1.

### `DurationSeconds`

**Type:** `double`

Total length of the shake in seconds, ramps included.

### `RampUpSeconds`

**Type:** `double`

Seconds spent building to full strength. Zero starts at full amplitude on the first frame,
which is what an impact wants and what anything building up does not.

### `RampDownSeconds`

**Type:** `double`

Seconds spent falling away at the end. Zero stops dead, which reads as a jolt. Set it to the
whole duration for an impact that decays from the first frame.

### `Curve`

**Type:** `ApiVisualEffects.ShakeCurve`

Easing applied to both ramps.

### `Frequency`

**Type:** `double`

Shake rate in Hz, or zero for the client default. Lower reads as a heavy rumble,
higher as a rattle - it separates a quake from a hit far more than trauma does.



## Enums
*No enums found.*

## Methods
