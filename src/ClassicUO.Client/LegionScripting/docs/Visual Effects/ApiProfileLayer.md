---
title: ApiProfileLayer
description: "One effect's participation in a composition: the layer, and how it combines with what is already beneath it. Order is the index in `ApiEffectProfile.Layers`, back to front."
---

## Class Description
One effect's participation in a composition: the layer, and how it combines with what is already
beneath it. Order is the index in `ApiEffectProfile.Layers`, back to front.

A wrapper rather than a blend field on the layer itself, because blending is a property of the
stack and not of the look - the same blur belongs in two compositions blended differently.

## Properties
### `Effect`

**Type:** `ApiLayerEffect?`

What this layer draws.

### `Blend`

**Type:** `ApiVisualEffects.OverlayBlend`

How the layer composites against what is beneath it. A highlight pass wants Additive - a
covering highlight reads as a second flat colour, an additive one as light caught on the
layer under it.


*No fields found.*

## Enums
*No enums found.*

## Methods
