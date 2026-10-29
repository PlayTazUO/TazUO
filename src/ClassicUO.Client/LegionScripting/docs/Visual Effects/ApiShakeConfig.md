---
title: ApiShakeConfig
description: "A screen shake with no look attached, for `API.VisualEffects.StartShake`. Build one with `API.VisualEffects.NewShakeConfig`."
---

## Class Description
A screen shake with no look attached, for `API.VisualEffects.StartShake`. Build one with
`API.VisualEffects.NewShakeConfig`.

Amplitude at any moment is `Intensity x gradient(progress) x rampUp(elapsed) x
rampDown(remaining)`, so `Gradient` decides the overall arc and the two ramps decide how abruptly
it starts and stops. Unlike `ApiShakeSpec`, which travels inside a composition, this one carries
its own scope and gradient because there is no composition to take them from.

## Properties
### `Seconds`

**Type:** `double`

Total length of the shake in seconds, ramps included. Must be above zero - there is no
open-ended shake, since a shake nothing is sustaining would never stop - and clamped to
86400 at the top.

### `Intensity`

**Type:** `double`

Peak trauma, 0 to 1, before the user's own shake setting scales it down. Linear in pixels.

### `RampUpSeconds`

**Type:** `double`

Seconds spent building to full strength. Zero is at full amplitude on the first
frame, which is what an impact wants.

### `RampDownSeconds`

**Type:** `double`

Seconds spent falling away at the end. Zero stops dead, which reads as a jolt.

### `Gradient`

**Type:** `ApiVisualEffects.ShakeGradient`

How intensity travels across the whole duration, independently of the ramps.

### `Curve`

**Type:** `ApiVisualEffects.ShakeCurve`

Easing applied to both ramps and to the gradient.

### `Frequency`

**Type:** `double`

Shake rate in Hz, or zero for the client default. Lower reads as a heavy rumble,
higher as a rattle.

### `MaxOffsetPixels`

**Type:** `double`

Peak displacement in pixels at full trauma, 0 to 24. Zero takes the client's own 24,
and anything above it is clamped down: 24 is what the world render target is padded by, so a
larger offset would shake past the pixels there are and expose the window's empty edge.

### `FullScreen`

**Type:** `bool`

Whether the shake displaces the whole window, UI included, rather than the game world alone.
Window scope moves the gumps and the cursor with it, which makes them hard to use for as long
as the shake lasts.


*No fields found.*

## Enums
*No enums found.*

## Methods
