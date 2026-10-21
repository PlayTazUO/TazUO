---
title: ApiJitterSpec
description: "Breaks up a layer's shape boundary with its own noise field. Without it the effect terminates along a straight iso-line and reads as a rectangle, so anything organic wants `ReachAmount` well above 0."
---

## Class Description
Breaks up a layer's shape boundary with its own noise field. Without it the effect terminates
along a straight iso-line and reads as a rectangle, so anything organic wants `ReachAmount` well
above 0.

## Properties
### `ReachAmount`

**Type:** `double`

How far the boundary can move, as a fraction of the shape distance. Only extends inward, so
raising it raises average reach too - compensate with `ApiShapeSpec.Reach`.

### `FeatherAmount`

**Type:** `double`

How much the same field stretches and compresses `ApiShapeSpec.Feather`. Without it a deep
run and a shallow one end just as abruptly.

### `Scale`

**Type:** `ApiVector2?`

Frequency of the displacement, and the knob most often set wrong. Must be coarser than
`ApiNoiseSpec.BaseScale` or the boundary buzzes at detail frequency, but X still has to cycle
several times across the screen or each edge gets one bulge and stays visibly rectangular.

### `Scroll`

**Type:** `ApiVector2?`

Texture-space velocity of the displacement field. Usually matched to the layer's own scroll
so the ragged edge travels with the effect rather than crawling across it.

### `Channel`

**Type:** `ApiVisualEffects.NoiseChannel`

Which packed noise channel drives the boundary displacement.


*No fields found.*

## Enums
*No enums found.*

## Methods
