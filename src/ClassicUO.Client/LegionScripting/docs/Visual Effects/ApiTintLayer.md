---
title: ApiTintLayer
description: "Colour painted over the scene. The only technique that reads a colour and the only one that never touches the frame behind it, which also makes it the only one that may sit anywhere in a stack. Build one with `API.VisualEffects.NewTintLayer`."
---

## Class Description
Colour painted over the scene. The only technique that reads a colour and the only one that never
touches the frame behind it, which also makes it the only one that may sit anywhere in a stack.
Build one with `API.VisualEffects.NewTintLayer`.

## Properties
### `R`

**Type:** `int`

Red channel of the layer's colour, 0-255. Clamped into range when the effect starts.

### `G`

**Type:** `int`

Green channel of the layer's colour, 0-255.

### `B`

**Type:** `int`

Blue channel of the layer's colour, 0-255.

### `A`

**Type:** `int`

Alpha channel of the layer's colour, 0-255. Note that `ApiLayerEffect.Strength` is what the
layer is actually painted at; this one tints rather than fades.


*No fields found.*

## Enums
*No enums found.*

## Methods
