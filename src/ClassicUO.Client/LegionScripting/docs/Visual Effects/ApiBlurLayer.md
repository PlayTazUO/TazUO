---
title: ApiBlurLayer
description: "Disk blur - out-of-focus vision. Build one with `API.VisualEffects.NewBlurLayer`."
---

## Class Description
Disk blur - out-of-focus vision. Build one with `API.VisualEffects.NewBlurLayer`.

Like every distortion technique it reads the frame as it stood <em>before</em> the effect was
drawn, so it must sit below anything it is meant to affect, and two distortion layers in one
composition do not compose: the upper one re-reads the original frame and overwrites the lower.

## Properties
### `Radius`

**Type:** `double`

Blur disk radius as a fraction of screen width, aspect-corrected so it stays circular. Small
values read as soft focus; past a couple of percent it becomes frosted glass. Capped when the
layer is baked.

### `Taps`

**Type:** `ApiVisualEffects.OverlaySampleTaps`

Samples taken per pixel, and the whole cost of the layer. Too few for the radius
shows as distinct ghost copies instead of a blur.


*No fields found.*

## Enums
*No enums found.*

## Methods
