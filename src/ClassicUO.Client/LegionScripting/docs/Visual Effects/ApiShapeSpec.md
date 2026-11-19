---
title: ApiShapeSpec
description: "Where on screen a layer lives: vignette or border shape, how far it extends, and how its boundary breaks up."
---

## Class Description
Where on screen a layer lives: vignette or border shape, how far it extends, and how its boundary
breaks up.

## Properties
### `Center`

**Type:** `ApiVector2?`

Centre of the radial falloff in screen uv. (0.5, 0.5) is the middle.

### `WobbleFreq`

**Type:** `ApiVector2?`

Rate `Center` drifts at, in Hz per axis. Different X and Y give a wander instead of a
pendulum; zero on an axis holds it still.

### `WobbleAmp`

**Type:** `double`

Peak drift of `Center`, in screen uv. Kept small - this unsteadies the pivot rather
than sending it touring the screen.

### `Reach`

**Type:** `double`

How far in from the screen edge the effect extends. Larger is thicker.

### `Feather`

**Type:** `double`

Width of the fade behind the boundary. Wide thins the effect into haze; narrow gives
it a defined surface.

### `EdgeBlend`

**Type:** `double`

0 = radial vignette, 1 = border trim. Avoid values in between: the radial term is normalised
to screen width, so any blend lands mostly on the left and right edges. Use `CornerBias` for
corner weighting instead - it is per-axis and has no aspect bias.

### `CornerBias`

**Type:** `double`

Corner weighting of the border trim, ignored when `EdgeBlend` is 0. Raising it
thickens and rounds the corners without favouring any one edge.

### `Jitter`

**Type:** `ApiJitterSpec?`

How the boundary is broken up, or all zero for a clean one. A smooth edge reads as
a drawn shape; a displaced one reads as something growing in from the frame.

### `FocusDir`

**Type:** `ApiVector2?`

Unit vector biasing the effect toward one side or corner.

### `FocusPower`

**Type:** `double`

Higher values tighten the directional lobe.

### `FocusAmount`

**Type:** `double`

0 = uniform all the way round, 1 = fully biased toward `FocusDir`.


*No fields found.*

## Enums
*No enums found.*

## Methods
