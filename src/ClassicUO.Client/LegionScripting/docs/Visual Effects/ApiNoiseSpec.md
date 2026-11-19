---
title: ApiNoiseSpec
description: "How a visual effect layer moves and what texture it has: two scrolling samples of the tiling noise texture, the second domain-warped by the first."
---

## Class Description
How a visual effect layer moves and what texture it has: two scrolling samples of the tiling
noise texture, the second domain-warped by the first.

## Properties
### `BaseScale`

**Type:** `ApiVector2?`

Frequency of the primary field. The X:Y ratio is the anisotropy, and the main control over
whether features read as blobs (near 1:1) or streaks (4:1 and beyond).

### `BaseScroll`

**Type:** `ApiVector2?`

Texture-space velocity. On-screen speed is `Scroll / Scale`, so two layers with matching
scroll but different scales visibly slide against each other. Derive it from a target screen
speed.

### `BaseChannel`

**Type:** `ApiVisualEffects.NoiseChannel`

Which packed noise channel the primary field reads. Red and Green are fBm and read as
organic; Blue is ridged and Alpha is Worley, and both draw cell outlines.

### `DetailScale`

**Type:** `ApiVector2?`

Frequency of the secondary field, whose lookup is warped by the primary.

### `DetailScroll`

**Type:** `ApiVector2?`

Texture-space velocity of the secondary field.

### `DetailChannel`

**Type:** `ApiVisualEffects.NoiseChannel`

Which packed noise channel the secondary field reads.

### `Offset`

**Type:** `ApiVector2?`

Static texture-space shift of both fields, never varying with time. Desyncs a layer from
another sharing its scale and scroll, so the two don't read as one texture traced twice.

### `WarpStrength`

**Type:** `double`

How far the primary field displaces the secondary's lookup. The gas-versus-fluid dial: high
churns and billows, near-zero lets the pattern translate coherently.

### `RidgeAmount`

**Type:** `double`

Outlines the field's median. Counterintuitive twice over: raising it makes the layer cover
more, and what it draws are outlines, so on a soft field it produces bordered cells.

### `Threshold`

**Type:** `double`

Cut-off applied to the field. Higher keeps less: sparser, with narrower features.

### `Softness`

**Type:** `double`

Width of the fade either side of `Threshold`. Small = hard surface, large = cloud.

### `FlatFloor`

**Type:** `double`

Solid fill blended under the noise. Anything above 0 makes the shape mask visible as a
geometric form, so a layer reading as discrete streaks needs exactly 0. It fills the shape
in; it is not a way to add weight.


*No fields found.*

## Enums
*No enums found.*

## Methods
