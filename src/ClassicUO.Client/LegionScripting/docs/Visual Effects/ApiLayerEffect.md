---
title: ApiLayerEffect
description: "One layer of a visual effect composition. The subtype is the technique, so a technique's knobs exist only on the type that reads them - there is no radius on a chromatic layer to mis-set."
---

## Class Description
One layer of a visual effect composition. The subtype is the technique, so a technique's knobs
exist only on the type that reads them - there is no radius on a chromatic layer to mis-set.

Build one with `API.VisualEffects.NewTintLayer`, `API.VisualEffects.NewBlurLayer`, `API.VisualEffects.NewRadialBlurLayer` or
`API.VisualEffects.NewChromaticLayer`, each of which hands back the client's own defaults for that technique.
Fixed set of four: the shader has four techniques and there is no way to describe a fifth.

## Properties
### `Shape`

**Type:** `ApiShapeSpec?`

Where on screen the layer lives and how its boundary breaks up. Every technique masks with
this - a distortion layer's mask doubles as its strength.

### `Noise`

**Type:** `ApiNoiseSpec?`

How the layer moves and what texture it has.

### `Strength`

**Type:** `double`

Peak strength where the mask is full: the alpha a tint is painted at, or the degree to which
a distortion replaces the sharp frame. The occurrence's intensity, the fade envelope and the
user's own intensity setting can only attenuate this.

### `Pulse`

**Type:** `ApiPulseSpec?`

How the strength swells over time, or all zero for a steady layer.


*No fields found.*

## Enums
*No enums found.*

## Methods
